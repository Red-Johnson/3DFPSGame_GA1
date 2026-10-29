using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UI_WeaponStatus : MonoBehaviour
{
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Sprite[] _modeIcons;
    [SerializeField] private TextMeshProUGUI _bulletText;
    [SerializeField] private TextMeshProUGUI _bombText;

    [SerializeField] private PlayerWeaponMode _weaponMode;
    [SerializeField] private PlayerGunFire _gunFire;
    [SerializeField] private PlayerBombFire _bombFire;

    private void OnEnable()
    {
        _weaponMode.OnModeChanged += RefreshIcon;
        _gunFire.OnBulletChanged += RefreshBullet;
        _bombFire.OnBombChanged += RefreshBomb;
    }

    private void OnDisable()
    {
        _weaponMode.OnModeChanged -= RefreshIcon;
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
        RefreshIcon(_weaponMode.Mode);
        RefreshBullet(_gunFire.BulletCount, _gunFire.MaxBulletCount);
        RefreshBomb(_bombFire.BombCount);
    }


    private void RefreshIcon(WeaponMode mode)
    {
        _weaponIcon.sprite = _modeIcons[(int)mode];
    }

    private void RefreshBullet(int count, int maxCount)
    {
        _bulletText.text = $"{count} / {maxCount}";
    }

    private void RefreshBomb(int count)
    {
        _bombText.text = $"폭탄 {count}";
    }
}
