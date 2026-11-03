using UnityEngine;

// 게임오버 메뉴: GameManager가 GameOver 상태가 되면 패널(뒷배경 어둡게 + 다시 시작, 게임 종료 버튼)을 보여 준다.
// 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙이고, 켜고 끌 패널은 _panel로 연결한다.
public class UI_GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            GameLogger.LogError("[UI_GameOverMenu] 씬에 GameManager가 없어 게임오버 메뉴를 쓸 수 없습니다.");
            return;
        }

        GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void Start()
    {
        _panel.SetActive(false);
    }

    private void HandleStateChanged(GameState state)
    {
        _panel.SetActive(state == GameState.GameOver);
    }

    // 버튼 onClick에서 호출한다.
    public void OnRestartClicked()
    {
        GameManager.Instance.Restart();
    }

    public void OnQuitClicked()
    {
        GameManager.Instance.Quit();
    }
}
