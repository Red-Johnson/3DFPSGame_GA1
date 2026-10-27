using UnityEngine;

// 총알 궤적 생성기: 궤적 설정과 풀을 들고, 총구에서 목표 지점까지 날아가는 궤적을 쏜다.
// 직렬화 클래스라 PlayerGunFire의 필드로 두면 설정이 인스펙터에 묶여서 보인다.
[System.Serializable]
public class BulletTracerSpawner
{
    private const int PoolDefaultCapacity = 20;
    private const int PoolMaxSize = 50;

    [SerializeField] private Transform _muzzle;                                        // 궤적이 시작될 총구. 비워두면 카메라 기준 오프셋을 쓴다.
    [SerializeField] private Vector3 _originOffset = new Vector3(0.3f, -0.2f, 0.5f);
    [SerializeField] private Material _material;                                       // 비워두면 Sprites/Default를 쓴다.
    [SerializeField] private Color _color = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float _width = 0.05f;
    [SerializeField] private float _speed = 200f;                                      // 선이 날아가는 속도
    [SerializeField] private float _length = 5f;                                       // 선의 길이

    private PrefabPool<BulletTracer> _pool;
    private Material _fallbackMaterial;

    public void Init()
    {
        _pool = new PrefabPool<BulletTracer>(CreateTracer, "TracerPool", PoolDefaultCapacity, PoolMaxSize);
    }

    // 총구 위치에서 목표 지점까지 날아가는 궤적을 쏜다.
    public void Fire(Transform cameraTransform, Vector3 endPoint)
    {
        Vector3 start = _muzzle != null
            ? _muzzle.position
            : cameraTransform.position + cameraTransform.TransformDirection(_originOffset);

        BulletTracer tracer = _pool.Get(start, Quaternion.identity);
        tracer.Fire(start, endPoint, _speed, _length);
    }

    // 프리팹 없이 선 렌더러를 달아서 궤적 오브젝트를 만든다. (보관함 아래에 만들어 꺼낼 때 활성화된다)
    private BulletTracer CreateTracer(Transform parent)
    {
        GameObject tracerObject = new GameObject("BulletTracer");
        tracerObject.transform.SetParent(parent);

        LineRenderer line = tracerObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = _width;
        line.endWidth = _width;
        line.sharedMaterial = GetMaterial();
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // 꼬리는 투명하게, 머리는 선명하게 해서 날아가는 느낌을 낸다.
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(_color, 0f), new GradientColorKey(_color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(_color.a, 1f) });
        line.colorGradient = gradient;

        return tracerObject.AddComponent<BulletTracer>();
    }

    private Material GetMaterial()
    {
        if (_material != null) return _material;

        if (_fallbackMaterial == null)
        {
            _fallbackMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return _fallbackMaterial;
    }
}
