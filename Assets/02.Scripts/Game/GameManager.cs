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
    [SerializeField] private Animator _playerAnimator;

    [Header("시작 연출")]
    [SerializeField] private float _readyDuration = 1.5f;   // "준비" 표시 시간
    [SerializeField] private float _startDuration = 0.5f;   // "시작!" 표시 시간

    private Player _player;

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
        _player = FindAnyObjectByType<Player>();
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

        _playerAnimator.SetFloat("MoveMotion", 0f);
    }

    private void SetState(GameState state)
    {
        _state = state;
        OnStateChanged?.Invoke(state);
    }
}
