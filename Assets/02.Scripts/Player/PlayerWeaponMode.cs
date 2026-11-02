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
        else if (GameInput.WeaponScrollStep != 0)
        {
            ChangeModeByScroll(GameInput.WeaponScrollStep);
        }
    }

    // 휠을 굴린 방향으로 다음, 이전 모드로 넘어간다. 끝에서는 반대쪽 끝으로 이어진다.
    private void ChangeModeByScroll(int step)
    {
        int count = System.Enum.GetValues(typeof(WeaponMode)).Length;
        int next = (((int)_mode + step) % count + count) % count;
        ChangeMode((WeaponMode)next);
    }

    private void ChangeMode(WeaponMode mode)
    {
        // 이미 그 모드면 아무것도 하지 않는다. (무기를 다시 꺼내는 연출이 되풀이되지 않게)
        if (mode == _mode) return;

        _mode = mode;
        OnModeChanged?.Invoke(mode);
        GameLogger.Log($"무기 모드: {mode}");
    }
}
