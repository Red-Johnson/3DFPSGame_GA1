using UnityEngine;
using UnityEngine.Serialization;

// 지정사수소총 줌: 지정사수소총 모드에서 줌 버튼을 누르는 동안 FOV를 줄이고, 조준점 대신 스코프 화면을 보여 준다.
// 줌 중에는 시점 감도를 시야각에 맞춰 낮춰서(LookSensitivityScale) 줌하지 않을 때와 조작감이 비슷하게 한다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerMarksmanZoom : MonoBehaviour
{
    [FormerlySerializedAs("_sniperZoom")]
    [SerializeField] private GameObject _scopeOverlay;
    [SerializeField] private float _normalFov = 60f;
    [SerializeField] private float _zoomFov = 15f;

    private PlayerWeaponMode _weaponMode;
    private Camera _camera;
    private CameraViewManager _cameraView;      // 조준점 표시는 이 컴포넌트가 정한다. 없으면 조준점을 건드리지 않는다.
    private bool _isZooming;

    public bool IsZooming => _isZooming;

    // 시점 감도에 곱할 배율. 줌 중에는 화면에 보이는 크기 비율(tan(FOV/2) 비)만큼 줄여서, 마우스를 같은 만큼 움직이면 화면에서 같은 만큼 움직이게 한다.
    public float LookSensitivityScale => _isZooming
        ? Mathf.Tan(_zoomFov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(_normalFov * 0.5f * Mathf.Deg2Rad)
        : 1f;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _camera = Camera.main;
        _cameraView = _camera.GetComponent<CameraViewManager>();
    }

    private void Start()
    {
        // 처음 상태(기본 FOV, 조준점 켜짐, 스코프 꺼짐)를 한 번 맞춘다.
        ApplyZoom(false);
    }

    private void Update()
    {
        // 게임이 진행 중이 아니거나 커서가 풀려 있으면 GameInput이 입력 없음으로 돌려준다.
        WeaponProfile profile = _weaponMode.CurrentProfile;
        bool isZooming = profile != null && profile.CanZoom && GameInput.ZoomHeld;

        // 줌 상태가 바뀔 때만 화면을 갱신한다.
        if (isZooming == _isZooming) return;

        ApplyZoom(isZooming);
    }

    private void ApplyZoom(bool isZooming)
    {
        _isZooming = isZooming;

        _camera.fieldOfView = isZooming ? _zoomFov : _normalFov;
        // 조준점은 시점(1인칭, 3인칭)도 함께 따져야 하므로 CameraViewManager가 켜고 끈다.
        if (_cameraView != null)
        {
            _cameraView.SetCrosshairHiddenByZoom(isZooming);
        }

        _scopeOverlay.SetActive(isZooming);
    }
}
