using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면에서 가로등 근처에 떠오르는 먼지와 불씨(작은 빛 알갱이)들.
// 가로등에서 많이 나고 나머지는 화면 아래에서 난다. 오브젝트를 새로 만들지 않고 정해진 개수를 돌려 쓴다.
public class UI_TitleEmbers : MonoBehaviour
{
    private class Ember
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Origin;
        public float Age;
        public float Life;
        public float Rise;      // 초당 오르는 거리
        public float DriftAmp;  // 좌우로 흔들리는 폭
        public float DriftFreq;
        public float Phase;
        public float Alpha;     // 최대 밝기
    }

    [SerializeField] private RectTransform _lampOrigin;                 // 가로등 머리 위치(알갱이가 가장 많이 나는 곳)
    [SerializeField] private Sprite _sprite;                            // 부드러운 점 그림
    [SerializeField] private float _canvasWidth = 1920f;
    [SerializeField] private float _canvasHeight = 1080f;
    [SerializeField] private int _count = 40;
    [SerializeField] private float _lampShare = 0.7f;                   // 가로등 근처에서 나는 비율
    [SerializeField] private float _lampSpread = 150f;
    [SerializeField] private Vector2 _sizeRange = new Vector2(4f, 12f);
    [SerializeField] private Vector2 _lifeRange = new Vector2(3f, 7f);
    [SerializeField] private Vector2 _riseRange = new Vector2(18f, 55f);
    [SerializeField] private Color _warm = new Color(1f, 0.72f, 0.3f, 1f);
    [SerializeField] private Color _hot = new Color(1f, 0.93f, 0.7f, 1f);
    [SerializeField] private Color _cold = new Color(0.5f, 0.65f, 1f, 1f);   // 가로등에서 먼 곳의 알갱이는 푸른 먼지

    private Ember[] _embers;

    private void Awake()
    {
        _embers = new Ember[_count];
        for (int i = 0; i < _count; i++)
        {
            GameObject go = new GameObject("Ember", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            Image image = go.GetComponent<Image>();
            image.sprite = _sprite;
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Ember e = new Ember { Rect = rect, Image = image };
            Respawn(e);
            e.Age = Random.value * e.Life; // 처음부터 화면에 골고루 퍼져 있게 한다.
            _embers[i] = e;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        foreach (Ember e in _embers)
        {
            e.Age += dt;
            if (e.Age >= e.Life) Respawn(e);

            float t = e.Age / e.Life;
            float x = e.Origin.x + Mathf.Sin(e.Age * e.DriftFreq + e.Phase) * e.DriftAmp;
            float y = e.Origin.y + e.Age * e.Rise;
            e.Rect.anchoredPosition = new Vector2(x, -(_canvasHeight - y));

            // 나타났다가 서서히 사라진다. 가로등 쪽은 밝은 색에서 호박색으로, 먼 곳은 푸른 먼지로 식는다.
            float fade = Mathf.Clamp01(t * 5f) * Mathf.Clamp01((1f - t) * 2.2f);
            Color c = e.Image.color;
            c.a = fade * e.Alpha;
            e.Image.color = c;
        }
    }

    // 알갱이를 새로 띄운다. 좌표는 캔버스 왼쪽 아래를 원점으로 한다(y는 위쪽이 +).
    private void Respawn(Ember e)
    {
        bool nearLamp = _lampOrigin != null && Random.value < _lampShare;
        Color baseColor;
        if (nearLamp)
        {
            // 가로등 머리의 캔버스 좌표(왼쪽 위 기준 anchoredPosition)를 왼쪽 아래 원점으로 바꾼다.
            Vector2 p = _lampOrigin.anchoredPosition;
            e.Origin = new Vector2(p.x + Random.Range(-_lampSpread, _lampSpread), _canvasHeight + p.y + Random.Range(-_lampSpread * 0.8f, _lampSpread * 0.3f));
            baseColor = Color.Lerp(_hot, _warm, Random.value);
            e.Alpha = Random.Range(0.5f, 0.95f);
        }
        else
        {
            e.Origin = new Vector2(Random.value * _canvasWidth, Random.Range(0f, _canvasHeight * 0.6f));
            baseColor = _cold;
            e.Alpha = Random.Range(0.12f, 0.35f);
        }

        e.Age = 0f;
        e.Life = Random.Range(_lifeRange.x, _lifeRange.y);
        e.Rise = Random.Range(_riseRange.x, _riseRange.y);
        e.DriftAmp = Random.Range(8f, 40f);
        e.DriftFreq = Random.Range(0.6f, 1.6f);
        e.Phase = Random.value * Mathf.PI * 2f;

        float size = Random.Range(_sizeRange.x, _sizeRange.y);
        e.Rect.sizeDelta = new Vector2(size, size);
        e.Image.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }
}
