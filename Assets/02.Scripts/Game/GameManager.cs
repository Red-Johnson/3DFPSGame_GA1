using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

[DefaultExecutionOrder(-100)]   // 다른 컴포넌트가 OnEnable에서 Instance를 써도 되도록 가장 먼저 초기화한다.
public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;

    // 게임 상태가 바뀌면 구독자에게 새 상태를 알린다.
    public event Action<GameState> OnStateChanged;

    [SerializeField] private TextMeshProUGUI _stateText;
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

    private void Start()
    {
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
        if (_player != null)
        {
            _player.OnDied -= GameOver;
        }
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(_readyDuration);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(_startDuration);
        _stateText.gameObject.SetActive(false);

        SetState(GameState.Playing);
    }

    public void GameOver()
    {
        if (_state == GameState.GameOver) return;

        SetState(GameState.GameOver);

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
    }

    private void SetState(GameState state)
    {
        _state = state;
        OnStateChanged?.Invoke(state);
    }
}
