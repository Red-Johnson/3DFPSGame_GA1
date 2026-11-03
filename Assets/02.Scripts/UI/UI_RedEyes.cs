using UnityEngine;
using UnityEngine.UI;

// 어둠 속에서 여기저기 번뜩이는 빨간 눈. 건물 창과 골목 같은 정해진 자리(둥지)마다 한 쌍씩 만들어 두고,
// 각 자리가 따로 "숨어 있다 → 눈을 뜸 → 깜빡이며 노려봄 → 눈을 감고 사라짐"을 되풀이한다.
// 아래쪽(가까운 곳)일수록 크고 밝으며, 위쪽(먼 곳)일수록 작고 흐리다. 시간이 멈춰도 도는 unscaledTime을 쓴다.
public class UI_RedEyes : MonoBehaviour
{
    private enum Phase { Hidden, Opening, Open, Closing }

    private class Lair
    {
        public RectTransform Root;
        public Image Left;
        public Image Right;
        public Phase Phase;
        public float Timer;        // 현재 단계가 끝나기까지 남은 시간
        public float OpenTime;     // 눈을 뜬 총 시간(드리프트 계산용)
        public float NextBlink;    // 다음 깜빡임까지 남은 시간
        public float BlinkLeft;    // 깜빡이는 중이면 남은 시간
        public float Scale;        // 크기 배율(가까울수록 큼)
        public float Brightness;   // 기본 밝기(멀수록 흐림)
        public Vector2 Home;
        public float Drift;        // 눈을 뜨고 있는 동안 옆으로 흐르는 속도
        public float Seed;
    }

    [Header("둥지 자리(캔버스 좌표, 왼쪽 위가 원점, y는 아래로 +)")]
    [SerializeField] private Rect[] _zones;
    [SerializeField] private int _lairCount = 14;
    [SerializeField] private int _seed = 1234;
    [SerializeField] private Sprite _eyeSprite;

    [Header("눈 모양과 박자")]
    [SerializeField] private Vector2 _eyeSize = new Vector2(30f, 24f);
    [SerializeField] private float _eyeSpacing = 66f;           // 두 눈 사이(크기 배율 1일 때)
    [SerializeField] private Vector2 _hiddenRange = new Vector2(1.5f, 8f);
    [SerializeField] private Vector2 _openRange = new Vector2(1.6f, 5.5f);
    [SerializeField] private float _fadeInTime = 0.35f;
    [SerializeField] private float _fadeOutTime = 0.5f;
    [SerializeField] private Vector2 _blinkGap = new Vector2(0.8f, 3f);
    [SerializeField] private float _blinkTime = 0.14f;
    [SerializeField] private float _startDelay = 2.5f;          // 화면이 밝아진 뒤부터 나타난다.
    [SerializeField] private float _minScale = 0.5f;
    [SerializeField] private float _maxScale = 1.6f;
    [SerializeField] private float _nearY = 1000f;              // 이 높이(아래쪽)가 가장 가까운 곳
    [SerializeField] private float _farY = 250f;

    private Lair[] _lairs;
    private float _elapsed;

    private void Awake()
    {
        if (_zones == null || _zones.Length == 0 || _eyeSprite == null)
        {
            GameLogger.LogError("[UI_RedEyes] 둥지 자리나 눈 그림이 연결되지 않아 눈을 만들지 않습니다.");
            _lairs = new Lair[0];
            return;
        }

        System.Random rng = new System.Random(_seed);
        _lairs = new Lair[_lairCount];
        for (int i = 0; i < _lairCount; i++)
        {
            Rect zone = _zones[rng.Next(0, _zones.Length)];
            Vector2 pos = new Vector2(zone.x + (float)rng.NextDouble() * zone.width, zone.y + (float)rng.NextDouble() * zone.height);
            float depth = Mathf.Clamp01((pos.y - _farY) / (_nearY - _farY));

            Lair lair = new Lair
            {
                Home = pos,
                Scale = Mathf.Lerp(_minScale, _maxScale, depth),
                Brightness = Mathf.Lerp(0.35f, 1f, depth),
                Seed = (float)rng.NextDouble() * 100f,
                Drift = ((float)rng.NextDouble() - 0.5f) * 14f,
                Phase = Phase.Hidden,
                Timer = _startDelay + Random.Range(0f, _hiddenRange.y),
            };

            lair.Root = NewRect("Lair", transform);
            lair.Root.anchoredPosition = new Vector2(pos.x, -pos.y);
            lair.Left = NewEye(lair.Root, -_eyeSpacing * 0.5f * lair.Scale, lair.Scale, false);
            lair.Right = NewEye(lair.Root, _eyeSpacing * 0.5f * lair.Scale, lair.Scale, true);
            SetAlpha(lair, 0f);
            _lairs[i] = lair;
        }
    }

    private RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = Vector2.zero;
        return r;
    }

    private Image NewEye(RectTransform parent, float x, float scale, bool mirror)
    {
        RectTransform r = NewRect("Eye", parent);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = _eyeSize * scale;
        r.anchoredPosition = new Vector2(x, 0f);
        r.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f); // 오른쪽 눈은 뒤집어 한 쌍이 안쪽으로 치켜 올라가게 한다.

        Image image = r.gameObject.AddComponent<Image>();
        image.sprite = _eyeSprite;
        image.raycastTarget = false;
        return image;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _elapsed += dt;

        foreach (Lair lair in _lairs)
        {
            lair.Timer -= dt;

            switch (lair.Phase)
            {
                case Phase.Hidden:
                    if (lair.Timer <= 0f) Begin(lair);
                    break;

                case Phase.Opening:
                    SetAlpha(lair, 1f - Mathf.Clamp01(lair.Timer / _fadeInTime));
                    if (lair.Timer <= 0f) { lair.Phase = Phase.Open; lair.Timer = Random.Range(_openRange.x, _openRange.y); }
                    break;

                case Phase.Open:
                    lair.OpenTime += dt;
                    UpdateBlink(lair, dt);
                    SetAlpha(lair, 1f);
                    if (lair.Timer <= 0f) { lair.Phase = Phase.Closing; lair.Timer = _fadeOutTime; }
                    break;

                case Phase.Closing:
                    SetAlpha(lair, Mathf.Clamp01(lair.Timer / _fadeOutTime));
                    if (lair.Timer <= 0f) { lair.Phase = Phase.Hidden; lair.Timer = Random.Range(_hiddenRange.x, _hiddenRange.y); SetAlpha(lair, 0f); }
                    break;
            }

            // 눈을 뜬 동안 천천히 옆으로 흐른다(어둠 속에서 다가오는 느낌).
            if (lair.Phase != Phase.Hidden)
            {
                lair.Root.anchoredPosition = new Vector2(lair.Home.x + lair.Drift * lair.OpenTime, -lair.Home.y);
            }
        }
    }

    private void Begin(Lair lair)
    {
        lair.Phase = Phase.Opening;
        lair.Timer = _fadeInTime;
        lair.OpenTime = 0f;
        lair.NextBlink = Random.Range(_blinkGap.x, _blinkGap.y);
        lair.BlinkLeft = 0f;
        lair.Root.localScale = Vector3.one;
    }

    private void UpdateBlink(Lair lair, float dt)
    {
        if (lair.BlinkLeft > 0f)
        {
            lair.BlinkLeft -= dt;
            // 감았다 뜨는 정도: 가운데에서 가장 감긴다.
            float t = 1f - Mathf.Clamp01(lair.BlinkLeft / _blinkTime);
            lair.Root.localScale = new Vector3(1f, Mathf.Max(0.05f, Mathf.Abs(t * 2f - 1f)), 1f);
            if (lair.BlinkLeft <= 0f) { lair.Root.localScale = Vector3.one; lair.NextBlink = Random.Range(_blinkGap.x, _blinkGap.y); }
            return;
        }

        lair.NextBlink -= dt;
        if (lair.NextBlink <= 0f) lair.BlinkLeft = _blinkTime;
    }

    // 눈의 밝기. 살아 있는 듯 조금씩 일렁인다.
    private void SetAlpha(Lair lair, float fade)
    {
        float shimmer = 0.85f + 0.15f * Mathf.PerlinNoise(lair.Seed, _elapsed * 2.2f);
        float a = Mathf.Clamp01(fade * lair.Brightness * shimmer);
        Color c = new Color(1f, 0.85f + 0.15f * (1f - lair.Brightness), 0.85f + 0.15f * (1f - lair.Brightness), a);
        lair.Left.color = c;
        lair.Right.color = c;
    }
}
