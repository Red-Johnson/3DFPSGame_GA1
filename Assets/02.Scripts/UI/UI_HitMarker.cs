using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 명중 표시: 총알이나 근접 공격이 몬스터(좀비)에 맞으면 기존 조준점과 별개로 45도 기울어진 조준점이 잠깐 커졌다가 사라진다.
// 일반 명중과 약점 공격은 같은 연출을 쓰되 색깔, 크기, 시간을 각각 따로 정한다.
// 표시할 이미지 오브젝트에 붙이고(같은 오브젝트의 Image, CanvasGroup 사용), 평소에는 투명하게 둔다.
// 이미지 색은 원본에 곱해지므로 흰색 이미지를 써야 지정한 색이 그대로 나온다.
// 스코프(줌) 화면에서는 기본 조준점이 숨겨져도 이 표시는 나온다.
[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class UI_HitMarker : MonoBehaviour
{
    // 명중 종류마다 따로 정하는 연출 값
    [System.Serializable]
    private class MarkerStyle
    {
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _size = 1f;              // 최종 크기 배율(오브젝트의 원래 크기에 곱한다)
        [SerializeField] private float _startScale = 1.5f;      // 처음 나타날 때 크기 배율(최종 크기로 줄어든다)
        [SerializeField] private float _popTime = 0.08f;        // 처음 크기에서 최종 크기로 줄어드는 시간
        [SerializeField] private float _holdTime = 0.1f;        // 완전히 보이는 상태로 머무는 시간
        [SerializeField] private float _fadeTime = 0.3f;        // 사라지는 시간

        public Color Color => _color;
        public float Size => _size;
        public float StartScale => _startScale;
        public float PopTime => _popTime;
        public float HoldTime => _holdTime;
        public float FadeTime => _fadeTime;

        public MarkerStyle(Color color, float size, float startScale, float popTime, float holdTime, float fadeTime)
        {
            _color = color;
            _size = size;
            _startScale = startScale;
            _popTime = popTime;
            _holdTime = holdTime;
            _fadeTime = fadeTime;
        }
    }

    [SerializeField] private PlayerGunFire _gunFire;
    [SerializeField] private PlayerMeleeAttack _meleeAttack;     // 근접 타격에도 일반 명중 표시를 낸다. 비워두면 총 명중만 표시한다.

    [Header("일반 명중")]
    [SerializeField] private MarkerStyle _hitStyle = new MarkerStyle(Color.white, 0.8f, 1.4f, 0.06f, 0.05f, 0.2f);

    [Header("약점 공격")]
    [FormerlySerializedAs("_headshotStyle")]
    [SerializeField] private MarkerStyle _weakPointStyle = new MarkerStyle(new Color(1f, 0.15f, 0.15f, 1f), 1.2f, 1.6f, 0.08f, 0.1f, 0.3f);

    private Image _image;
    private CanvasGroup _group;
    private Vector3 _baseScale;
    private Coroutine _showCoroutine;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _group = GetComponent<CanvasGroup>();
        _baseScale = transform.localScale;
        _group.alpha = 0f;
    }

    private void OnEnable()
    {
        _gunFire.OnHit += Show;

        if (_meleeAttack != null)
        {
            _meleeAttack.OnHit += OnMeleeHit;
        }
    }

    private void OnDisable()
    {
        _gunFire.OnHit -= Show;

        if (_meleeAttack != null)
        {
            _meleeAttack.OnHit -= OnMeleeHit;
        }

        _group.alpha = 0f;
        _showCoroutine = null;
    }

    // 근접 타격은 약점이 없으므로 일반 명중 연출을 쓴다.
    private void OnMeleeHit(bool killedMonster)
    {
        Show(false);
    }

    // 이미 표시 중이어도 처음부터 다시 보여 준다.
    private void Show(bool isWeakPointHit)
    {
        MarkerStyle style = isWeakPointHit ? _weakPointStyle : _hitStyle;
        _image.color = style.Color;

        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
        }

        _showCoroutine = StartCoroutine(Show_Coroutine(style));
    }

    private IEnumerator Show_Coroutine(MarkerStyle style)
    {
        Vector3 finalScale = _baseScale * style.Size;
        _group.alpha = 1f;

        // 커졌던 크기에서 최종 크기로 빠르게 줄어든다.
        for (float time = 0f; time < style.PopTime; time += Time.deltaTime)
        {
            transform.localScale = finalScale * Mathf.Lerp(style.StartScale, 1f, time / style.PopTime);
            yield return null;
        }

        transform.localScale = finalScale;
        yield return new WaitForSeconds(style.HoldTime);

        // 서서히 투명해진다.
        for (float time = 0f; time < style.FadeTime; time += Time.deltaTime)
        {
            _group.alpha = 1f - (time / style.FadeTime);
            yield return null;
        }

        _group.alpha = 0f;
        _showCoroutine = null;
    }
}
