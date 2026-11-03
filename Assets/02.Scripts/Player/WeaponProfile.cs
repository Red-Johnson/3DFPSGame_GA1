using UnityEngine;

// 무기의 종류. 총은 PlayerGunFire가, 근접은 PlayerMeleeAttack이 맡는다.
public enum WeaponKind
{
    Gun,
    Melee
}

// 무기 모드 하나의 성질을 한곳에 모은 데이터(ScriptableObject 에셋). 무기마다 에셋을 하나씩 만든다.
// 각 컴포넌트는 모드를 직접 비교하지 않고 PlayerWeaponMode.CurrentProfile의 값만 읽는다.
// (예: 폭탄을 던질 수 있는지, 줌이 되는지, 3인칭인지, 가로등 색, UI에 보일 이름과 아이콘)
// 새 무기는 WeaponMode 값을 하나 추가하고, 이 에셋을 만들어 PlayerWeaponMode의 목록에 넣고, 모드를 고르는 키를 정하면 된다.
[CreateAssetMenu(fileName = "Weapon_", menuName = "Game/Weapon Profile")]
public class WeaponProfile : ScriptableObject
{
    [Header("기본")]
    [SerializeField] private WeaponMode _mode;
    [SerializeField] private WeaponKind _kind = WeaponKind.Gun;
    [SerializeField] private string _displayName = "";          // UI에 보일 모드 이름
    [SerializeField] private Sprite _icon;                      // UI에 보일 아이콘. 비워두면 아이콘을 숨긴다.

    [Header("규칙")]
    [SerializeField] private bool _canThrowBomb = true;         // 이 모드에서 폭탄을 던질 수 있는지
    [SerializeField] private bool _canZoom = false;             // 마우스 오른쪽을 누르는 동안 줌이 되는지
    [SerializeField] private bool _usesThirdPerson = false;     // true면 3인칭, false면 1인칭으로 본다.

    [Header("연출")]
    [SerializeField] private Color _lampColor = Color.white;    // 손에 든 가로등, 총구 섬광, 총알 궤적 색

    [Header("총 성능 (종류가 총일 때만 쓴다)")]
    [SerializeField] private GunSpec _gunSpec;

    public WeaponMode Mode => _mode;
    public WeaponKind Kind => _kind;
    public bool IsGun => _kind == WeaponKind.Gun;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public bool CanThrowBomb => _canThrowBomb;
    public bool CanZoom => _canZoom;
    public bool UsesThirdPerson => _usesThirdPerson;
    public Color LampColor => _lampColor;
    public GunSpec GunSpec => _gunSpec;
}
