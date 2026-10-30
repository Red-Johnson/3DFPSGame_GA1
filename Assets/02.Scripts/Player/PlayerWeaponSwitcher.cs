using UnityEngine;

// 무기 모드에 맞춰 무기 컴포넌트를 켜고 끈다.
// - 일반, 저격: 총(PlayerGunFire)을 쓰고, 총 모델을 보여 주며, 근접 무기(칼)는 숨긴다.
// - 근접: 칼 베기(PlayerMeleeAttack)를 쓰고, 칼을 보여 주며, 총과 총 모델은 끈다.
// 총은 모드에 상관없이 코드를 지우지 않고 컴포넌트만 켜고 끈다. 시점 전환(V)은 모드와 상관없이 항상 된다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerWeaponSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _meleeWeaponModel;      // 근접 모드에서만 보일 칼
    [SerializeField] private GameObject _gunWeaponModel;        // 일반, 저격 모드에서만 보일 총(총처럼 든 가로등). 비워두면 건드리지 않는다.
    [SerializeField] private CameraViewManager _cameraView;     // 3인칭에서 총 모드일 때 조준점을 보이게 한다. 비워두면 건드리지 않는다.

    private PlayerWeaponMode _weaponMode;
    private PlayerGunFire _gunFire;
    private PlayerMeleeAttack _meleeAttack;
    private PlayerAnimator _playerAnimator;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _gunFire = GetComponent<PlayerGunFire>();
        _meleeAttack = GetComponent<PlayerMeleeAttack>();
        _playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void OnEnable()
    {
        _weaponMode.OnModeChanged += Apply;
    }

    private void OnDisable()
    {
        _weaponMode.OnModeChanged -= Apply;
    }

    private void Start()
    {
        // 모든 컴포넌트의 Awake가 끝난 뒤 처음 모드를 한 번 맞춘다.
        Apply(_weaponMode.Mode);
    }

    private void Apply(WeaponMode mode)
    {
        bool isMelee = mode == WeaponMode.Melee;

        if (_gunFire != null) _gunFire.enabled = !isMelee;
        if (_meleeAttack != null) _meleeAttack.enabled = isMelee;
        if (_meleeWeaponModel != null) _meleeWeaponModel.SetActive(isMelee);
        if (_gunWeaponModel != null) _gunWeaponModel.SetActive(!isMelee);

        // 근접 자세를 맞춘다.
        _playerAnimator.SetMeleeStance(isMelee);

        if (_cameraView != null)
        {
            _cameraView.SetThirdPersonCrosshair(!isMelee);
        }
    }
}
