using UnityEngine;

// 마우스 커서를 화면 가운데에 잠그고 숨긴다.
// 왼쪽 Alt를 누르는 동안과, 일시정지·게임오버 메뉴가 떠 있는 동안에는 커서를 풀어 준다.
public class CursorManager : MonoBehaviour
{
    private void Awake()
    {
        LockCursor();
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            GameLogger.LogError("[CursorManager] 씬에 GameManager가 없어 일시정지, 게임오버 때 커서를 풀지 못합니다.");
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

    // 일시정지, 게임오버 메뉴를 클릭할 수 있게 풀고, 게임으로 돌아오면 다시 잠근다.
    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Paused || state == GameState.GameOver) UnlockCursor();
        else if (state == GameState.Playing) LockCursor();
    }

    private void Update()
    {
        // 게임이 진행 중이 아니면 GameInput이 입력 없음으로 돌려준다.
        if (GameInput.CursorUnlockPressed)
        {
            UnlockCursor();
        }
        else if (GameInput.CursorUnlockReleased)
        {
            LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 화면 가운데에 고정
        Cursor.visible = false; // 커서 숨기기
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
