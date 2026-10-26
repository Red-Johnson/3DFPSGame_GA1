using System.Collections;
using UnityEngine;


// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    private const int TracerPoolDefaultCapacity = 20;
    private const int TracerPoolMaxSize = 50;

    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("총알 궤적")]
    [SerializeField] private Transform _muzzle;                                        // 궤적이 시작될 총구. 비워두면 카메라 기준 오프셋을 쓴다.
    [SerializeField] private Vector3 _tracerOriginOffset = new Vector3(0.3f, -0.2f, 0.5f);
    [SerializeField] private Material _tracerMaterial;                                 // 비워두면 Sprites/Default를 쓴다.
    [SerializeField] private Color _tracerColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float _tracerWidth = 0.05f;
    [SerializeField] private float _tracerSpeed = 200f;                                // 선이 날아가는 속도
    [SerializeField] private float _tracerLength = 5f;                                 // 선의 길이

    [Header("스탯")]
    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f;            // 사거리
    [SerializeField] private float _damage = 10f;            // 공격력
    [SerializeField] private float _fireInterval = 0.1f;     // 발사 속도
    
    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine = null;
    private PrefabPool<BulletTracer> _tracerPool;
    private Material _fallbackMaterial;


    private void Awake()
    {
        _tracerPool = new PrefabPool<BulletTracer>(CreateTracer, "TracerPool", TracerPoolDefaultCapacity, TracerPoolMaxSize);
    }

    private void Start()
    {
        _bulletCount = _maxBulletCount;
    }

    // 프리팹 없이 선 렌더러를 달아서 궤적 오브젝트를 만든다. (보관함 아래에 만들어 꺼낼 때 활성화된다)
    private BulletTracer CreateTracer(Transform parent)
    {
        GameObject tracerObject = new GameObject("BulletTracer");
        tracerObject.transform.SetParent(parent);

        LineRenderer line = tracerObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = _tracerWidth;
        line.endWidth = _tracerWidth;
        line.sharedMaterial = GetTracerMaterial();
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // 꼬리는 투명하게, 머리는 선명하게 해서 날아가는 느낌을 낸다.
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(_tracerColor, 0f), new GradientColorKey(_tracerColor, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(_tracerColor.a, 1f) });
        line.colorGradient = gradient;

        return tracerObject.AddComponent<BulletTracer>();
    }

    private Material GetTracerMaterial()
    {
        if (_tracerMaterial != null) return _tracerMaterial;

        if (_fallbackMaterial == null)
        {
            _fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return _fallbackMaterial;
    }

    // 총구 위치에서 목표 지점까지 날아가는 궤적을 쏜다.
    private void FireTracer(Transform cameraTransform, Vector3 endPoint)
    {
        Vector3 start = _muzzle != null
            ? _muzzle.position
            : cameraTransform.position + cameraTransform.TransformDirection(_tracerOriginOffset);

        BulletTracer tracer = _tracerPool.Get(start, Quaternion.identity);
        tracer.Fire(start, endPoint, _tracerSpeed, _tracerLength);
    }
    
    
    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;
        
        // 1. 마우스 왼쪽 버튼 입력 받기
        bool isMouseVisible = Cursor.visible;
        if (!isMouseVisible && Input.GetButton("Fire1") && _fireTimer <= 0f && _bulletCount > 0)
        {
            _bulletCount--;
            _fireTimer = _fireInterval;
            Shoot();
            
            // 이 방식은: 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
            // StopAllCoroutines();
            
            if(_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소!");
                _reloadCoroutine = null;
            }
        }
        
        // 2. R키 누르면 총알 재장전 시작
        if (Input.GetKeyDown(KeyCode.R) && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작!");
        
        yield return new WaitForSeconds(_reloadingTime);
        
        _bulletCount = _maxBulletCount;
        
        Debug.Log("재장전 완료!");
        _reloadCoroutine = null;
    }
    

    private void Shoot()
    {
        // Ray: 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체
        
        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);

        // 맞았으면 맞은 자리까지, 허공이면 사거리 끝까지 궤적을 날린다.
        FireTracer(cameraTransform, isHit ? hitInfo.point : ray.GetPoint(_range));

        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 대미지: {_damage}");
            
            // 맞은 자리에 총알 자국 이펙트 만들기
            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();
            
            // 대미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;
            
            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
            
            /*
             hitInfo.collider.GetComponent<Zombie>()?.TakeDamage(_damage);
            hitInfo.collider.GetComponent<Drum>()?.TakeDamage(_damage);
            hitInfo.collider.GetComponent<Box>()?.TakeDamage(_damage);
            */
            
            // 황유찬 :         Player
            // 김민쉬:     Zombie, Boss
            // 박현서:  Drum, Box, Bird
        }
    }
}
