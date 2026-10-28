using System.Collections;
using UnityEngine;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
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

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine = null;
    private Camera _camera;
    private PlayerBombFire _bombFire;
    private Animator _animator;

    private bool IsReloading => _reloadCoroutine != null;


    private void Awake()
    {
        _camera = Camera.main;
        _bombFire = GetComponent<PlayerBombFire>();
        _animator = GetComponentInChildren<Animator>();

        // 직렬화 데이터가 없는 오래된 씬에서도 동작하도록 한다.
        _tracer ??= new BulletTracerSpawner();
        _tracer.Init();
    }

    // 재장전은 폭탄을 던지거나(잔탄과 무관), 잔탄이 있는 상태에서 발사하면 취소된다.
    private void OnEnable()
    {
        if (_bombFire != null)
        {
            _bombFire.OnBombThrown += CancelReload;
        }
    }

    private void OnDisable()
    {
        if (_bombFire != null)
        {
            _bombFire.OnBombThrown -= CancelReload;
        }
    }

    private void Start()
    {
        _bulletCount = _maxBulletCount;
    }


    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기 (커서가 풀려 있는 동안은 GameInput이 입력 없음으로 돌려준다.)
        // 잔탄이 있으면 재장전 중에도 쏠 수 있고, 쏘면 재장전이 취소된다.
        // 잔탄이 없으면 쏠 수 없으므로(빈 탄창으로 시작한 재장전 포함) 발사 시도로는 재장전이 취소되지 않는다.
        if (GameInput.FireHeld && _fireTimer <= 0f && _bulletCount > 0)
        {
            CancelReload();

            _bulletCount--;
            _fireTimer = _fireInterval;
            Shoot();
            _animator.SetBool("Fire", true);
        }
        else
        {
            _animator.SetBool("Fire", false);
        }

        // 2. R키 누르면 총알 재장전 시작 (탄창이 가득 차 있거나 이미 재장전 중이면 시작하지 않는다.)
        if (GameInput.ReloadPressed && !IsReloading && _bulletCount < _maxBulletCount)
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

        GameLogger.Log("재장전 완료!");
        _reloadCoroutine = null;
    }


    private void Shoot()
    {
        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = _camera.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        // 트리거 콜라이더와 Ignore Raycast 레이어(경계 벽)는 총알이 맞지 않는다.
        bool isHit = Physics.Raycast(ray, out hitInfo, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // 맞았으면 맞은 자리까지, 허공이면 사거리 끝까지 궤적을 날린다.
        _tracer.Fire(cameraTransform, isHit ? hitInfo.point : ray.GetPoint(_range));

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
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            target?.TakeDamage(damageInfo);
        }
    }
}
