using UnityEngine;

// 총알 궤적 생성기: 궤적 설정과 풀을 들고, 총구에서 목표 지점까지 날아가는 궤적을 쏜다.
// 직렬화 클래스라 PlayerGunFire의 필드로 두면 설정이 인스펙터에 묶여서 보인다.
[System.Serializable]
public class BulletTracerSpawner
{
    private const int PoolDefaultCapacity = 20;
    private const int PoolMaxSize = 50;

    [SerializeField] private Material _material;                                       // 비워두면 Sprites/Default를 쓴다.
    [SerializeField] private Color _color = new Color(1f, 0.25f, 0.2f, 1f);            // 색을 따로 주지 않을 때 쓰는 기본 색
    [SerializeField] private float _width = 0.05f;
    [SerializeField] private float _speed = 200f;                                      // 선이 날아가는 속도
    [SerializeField] private float _length = 5f;                                       // 선의 길이

    private PrefabPool<BulletTracer> _pool;
    private Material _fallbackMaterial;

    public void Init()
    {
        _pool = new PrefabPool<BulletTracer>(CreateTracer, "TracerPool", PoolDefaultCapacity, PoolMaxSize);
    }

    // 시작 지점(총구)에서 목표 지점까지 날아가는 궤적을 기본 색으로 쏜다.
    public void Fire(Vector3 start, Vector3 endPoint)
    {
        Fire(start, endPoint, _color);
    }

    // 시작 지점(총구)에서 목표 지점까지 날아가는 궤적을 주어진 색으로 쏜다. (머리의 선명함은 기본 색의 알파를 따른다.)
    public void Fire(Vector3 start, Vector3 endPoint, Color color)
    {
        color.a = _color.a;

        BulletTracer tracer = _pool.Get(start, Quaternion.identity);
        tracer.Fire(start, endPoint, _speed, _length, color);
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

        // 색(꼬리는 투명하게, 머리는 선명하게)은 쏠 때마다 BulletTracer.Fire가 입힌다.
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
