using UnityEngine;

public enum CameraViewMode
{
    FirstPerson,
    ThirdPerson,
}

// 시점 전환: 무기 모드가 1인칭(CameraFollow, CameraRotate)과 3인칭(ThirdPersonCamera)을 정한다. (총 모드는 1인칭, 근접 모드는 3인칭)
// 메인 카메라에 붙이고, 같은 오브젝트의 시점 컴포넌트는 Awake에서 찾는다.
// 플레이어의 회전과 이동은 이 값을 보고 1인칭(마우스로 몸을 돌림)과 3인칭(카메라 기준으로 걷고 이동 방향을 봄)을 나눈다.
[DefaultExecutionOrder(-95)]
public class CameraViewManager : MonoBehaviour
{
    [SerializeField] private CameraViewMode _startMode = CameraViewMode.FirstPerson;
    [SerializeField] private GameObject _crosshair;     // 3인칭(근접 모드)에서는 숨길 조준점. 비워두면 건드리지 않는다.

    private CameraFollow _firstPersonFollow;
    private CameraRotate _firstPersonRotate;
    private ThirdPersonCamera _thirdPerson;

    public CameraViewMode Mode { get; private set; }
    public bool IsThirdPerson => Mode == CameraViewMode.ThirdPerson;

    // 3인칭 카메라가 보는 좌우 방향. 플레이어가 카메라 기준으로 걸을 때 쓴다.
    public float ThirdPersonYaw => _thirdPerson != null ? _thirdPerson.Yaw : 0f;

    [SerializeField] private float _transitionDuration = 0.3f;     // 시점이 바뀔 때 카메라가 새 위치로 이동하는 시간(초). 0이면 바로 바뀐다.

    private CameraViewTransition _transition;
    private bool _isCrosshairHiddenByZoom;      // 줌 중이라 조준점을 숨기고 있는지. (줌 컴포넌트가 알려 준다.)

    // 줌 중에는 조준점 대신 스코프가 보이므로 조준점을 숨긴다. 조준점을 켜고 끄는 일은 이 클래스가 한곳에서 정한다.
    public void SetCrosshairHiddenByZoom(bool isHidden)
    {
        _isCrosshairHiddenByZoom = isHidden;
        ApplyCrosshair();
    }

    // 시점을 정한다. (총 모드는 1인칭, 근접 모드는 3인칭. 플레이어가 직접 바꾸는 키는 없다.)
    public void ForceView(CameraViewMode mode)
    {
        // 이미 그 시점이면 다시 전환하지 않는다.
        CameraViewMode target = mode == CameraViewMode.ThirdPerson && _thirdPerson != null ? CameraViewMode.ThirdPerson : CameraViewMode.FirstPerson;
        if (target == Mode) return;

        Apply(target, true);
    }

    // 조준점은 1인칭이고 줌 중이 아닐 때만 보인다. (3인칭에서는 숨기고, 줌 중에는 스코프가 대신한다.)
    private void ApplyCrosshair()
    {
        if (_crosshair != null)
        {
            _crosshair.SetActive(!IsThirdPerson && !_isCrosshairHiddenByZoom);
        }
    }

    private void Awake()
    {
        _firstPersonFollow = GetComponent<CameraFollow>();
        _firstPersonRotate = GetComponent<CameraRotate>();
        _thirdPerson = GetComponent<ThirdPersonCamera>();

        if (_thirdPerson == null)
        {
            GameLogger.LogWarning("[CameraViewManager] ThirdPersonCamera가 없어 3인칭으로 바꿀 수 없습니다.");
        }

        _transition = GetComponent<CameraViewTransition>();
        if (_transition == null)
        {
            _transition = gameObject.AddComponent<CameraViewTransition>();
        }

        Apply(_startMode, false);
    }

    private void Apply(CameraViewMode mode, bool useTransition)
    {
        // 바꾸기 전 카메라 위치와 회전을 기억해 두었다가 새 시점으로 부드럽게 옮겨 간다.
        Vector3 fromPosition = transform.position;
        Quaternion fromRotation = transform.rotation;

        bool toThird = mode == CameraViewMode.ThirdPerson && _thirdPerson != null;
        Mode = toThird ? CameraViewMode.ThirdPerson : CameraViewMode.FirstPerson;

        if (toThird)
        {
            // 지금 카메라가 보던 좌우 방향에서 이어서 돈다.
            _thirdPerson.AlignTo(transform.eulerAngles.y);
        }

        if (_firstPersonFollow != null) _firstPersonFollow.enabled = !toThird;
        if (_firstPersonRotate != null) _firstPersonRotate.enabled = !toThird;
        if (_thirdPerson != null) _thirdPerson.enabled = toThird;
        ApplyCrosshair();

        if (useTransition && _transition != null)
        {
            _transition.Begin(fromPosition, fromRotation, _transitionDuration);
        }
    }
}
