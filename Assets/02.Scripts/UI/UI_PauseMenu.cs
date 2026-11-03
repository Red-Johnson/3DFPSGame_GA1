using UnityEngine;

// 일시정지 메뉴: GameManager가 Paused 상태일 때만 패널(뒷배경 어둡게 + 버튼)을 보여 준다.
// 버튼 onClick은 계속하기, 다시 시작, 게임 종료 메서드로 연결한다.
// 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙이고, 켜고 끌 패널은 _panel로 연결한다.
public class UI_PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            GameLogger.LogError("[UI_PauseMenu] 씬에 GameManager가 없어 일시정지 메뉴를 쓸 수 없습니다.");
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
        _panel.SetActive(state == GameState.Paused);
    }

    // 버튼 onClick에서 호출한다.
    public void OnContinueClicked()
    {
        GameManager.Instance.Resume();
    }

    public void OnRestartClicked()
    {
        GameManager.Instance.Restart();
    }

    public void OnQuitClicked()
    {
        GameManager.Instance.Quit();
    }
}
