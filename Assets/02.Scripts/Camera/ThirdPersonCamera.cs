using UnityEngine;

// 3인칭 시점: 플레이어 등 뒤에서 조금 떨어져 플레이어를 감싸고 도는 카메라 (다크 소울 같은 자유 시점)
// - 마우스로 카메라가 플레이어 주위를 돈다. 플레이어의 방향은 따로 PlayerRotate가 정한다.
// - 벽이나 지형에 가리면 카메라가 플레이어 쪽으로 당겨진다.
// CameraViewManager가 시점을 바꿀 때 켜고 끈다. (끄면 이 컴포넌트는 아무것도 하지 않는다.)
public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;                                 // 따라다닐 플레이어
    [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 4f, 0f);  // 카메라가 도는 중심(플레이어 위치 기준, 어깨 높이)
    [SerializeField] private float _shoulderOffset = 0.8f;                      // 카메라가 도는 방향 기준 오른쪽(+)/왼쪽(-)으로 비켜 서는 거리(숄더뷰)
    [SerializeField] private float _distance = 3f;                           // 중심에서 카메라까지의 거리
    [SerializeField] private float _sensitivity = 3.3f;                         // 마우스 한 칸당 회전 각도
    [SerializeField] private float _minPitch = -20f;                            // 아래에서 올려다보는 한계
    [SerializeField] private float _maxPitch = 60f;                             // 위에서 내려다보는 한계
    [SerializeField] private float _followSmoothTime = 0.06f;                   // 중심이 플레이어를 따라가는 부드러움
    [SerializeField] private float _collisionRadius = 0.25f;
    [SerializeField] private float _collisionPadding = 0.15f;                   // 가린 물체 앞에서 띄울 거리
    [SerializeField] private LayerMask _collisionLayers = ~0;                   // 카메라를 가로막는 레이어(플레이어, 좀비 등은 제외)

    private PlayerRotate _playerRotate;     // 줌 중 시점 감도 배율을 읽는다.
    private float _yaw;
    private float _pitch = 15f;
    private Vector3 _pivot;
    private Vector3 _pivotVelocity;

    public float Yaw => _yaw;

    private void Awake()
    {
        if (_target == null)
        {
            GameLogger.LogWarning("[ThirdPersonCamera] 따라다닐 대상(_target)이 연결되지 않았습니다.");
        }
        else
        {
            _playerRotate = _target.GetComponent<PlayerRotate>();
        }
    }

    // 시점을 바꾼 순간 지금 카메라가 보던 방향에서 이어서 시작한다.
    public void AlignTo(float yaw)
    {
        _yaw = yaw;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        if (_target != null)
        {
            _pivot = _target.position + _pivotOffset;
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        // 마우스 입력은 이미 프레임마다 움직인 양이라 프레임 시간을 곱하지 않는다.
        // 줌 중에는 감도를 낮춰서 줌하지 않을 때와 조작감이 비슷하게 한다.
        float sensitivity = _sensitivity * (_playerRotate != null ? _playerRotate.LookSensitivityScale : 1f);
        Vector2 look = GameInput.LookDelta;
        _yaw += look.x * sensitivity;
        _pitch = Mathf.Clamp(_pitch - (look.y * sensitivity), _minPitch, _maxPitch);
    }

    // 플레이어가 움직인 뒤에 카메라를 옮겨야 떨리지 않는다.
    private void LateUpdate()
    {
        if (_target == null) return;

        _pivot = Vector3.SmoothDamp(_pivot, _target.position + _pivotOffset, ref _pivotVelocity, _followSmoothTime);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 backward = rotation * Vector3.back;

        // 숄더뷰: 월드 좌표가 아니라 카메라 좌우 방향(yaw)을 기준으로 옆으로 비켜 서야 플레이어가 돌아도 상대 위치가 변하지 않는다.
        Vector3 shoulderPivot = _pivot + (Quaternion.Euler(0f, _yaw, 0f) * Vector3.right * _shoulderOffset);

        // 중심에서 카메라 자리까지 가로막는 것이 있으면 그 앞까지만 물러난다.
        float distance = _distance;
        if (Physics.SphereCast(shoulderPivot, _collisionRadius, backward, out RaycastHit hit, _distance, _collisionLayers, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0.3f, hit.distance - _collisionPadding);
        }

        transform.SetPositionAndRotation(shoulderPivot + (backward * distance), rotation);
    }
}
