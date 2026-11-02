using System.Collections;
using UnityEngine;

// 무기 모드에 맞춰 무기 컴포넌트를 켜고 끈다.
// - 일반, 저격: 총(PlayerGunFire)을 쓰고, 총 모델을 보여 주며, 근접 무기(칼)는 숨긴다.
// - 근접: 칼 베기(PlayerMeleeAttack)를 쓰고, 칼을 보여 주며, 총과 총 모델은 끈다.
// 총은 모드에 상관없이 코드를 지우지 않고 컴포넌트만 켜고 끈다. 시점은 모드가 정한다. (총 모드는 1인칭, 근접 모드는 3인칭)
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerWeaponSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _meleeWeaponModel;      // 근접 모드에서만 보일 칼
    [SerializeField] private GameObject _gunWeaponModel;        // 일반, 저격 모드에서만 보일 총(총처럼 든 가로등). 비워두면 건드리지 않는다.
    [SerializeField] private CameraViewManager _cameraView;     // 3인칭에서 총 모드일 때 조준점을 보이게 한다. 비워두면 건드리지 않는다.

    [Header("총 꺼내기 연출")]
    [SerializeField] private float _drawDuration = 0.25f;       // 총이 올라오는 데 걸리는 시간(초)
    [SerializeField] private float _drawDistance = 0.6f;        // 시작할 때 원래 위치보다 얼마나 아래(월드 기준, m)에서 올라올지

    private Vector3 _gunRestLocalPosition;      // 총이 원래 있어야 할 손 기준 위치
    private Coroutine _drawCoroutine;
    private bool _isFirstApply = true;
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

        if (_gunWeaponModel != null)
        {
            _gunRestLocalPosition = _gunWeaponModel.transform.localPosition;
        }
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

        // 총을 들게 되면(처음 시작할 때는 제외) 아래에서 올라오는 연출을 한다.
        if (!isMelee && !_isFirstApply)
        {
            StartDraw();
        }
        _isFirstApply = false;

        // 근접 자세를 맞춘다.
        _playerAnimator.SetMeleeStance(isMelee);

        if (_cameraView != null)
        {
            // 근접 모드는 3인칭, 총 모드는 1인칭으로 바꾼다.
            _cameraView.ForceView(isMelee ? CameraViewMode.ThirdPerson : CameraViewMode.FirstPerson);
            _cameraView.SetThirdPersonCrosshair(!isMelee);
        }
    }

    private void StartDraw()
    {
        if (_gunWeaponModel == null) return;

        if (_drawCoroutine != null)
        {
            StopCoroutine(_drawCoroutine);
        }

        _drawCoroutine = StartCoroutine(Draw_Coroutine());
    }

    // 총을 손 기준 원래 위치보다 아래에서 시작해 원래 위치로 스윽 올린다. (손 뼈가 움직여도 월드 기준으로 아래에서 올라온다.)
    private IEnumerator Draw_Coroutine()
    {
        Transform gun = _gunWeaponModel.transform;
        float elapsed = 0f;

        while (elapsed < _drawDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _drawDuration);
            float remain = (1f - t) * (1f - t);     // 처음엔 빠르게, 끝에서 부드럽게 멈춘다.

            Vector3 offset = gun.parent.InverseTransformVector(Vector3.down * (_drawDistance * remain));
            gun.localPosition = _gunRestLocalPosition + offset;

            // 손 뼈는 애니메이터가 매 프레임 움직이므로, 그 뒤에 적용되도록 다음 프레임까지 기다린다.
            yield return null;
        }

        gun.localPosition = _gunRestLocalPosition;
        _drawCoroutine = null;
    }
}
