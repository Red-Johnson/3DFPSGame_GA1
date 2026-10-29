using UnityEngine;

// 1인칭: 마우스를 좌우로 움직이면 플레이어를 그 방향으로 회전한다. 카메라(CameraRotate)가 이 회전값을 따라간다.
// 3인칭: 마우스는 카메라만 돌린다. 플레이어는 카메라 기준으로 걷는 방향을 향해 돌고, 공격 중에는 카메라가 보는 방향을 향한다.
// 마우스 입력은 이미 프레임마다 움직인 양이라 프레임 시간을 곱하지 않는다. (곱하면 프레임레이트에 따라 감도가 달라진다.)
[DefaultExecutionOrder(-100)]   // 카메라와 총기보다 먼저 회전해야 같은 프레임의 조준이 어긋나지 않는다.
public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 3.3f;     // 마우스 한 칸당 회전 각도
    [SerializeField] private float _turnSpeed = 720f;       // 3인칭에서 가려는 방향으로 도는 속도(도/초)
    [SerializeField] private CameraViewManager _cameraView; // 비워두면 항상 1인칭 방식으로 돈다.

    private float _yaw = 0f; // 좌우 회전 각도

    public float Yaw => _yaw;

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        if (_cameraView != null && _cameraView.IsThirdPerson)
        {
            TurnTowardTarget(_cameraView.ThirdPersonYaw);
        }
        else
        {
            _yaw += GameInput.LookDelta.x * _sensitivity;
        }

        transform.eulerAngles = new Vector3(0f, _yaw, 0f);
    }

    // 3인칭: 공격 중이면 카메라 방향, 아니면 입력한 이동 방향(카메라 기준)으로 몸을 돌린다. 입력이 없으면 그대로 둔다.
    private void TurnTowardTarget(float cameraYaw)
    {
        Vector2 move = GameInput.MoveAxis;
        float targetYaw;

        if (GameInput.FireHeld)
        {
            targetYaw = cameraYaw;
        }
        else if (move.sqrMagnitude > 0.01f)
        {
            targetYaw = cameraYaw + (Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg);
        }
        else
        {
            return;
        }

        _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, _turnSpeed * Time.deltaTime);
    }
}
