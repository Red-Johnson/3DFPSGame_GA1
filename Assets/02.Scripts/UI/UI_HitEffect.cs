using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 피격 이펙트: 플레이어가 맞으면 화면을 붉게 번쩍이고 서서히 지운다.
// 평소에 숨기려면 GameObject가 아니라 Image 컴포넌트를 끈다. (오브젝트가 비활성이면 OnEnable에서 구독하지 못한다.)
// 게임오버에서는 시간이 멈춰 페이드가 중간에 얼어붙으므로, 상태가 GameOver가 되면 바로 지운다.
[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _fadeTime = 0.3f;
    [SerializeField] private float _hitAlpha = 0.6f;    // 플레이어가 맞았을 때 화면이 붉어지는 처음 투명도

    [SerializeField] private Player _player;    // 비워두면 씬에서 찾는다.

    private Image _image;
    private Coroutine _routine;     // 진행 중인 연출. 새 연출이 시작되면 먼저 멈춘다.

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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _player.OnDamaged -= OnPlayerDamaged;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        }

        Clear();
    }

    private void OnPlayerDamaged()
    {
        SetAlpha(_hitAlpha);

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Fade_Coroutine());
    }

    // 게임오버로 시간이 멈추면 페이드가 끝나지 않으므로 남은 연출을 지운다.
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            Clear();
        }
    }

    private void Clear()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (_image != null)
        {
            _image.enabled = false;
        }
    }

    private IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;

        float elapsedTime = 0f;

        float startAlpha = _image.color.a;

        while (elapsedTime < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / _fadeTime);

            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;
            yield return null; // 한 프레임씩 쉰다!
        }

        SetAlpha(0f);
        _image.enabled = false;
        _routine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}
