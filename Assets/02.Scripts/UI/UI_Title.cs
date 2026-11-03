using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면의 연출을 맡는다. 로그인 처리는 UI_Login이 한다.
// 시작 순서: 검은 화면 → 어두운 도시가 서서히 보임 → 꺼져 가던 가로등이 깜빡이며 켜짐 → 제목 → 로그인 창.
// 상시 연출: 가로등 빛 일렁임(가끔 꺼질 듯 깜빡임), 마우스를 따라 도는 배경 시차, 일렁이는 푸른 안개,
// 가끔 번쩍이는 번개, 제목 뒤 푸른 번짐. (빨간 눈은 UI_RedEyes, 비는 UI_Rain, 먼지는 UI_TitleEmbers가 맡는다.)
// 시간이 멈춰 있어도 도는 unscaledTime을 쓴다.
public class UI_Title : MonoBehaviour
{
    // 마우스에 따라 움직이는 배경 한 겹. Strength는 캔버스 단위로 최대 움직임 양이다(가까울수록 크게).
    [Serializable]
    private class ParallaxLayer
    {
        public RectTransform Rect;
        public Vector2 Strength = new Vector2(8f, 4f);
        [NonSerialized] public Vector2 BasePos;
    }

    // 화면 전체를 덮고 바람에 날리듯 옆으로 흐르는 수증기 한 겹. 돌풍이 불면 빨라지고, 위아래로 일렁이며, 짙기도 변한다.
    [Serializable]
    private class FogLayer
    {
        public RawImage Image;
        public float Speed = 0.1f;          // 초당 텍스처 한 바퀴의 비율(음수면 반대로 흐른다)
        public float Gust = 0.6f;           // 돌풍으로 속도가 변하는 폭(0이면 일정한 속도)
        public float BobAmplitude = 14f;    // 위아래로 일렁이는 폭
        public float BobSpeed = 0.35f;
        public float AlphaPulse = 0.15f;    // 짙기가 변하는 폭
        [NonSerialized] public Vector2 BasePos;
        [NonSerialized] public float BaseAlpha;
        [NonSerialized] public float Seed;
        [NonSerialized] public float Offset; // 지금까지 흘러간 텍스처 위치
    }

    [Header("연출 대상")]
    [SerializeField] private UI_ScreenFader _fader;
    [SerializeField] private Image _lampGlow;          // 가로등 머리 번짐
    [SerializeField] private Image _lampCone;          // 가로등이 내리쬐는 빛줄기
    [SerializeField] private Image _groundGlow;        // 바닥에 고이는 빛
    [SerializeField] private CanvasGroup _titleGroup;
    [SerializeField] private RectTransform _titleRect;
    [SerializeField] private Image _titleGlow;         // 제목 뒤 푸른 번짐
    [SerializeField] private CanvasGroup _loginGroup;
    [SerializeField] private RectTransform _loginRect;
    [SerializeField] private Image _lightning;        // 번개 섬광(화면 전체)
    [SerializeField] private RectTransform _bolt;      // 하늘에서 갈라져 내려오는 번개 줄기(위쪽 가운데가 기준)
    [SerializeField] private ParallaxLayer[] _parallaxLayers;
    [SerializeField] private FogLayer[] _fogLayers;

    [Header("튜닝")]
    [SerializeField] private float _startDelay = 0.4f;
    [SerializeField] private float _backgroundFadeTime = 2.4f;
    [SerializeField] private float _titleFadeTime = 1.4f;
    [SerializeField] private float _loginFadeTime = 0.9f;
    [SerializeField] private float _riseDistance = 28f;         // 제목과 로그인 창이 떠오르는 거리
    [SerializeField] private float _lampGlowAlpha = 0.5f;
    [SerializeField] private float _lampConeAlpha = 0.2f;
    [SerializeField] private float _groundGlowAlpha = 0.4f;
    [SerializeField] private float _titleGlowAlpha = 0.12f;
    [SerializeField] private float _parallaxSmooth = 3f;
    [SerializeField] private Vector2 _lightningInterval = new Vector2(4f, 10f);
    [SerializeField] private float _lightningPeak = 0.2f;

    // 가로등이 켜지는 깜빡임 순서: (밝기, 유지 시간)
    private static readonly Vector2[] IgniteSteps =
    {
        new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.09f), new Vector2(0.08f, 0.14f),
        new Vector2(1.0f, 0.18f), new Vector2(0.35f, 0.08f), new Vector2(1.0f, 0.3f),
    };

    private float _ignite;       // 0(꺼짐)~1(켜짐). 가로등 빛과 제목 번짐의 바탕 밝기
    private Vector2 _pointer;
    private Vector2 _titleBasePos;
    private Vector2 _loginBasePos;
    private float _titleRise;    // 0~1, 제목이 떠오른 정도
    private float _loginRise;
    private float _lightningLevel;

    // 시작 연출이 끝나 로그인을 눌러도 되는지
    public bool IntroFinished { get; private set; }

    private void Awake()
    {
        if (_fader == null) GameLogger.LogError("[UI_Title] 페이더가 연결되지 않았습니다.");

        foreach (ParallaxLayer layer in _parallaxLayers)
        {
            if (layer.Rect != null) layer.BasePos = layer.Rect.anchoredPosition;
        }

        foreach (FogLayer fog in _fogLayers)
        {
            if (fog.Image == null) continue;
            fog.BasePos = fog.Image.rectTransform.anchoredPosition;
            fog.BaseAlpha = fog.Image.color.a;
            fog.Seed = UnityEngine.Random.value * 100f;
        }

        if (_titleRect != null) _titleBasePos = _titleRect.anchoredPosition;
        if (_loginRect != null) _loginBasePos = _loginRect.anchoredPosition;
    }

    private void Start()
    {
        // 이전 씬에서 시간이 멈춘 채로 넘어왔어도 타이틀은 정상 속도로 돈다.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetAlpha(_titleGroup, 0f);
        SetAlpha(_loginGroup, 0f);
        if (_loginGroup != null)
        {
            _loginGroup.interactable = false;
            _loginGroup.blocksRaycasts = false;
        }

        ApplyGlow(0f);
        _fader.SetAlpha(1f);
        StartCoroutine(Intro_Coroutine());
    }

    private IEnumerator Intro_Coroutine()
    {
        yield return new WaitForSecondsRealtime(_startDelay);
        yield return _fader.FadeIn_Coroutine(_backgroundFadeTime);

        // 꺼져 가던 가로등이 깜빡이며 켜진다.
        foreach (Vector2 step in IgniteSteps)
        {
            _ignite = step.x;
            yield return new WaitForSecondsRealtime(step.y);
        }
        _ignite = 1f;

        // 제목이 떠오른다.
        yield return Rise_Coroutine(_titleGroup, _titleFadeTime, v => _titleRise = v);

        // 로그인 창이 떠오른다.
        yield return Rise_Coroutine(_loginGroup, _loginFadeTime, v => _loginRise = v);

        if (_loginGroup != null)
        {
            _loginGroup.interactable = true;
            _loginGroup.blocksRaycasts = true;
        }
        IntroFinished = true;

        StartCoroutine(Lightning_Coroutine());
    }

    // 그룹의 투명도를 0에서 1로 올리면서 떠오른 정도(0~1)를 알려 준다.
    private IEnumerator Rise_Coroutine(CanvasGroup group, float duration, Action<float> onProgress)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float eased = 1f - Mathf.Pow(1f - t / duration, 3f); // 빠르게 시작해 천천히 멈춘다.
            if (group != null) group.alpha = eased;
            onProgress(eased);
            yield return null;
        }

        if (group != null) group.alpha = 1f;
        onProgress(1f);
    }

    // 먼 하늘에서 번개가 번쩍인다. 두세 번 연달아 깜빡이며 잦아든다.
    private IEnumerator Lightning_Coroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(_lightningInterval.x, _lightningInterval.y));

            PlaceBolt();
            int flashes = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < flashes; i++)
            {
                _lightningLevel = UnityEngine.Random.Range(0.5f, 1f) * (i == 0 ? 1f : 0.7f);
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.04f, 0.09f));
                _lightningLevel = 0.1f;
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.05f, 0.12f));
            }

            // 마지막 섬광은 천천히 사그라든다.
            _lightningLevel = 0.6f;
            while (_lightningLevel > 0.001f)
            {
                _lightningLevel = Mathf.MoveTowards(_lightningLevel, 0f, Time.unscaledDeltaTime * 1.6f);
                yield return null;
            }
            _lightningLevel = 0f;
        }
    }

    // 번개 줄기를 하늘의 아무 데나, 좌우를 무작위로 뒤집어 놓는다. 크기도 조금씩 다르게 한다.
    private void PlaceBolt()
    {
        if (_bolt == null) return;

        float scale = UnityEngine.Random.Range(0.8f, 1.2f);
        bool mirror = UnityEngine.Random.value < 0.5f;
        _bolt.localScale = new Vector3(mirror ? -scale : scale, scale, 1f);
        _bolt.anchoredPosition = new Vector2(UnityEngine.Random.Range(160f, 1760f), 0f);
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        // 가로등 빛은 늘 조금씩 일렁이고, 가끔 꺼질 듯 깜빡인다(오래된 가로등).
        float flicker = 0.86f + 0.14f * Mathf.PerlinNoise(time * 3.5f, 0.3f);
        if (Mathf.PerlinNoise(time * 0.9f, 7.1f) > 0.86f) flicker *= 0.45f;
        ApplyGlow(_ignite * flicker);

        // 마우스를 따라 배경이 움직인다.
        _pointer = Vector2.Lerp(_pointer, GameInput.PointerViewport, 1f - Mathf.Exp(-_parallaxSmooth * Time.unscaledDeltaTime));
        foreach (ParallaxLayer layer in _parallaxLayers)
        {
            if (layer.Rect == null) continue;
            layer.Rect.anchoredPosition = layer.BasePos + new Vector2(-_pointer.x * layer.Strength.x, -_pointer.y * layer.Strength.y);
        }

        // 안개는 옆으로 흐르고, 위아래로 일렁이며, 숨쉬듯 짙어졌다 옅어진다.
        foreach (FogLayer fog in _fogLayers)
        {
            if (fog.Image == null) continue;

            // 돌풍: 잡음 값에 따라 속도가 느려졌다 빨라진다.
            float gust = 1f + fog.Gust * (Mathf.PerlinNoise(time * 0.45f + fog.Seed, fog.Seed) * 2f - 1f);
            fog.Offset = Mathf.Repeat(fog.Offset + fog.Speed * gust * Time.unscaledDeltaTime, 1f);
            Rect uv = fog.Image.uvRect;
            uv.x = fog.Offset;
            fog.Image.uvRect = uv;

            float bob = Mathf.Sin(time * fog.BobSpeed + fog.Seed) * fog.BobAmplitude;
            fog.Image.rectTransform.anchoredPosition = fog.BasePos + new Vector2(0f, bob);

            Color c = fog.Image.color;
            c.a = fog.BaseAlpha * (1f + fog.AlphaPulse * Mathf.Sin(time * fog.BobSpeed * 0.7f + fog.Seed * 1.7f));
            fog.Image.color = c;
        }

        // 번개
        if (_lightning != null) SetImageAlpha(_lightning, _lightningLevel * _lightningPeak);
        if (_bolt != null) SetImageAlpha(_bolt.GetComponent<Image>(), _lightningLevel * 1.6f);

        // 제목과 로그인 창이 떠오르는 위치
        if (_titleRect != null) _titleRect.anchoredPosition = _titleBasePos + new Vector2(0f, -(1f - _titleRise) * _riseDistance);
        if (_loginRect != null) _loginRect.anchoredPosition = _loginBasePos + new Vector2(0f, -(1f - _loginRise) * _riseDistance);

        // 제목 뒤 푸른 번짐은 천천히 숨쉬고, 번개가 칠 때 확 밝아진다.
        if (_titleGlow != null)
        {
            float pulse = 0.8f + 0.2f * Mathf.Sin(time * 1.2f);
            SetImageAlpha(_titleGlow, (_titleGlowAlpha * pulse + _lightningLevel * 0.3f) * _titleRise);
        }
    }

    private void ApplyGlow(float intensity)
    {
        SetImageAlpha(_lampGlow, _lampGlowAlpha * intensity);
        SetImageAlpha(_lampCone, _lampConeAlpha * intensity);
        SetImageAlpha(_groundGlow, _groundGlowAlpha * intensity);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }
}
