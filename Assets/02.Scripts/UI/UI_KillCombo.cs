using System.Collections;
using TMPro;
using UnityEngine;

// 처치 콤보 UI: 좀비를 죽이면 조준점 아래에 랜턴 아이콘이 나타나고, 마지막 처치 후 제한 시간 안에 또 죽이면 콤보가 쌓여 "X2"처럼 파란 글자가 붙는다.
// 제한 시간이 지나면 콤보가 끝나고 랜턴이 서서히 사라진다. 처치할 때마다 제한 시간은 다시 시작한다.
// 어떤 방법으로 죽였는지는 구분하지 않는다. (ZombieManager의 처치 알림을 구독한다.) 한 마리당 올라가는 콤보 수는 ZombieStats가 정한다.
// 아이콘 이미지는 중앙에 고정하고 숫자 글자는 그 오른쪽에 붙인다. 표시 전체(CanvasGroup)가 이 컴포넌트의 오브젝트다.
// 아이콘 아래의 파란 바(UI_ComboTimeBar)는 남은 콤보 시간을 보여 준다. 바는 씬에 배치해 인스펙터로 연결한다.
[RequireComponent(typeof(CanvasGroup))]
public class UI_KillCombo : MonoBehaviour
{
    private const int FirstShownComboCount = 2;     // 이 콤보부터 숫자를 보여 준다. (첫 처치는 아이콘만)

    [SerializeField] private TextMeshProUGUI _comboText;
    [SerializeField] private UI_ComboTimeBar _timeBar;      // 남은 콤보 시간을 보여 주는 바
    [SerializeField] private float _comboTime = 5f;         // 마지막 처치 후 콤보가 이어지는 시간(초)
    [SerializeField] private float _fadeTime = 0.3f;        // 콤보가 끝난 뒤 사라지는 시간
    [SerializeField] private float _startScale = 1.4f;      // 처치할 때 커졌다가 원래 크기로 줄어드는 시작 배율
    [SerializeField] private float _popTime = 0.1f;         // 위 배율에서 원래 크기로 줄어드는 시간

    private CanvasGroup _group;
    private Vector3 _baseScale;
    private Coroutine _comboCoroutine;
    private int _comboCount;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _baseScale = transform.localScale;

        if (_timeBar == null)
        {
            Debug.LogWarning("UI_KillCombo: _timeBar가 연결되지 않아 콤보 시간 바를 보여 주지 않습니다.", this);
        }

        Hide();
    }

    // 바가 연결되지 않아도 콤보는 동작하도록 null을 건너뛴다.
    private void SetTimeBarFill(float fill)
    {
        if (_timeBar != null)
        {
            _timeBar.SetFill(fill);
        }
    }

    // ZombieManager는 DefaultExecutionOrder(-100)으로 먼저 초기화되므로 OnEnable에서 Instance를 써도 된다.
    private void OnEnable()
    {
        ZombieManager.Instance.OnZombieKilled += OnZombieKilled;
    }

    private void OnDisable()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.OnZombieKilled -= OnZombieKilled;
        }

        _comboCoroutine = null;
        Hide();
    }

    private void OnZombieKilled(Zombie zombie)
    {
        // 게임이 끝난 뒤의 처치는 세지 않는다.
        if (!GameManager.Instance.IsPlaying) return;

        // 사라지는 중이면 콤보는 이미 끝난 것이라 0에서 다시 센다. (콤보가 이어지는 중이면 쌓인다.)
        // 처치한 몬스터마다 올라가는 수가 다르다. (일반 좀비 1, 보스 5 등. ZombieStats의 ComboValue)
        _comboCount += zombie.Stats.ComboValue;

        if (_comboCoroutine != null)
        {
            StopCoroutine(_comboCoroutine);
        }

        _comboCoroutine = StartCoroutine(Combo_Coroutine());
    }

    private IEnumerator Combo_Coroutine()
    {
        _comboText.gameObject.SetActive(_comboCount >= FirstShownComboCount);
        _comboText.text = $"X{_comboCount}";
        _group.alpha = 1f;
        SetTimeBarFill(1f);

        // 처치할 때마다 커졌다가 원래 크기로 빠르게 줄어든다.
        for (float time = 0f; time < _popTime; time += Time.deltaTime)
        {
            transform.localScale = _baseScale * Mathf.Lerp(_startScale, 1f, time / _popTime);
            yield return null;
        }

        transform.localScale = _baseScale;

        // 제한 시간 동안 바가 줄어든다.
        for (float time = 0f; time < _comboTime; time += Time.deltaTime)
        {
            SetTimeBarFill(1f - time / _comboTime);
            yield return null;
        }

        SetTimeBarFill(0f);

        // 제한 시간이 지나면 콤보가 끝난다. 사라지는 동안 또 죽이면 새 콤보가 시작된다.
        _comboCount = 0;

        for (float time = 0f; time < _fadeTime; time += Time.deltaTime)
        {
            _group.alpha = 1f - (time / _fadeTime);
            yield return null;
        }

        Hide();
        _comboCoroutine = null;
    }

    private void Hide()
    {
        _group.alpha = 0f;
        _comboCount = 0;
        transform.localScale = _baseScale;
        SetTimeBarFill(0f);
    }
}
