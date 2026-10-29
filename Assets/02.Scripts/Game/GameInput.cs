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
    private const int ZoomMouseButton = 1;      // 저격 모드 줌. 폭탄과 같은 버튼이지만 무기 모드가 달라 겹치지 않는다.

    private const KeyCode NormalModeKey = KeyCode.Alpha1;       // 무기 모드: 일반
    private const KeyCode SniperModeKey = KeyCode.Alpha2;       // 무기 모드: 저격
    private const KeyCode CursorUnlockKey = KeyCode.LeftAlt;    // 누르고 있는 동안 커서를 푼다.
    private const KeyCode ViewToggleKey = KeyCode.V;            // 1인칭 / 3인칭 시점 전환
    private const KeyCode MinimapZoomInKey = KeyCode.Alpha0;    // 미니맵 확대
    private const KeyCode MinimapZoomOutKey = KeyCode.Alpha9;   // 미니맵 축소

    // 이동 입력 (-1, 0, 1). x: 좌우, y: 앞뒤
    public static Vector2 MoveAxis => IsPlaying ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector2.zero;

    // 이번 프레임에 마우스가 움직인 양. x: 좌우, y: 위아래 (프레임 시간을 곱하지 않는다.)
    public static Vector2 LookDelta => IsPlaying ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) : Vector2.zero;

    public static bool JumpPressed => IsPlaying && Input.GetButtonDown("Jump");
    public static bool SprintHeld => IsPlaying && Input.GetButton("Sprint");
    public static bool ReloadPressed => IsPlaying && Input.GetKeyDown(KeyCode.R);

    public static bool FireHeld => CanUseWeapon && Input.GetMouseButton(FireMouseButton);
    public static bool BombPressed => CanUseWeapon && Input.GetMouseButtonDown(BombMouseButton);
    public static bool BombHeld => CanUseWeapon && Input.GetMouseButton(BombMouseButton);
    public static bool BombReleased => CanUseWeapon && Input.GetMouseButtonUp(BombMouseButton);

    public static bool ZoomHeld => CanUseWeapon && Input.GetMouseButton(ZoomMouseButton);

    public static bool NormalModePressed => IsPlaying && Input.GetKeyDown(NormalModeKey);
    public static bool SniperModePressed => IsPlaying && Input.GetKeyDown(SniperModeKey);

    public static bool CursorUnlockPressed => IsPlaying && Input.GetKeyDown(CursorUnlockKey);
    public static bool CursorUnlockReleased => IsPlaying && Input.GetKeyUp(CursorUnlockKey);

    public static bool ViewTogglePressed => IsPlaying && Input.GetKeyDown(ViewToggleKey);

    public static bool MinimapZoomInPressed => IsPlaying && Input.GetKeyDown(MinimapZoomInKey);
    public static bool MinimapZoomOutPressed => IsPlaying && Input.GetKeyDown(MinimapZoomOutKey);

    private static bool IsPlaying => GameManager.Instance != null && GameManager.Instance.IsPlaying;

    // 무기(발사, 폭탄, 줌)는 게임 중이고 커서가 잠겨 있을 때만 쓸 수 있다.
    private static bool CanUseWeapon => IsPlaying && !Cursor.visible;
}
