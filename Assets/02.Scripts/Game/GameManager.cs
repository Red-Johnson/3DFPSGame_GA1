using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    GameOver,
    Paused
}

[DefaultExecutionOrder(-100)]   // 다른 컴포넌트가 OnEnable에서 Instance를 써도 되도록 가장 먼저 초기화한다.
public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;

    // 코루틴에서 "게임이 진행 중이 될 때까지 기다린다"를 쓸 때 yield return으로 쓴다. 한 번만 만들어 모든 코루틴이 공유한다.
    public static readonly WaitUntil WaitUntilPlaying = new WaitUntil(() => _instance != null && _instance.IsPlaying);

    // 게임 상태가 바뀌면 구독자에게 새 상태를 알린다.
    public event Action<GameState> OnStateChanged;

    // 시작 연출 문구("준비", "시작!")를 알린다. 문구를 없애야 하면 null을 알린다. (UI_StartMessage가 구독한다. 이 클래스는 UI를 모른다.)
    public event Action<string> OnStartMessage;

    [SerializeField] private Player _player;    // 비워두면 씬에서 찾는다.

    [Header("시작 연출")]
    [SerializeField] private float _readyDuration = 1.5f;   // "준비" 표시 시간
    [SerializeField] private float _startDuration = 0.5f;   // "시작!" 표시 시간

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Update()
    {
        if (!GameInput.PausePressed) return;

        if (_state == GameState.Playing) Pause();
        else if (_state == GameState.Paused) Resume();
    }

    // 일시정지: 게임 시간을 멈춘다. 메뉴 표시는 UI_StatePanel이 OnStateChanged를 구독해 맡는다.
    public void Pause()
    {
        if (_state != GameState.Playing) return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    // 재개: 일시정지에서 Playing으로 돌아간다.
    public void Resume()
    {
        if (_state != GameState.Paused) return;

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    // 다시 시작: 현재 씬을 처음부터 다시 불러온다.
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 게임 종료: 에디터에서는 플레이 모드를 끈다.
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Start()
    {
        // 중복 인스턴스는 Awake에서 파괴 예약되었으므로 시작 연출을 되풀이하지 않는다.
        if (_instance != this) return;

        // 플레이어가 죽으면 게임 오버로 넘어간다. (플레이어는 게임 흐름을 직접 알지 못한다.)
        if (_player == null)
        {
            _player = FindAnyObjectByType<Player>();
            GameLogger.LogWarning("[GameManager] _player가 연결되지 않아 씬에서 찾았습니다. 인스펙터에서 연결하세요.");
        }

        if (_player != null)
        {
            _player.OnDied += GameOver;
        }

        StartCoroutine(StartSequence_Coroutine());
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Time.timeScale = 1f;    // 일시정지 중 씬이 바뀌어도 멈춘 채로 남지 않게 한다.
            _instance = null;       // 파괴된 인스턴스를 가리키지 않게 비운다. (구독 해제는 Instance가 null인지 확인하고 건너뛴다.)
        }

        if (_player != null)
        {
            _player.OnDied -= GameOver;
        }
    }

    // 시작 연출("준비", "시작!")을 알린 뒤 Playing으로 넘어간다. 문구를 보여 주는 것은 UI가 맡으므로, 구독자가 없어도 게임은 시작한다.
    private IEnumerator StartSequence_Coroutine()
    {
        OnStartMessage?.Invoke("준비");
        yield return new WaitForSeconds(_readyDuration);
        OnStartMessage?.Invoke("시작!");
        yield return new WaitForSeconds(_startDuration);
        OnStartMessage?.Invoke(null);

        // 연출 중에 게임오버가 되었으면 Playing으로 덮어쓰지 않는다.
        if (_state != GameState.Ready) yield break;

        SetState(GameState.Playing);
    }

    public void GameOver()
    {
        if (_state == GameState.GameOver) return;

        // 시간을 먼저 멈춘 뒤 알린다. (Pause와 같은 순서라 구독자가 어느 상태에서나 같은 timeScale을 본다.)
        Time.timeScale = 0f;

        // 시작 연출 중에 죽으면 시간이 멈춰 연출이 끝나지 못하므로, 연출을 멈추고 문구를 지운다.
        StopAllCoroutines();
        OnStartMessage?.Invoke(null);

        SetState(GameState.GameOver);
        // 게임오버 문구와 버튼은 UI_StatePanel이 OnStateChanged를 구독해 보여 준다.
    }

    private void SetState(GameState state)
    {
        _state = state;
        OnStateChanged?.Invoke(state);
    }
}
