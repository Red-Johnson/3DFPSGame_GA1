using UnityEngine;

// 플레이어 입력을 한곳에서 읽는다. 컴포넌트가 아닌 정적 클래스라 씬에 붙일 것이 없다.
// - 게임이 진행 중이 아니면 모두 입력 없음으로 돌려준다.
// - 발사와 폭탄은 커서가 풀려 있는 동안(UI를 조작하는 중) 입력 없음으로 돌려준다.
public static class GameInput
{
    // 폭탄을 던지는 마우스 버튼 (0: 왼쪽, 1: 오른쪽). Input Manager의 Fire2는 left alt도 포함하므로 쓰지 않는다.
    private const int BombMouseButton = 1;

    // 이동 입력 (-1, 0, 1). x: 좌우, y: 앞뒤
    public static Vector2 MoveAxis => IsPlaying ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector2.zero;

    // 이번 프레임에 마우스가 움직인 양. x: 좌우, y: 위아래 (프레임 시간을 곱하지 않는다.)
    public static Vector2 LookDelta => IsPlaying ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) : Vector2.zero;

    public static bool JumpPressed => IsPlaying && Input.GetButtonDown("Jump");
    public static bool SprintHeld => IsPlaying && Input.GetButton("Sprint");
    public static bool ReloadPressed => IsPlaying && Input.GetKeyDown(KeyCode.R);

    public static bool FireHeld => CanShoot && Input.GetButton("Fire1");
    public static bool BombPressed => CanShoot && Input.GetMouseButtonDown(BombMouseButton);
    public static bool BombHeld => CanShoot && Input.GetMouseButton(BombMouseButton);
    public static bool BombReleased => CanShoot && Input.GetMouseButtonUp(BombMouseButton);

    private static bool IsPlaying => GameManager.Instance != null && GameManager.Instance.IsPlaying;

    private static bool CanShoot => IsPlaying && !Cursor.visible;
}
