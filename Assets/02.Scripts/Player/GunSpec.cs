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

    [Header("반동 (화면과 몸만 움직이고 조준은 바뀌지 않는다)")]
    [SerializeField] private float _cameraRecoilPitch = 0f;     // 한 발마다 화면이 위로 튀는 각도(도)
    [SerializeField] private float _cameraRecoilSide = 0f;      // 한 발마다 화면이 좌우로 무작위로 흔들리는 최대 각도(도)
    [SerializeField] private float _bodyRecoilAngle = 0f;       // 한 발마다 총을 든 팔이 위로 젖혀지는 각도(도). 3인칭에서 보인다.

    [Header("조준 보정 (줌 중에만, 총알 방향만 약점 쪽으로 끌어당긴다)")]
    [SerializeField] private float _aimAssistRadius = 0f;       // 조준점이 약점 중심에서 이 거리(m) 안으로 지나가면 보정한다. 거리와 상관없이 같은 크기다.
    [SerializeField, Range(0f, 1f)] private float _aimAssistStrength = 0f;  // 약점 중심에서 벗어난 만큼의 몇 %를 지워 줄지. 0.5면 오차가 절반으로, 1이면 범위 안에서 약점 중심으로 맞춘다.

    public int MaxBulletCount => _maxBulletCount;
    public float ReloadingTime => _reloadingTime;
    public float Range => _range;
    public float Damage => _damage;
    public float FireInterval => _fireInterval;
    public bool IsAutomatic => _isAutomatic;
    public float WeakPointDamage => _weakPointDamage;
    public bool CanHitWeakPoint => _weakPointDamage > 0f;
    public float CameraRecoilPitch => _cameraRecoilPitch;
    public float CameraRecoilSide => _cameraRecoilSide;
    public float BodyRecoilAngle => _bodyRecoilAngle;
    public float AimAssistStrength => _aimAssistStrength;
    public float AimAssistRadius => _aimAssistRadius;
    public bool HasAimAssist => _aimAssistStrength > 0f && _aimAssistRadius > 0f;

    public GunSpec(int maxBulletCount, float reloadingTime, float range, float damage, float fireInterval, bool isAutomatic, float weakPointDamage = 0f, float cameraRecoilPitch = 0f, float cameraRecoilSide = 0f, float bodyRecoilAngle = 0f, float aimAssistRadius = 0f, float aimAssistStrength = 0f)
    {
        _maxBulletCount = maxBulletCount;
        _reloadingTime = reloadingTime;
        _range = range;
        _damage = damage;
        _fireInterval = fireInterval;
        _isAutomatic = isAutomatic;
        _weakPointDamage = weakPointDamage;
        _cameraRecoilPitch = cameraRecoilPitch;
        _cameraRecoilSide = cameraRecoilSide;
        _bodyRecoilAngle = bodyRecoilAngle;
        _aimAssistRadius = aimAssistRadius;
        _aimAssistStrength = aimAssistStrength;
    }
}
