using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"
//
// 상태 패턴: 상태마다 클래스(States/ 폴더)로 나누고, Zombie는 현재 상태에 Update를 맡기기만 한다.
// - Idle: 제자리에서 쉰다. / Roaming: 주변 무작위 위치로 걷는다.
// - Trace: 플레이어를 쫓아간다. / Attack: 일정 시간마다 플레이어를 때린다.
// - Hit: 공격을 받아 잠깐 멈칫한다. / Death: 쓰러지고 잠시 뒤 사라진다.
//
// 역할 분담: 설정값은 ZombieStats(에셋), 이동과 점프는 ZombieMotor, 애니메이션은 ZombieAnimator가 맡고,
// Zombie는 상태 머신, 체력과 피격, 풀링을 맡는다.
public class Zombie : MonoBehaviour, IDamageable, IPoolable<Zombie>
{
    [SerializeField] private ZombieStats _stats;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    // _stats를 연결하지 않았을 때 같은 기본값으로 동작하도록 모든 좀비가 나눠 쓰는 대체 설정
    private static ZombieStats _fallbackStats;

    private ZombieMotor _motor;
    private ZombieAnimator _anim;
    private Collider[] _colliders;     // 몸통과 약점 등 피격 판정 콜라이더 전부
    private Transform _target;
    private IDamageable _targetDamageable;
    private float _health;

    public ZombieStats Stats => _stats;
    public ZombieMotor Motor => _motor;
    public ZombieAnimator Anim => _anim;
    public Transform Target => _target;
    public IDamageable TargetDamageable => _targetDamageable;

    public float MaxHealth => _stats.MaxHealth;
    public float Health => _health;

    // 체력이 바뀌면(대미지를 받거나 다시 살아나면) 구독자(체력바 등)에게 알린다.
    public event Action OnHealthChanged;

    // 좀비가 죽으면 구독자(남은 좀비 수 UI 등)에게 알린다.
    public event Action OnDied;

    // 상태 객체는 좀비마다 한 번만 만들어 재사용한다. (오브젝트 풀링으로 재사용해도 그대로)
    private ZombieIdleState _idleState;
    private ZombieRoamingState _roamingState;
    private ZombieTraceState _traceState;
    private ZombieAttackState _attackState;
    private ZombieHitState _hitState;
    private ZombieDeathState _deathState;

    public ZombieIdleState IdleState => _idleState;
    public ZombieRoamingState RoamingState => _roamingState;
    public ZombieTraceState TraceState => _traceState;
    public ZombieAttackState AttackState => _attackState;

    private ZombieStateBase _currentState;

    // 풀에서 꺼낸 좀비면 값이 있다. 씬에 미리 배치한 좀비는 null이라 Destroy로 없앤다.
    private IObjectPool<Zombie> _pool;

    // 좀비의 자식으로 만든 피 이펙트. 재사용되기 전에 정리한다.
    private readonly List<GameObject> _bloodSprays = new List<GameObject>();

    private bool IsDead => _currentState == _deathState;

    private void Awake()
    {
        if (_stats == null)
        {
            _stats = GetFallbackStats();
        }

        _colliders = GetComponentsInChildren<Collider>(true);
        _motor = new ZombieMotor(GetComponent<NavMeshAgent>(), _stats);
        _anim = new ZombieAnimator(GetComponentInChildren<Animator>(), _stats.AnimationFadeDuration);

        _idleState = new ZombieIdleState(this);
        _roamingState = new ZombieRoamingState(this);
        _traceState = new ZombieTraceState(this);
        _attackState = new ZombieAttackState(this);
        _hitState = new ZombieHitState(this);
        _deathState = new ZombieDeathState(this);
    }

    // 활성화될 때마다 처음 상태로 되돌린다. (오브젝트 풀링으로 재사용해도 같은 경로)
    private void OnEnable()
    {
        _health = _stats.MaxHealth;
        SetCollidersEnabled(true);
        _motor.Enable();

        OnHealthChanged?.Invoke();

        ChangeState(_idleState);

        RegisterToManager();
    }

    private void OnDisable()
    {
        UnregisterFromManager();
        ClearBloodSprays();
    }

    public void SetPool(IObjectPool<Zombie> pool)
    {
        _pool = pool;
    }

    // 쫓아갈 대상을 정한다. 스포너가 좀비를 꺼낼 때 넘겨 준다.
    public void SetTarget(Transform target)
    {
        _target = target;
        _targetDamageable = target.GetComponent<IDamageable>();
    }

    private void Start()
    {
        // OnEnable 시점에 매니저가 아직 없었을 수 있으니 한 번 더 등록한다. (중복 등록은 무시됨)
        RegisterToManager();

        // 미리 배치한 좀비처럼 대상을 받지 못했다면 플레이어를 찾는다.
        if (_target == null)
        {
            SetTarget(GameObject.FindGameObjectWithTag("Player").transform);
        }
    }

    // 좀비 수 집계에 등록/해제한다. (스포너가 만든 좀비도 같은 경로)
    private void RegisterToManager()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.Register(this);
        }
    }

    private void UnregisterFromManager()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.Unregister(this);
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (_motor.IsJumping) return;

        if (_motor.IsOnOffMeshLink)
        {
            StartCoroutine(_motor.Jump_Coroutine());
            return;
        }

        _currentState.Update();
    }

    // 공격 애니메이션의 타격 프레임에서 불린다. (ZombieAnimationEvents가 전달)
    // 공격 상태가 아닐 때(피격, 죽음 등) 온 이벤트는 상태가 무시한다.
    public void OnAttackHit()
    {
        _currentState?.OnAttackHit();
    }

    // 현재 상태를 마치고 다음 상태로 넘어간다.
    public void ChangeState(ZombieStateBase nextState)
    {
        _currentState?.Exit();
        _currentState = nextState;
        _currentState.Enter();
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead)
        {
            return;
        }

        _health -= damageInfo.Value;
        OnHealthChanged?.Invoke();

        if (_health > 0f)
        {
            // 좀비의 자식으로 블러드 스프레이를 생성! -> 위치 수정!
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            _bloodSprays.Add(bloodSpray);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            // 잠깐 멈칫 하고 싶다! (링크를 건너는 중에는 멈추면 경로가 꼬이므로 멈칫하지 않는다.)
            if (!_motor.IsJumping)
            {
                ChangeState(_hitState);
            }
        }
        else
        {
            UnregisterFromManager();
            OnDied?.Invoke();

            // 쓰러지기 시작하면 출혈 이펙트를 바로 없앤다. (좀비는 쓰러지는데 피만 공중에 남지 않게)
            ClearBloodSprays();
            ChangeState(_deathState);
        }
    }

    // 플레이어를 발견했는지
    public bool IsPlayerInDetectRange()
    {
        return Vector3.Distance(transform.position, _target.position) <= _stats.DetectDistance;
    }

    // 추격하던 플레이어가 충분히 멀어졌는지 (추격 포기 거리 밖인지)
    public bool IsPlayerOutOfLoseRange()
    {
        return Vector3.Distance(transform.position, _target.position) > _stats.LoseDistance;
    }

    // 죽으면 이동과 피격 판정을 끈다. (다시 살아날 때 OnEnable에서 켠다.)
    public void DisableMovement()
    {
        _motor.Disable();
        SetCollidersEnabled(false);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        foreach (Collider collider in _colliders)
        {
            collider.enabled = isEnabled;
        }
    }

    public void StartDespawn()
    {
        StartCoroutine(Despawn_Coroutine());
    }

    private IEnumerator Despawn_Coroutine()
    {
        yield return new WaitForSeconds(_stats.DespawnDelay);
        Despawn();
    }

    // 풀에서 꺼낸 좀비는 풀로 돌려보내고, 아니면 삭제한다.
    private void Despawn()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ClearBloodSprays()
    {
        foreach (GameObject bloodSpray in _bloodSprays)
        {
            if (bloodSpray != null)
            {
                Destroy(bloodSpray);
            }
        }

        _bloodSprays.Clear();
    }

    private static ZombieStats GetFallbackStats()
    {
        if (_fallbackStats == null)
        {
            _fallbackStats = ScriptableObject.CreateInstance<ZombieStats>();
            GameLogger.LogWarning("[Zombie] ZombieStats가 연결되지 않아 기본값을 사용합니다. 좀비 프리팹에 ZombieStats 에셋을 연결하세요.");
        }

        return _fallbackStats;
    }
}
