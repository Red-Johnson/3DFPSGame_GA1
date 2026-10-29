using UnityEngine;

// 마우스 커서를 화면 가운데에 잠그고 숨긴다.
public class CursorManager : MonoBehaviour
{
    private void Awake()
    {
        LockCursor();
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