using UnityEngine;

// 상태 패널: GameManager가 정한 상태(_visibleState)일 때만 패널(뒷배경 + 버튼)을 보여 준다.
// 일시정지 메뉴(Paused), 게임오버 메뉴(GameOver)가 같은 컴포넌트를 쓰고, 보일 상태만 인스펙터에서 다르게 정한다.
// 버튼 onClick은 GameManager의 Resume, Restart, Quit에 직접 연결한다. (이 컴포넌트는 패널을 켜고 끄기만 한다.)
// 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙이고, 켜고 끌 패널은 _panel로 연결한다.
public class UI_StatePanel : MonoBehaviour
{
    [SerializeField] private GameState _visibleState = GameState.Paused;    // 이 상태일 때만 패널이 보인다.
    [SerializeField] private GameObject _panel;

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            GameLogger.LogError($"[UI_StatePanel] 씬에 GameManager가 없어 {_visibleState} 패널을 쓸 수 없습니다.");
            return;
        }

        GameManager.Instance.OnStateChanged += Refresh;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= Refresh;
        }
    }

    private void Start()
    {
        // 처음 상태에 맞춰 한 번 갱신한다. (모든 컴포넌트의 Awake가 끝난 뒤)
        if (GameManager.Instance != null)
        {
            Refresh(GameManager.Instance.State);
        }
    }

    private void Refresh(GameState state)
    {
        _panel.SetActive(state == _visibleState);
    }
}
