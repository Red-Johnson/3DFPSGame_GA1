using System.Collections;
using UnityEngine;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("총알 궤적")]
    [SerializeField] private BulletTracerSpawner _tracer;

    [Header("스탯")]
    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f;            // 사거리
    [SerializeField] private float _damage = 10f;            // 공격력
    [SerializeField] private float _fireInterval = 0.1f;     // 발사 속도

    [Header("총구")]
    [SerializeField] private Transform _muzzlePosition;      // 총알과 궤적이 나가는 총구. 화염은 이 아래에 둔다.
    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _flashTime = 0.05f;

    private const float AimPointMargin = 0.1f;

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine = null;
    private Camera _camera;
    private PlayerBombFire _bombFire;
    private PlayerAnimator _playerAnimator;

    private bool IsReloading => _reloadCoroutine != null;

    public int BulletCount => _bulletCount;
    public int MaxBulletCount => _maxBulletCount;
    public event System.Action<int, int> OnBulletChanged;

    // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 쏠 수 없다. 재장전은 충전 중에만 막는다.
    private bool IsFireBlockedByBomb => _bombFire != null && (_bombFire.IsCharging || _bombFire.IsThrowing);
    private bool IsReloadBlockedByBomb => _bombFire != null && _bombFire.IsCharging;


    private void Awake()
    {
        _camera = Camera.main;
        _bombFire = GetComponent<PlayerBombFire>();
        _playerAnimator = GetComponent<PlayerAnimator>();

        // UI가 Start에서 총알 수를 읽어도 되도록 Awake에서 채운다. (Start 실행 순서에 의존하지 않는다.)
        _bulletCount = _maxBulletCount;

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
    }

    private void OnDisable()
    {
        if (_bombFire != null)
        {
            _bombFire.OnChargeStarted -= CancelReload;
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying)
        {
            // 게임이 끝났을 때 사격 모션이 켜진 채 남지 않게 한다.
            _playerAnimator.SetFiring(false);
            return;
        }

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기 (커서가 풀려 있는 동안은 GameInput이 입력 없음으로 돌려준다.)
        // 잔탄이 있으면 재장전 중에도 쏠 수 있고, 쏘면 재장전이 취소된다.
        // 잔탄이 없으면 쏠 수 없으므로(빈 탄창으로 시작한 재장전 포함) 발사 시도로는 재장전이 취소되지 않는다.
        // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 쏠 수 없다.
        if (GameInput.FireHeld && _fireTimer <= 0f && _bulletCount > 0 && !IsFireBlockedByBomb)
        {
            CancelReload();

            _bulletCount--;
            OnBulletChanged?.Invoke(_bulletCount, _maxBulletCount);
            _fireTimer = _fireInterval;
            Shoot();
            _playerAnimator.SetFiring(true);
        }
        else
        {
            _playerAnimator.SetFiring(false);
        }

        // 2. R키 누르면 총알 재장전 시작 (탄창이 가득 차 있거나 이미 재장전 중이거나 폭탄을 충전 중이면 시작하지 않는다.)
        if (GameInput.ReloadPressed && !IsReloading && _bulletCount < _maxBulletCount && !IsReloadBlockedByBomb)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    // 진행 중인 재장전을 취소한다. 재장전 중이 아니면 아무 일도 없다.
    private void CancelReload()
    {
        if (!IsReloading) return;

        StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = null;

        GameLogger.Log("재장전 취소!");
    }

    private IEnumerator Reloading_Coroutine()
    {
        GameLogger.Log("재장전 시작!");

        yield return new WaitForSeconds(_reloadingTime);

        _bulletCount = _maxBulletCount;
        OnBulletChanged?.Invoke(_bulletCount, _maxBulletCount);

        GameLogger.Log("재장전 완료!");
        _reloadCoroutine = null;
    }


    private void Shoot()
    {
        StartCoroutine(MuzzleFlash_Coroutine());

        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 1. 조준점 찾기: 화면 중앙(조준점)이 가리키는 곳을 카메라에서 쏜 레이로 먼저 정한다.
        // 트리거 콜라이더와 Ignore Raycast 레이어(경계 벽)는 총알이 맞지 않는다.
        Transform cameraTransform = _camera.transform;
        Ray aimRay = new Ray(cameraTransform.position, cameraTransform.forward);
        Vector3 aimPoint = Physics.Raycast(aimRay, out RaycastHit aimHit, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? aimHit.point
            : aimRay.GetPoint(_range);

        // 2. 실제 총알 레이는 총구에서 조준점을 향해 쏜다. (총구가 없으면 카메라에서 그대로 쏜다.)
        Vector3 muzzle = _muzzlePosition != null ? _muzzlePosition.position : cameraTransform.position;
        Vector3 toAim = aimPoint - muzzle;
        Ray ray = new Ray(muzzle, toAim);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        // 조준점 바로 앞에서 끊기지 않도록 조준점까지의 거리에 여유를 조금 둔다.
        bool isHit = Physics.Raycast(ray, out hitInfo, toAim.magnitude + AimPointMargin, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // 맞았으면 맞은 자리까지, 허공이면 조준점(사거리 끝)까지 궤적을 날린다.
        _tracer.Fire(muzzle, isHit ? hitInfo.point : aimPoint);

        if (isHit)
        {
            IDamageable target = hitInfo.collider.GetComponent<IDamageable>();

            // 맞은 자리에 총알 자국 이펙트 만들기 (끝나면 풀로 돌아가 재사용된다.)
            // 좀비는 맞으면 피가 튀므로, 벽에 맞은 자국 이펙트는 내지 않는다.
            if (target is not Zombie)
            {
                EffectPool.For(_hitEffectPrefab).Play(hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }

            // 대미지 구조체 만들기
            target?.TakeDamage(new DamageInfo(_damage, hitInfo.point, hitInfo.normal));
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
