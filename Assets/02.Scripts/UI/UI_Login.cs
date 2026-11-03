using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀의 로그인 창. 아직 실제 로그인은 없어서, 로그인 버튼(또는 Enter)을 누르면 입력값과 상관없이 게임을 시작한다.
// 순서: 가로등 빛 섬광 → 화면이 검게 어두워짐 → 게임 씬을 불러옴. 회원가입 버튼은 아직 쓸 수 없다.
public class UI_Login : MonoBehaviour
{
    [SerializeField] private UI_Title _title;
    [SerializeField] private UI_ScreenFader _fader;
    [SerializeField] private CanvasGroup _loginGroup;
    [SerializeField] private TMP_InputField _idField;
    [SerializeField] private TMP_InputField _passwordField;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Image _flash;                    // 섬광(화면 전체를 덮는 이미지)
    [SerializeField] private string _gameSceneName = "CityScene";
    [SerializeField] private float _flashRiseTime = 0.25f;
    [SerializeField] private float _flashHoldTime = 0.15f;
    [SerializeField] private float _fadeOutTime = 0.9f;

    private bool _starting;

    private void Awake()
    {
        if (_signUpButton != null) _signUpButton.interactable = false; // 회원가입은 아직 없다.
    }

    private void OnEnable()
    {
        if (_loginButton != null) _loginButton.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        if (_loginButton != null) _loginButton.onClick.RemoveListener(StartGame);
    }

    private void Update()
    {
        // 입력창에서 Enter를 눌러도 로그인한다.
        if (GameInput.SubmitPressed) StartGame();
    }

    public void StartGame()
    {
        // 시작 연출이 끝나기 전, 또는 이미 시작한 뒤에는 무시한다.
        if (_starting || (_title != null && !_title.IntroFinished)) return;

        _starting = true;
        StartCoroutine(StartGame_Coroutine());
    }

    private IEnumerator StartGame_Coroutine()
    {
        if (_loginGroup != null)
        {
            _loginGroup.interactable = false;
            _loginGroup.blocksRaycasts = false;
        }

        // 씬을 미리 불러 두고(활성화는 막아 둠), 연출이 끝나면 넘어간다.
        AsyncOperation load = SceneManager.LoadSceneAsync(_gameSceneName);
        load.allowSceneActivation = false;

        yield return Flash_Coroutine(0f, 1f, _flashRiseTime);
        yield return new WaitForSecondsRealtime(_flashHoldTime);
        yield return _fader.FadeOut_Coroutine(_fadeOutTime);

        // allowSceneActivation이 false이면 진행도는 0.9에서 멈춘다.
        while (load.progress < 0.9f) yield return null;
        load.allowSceneActivation = true;
    }

    private IEnumerator Flash_Coroutine(float from, float to, float duration)
    {
        if (_flash == null) yield break;

        Color c = _flash.color;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / duration);
            _flash.color = c;
            yield return null;
        }

        c.a = to;
        _flash.color = c;
    }
}
