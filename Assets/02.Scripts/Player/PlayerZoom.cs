using UnityEngine;

// 저격 줌: 저격 모드에서 줌 버튼을 누르는 동안 FOV를 줄이고, 조준점 대신 스코프 화면을 보여 준다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalFov = 60f;
    [SerializeField] private float _zoomFov = 15f;

    private PlayerWeaponMode _weaponMode;
    private Camera _camera;
    private bool _isZooming;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _camera = Camera.main;
    }

    private void Start()
    {
        // 처음 상태(기본 FOV, 조준점 켜짐, 스코프 꺼짐)를 한 번 맞춘다.
        ApplyZoom(false);
    }

    private void Update()
    {
        // 게임이 진행 중이 아니거나 커서가 풀려 있으면 GameInput이 입력 없음으로 돌려준다.
        bool isZooming = _weaponMode.Mode == WeaponMode.Sniper && GameInput.ZoomHeld;

        // 줌 상태가 바뀔 때만 화면을 갱신한다.
        if (isZooming == _isZooming) return;

        ApplyZoom(isZooming);
    }

    private void ApplyZoom(bool isZooming)
    {
        _isZooming = isZooming;

        _camera.fieldOfView = isZooming ? _zoomFov : _normalFov;
        _crosshair.SetActive(!isZooming);
        _sniperZoom.SetActive(isZooming);
    }
}
