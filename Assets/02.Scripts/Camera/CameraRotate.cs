using UnityEngine;

// 역할: 마우스를 위아래로 움직이면 카메라를 그 방향으로 회전하고 싶다.
// 좌우 회전은 따로 누적하지 않고 플레이어(PlayerRotate)의 회전값을 그대로 따라간다.
// 마우스 입력은 이미 프레임마다 움직인 양이라 프레임 시간을 곱하지 않는다.
[DefaultExecutionOrder(-90)]    // PlayerRotate 다음, 총기보다는 먼저 회전한다.
public class CameraRotate : MonoBehaviour
{
    private const float MaxPitch = 90f;     // 위아래로 볼 수 있는 최대 각도

    [SerializeField] private float _sensitivity = 3.3f;     // 마우스 한 칸당 회전 각도

    private PlayerRotate _playerRotate;
    private float _pitch = 0f; // 위아래 회전 각도

    private void Awake()
    {
        _playerRotate = FindAnyObjectByType<PlayerRotate>();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        // 1. 마우스 입력만큼 위아래 회전값을 누적하고 -90 ~ 90도로 제한한다.
        _pitch += -GameInput.LookDelta.y * _sensitivity;
        _pitch = Mathf.Clamp(_pitch, -MaxPitch, MaxPitch);

        // 2. 좌우는 플레이어가 바라보는 방향을 따른다.
        float yaw = _playerRotate != null ? _playerRotate.Yaw : 0f;

        // 3. 누적한 회전값을 카메라에 적용한다.
        transform.eulerAngles = new Vector3(_pitch, yaw, 0f);
    }
}
