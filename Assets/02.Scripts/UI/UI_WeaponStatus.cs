using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 무기 상태 UI: 현재 무기 모드(이름, 아이콘), 총알 수, 폭탄 수를 표시한다.
public class UI_WeaponStatus : MonoBehaviour
{
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Sprite[] _modeIcons;
    [SerializeField] private TextMeshProUGUI _modeText;
    [SerializeField] private TextMeshProUGUI _bulletText;
    [SerializeField] private TextMeshProUGUI _bombText;

    [SerializeField] private PlayerWeaponMode _weaponMode;
    [SerializeField] private PlayerGunFire _gunFire;
    [SerializeField] private PlayerBombFire _bombFire;

    private void OnEnable()
    {
        _weaponMode.OnModeChanged += RefreshMode;
        _gunFire.OnBulletChanged += RefreshBullet;
        _bombFire.OnBombChanged += RefreshBomb;
    }

    private void OnDisable()
    {
        _weaponMode.OnModeChanged -= RefreshMode;
        _gunFire.OnBulletChanged -= RefreshBullet;
        _bombFire.OnBombChanged -= RefreshBomb;
    }

    private void Start()
    {
        // 모든 컴포넌트의 Awake가 끝난 뒤 한 번 전부 갱신해서, 처음부터 정확한 값을 보여 준다.
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshMode(_weaponMode.Mode);
        RefreshBullet(_gunFire.BulletCount, _gunFire.MaxBulletCount);
        RefreshBomb(_bombFire.BombCount);
    }

    private void RefreshMode(WeaponMode mode)
    {
        _weaponIcon.sprite = _modeIcons[(int)mode];

        // 텍스트 오브젝트를 아직 연결하지 않았어도 나머지 UI는 동작하게 한다.
        if (_modeText != null)
        {
            _modeText.text = GetModeName(mode);
        }
    }

    private void RefreshBullet(int count, int maxCount)
    {
        _bulletText.text = $"{count} / {maxCount}";
    }

    private void RefreshBomb(int count)
    {
        _bombText.text = $"폭탄 {count}";
    }

    private static string GetModeName(WeaponMode mode)
    {
        switch (mode)
        {
            case WeaponMode.Normal:
                return "일반 모드";
            case WeaponMode.Sniper:
                return "저격 모드";
            default:
                return mode.ToString();
        }
    }
}
