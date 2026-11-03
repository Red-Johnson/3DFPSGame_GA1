using UnityEngine;

// 플레이어 입력을 한곳에서 읽는다. 컴포넌트가 아닌 정적 클래스라 씬에 붙일 것이 없다.
// - 게임이 진행 중이 아니면 모두 입력 없음으로 돌려준다.
// - 발사, 폭탄, 줌은 커서가 풀려 있는 동안(UI를 조작하는 중) 입력 없음으로 돌려준다.
// - 키와 마우스 버튼은 모두 아래 상수로 정하고, 다른 곳에서는 Input을 직접 읽지 않는다.
public static class GameInput
{
    // 마우스 버튼 (0: 왼쪽, 1: 오른쪽).
    // Input Manager의 Fire1은 left ctrl, Fire2는 left alt도 포함하므로 쓰지 않고 마우스 버튼을 직접 읽는다.
    private const int FireMouseButton = 0;      // 사격
    private const int BombMouseButton = 1;      // 폭탄 던지기(누르고 있으면 충전)
    private const int ZoomMouseButton = 1;      // 지정사수소총 줌. 폭탄과 같은 버튼이지만 무기 모드가 달라 겹치지 않는다.

    private const KeyCode AssaultRifleModeKey = KeyCode.Alpha1;    // 무기 모드: 돌격소총
    private const KeyCode MarksmanRifleModeKey = KeyCode.Alpha2;  // 무기 모드: 지정사수소총
    private const KeyCode MeleeModeKey = KeyCode.Alpha3;        // 무기 모드: 근접
    private const KeyCode CursorUnlockKey = KeyCode.LeftAlt;    // 누르고 있는 동안 커서를 푼다.
    private const KeyCode ReloadKey = KeyCode.R;                // 장전
    private const KeyCode PauseKey = KeyCode.Escape;            // 일시정지, 재개

    // Input Manager의 축, 버튼 이름
    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";
    private const string JumpButton = "Jump";
    private const string SprintButton = "Sprint";
    private const KeyCode MinimapZoomInKey = KeyCode.Alpha0;    // 미니맵 확대
    private const KeyCode MinimapZoomOutKey = KeyCode.Alpha9;   // 미니맵 축소

    // 이동 입력 (-1, 0, 1). x: 좌우, y: 앞뒤
    public static Vector2 MoveAxis => IsPlaying ? new Vector2(Input.GetAxisRaw(HorizontalAxis), Input.GetAxisRaw(VerticalAxis)) : Vector2.zero;

    // 이번 프레임에 마우스가 움직인 양. x: 좌우, y: 위아래 (프레임 시간을 곱하지 않는다.)
    // 커서가 보이는 동안(Alt)에는 시점이 돌지 않는다.
    public static Vector2 LookDelta => CanUseWeapon ? new Vector2(Input.GetAxis(MouseXAxis), Input.GetAxis(MouseYAxis)) : Vector2.zero;

    public static bool JumpPressed => IsPlaying && Input.GetButtonDown(JumpButton);
    public static bool SprintHeld => IsPlaying && Input.GetButton(SprintButton);
    public static bool ReloadPressed => IsPlaying && Input.GetKeyDown(ReloadKey);

    public static bool FireHeld => CanUseWeapon && Input.GetMouseButton(FireMouseButton);        // 연사 총, 근접 공격
    public static bool FirePressed => CanUseWeapon && Input.GetMouseButtonDown(FireMouseButton);   // 단발 총
    public static bool BombPressed => CanUseWeapon && Input.GetMouseButtonDown(BombMouseButton);
    public static bool BombHeld => CanUseWeapon && Input.GetMouseButton(BombMouseButton);
    public static bool BombReleased => CanUseWeapon && Input.GetMouseButtonUp(BombMouseButton);

    public static bool ZoomHeld => CanUseWeapon && Input.GetMouseButton(ZoomMouseButton);

    public static bool AssaultRifleModePressed => IsPlaying && Input.GetKeyDown(AssaultRifleModeKey);
    public static bool MarksmanRifleModePressed => IsPlaying && Input.GetKeyDown(MarksmanRifleModeKey);
    public static bool MeleeModePressed => IsPlaying && Input.GetKeyDown(MeleeModeKey);

    // 마우스 휠로 무기 모드를 넘긴다. 아래로 굴리면 다음 모드(1), 위로 굴리면 이전 모드(-1), 안 굴리면 0.
    public static int WeaponScrollStep
    {
        get
        {
            if (!IsPlaying) return 0;

            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0f) return -1;
            if (scroll < 0f) return 1;
            return 0;
        }
    }

    // 일시정지 중에도 읽어야 재개할 수 있으므로 게임 상태를 확인하지 않는다. (상태 판단은 GameManager가 한다.)
    public static bool PausePressed => Input.GetKeyDown(PauseKey);

    public static bool CursorUnlockPressed => IsPlaying && Input.GetKeyDown(CursorUnlockKey);
    public static bool CursorUnlockReleased => IsPlaying && Input.GetKeyUp(CursorUnlockKey);


    public static bool MinimapZoomInPressed => IsPlaying && Input.GetKeyDown(MinimapZoomInKey);
    public static bool MinimapZoomOutPressed => IsPlaying && Input.GetKeyDown(MinimapZoomOutKey);

    private static bool IsPlaying => GameManager.Instance != null && GameManager.Instance.IsPlaying;

    // 무기(발사, 폭탄, 줌)와 시점 조절은 게임 중이고 커서가 잠겨 있을 때만 쓸 수 있다.
    private static bool CanUseWeapon => IsPlaying && !Cursor.visible;
}
