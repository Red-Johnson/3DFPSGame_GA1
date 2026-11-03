using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면에 내리는 빗줄기. 정해진 개수의 가는 선을 위에서 아래로 돌려 쓴다.
// 멀리 있는 비는 가늘고 느리고 흐리게, 가까운 비는 길고 빠르고 진하게 그려 깊이감을 준다.
public class UI_Rain : MonoBehaviour
{
    private class Drop
    {
        public RectTransform Rect;
        public float Speed;
    }

    [SerializeField] private int _count = 140;
    [SerializeField] private float _canvasWidth = 1920f;
    [SerializeField] private float _canvasHeight = 1080f;
    [SerializeField] private Vector2 _speedRange = new Vector2(900f, 1700f);
    [SerializeField] private Vector2 _lengthRange = new Vector2(24f, 64f);
    [SerializeField] private float _wind = -0.18f;               // 가로 속도 / 세로 속도(음수면 왼쪽으로 쏠림)
    [SerializeField] private Color _color = new Color(0.6f, 0.75f, 1f, 1f);
    [SerializeField] private Vector2 _alphaRange = new Vector2(0.06f, 0.22f);

    private Drop[] _drops;

    private void Awake()
    {
        float angle = Mathf.Atan(-_wind) * Mathf.Rad2Deg; // 빗줄기가 바람 방향으로 기운다.
        _drops = new Drop[_count];

        for (int i = 0; i < _count; i++)
        {
            float depth = Random.value; // 0 먼 곳, 1 가까운 곳
            GameObject go = new GameObject("Drop", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            Color c = _color;
            c.a = Mathf.Lerp(_alphaRange.x, _alphaRange.y, depth);
            image.color = c;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Mathf.Lerp(1f, 2.5f, depth), Mathf.Lerp(_lengthRange.x, _lengthRange.y, depth));
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            rect.anchoredPosition = new Vector2(Random.value * (_canvasWidth + 400f), -Random.value * _canvasHeight);

            _drops[i] = new Drop { Rect = rect, Speed = Mathf.Lerp(_speedRange.x, _speedRange.y, depth) };
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        foreach (Drop d in _drops)
        {
            Vector2 p = d.Rect.anchoredPosition;
            p.y -= d.Speed * dt;
            p.x += d.Speed * _wind * dt;

            // 화면 아래로 나가면 위에서 다시 내린다.
            if (p.y < -_canvasHeight - 80f)
            {
                p.y = 80f;
                p.x = Random.value * (_canvasWidth + 400f);
            }
            d.Rect.anchoredPosition = p;
        }
    }
}
