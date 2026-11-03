using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
// 총의 성능(GunSpec)은 무기 프로필(WeaponProfile)에서 읽고, 잔탄은 총마다 따로 가진다. (프로필 목록의 총마다 자동으로 만든다.)
// 무기를 바꿨다가 돌아와도 잔탄이 유지되고, 다른 총으로 바꾸면 진행 중인 재장전은 취소된다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("총알 궤적")]
    [SerializeField] private BulletTracerSpawner _tracer;

    [Header("총구")]
    [SerializeField] private Transform _muzzlePosition;      // 총알과 궤적이 나가는 총구. 화염은 이 아래에 둔다.
    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _flashTime = 0.05f;

    private readonly Dictionary<WeaponMode, GunState> _states = new Dictionary<WeaponMode, GunState>();    // 총마다 따로 가진 잔탄과 쿨타임

    private WeaponMode _gunMode;      // 지금 들고 있는 총. (근접 모드일 때는 마지막으로 들었던 총)
    private GunHitScan _hitScan;      // 총알 판정과 조준 보정
    private readonly HashSet<IDamageable> _piercedTargets = new HashSet<IDamageable>();     // 관통 사격에서 이미 맞힌 몬스터. 한 발 안에서만 쓴다.
    private Coroutine _reloadCoroutine = null;
    private Camera _camera;
    private PlayerWeaponMode _weaponMode;
    private PlayerBombFire _bombFire;
    private PlayerMarksmanZoom _zoom;       // 줌 중에만 조준 보정을 쓴다. 없으면 보정하지 않는다.
    private PlayerLampColor _lampColor;     // 총알 궤적 색을 가로등 색에 맞춘다. 없으면 궤적의 기본 색을 쓴다.

    private bool IsReloading => _reloadCoroutine != null;
    private GunSpec CurrentSpec => GetSpec(_gunMode);

    public int BulletCount => _states[_gunMode].BulletCount;
    public int MaxBulletCount => CurrentSpec.MaxBulletCount;
    public event System.Action<int, int> OnBulletChanged;

    // 총알이 한 발 나갈 때마다 알린다. (반동 연출이 구독한다. 이 클래스는 반동을 모른다.)
    public event System.Action<GunSpec> OnShot;

    // 총알이 몬스터(좀비)에 맞았을 때 알린다. 인자는 약점에 맞았는지다. (명중 표시 UI가 구독한다. 이 클래스는 UI를 모른다.)
    public event System.Action<bool> OnHit;

    // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 쏠 수 없다. 재장전은 충전 중에만 막는다.
    private bool IsFireBlockedByBomb => _bombFire != null && (_bombFire.IsCharging || _bombFire.IsThrowing);
    private bool IsReloadBlockedByBomb => _bombFire != null && _bombFire.IsCharging;


    private void Awake()
    {
        _camera = Camera.main;
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _bombFire = GetComponent<PlayerBombFire>();
        _zoom = GetComponent<PlayerMarksmanZoom>();
        _lampColor = GetComponent<PlayerLampColor>();
        _hitScan = new GunHitScan(transform);

        // UI가 Start에서 총알 수를 읽어도 되도록 Awake에서 채운다. (Start 실행 순서에 의존하지 않는다.)
        bool hasGun = false;
        foreach (WeaponProfile profile in _weaponMode.Profiles)
        {
            if (profile == null || !profile.IsGun) continue;

            _states[profile.Mode] = new GunState(profile.GunSpec.MaxBulletCount);

            // 근접 모드로 시작하는 경우를 대비해, 처음 들 총은 목록의 첫 총으로 정한다.
            if (!hasGun)
            {
                _gunMode = profile.Mode;
                hasGun = true;
            }
        }

        if (!hasGun)
        {
            GameLogger.LogError("[PlayerGunFire] PlayerWeaponMode의 프로필 목록에 총이 없어 사격을 쓸 수 없습니다.");
            enabled = false;
            return;
        }

        if (IsGun(_weaponMode.Mode))
        {
            _gunMode = _weaponMode.Mode;
        }

        // 총구 화염은 쏠 때만 잠깐 켜므로, 시작할 때는 모두 꺼 둔다. (씬에는 켜진 채로 배치되어 있다.)
        HideMuzzleFlashes();

        // 직렬화 데이터가 없는 오래된 씬에서도 동작하도록 한다.
        _tracer ??= new BulletTracerSpawner();
        _tracer.Init();
    }

    // 재장전은 폭탄 충전을 시작하거나(잔탄과 무관), 잔탄이 있는 상태에서 발사하면 취소된다.
    private void OnEnable()
    {
        if (_bombFire != null)
        {
            _bombFire.OnChargeStarted += CancelReload;
        }

        // 근접 모드에서 돌아오면 꺼져 있던 동안 바뀐 총을 맞춘다.
        _weaponMode.OnModeChanged += Equip;
        Equip(_weaponMode.Mode);
    }

    private void OnDisable()
    {
        if (_bombFire != null)
        {
            _bombFire.OnChargeStarted -= CancelReload;
        }

        _weaponMode.OnModeChanged -= Equip;
        CancelReload();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        GunSpec spec = CurrentSpec;

        // 1. 마우스 왼쪽 버튼 입력 받기 (커서가 풀려 있는 동안은 GameInput이 입력 없음으로 돌려준다.)
        // 연사 총은 누르고 있는 동안, 단발 총은 누르는 순간에만 쏜다. 단발 총을 쿨타임 중에 눌러도 나중에 쏘지 않는다.
        // 잔탄이 있으면 재장전 중에도 쏠 수 있고, 쏘면 재장전이 취소된다.
        // 잔탄이 없으면 쏠 수 없으므로(빈 탄창으로 시작한 재장전 포함) 발사 시도로는 재장전이 취소되지 않는다.
        // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 쏠 수 없다.
        bool isTriggered = spec.IsAutomatic ? GameInput.FireHeld : GameInput.FirePressed;

        GunState state = _states[_gunMode];

        if (isTriggered && Time.time >= state.NextFireTime && state.BulletCount > 0 && !IsFireBlockedByBomb)
        {
            CancelReload();

            state.BulletCount--;
            OnBulletChanged?.Invoke(BulletCount, MaxBulletCount);
            state.NextFireTime = Time.time + spec.FireInterval;
            Shoot(spec);
            OnShot?.Invoke(spec);
        }

        // 2. R키 누르면 총알 재장전 시작 (탄창이 가득 차 있거나 이미 재장전 중이거나 폭탄을 충전 중이면 시작하지 않는다.)
        if (GameInput.ReloadPressed && !IsReloading && BulletCount < MaxBulletCount && !IsReloadBlockedByBomb)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine(_gunMode));
        }
    }

    // 무기 모드가 바뀌면 들고 있는 총을 바꾼다. 근접 모드는 총이 아니므로 무시한다.
    private void Equip(WeaponMode mode)
    {
        if (!IsGun(mode)) return;

        // 다른 총으로 바꾸면 진행 중인 재장전은 취소한다. (잔탄은 총마다 따로 남아 있다.)
        if (mode != _gunMode)
        {
            CancelReload();
            _gunMode = mode;
        }

        OnBulletChanged?.Invoke(BulletCount, MaxBulletCount);
    }

    private bool IsGun(WeaponMode mode)
    {
        WeaponProfile profile = _weaponMode.GetProfile(mode);
        return profile != null && profile.IsGun;
    }

    private GunSpec GetSpec(WeaponMode mode)
    {
        return _weaponMode.GetProfile(mode).GunSpec;
    }

    // 진행 중인 재장전을 취소한다. 재장전 중이 아니면 아무 일도 없다.
    private void CancelReload()
    {
        if (!IsReloading) return;

        StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = null;

        GameLogger.Log("재장전 취소!");
    }

    private IEnumerator Reloading_Coroutine(WeaponMode mode)
    {
        GameLogger.Log("재장전 시작!");

        yield return new WaitForSeconds(GetSpec(mode).ReloadingTime);

        _states[mode].BulletCount = GetSpec(mode).MaxBulletCount;
        OnBulletChanged?.Invoke(BulletCount, MaxBulletCount);

        GameLogger.Log("재장전 완료!");
        _reloadCoroutine = null;
    }


    private void Shoot(GunSpec spec)
    {
        StartCoroutine(MuzzleFlash_Coroutine());

        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 1. 조준점 찾기: 화면 중앙(조준점)이 가리키는 곳을 카메라에서 쏜 레이로 먼저 정한다.
        // 트리거 콜라이더와 Ignore Raycast 레이어(경계 벽)는 총알이 맞지 않는다.
        Transform cameraTransform = _camera.transform;
        bool isZooming = _zoom != null && _zoom.IsZooming;      // 줌 중에만 조준 보정을 쓴다. 줌 컴포넌트가 없으면 보정하지 않는다.
        Ray aimRay = new Ray(cameraTransform.position, _hitScan.ApplyAimAssist(cameraTransform.position, cameraTransform.forward, spec, isZooming));
        // 약점을 맞출 수 있는 총은 조준점도 몸통 안쪽의 약점 판정까지 들어가서 잡아야, 총구에서 쏜 총알도 약점에 닿는다.
        Vector3 aimPoint = _hitScan.Raycast(aimRay, spec.Range, spec.CanHitWeakPoint, out RaycastHit aimHit)
            ? aimHit.point
            : aimRay.GetPoint(spec.Range);

        // 2. 실제 총알 레이는 총구에서 조준점을 향해 쏜다. (총구가 없으면 카메라에서 그대로 쏜다.)
        Vector3 muzzle = _muzzlePosition != null ? _muzzlePosition.position : cameraTransform.position;
        Vector3 toAim = aimPoint - muzzle;
        Ray ray = new Ray(muzzle, toAim);

        // 관통하는 총은 조준점에서 멈추지 않고 사거리 끝까지 뚫고 나간다.
        if (spec.CanPierce)
        {
            ShootPiercing(spec, ray, muzzle);
            return;
        }

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        // 조준점 바로 앞에서 끊기지 않도록 조준점까지의 거리에 여유를 조금 둔다.
        bool isHit = _hitScan.Raycast(ray, toAim.magnitude + GunHitScan.AimPointMargin, spec.CanHitWeakPoint, out hitInfo);

        // 맞았으면 맞은 자리까지, 허공이면 조준점(사거리 끝)까지 궤적을 날린다.
        FireTracer(muzzle, isHit ? hitInfo.point : aimPoint);

        if (isHit)
        {
            IDamageable target = hitInfo.collider.GetComponentInParent<IDamageable>();

            // 약점을 맞출 수 있는 총이 약점 판정(WeakPointHitbox)에 맞았으면 약점 공격이다.
            bool isWeakPointHit = spec.CanHitWeakPoint && target != null && hitInfo.collider.GetComponent<WeakPointHitbox>() != null;

            // 맞은 자리에 총알 자국 이펙트 만들기 (끝나면 풀로 돌아가 재사용된다.)
            // 좀비는 맞으면 피가 튀므로, 벽에 맞은 자국 이펙트는 내지 않는다.
            if (target is not IMonster)
            {
                EffectPool.For(_hitEffectPrefab).Play(hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }

            // 대미지 구조체 만들기
            float damage = isWeakPointHit ? spec.WeakPointDamage : spec.Damage;
            target?.TakeDamage(new DamageInfo(damage, hitInfo.point, hitInfo.normal, isWeakPointHit));

            // 몬스터(좀비)에 맞았을 때만 명중으로 친다. 벽, 드럼통, 박스 같은 것에 맞은 것은 표시하지 않는다.
            if (target is IMonster)
            {
                OnHit?.Invoke(isWeakPointHit);
            }
        }
    }

    // 관통 사격: 레이가 지나는 맞은 지점을 가까운 순서로 처리한다.
    // - 몬스터는 한 마리씩 대미지를 주고 계속 나아간다. 한 몬스터는 몸통과 약점 판정이 따로 있어도 한 번만 맞고, 레이 위에 약점이 있으면 약점 공격이다.
    // - 벽, 건물, 드럼통, 차량 같은 몬스터가 아닌 것에서는 멈춘다. (대미지를 받는 것이면 대미지를 주고 멈춘다.)
    // - spec의 관통 수(처음 맞힌 몬스터 뒤로 더 뚫을 수)를 다 쓰면 거기서 멈춘다. 음수면 제한이 없다.
    // - 뚫을 때마다 대미지가 PierceDamageLoss 비율만큼 줄어든다.
    private void ShootPiercing(GunSpec spec, Ray ray, Vector3 muzzle)
    {
        int count = _hitScan.RaycastAllSorted(ray, spec.Range, out RaycastHit[] hits);

        Vector3 tracerEnd = ray.GetPoint(spec.Range);   // 아무것도 멈추게 하지 않으면 사거리 끝까지
        int monstersHit = 0;
        bool anyWeakPointHit = false;
        float damageScale = 1f;
        _piercedTargets.Clear();

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

            if (target is IMonster)
            {
                // 몸통과 약점이 따로 있어도 한 마리는 한 번만 때린다.
                if (!_piercedTargets.Add(target)) continue;

                // 이 몬스터의 약점 판정이 레이 위에 있으면(가까운 것부터 찾는다) 약점 공격이다.
                RaycastHit damageHit = hit;
                bool isWeakPointHit = false;
                if (spec.CanHitWeakPoint)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (hits[j].collider.GetComponent<WeakPointHitbox>() == null) continue;
                        if (hits[j].collider.GetComponentInParent<IDamageable>() != target) continue;

                        damageHit = hits[j];
                        isWeakPointHit = true;
                        break;
                    }
                }

                float damage = (isWeakPointHit ? spec.WeakPointDamage : spec.Damage) * damageScale;
                target.TakeDamage(new DamageInfo(damage, damageHit.point, damageHit.normal, isWeakPointHit));

                anyWeakPointHit |= isWeakPointHit;
                monstersHit++;
                damageScale *= 1f - spec.PierceDamageLoss;

                // 뚫을 수 있는 수를 다 썼으면 여기서 멈춘다. (처음 맞힌 몬스터 + 관통 수만큼 맞힌다.)
                if (spec.PierceCount > 0 && monstersHit > spec.PierceCount)
                {
                    tracerEnd = damageHit.point;
                    break;
                }

                continue;
            }

            // 몬스터가 아닌 것은 총알을 멈춘다. 맞은 자리에 총알 자국을 남기고, 대미지를 받는 것이면 대미지를 준다.
            tracerEnd = hit.point;
            EffectPool.For(_hitEffectPrefab).Play(hit.point, Quaternion.LookRotation(hit.normal));
            target?.TakeDamage(new DamageInfo(spec.Damage * damageScale, hit.point, hit.normal));
            break;
        }

        FireTracer(muzzle, tracerEnd);

        // 몬스터를 하나라도 맞혔으면 한 발에 한 번만 명중으로 알린다. 약점에 맞은 몬스터가 있으면 약점 명중이다.
        if (monstersHit > 0)
        {
            OnHit?.Invoke(anyWeakPointHit);
        }
    }

    // 총구에서 end까지 총알 궤적을 날린다. 궤적 색은 들고 있는 총의 가로등 색을 따른다. (PlayerLampColor가 없으면 궤적의 기본 색을 쓴다.)
    private void FireTracer(Vector3 muzzle, Vector3 end)
    {
        if (_lampColor != null)
        {
            _tracer.Fire(muzzle, end, _lampColor.GetLampColor(_gunMode));
        }
        else
        {
            _tracer.Fire(muzzle, end);
        }
    }

    private void HideMuzzleFlashes()
    {
        if (_muzzleFlashes == null) return;

        foreach (GameObject flash in _muzzleFlashes)
        {
            if (flash != null)
            {
                flash.SetActive(false);
            }
        }
    }

    private IEnumerator MuzzleFlash_Coroutine()
    {
        // 총구 화염 오브젝트를 아직 연결하지 않았다면 아무것도 하지 않는다.
        if (_muzzleFlashes == null || _muzzleFlashes.Length == 0) yield break;

        int randomIndex = UnityEngine.Random.Range(0, _muzzleFlashes.Length);
        GameObject flash = _muzzleFlashes[randomIndex];
        flash.SetActive(true);

        yield return new WaitForSeconds(_flashTime);

        flash.SetActive(false);
    }
}
