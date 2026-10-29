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

        StartCoroutine(Flash_Coroutine());
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

        StartCoroutine(Fade_Coroutine());
        // 2. 0.4에서 서서히 0으로 가서 사라지게 한다.
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
