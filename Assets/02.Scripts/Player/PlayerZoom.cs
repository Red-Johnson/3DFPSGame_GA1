using UnityEngine;

[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalFov = 60f;
    [SerializeField] private float _zoomFov = 15f;

    private PlayerWeaponMode _weaponMode;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
    }


    private void Update()
    {
        // 게임이 진행 중이 아니거나 커서가 풀려 있으면 GameInput이 입력 없음으로 돌려준다.
        bool isZooming = _weaponMode.Mode == WeaponMode.Sniper && GameInput.ZoomHeld;
        Camera.main.fieldOfView = isZooming ? _zoomFov : _normalFov;
        _crosshair.SetActive(!isZooming);
        _sniperZoom.SetActive(isZooming);
    }
}
