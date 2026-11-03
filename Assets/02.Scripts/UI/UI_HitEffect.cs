using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.06f;
    [SerializeField] private float _fadeTime = 0.3f;
    [SerializeField] private float _hitAlpha = 0.6f;    // 플레이어가 맞았을 때 화면이 붉어지는 처음 투명도

    [SerializeField] private Player _player;    // 비워두면 씬에서 찾는다.

    private Image _image;
    private Coroutine _routine;     // 진행 중인 연출. 새 연출이 시작되면 먼저 멈춘다.

    // 평소에 꺼 두려면 GameObject가 아니라 Image 컴포넌트를 끈다.
    // 이 오브젝트가 비활성이면 OnEnable에서 이벤트를 구독하지 못해 피격 연출이 나오지 않는다.
    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
    }

    private void OnEnable()
    {
        if (_player == null)
        {
            _player = FindAnyObjectByType<Player>();
            GameLogger.LogWarning("[UI_HitEffect] _player가 연결되지 않아 씬에서 찾았습니다. 인스펙터에서 연결하세요.");
        }

        if (_player != null)
        {
            _player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _player.OnDamaged -= OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(_hitAlpha);
    }

    public void Flash(float alpha)
    {
        SetAlpha(alpha);

        StartRoutine(Flash_Coroutine());
    }

    private IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return new WaitForSeconds(_flashTime);

        _image.enabled = false;
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);

        StartRoutine(Fade_Coroutine());
    }

    private void StartRoutine(IEnumerator routine)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(routine);
    }

    private IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;

        float elpasedTime = 0f;

        float startAlpha = _image.color.a;

        while (elpasedTime < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elpasedTime / _fadeTime);

            SetAlpha(alpha);

            elpasedTime += Time.deltaTime;
            yield return null; // 한 프레임씩 쉰다!
        }

        SetAlpha(0f);
        _image.enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}
