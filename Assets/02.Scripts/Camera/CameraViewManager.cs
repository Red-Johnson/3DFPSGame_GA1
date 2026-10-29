using System;
using UnityEngine;

public enum CameraViewMode
{
    FirstPerson,
    ThirdPerson,
}

// 시점 전환: V키로 1인칭(CameraFollow, CameraRotate)과 3인칭(ThirdPersonCamera)을 바꾼다.
// 메인 카메라에 붙이고, 같은 오브젝트의 시점 컴포넌트는 Awake에서 찾는다.
// 플레이어의 회전과 이동은 이 값을 보고 1인칭(마우스로 몸을 돌림)과 3인칭(카메라 기준으로 걷고 이동 방향을 봄)을 나눈다.
[DefaultExecutionOrder(-95)]
public class CameraViewManager : MonoBehaviour
{
    [SerializeField] private CameraViewMode _startMode = CameraViewMode.FirstPerson;
    [SerializeField] private GameObject _crosshair;     // 3인칭에서는 숨길 조준점. 비워두면 건드리지 않는다.

    private CameraFollow _firstPersonFollow;
    private CameraRotate _firstPersonRotate;
    private ThirdPersonCamera _thirdPerson;

    public CameraViewMode Mode { get; private set; }
    public bool IsThirdPerson => Mode == CameraViewMode.ThirdPerson;

    // 3인칭 카메라가 보는 좌우 방향. 플레이어가 카메라 기준으로 걸을 때 쓴다.
    public float ThirdPersonYaw => _thirdPerson != null ? _thirdPerson.Yaw : 0f;

    public event Action<CameraViewMode> OnViewChanged;

    private void Awake()
    {
        _firstPersonFollow = GetComponent<CameraFollow>();
        _firstPersonRotate = GetComponent<CameraRotate>();
        _thirdPerson = GetComponent<ThirdPersonCamera>();

        if (_thirdPerson == null)
        {
            GameLogger.LogWarning("[CameraViewManager] ThirdPersonCamera가 없어 3인칭으로 바꿀 수 없습니다.");
        }

        Apply(_startMode);
    }

    private void Update()
    {
        if (_thirdPerson == null || !GameInput.ViewTogglePressed) return;

        Apply(IsThirdPerson ? CameraViewMode.FirstPerson : CameraViewMode.ThirdPerson);
    }

    private void Apply(CameraViewMode mode)
    {
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
        if (_crosshair != null) _crosshair.SetActive(!toThird);

        OnViewChanged?.Invoke(Mode);
    }
}
