using UnityEngine;
using UnityEngine.Serialization;

// 총 한 자루의 성능. 총마다 따로 인스펙터에서 조절한다. (직렬화 클래스라 PlayerGunFire의 필드로 두면 묶여서 보인다.)
[System.Serializable]
public class GunSpec
{
    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f;               // 사거리
    [SerializeField] private float _damage = 10f;               // 공격력
    [SerializeField] private float _fireInterval = 0.1f;        // 발사 간격(초). 단발도 이 간격보다 빨리는 쏠 수 없다.
    [SerializeField] private bool _isAutomatic = true;          // true: 누르고 있으면 계속 발사, false: 한 번 누를 때마다 한 발
    [FormerlySerializedAs("_headshotDamage")]
    [SerializeField] private float _weakPointDamage = 0f;       // 약점(WeakPointHitbox)에 맞았을 때의 공격력. 0 이하면 약점 공격이 없다.

    public int MaxBulletCount => _maxBulletCount;
    public float ReloadingTime => _reloadingTime;
    public float Range => _range;
    public float Damage => _damage;
    public float FireInterval => _fireInterval;
    public bool IsAutomatic => _isAutomatic;
    public float WeakPointDamage => _weakPointDamage;
    public bool CanHitWeakPoint => _weakPointDamage > 0f;

    public GunSpec(int maxBulletCount, float reloadingTime, float range, float damage, float fireInterval, bool isAutomatic, float weakPointDamage = 0f)
    {
        _maxBulletCount = maxBulletCount;
        _reloadingTime = reloadingTime;
        _range = range;
        _damage = damage;
        _fireInterval = fireInterval;
        _isAutomatic = isAutomatic;
        _weakPointDamage = weakPointDamage;
    }
}
