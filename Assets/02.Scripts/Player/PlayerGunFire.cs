using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
// 총은 돌격소총(1번)과 지정사수소총(2번) 두 자루이고, 성능(GunSpec)과 잔탄은 총마다 따로 가진다.
// 무기를 바꿨다가 돌아와도 잔탄이 유지되고, 다른 총으로 바꾸면 진행 중인 재장전은 취소된다.
[RequireComponent(typeof(PlayerAnimator), typeof(PlayerWeaponMode))]
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("총알 궤적")]
    [SerializeField] private BulletTracerSpawner _tracer;

    [Header("돌격소총 (1번): 누르고 있으면 연사")]
    [SerializeField] private GunSpec _assaultRifle = new GunSpec(60, 1f, 100f, 10f, 0.1f, true);

    [Header("지정사수소총 (2번): 한 번 누를 때마다 한 발")]
    [SerializeField] private GunSpec _marksmanRifle = new GunSpec(12, 3f, 150f, 40f, 1f, false);

    [Header("총구")]
    [SerializeField] private Transform _muzzlePosition;      // 총알과 궤적이 나가는 총구. 화염은 이 아래에 둔다.
    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _flashTime = 0.05f;

    private const float AimPointMargin = 0.1f;

    private readonly Dictionary<WeaponMode, int> _bulletCounts = new Dictionary<WeaponMode, int>();      // 총마다 따로 남은 총알
    private readonly Dictionary<WeaponMode, float> _nextFireTimes = new Dictionary<WeaponMode, float>(); // 총마다 따로 다음에 쏠 수 있는 시각

    private WeaponMode _gunMode = WeaponMode.AssaultRifle;      // 지금 들고 있는 총. (근접 모드일 때는 마지막으로 들었던 총)
    private Coroutine _reloadCoroutine = null;
    private Camera _camera;
    private PlayerWeaponMode _weaponMode;
    private PlayerBombFire _bombFire;
    private PlayerAnimator _playerAnimator;

    private bool IsReloading => _reloadCoroutine != null;
    private GunSpec CurrentSpec => GetSpec(_gunMode);

    public int BulletCount => _bulletCounts[_gunMode];
    public int MaxBulletCount => CurrentSpec.MaxBulletCount;
    public event System.Action<int, int> OnBulletChanged;

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
        _playerAnimator = GetComponent<PlayerAnimator>();

        // UI가 Start에서 총알 수를 읽어도 되도록 Awake에서 채운다. (Start 실행 순서에 의존하지 않는다.)
        _bulletCounts[WeaponMode.AssaultRifle] = _assaultRifle.MaxBulletCount;
        _bulletCounts[WeaponMode.MarksmanRifle] = _marksmanRifle.MaxBulletCount;
        _nextFireTimes[WeaponMode.AssaultRifle] = 0f;
        _nextFireTimes[WeaponMode.MarksmanRifle] = 0f;

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
        if (!GameManager.Instance.IsPlaying)
        {
            // 게임이 끝났을 때 사격 모션이 켜진 채 남지 않게 한다.
            _playerAnimator.SetFiring(false);
            return;
        }

        GunSpec spec = CurrentSpec;

        // 1. 마우스 왼쪽 버튼 입력 받기 (커서가 풀려 있는 동안은 GameInput이 입력 없음으로 돌려준다.)
        // 연사 총은 누르고 있는 동안, 단발 총은 누르는 순간에만 쏜다. 단발 총을 쿨타임 중에 눌러도 나중에 쏘지 않는다.
        // 잔탄이 있으면 재장전 중에도 쏠 수 있고, 쏘면 재장전이 취소된다.
        // 잔탄이 없으면 쏠 수 없으므로(빈 탄창으로 시작한 재장전 포함) 발사 시도로는 재장전이 취소되지 않는다.
        // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 쏠 수 없다.
        bool isTriggered = spec.IsAutomatic ? GameInput.FireHeld : GameInput.FirePressed;

        if (isTriggered && Time.time >= _nextFireTimes[_gunMode] && BulletCount > 0 && !IsFireBlockedByBomb)
        {
            CancelReload();

            _bulletCounts[_gunMode]--;
            OnBulletChanged?.Invoke(BulletCount, MaxBulletCount);
            _nextFireTimes[_gunMode] = Time.time + spec.FireInterval;
            Shoot(spec);
            _playerAnimator.SetFiring(true);
        }
        else
        {
            _playerAnimator.SetFiring(false);
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

    private static bool IsGun(WeaponMode mode)
    {
        return mode == WeaponMode.AssaultRifle || mode == WeaponMode.MarksmanRifle;
    }

    private GunSpec GetSpec(WeaponMode mode)
    {
        return mode == WeaponMode.MarksmanRifle ? _marksmanRifle : _assaultRifle;
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

        _bulletCounts[mode] = GetSpec(mode).MaxBulletCount;
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
        Ray aimRay = new Ray(cameraTransform.position, cameraTransform.forward);
        // 약점을 맞출 수 있는 총은 조준점도 몸통 안쪽의 약점 판정까지 들어가서 잡아야, 총구에서 쏜 총알도 약점에 닿는다.
        Vector3 aimPoint = RaycastIgnoringSelf(aimRay, spec.Range, spec.CanHitWeakPoint, out RaycastHit aimHit)
            ? aimHit.point
            : aimRay.GetPoint(spec.Range);

        // 2. 실제 총알 레이는 총구에서 조준점을 향해 쏜다. (총구가 없으면 카메라에서 그대로 쏜다.)
        Vector3 muzzle = _muzzlePosition != null ? _muzzlePosition.position : cameraTransform.position;
        Vector3 toAim = aimPoint - muzzle;
        Ray ray = new Ray(muzzle, toAim);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        // 조준점 바로 앞에서 끊기지 않도록 조준점까지의 거리에 여유를 조금 둔다.
        bool isHit = RaycastIgnoringSelf(ray, toAim.magnitude + AimPointMargin, spec.CanHitWeakPoint, out hitInfo);

        // 맞았으면 맞은 자리까지, 허공이면 조준점(사거리 끝)까지 궤적을 날린다.
        _tracer.Fire(muzzle, isHit ? hitInfo.point : aimPoint);

        if (isHit)
        {
            IDamageable target = hitInfo.collider.GetComponentInParent<IDamageable>();

            // 약점을 맞출 수 있는 총이 약점 판정(WeakPointHitbox)에 맞았으면 약점 공격이다.
            bool isWeakPointHit = spec.CanHitWeakPoint && target != null && hitInfo.collider.GetComponent<WeakPointHitbox>() != null;

            // 맞은 자리에 총알 자국 이펙트 만들기 (끝나면 풀로 돌아가 재사용된다.)
            // 좀비는 맞으면 피가 튀므로, 벽에 맞은 자국 이펙트는 내지 않는다.
            if (target is not Zombie)
            {
                EffectPool.For(_hitEffectPrefab).Play(hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }

            // 대미지 구조체 만들기
            float damage = isWeakPointHit ? spec.WeakPointDamage : spec.Damage;
            target?.TakeDamage(new DamageInfo(damage, hitInfo.point, hitInfo.normal, isWeakPointHit));

            // 몬스터(좀비)에 맞았을 때만 명중으로 친다. 벽, 드럼통, 박스 같은 것에 맞은 것은 표시하지 않는다.
            if (target is Zombie)
            {
                OnHit?.Invoke(isWeakPointHit);
            }
        }
    }

    // 가장 가까운 맞은 물체를 찾되 플레이어 자신의 콜라이더는 건너뛴다. (3인칭에서는 카메라가 플레이어 뒤에 있어 레이가 자신부터 맞기 때문)
    // preferWeakPoint가 켜져 있으면, 맞은 대상의 약점 판정(WeakPointHitbox)이 레이 위에 있을 때 몸통 대신 약점을 맞은 것으로 돌려준다.
    // (약점 판정은 몸통 콜라이더 안쪽에 있을 수 있어서, 가장 가까운 것만 고르면 항상 몸통이 먼저 잡히기 때문)
    private bool RaycastIgnoringSelf(Ray ray, float distance, bool preferWeakPoint, out RaycastHit result)
    {
        result = default;
        float nearest = float.MaxValue;
        bool found = false;

        RaycastHit[] hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.root == transform.root || hit.distance >= nearest) continue;

            nearest = hit.distance;
            result = hit;
            found = true;
        }

        if (!found || !preferWeakPoint) return found;

        IDamageable owner = result.collider.GetComponentInParent<IDamageable>();
        if (owner == null) return true;

        float nearestWeakPoint = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= nearestWeakPoint || hit.collider.GetComponent<WeakPointHitbox>() == null) continue;
            if (hit.collider.GetComponentInParent<IDamageable>() != owner) continue;

            nearestWeakPoint = hit.distance;
            result = hit;
        }

        return true;
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
