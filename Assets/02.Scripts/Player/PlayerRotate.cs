using UnityEngine;

// 마우스를 좌우로 움직이면 플레이어를 그 방향으로 회전한다. 카메라(CameraRotate)가 이 회전값을 따라간다.
// 마우스 입력은 이미 프레임마다 움직인 양이라 프레임 시간을 곱하지 않는다. (곱하면 프레임레이트에 따라 감도가 달라진다.)
[DefaultExecutionOrder(-100)]   // 카메라와 총기보다 먼저 회전해야 같은 프레임의 조준이 어긋나지 않는다.
public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 3.3f;     // 마우스 한 칸당 회전 각도

    private float _yaw = 0f; // 좌우 회전 각도

    public float Yaw => _yaw;

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        _yaw += GameInput.LookDelta.x * _sensitivity;

        transform.eulerAngles = new Vector3(0f, _yaw, 0f);
    }
}
