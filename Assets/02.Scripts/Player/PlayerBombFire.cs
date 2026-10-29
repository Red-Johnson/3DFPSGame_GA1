using System;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르고 있으면 폭탄을 들고 충전하고, 떼는 즉시 충전한 만큼 세게 던진다.
// 던지는 모션(상체만)은 누르면 첫 자세에서 멈춘 채 시작하고, 떼면 이어서 재생된다. (애니메이터 호출은 PlayerAnimator가 맡는다.)
// 폭탄은 모션이 끝나길 기다리지 않고 버튼을 떼자마자 손을 떠난다.
// 무기 모드가 Normal일 때만 던질 수 있다. 충전 중에 다른 모드로 바뀌면 던지지 않고 충전을 취소한다.
[RequireComponent(typeof(PlayerWeaponMode), typeof(PlayerAnimator))]
public class PlayerBombFire : MonoBehaviour
{
    private const int BombPoolDefaultCapacity = 5;
    private const int BombPoolMaxSize = 20;

    private const float MinReleaseAnimationSpeed = 0.1f;

    private enum Phase
    {
        Idle,       // 던질 수 있는 상태
        Charging,   // 버튼을 누르고 충전 중
        Throwing,   // 버튼을 떼서 폭탄은 나갔고, 던지는 모션이 끝날 때까지
    }

    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;

    [Header("충전")]
    [SerializeField] private float _minThrowPower = 5f;     // 충전하지 않고 바로 뗐을 때의 힘
    [SerializeField] private float _maxThrowPower = 20f;    // 최대로 충전했을 때의 힘
    [SerializeField] private float _maxChargeTime = 2f;     // 이 시간 이상 눌러도 더 세지지 않는다.
    [SerializeField] private AnimationCurve _chargeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);   // 충전 정도(0~1)에 따른 힘의 증가 모양

    [Header("던지는 모션")]
    [SerializeField] private float _releaseAnimationSpeed = 3f;     // 버튼을 뗀 뒤 던지는 모션의 재생 배속. 클수록 모션이 빨리 끝나 사격이 빨리 풀린다.

    [SerializeField] private int _maxBombCount = 6;

    private PrefabPool<Bomb> _bombPool;
    private Camera _camera;
    private PlayerAnimator _playerAnimator;
    private PlayerWeaponMode _weaponMode;
    private int _bombCount;

    private Phase _phase = Phase.Idle;
    private float _chargeTime;      // 버튼을 누르고 있던 시간

    public bool IsCharging => _phase == Phase.Charging;
    public bool IsThrowing => _phase == Phase.Throwing;

    // 폭탄은 무기 모드가 Normal일 때만 던질 수 있다. (PlayerWeaponMode가 없으면 막지 않는다.)
    private bool CanThrowInCurrentMode => _weaponMode == null || _weaponMode.Mode == WeaponMode.Normal;

    // 폭탄 충전을 시작하면 구독자(총기의 재장전 취소 등)에게 알린다.
    public event Action OnChargeStarted;

    public int BombCount => _bombCount;
    public event Action<int> OnBombChanged;

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _camera = Camera.main;
        _bombPool = new PrefabPool<Bomb>(_bombPrefab, "BombPool", BombPoolDefaultCapacity, BombPoolMaxSize);

        if (_weaponMode == null)
        {
            GameLogger.LogError("[PlayerBombFire] PlayerWeaponMode가 없어 무기 모드와 상관없이 던집니다.");
        }

        // UI가 Start에서 폭탄 수를 읽어도 되도록 Awake에서 채운다.
        _bombCount = _maxBombCount;
    }

    private void OnValidate()
    {
        _maxThrowPower = Mathf.Max(_maxThrowPower, _minThrowPower);
        _maxChargeTime = Mathf.Max(_maxChargeTime, 0f);
        _releaseAnimationSpeed = Mathf.Max(_releaseAnimationSpeed, MinReleaseAnimationSpeed);   // 0이면 모션이 멈춘 채 끝나지 않는다.
    }

    private void Update()
    {
        // 게임이 멈추면(게임 오버 등) 충전을 취소한다.
        if (!GameManager.Instance.IsPlaying)
        {
            if (_phase == Phase.Charging)
            {
                CancelCharge();
            }

            return;
        }

        switch (_phase)
        {
            case Phase.Idle:
                // 버튼을 누르면 충전을 시작한다. (커서가 풀려 있으면 GameInput이 입력 없음으로 돌려준다.)
                // 무기 모드가 Normal이 아니거나 폭탄이 없으면 눌러도 시작하지 않는다.
                if (CanThrowInCurrentMode && GameInput.BombPressed && _bombCount > 0)
                {
                    StartCharge();
                }
                break;

            case Phase.Charging:
                UpdateCharging();
                break;

            case Phase.Throwing:
                // 던지는 모션이 끝나면 다음 던지기를 받는다.
                if (!_playerAnimator.IsThrowPlaying())
                {
                    _phase = Phase.Idle;
                }
                break;
        }
    }

    // 버튼을 누른 순간: 충전을 시작하고, 던지는 모션의 첫 자세에서 멈춘 채 시작한다.
    private void StartCharge()
    {
        _phase = Phase.Charging;
        _chargeTime = 0f;

        _playerAnimator.StartThrow();

        OnChargeStarted?.Invoke();
    }

    private void UpdateCharging()
    {
        // 충전 중에 무기 모드가 바뀌었다: 던지지 않고 취소한다.
        if (!CanThrowInCurrentMode)
        {
            CancelCharge();
            return;
        }

        // 버튼을 뗐다: 그 즉시 충전한 만큼의 힘으로 던진다.
        if (GameInput.BombReleased)
        {
            ReleaseBomb();
            return;
        }

        // 뗀 것이 아닌데 눌려 있지 않다: 커서가 풀리는 등 입력이 막혔으므로 던지지 않고 취소한다.
        if (!GameInput.BombHeld)
        {
            CancelCharge();
            return;
        }

        _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, _maxChargeTime);
    }

    // 버튼을 뗀 순간: 폭탄을 바로 던지고, 멈춰 있던 던지는 모션을 이어서 재생한다.
    private void ReleaseBomb()
    {
        SpawnBomb(EvaluateThrowPower());

        // 던지는 모션이 있든 없든 폭탄은 하나 줄어든다.
        _bombCount--;
        OnBombChanged?.Invoke(_bombCount);

        if (!_playerAnimator.HasThrowAnimation)
        {
            _phase = Phase.Idle;
            return;
        }

        // 모션이 끝날 때까지는 사격과 다음 던지기가 막힌다. 빨리 풀리도록 배속으로 재생한다.
        _phase = Phase.Throwing;
        _playerAnimator.ReleaseThrow(_releaseAnimationSpeed);
    }

    // 던지지 않고 충전만 취소한다. 멈춰 있던 모션도 풀어서 원래 자세로 돌린다.
    private void CancelCharge()
    {
        _phase = Phase.Idle;

        _playerAnimator.CancelThrow();
    }

    // 충전한 시간에 따라 최소 힘과 최대 힘 사이의 힘을 구한다.
    private float EvaluateThrowPower()
    {
        float charge = _maxChargeTime > 0f ? Mathf.Clamp01(_chargeTime / _maxChargeTime) : 1f;
        return Mathf.Lerp(_minThrowPower, _maxThrowPower, _chargeCurve.Evaluate(charge));
    }

    private void SpawnBomb(float power)
    {
        // 발사 위치에서 수류탄 꺼내기 (풀에서 재사용)
        Bomb bomb = _bombPool.Get(_firePoint.position, Quaternion.identity);

        // 꺼낸 수류탄을 시선 방향으로 던지기
        bomb.Launch(_camera.transform.forward * power);
    }
}
