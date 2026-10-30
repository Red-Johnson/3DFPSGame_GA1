using UnityEngine;

// 1인칭: 마우스를 좌우로 움직이면 플레이어를 그 방향으로 회전한다. 카메라(CameraRotate)가 이 회전값을 따라간다.
// 3인칭: 마우스는 카메라만 돌린다. 플레이어의 몸은 이동 방향과 상관없이 항상 카메라가 보는 방향을 향한다.
// 마우스 입력은 이미 프레임마다 움직인 양이라 프레임 시간을 곱하지 않는다. (곱하면 프레임레이트에 따라 감도가 달라진다.)
[DefaultExecutionOrder(-100)]   // 카메라와 총기보다 먼저 회전해야 같은 프레임의 조준이 어긋나지 않는다.
public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 3.3f;     // 마우스 한 칸당 회전 각도
    [SerializeField] private float _turnSpeed = 1080f;      // 3인칭에서 몸이 카메라 방향으로 따라 도는 속도(도/초). 클수록 즉시 따라간다.
    [SerializeField] private CameraViewManager _cameraView; // 비워두면 항상 1인칭 방식으로 돈다.

    private float _yaw = 0f; // 좌우 회전 각도
    private PlayerMarksmanZoom _zoom;

    public float Yaw => _yaw;

    // 시점 감도에 곱할 배율. 줌 중에는 줄어든다. 카메라 회전(CameraRotate, ThirdPersonCamera)도 같은 배율을 쓴다.
    public float LookSensitivityScale => _zoom != null ? _zoom.LookSensitivityScale : 1f;

    private void Awake()
    {
        _zoom = GetComponent<PlayerMarksmanZoom>();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        if (_cameraView != null && _cameraView.IsThirdPerson)
        {
            TurnTowardTarget(_cameraView.ThirdPersonYaw);
        }
        else
        {
            _yaw += GameInput.LookDelta.x * _sensitivity * LookSensitivityScale;
        }

        transform.eulerAngles = new Vector3(0f, _yaw, 0f);
    }

    // 3인칭: 이동 방향과 상관없이 몸이 항상 카메라가 보는 방향을 유지한다. (걸을 때는 옆걸음, 뒷걸음이 된다.)
    private void TurnTowardTarget(float cameraYaw)
    {
        _yaw = Mathf.MoveTowardsAngle(_yaw, cameraYaw, _turnSpeed * Time.deltaTime);
    }
}
