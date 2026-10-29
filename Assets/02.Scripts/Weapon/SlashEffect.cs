using UnityEngine;

// 칼 공격 이펙트: 앞쪽으로 퍼지는 부채꼴(고리 모양) 띠가 커지면서 서서히 사라진다.
// 메시를 코드로 만들고 한 번 만든 오브젝트를 계속 재사용한다. (공격마다 새로 만들지 않는다.)
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlashEffect : MonoBehaviour
{
    private const int ArcSegments = 24;

    [SerializeField] private Color _color = new Color(0.7f, 0.9f, 1f, 0.9f);
    [SerializeField, Range(0f, 0.95f)] private float _innerRatio = 0.45f;   // 반경 중 안쪽 빈 부분의 비율
    [SerializeField] private float _duration = 0.25f;
    [SerializeField, Range(0.1f, 1f)] private float _startScale = 0.6f;     // 처음 크기(최종 크기에 대한 비율)

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material _material;
    private Mesh _mesh;

    private float _builtAngle = -1f;
    private float _radius;
    private float _elapsed;
    private bool _isPlaying;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        // 어느 파이프라인에서나 정점 색을 쓰는 단순한 투명 셰이더
        _material = new Material(Shader.Find("Sprites/Default"));
        _meshRenderer.sharedMaterial = _material;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
        _meshRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
        if (_mesh != null) Destroy(_mesh);
    }

    // 위치와 방향을 정하고 처음부터 재생한다. 바라보는 방향(rotation의 앞쪽)을 중심으로 angle도 만큼 퍼진다.
    public void Play(Vector3 position, Quaternion rotation, float radius, float angle)
    {
        if (!Mathf.Approximately(angle, _builtAngle))
        {
            BuildMesh(angle);
        }

        transform.SetPositionAndRotation(position, rotation);
        _radius = radius;
        _elapsed = 0f;
        _isPlaying = true;

        Apply(0f);
        _meshRenderer.enabled = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float t = _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;
        Apply(t);

        if (t >= 1f)
        {
            _isPlaying = false;
            _meshRenderer.enabled = false;
        }
    }

    // 진행도(0~1)에 맞게 크기와 투명도를 정한다. 처음에 빨리 커지고 끝으로 갈수록 투명해진다.
    private void Apply(float t)
    {
        float grow = 1f - ((1f - t) * (1f - t));
        float scale = _radius * Mathf.Lerp(_startScale, 1f, grow);
        transform.localScale = new Vector3(scale, 1f, scale);

        Color tint = Color.white;
        tint.a = 1f - t;
        _material.color = tint;
    }

    // 반지름 1인 부채꼴 띠 메시를 만든다. 안쪽 가장자리는 투명하고 바깥쪽 가장자리가 가장 진하다.
    private void BuildMesh(float angle)
    {
        _builtAngle = angle;
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "SlashFan" };
        }

        _mesh.Clear();

        int vertexCount = (ArcSegments + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] triangles = new int[ArcSegments * 6];

        Color inner = _color;
        inner.a = 0f;

        float half = angle * 0.5f;
        for (int i = 0; i <= ArcSegments; i++)
        {
            float degree = Mathf.Lerp(-half, half, (float)i / ArcSegments);
            Vector3 direction = Quaternion.Euler(0f, degree, 0f) * Vector3.forward;

            vertices[i * 2] = direction * _innerRatio;
            vertices[(i * 2) + 1] = direction;
            colors[i * 2] = inner;
            colors[(i * 2) + 1] = _color;
        }

        for (int i = 0; i < ArcSegments; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
        }

        _mesh.vertices = vertices;
        _mesh.colors = colors;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
        _meshFilter.sharedMesh = _mesh;
    }
}
