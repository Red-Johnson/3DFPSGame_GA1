using UnityEngine;

// 무기 모드: 1·2·3키로 모드(돌격소총, 지정사수소총, 근접)를 바꾸고, 바뀌면 구독자(UI 등)에게 알린다.
// 모드 이름과 아이콘 표시는 UI_WeaponStatus가 맡는다. (이 클래스는 UI를 모른다.)
public class PlayerWeaponMode : MonoBehaviour
{
    private WeaponMode _mode = WeaponMode.AssaultRifle;
    public WeaponMode Mode => _mode;

    public event System.Action<WeaponMode> OnModeChanged;

    private void Update()
    {
        // 게임이 진행 중이 아니면 GameInput이 입력 없음으로 돌려준다.
        if (GameInput.AssaultRifleModePressed)
        {
            ChangeMode(WeaponMode.AssaultRifle);
        }
        else if (GameInput.MarksmanRifleModePressed)
        {
            ChangeMode(WeaponMode.MarksmanRifle);
        }
        else if (GameInput.MeleeModePressed)
        {
            ChangeMode(WeaponMode.Melee);
        }
    }

    private void ChangeMode(WeaponMode mode)
    {
        _mode = mode;
        OnModeChanged?.Invoke(mode);
        GameLogger.Log($"무기 모드: {mode}");
    }
}
