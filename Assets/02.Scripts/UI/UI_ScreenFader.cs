using System.Collections;
using UnityEngine;

// 화면 전체를 덮는 검은 막의 투명도를 바꿔 페이드 인/아웃을 한다.
// 시간이 멈춘 상태(timeScale 0)에서도 돌도록 unscaledDeltaTime을 쓰고, 막이 보이는 동안에는 뒤의 UI 클릭을 막는다.
[RequireComponent(typeof(CanvasGroup))]
public class UI_ScreenFader : MonoBehaviour
{
    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
    }

    // 검은 막을 즉시 이 투명도로 둔다. (0 투명, 1 완전히 검음)
    public void SetAlpha(float alpha)
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        _group.alpha = alpha;
        _group.blocksRaycasts = alpha > 0.01f;
    }

    // 검은 화면에서 서서히 밝아진다.
    public IEnumerator FadeIn_Coroutine(float duration)
    {
        yield return Fade_Coroutine(0f, duration);
    }

    // 서서히 검게 어두워진다.
    public IEnumerator FadeOut_Coroutine(float duration)
    {
        yield return Fade_Coroutine(1f, duration);
    }

    private IEnumerator Fade_Coroutine(float target, float duration)
    {
        float start = _group.alpha;
        _group.blocksRaycasts = true;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        SetAlpha(target);
    }
}
