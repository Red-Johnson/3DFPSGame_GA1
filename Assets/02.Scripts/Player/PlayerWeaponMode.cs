using UnityEngine;
using TMPro;
public class PlayerWeaponMode : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _modeText;

    private WeaponMode _mode = WeaponMode.Normal;
    public WeaponMode Mode => _mode;

    private void Start()
    {
        ShowMode();
    }

    private void Update()
    {
        // 게임이 진행 중이 아니면 GameInput이 입력 없음으로 돌려준다.
        if (GameInput.NormalModePressed)
        {
            ChangeMode(WeaponMode.Normal);
        }
        else if (GameInput.SniperModePressed)
        {
            ChangeMode(WeaponMode.Sniper);
        }
    }

    private void ChangeMode(WeaponMode mode)
    {
        _mode = mode;
        ShowMode();
        Debug.Log($"무기 모드: {mode}");
    }
    
    private void ShowMode()
    {
        switch (_mode)
        {
            case WeaponMode.Normal:
                _modeText.text = "일반 모드";
                break;
            case WeaponMode.Sniper:
                _modeText.text = "저격 모드";
                break;
                
        }
    }
}
