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


    private void Awake()
    {
        _camera = Camera.main;

        // 직렬화 데이터가 없는 오래된 씬에서도 동작하도록 한다.
        _tracer ??= new BulletTracerSpawner();
        _tracer.Init();
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
        if (GameInput.FireHeld && _fireTimer <= 0f && _bulletCount > 0)
        {
            _bulletCount--;
            _fireTimer = _fireInterval;
            Shoot();

            // 이 방식은: 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
            // StopAllCoroutines();

            if(_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                GameLogger.Log("재장전 취소!");
                _reloadCoroutine = null;
            }
        }

        // 2. R키 누르면 총알 재장전 시작
        if (GameInput.ReloadPressed && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
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
            // 맞은 자리에 총알 자국 이펙트 만들기 (끝나면 풀로 돌아가 재사용된다.)
            EffectPool.For(_hitEffectPrefab).Play(hitInfo.point, Quaternion.LookRotation(hitInfo.normal));

            // 대미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}
