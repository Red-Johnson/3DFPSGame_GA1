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
public class Zombie : MonoBehaviour, IDamageable, IPoolable<Zombie>
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance  = 8;    // 플레이어를 발견하는 거리
    [SerializeField] private float _loseDistanceRatio = 1.5f;   // 추격을 포기하는 거리 = 발견 거리 * 이 비율 (발견 거리보다 길어야 경계에서 Idle/Trace가 번갈아 바뀌지 않는다)
    [SerializeField] private float _attackDamage = 10f;     // 공격 대미지
    [SerializeField] private float _attackRange = 1.5f;     // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f;  // 공격 간격
    [SerializeField] private float _moveSpeed = 5;          // 이동속도
    [SerializeField] private float _maxHealth = 30;
    [SerializeField] private float _health;

    [Header("로밍")]
    [SerializeField] private float _roamDistance = 3f;      // 로밍할 때 현재 위치에서 이동할 최대 거리
    [SerializeField] private float _idleDuration = 1f;      // 대기 시간

    [Header("길찾기")]
    [SerializeField] private float _destinationUpdateInterval = 0.2f;   // 추격 중 목적지를 다시 계산하는 간격
    [SerializeField] private float _destinationRepathDistance = 0.5f;   // 플레이어가 이만큼 움직였을 때만 추격 경로를 다시 계산한다.
    [SerializeField] private float _chaseStopDistanceRatio = 0.8f;      // 추격 정지 거리 = 공격 범위 * 이 비율 (경계에서 떨리지 않게 공격 범위보다 짧게)
    [SerializeField] private int _minAvoidancePriority = 30;            // 좀비끼리 길이 막히지 않도록 회피 우선순위를 무작위로 준다.
    [SerializeField] private int _maxAvoidancePriority = 60;

    [Header("점프")]
    [SerializeField] private float _jumpDuration = 0.6f;    // NavMeshLink를 건너는 데 걸리는 시간
    [SerializeField] private float _jumpHeight = 1.5f;      // 시작점과 도착점을 잇는 직선보다 위로 솟는 최대 높이

    [Header("애니메이션")]
    [SerializeField] private float _animationFadeDuration = 0.15f;  // 애니메이션이 서로 섞이는 시간

    [Header("피격/죽음")]
    [SerializeField] private float _hitStunDuration = 0.3f; // 맞고 멈칫하는 시간
    [SerializeField] private float _despawnDelay = 0.5f;    // 죽고 사라지기까지 걸리는 시간

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    // 애니메이터 상태 이름. 컨트롤러의 상태 이름과 똑같아야 한다.
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int TraceHash = Animator.StringToHash("Trace");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackDelayHash = Animator.StringToHash("AttackDelay");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int[] DeathHashes = { Animator.StringToHash("Death1"), Animator.StringToHash("Death2") };

    private NavMeshAgent _agent;
    private Collider _collider;
    private Animator _animator;
    private GameObject _player;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    public float AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float MoveSpeed => _moveSpeed;
    public float RoamDistance => _roamDistance;
    public float IdleDuration => _idleDuration;
    public float DestinationUpdateInterval => _destinationUpdateInterval;
    public float DestinationRepathDistance => _destinationRepathDistance;
    public float ChaseStopDistance => _attackRange * _chaseStopDistanceRatio;
    public float HitStunDuration => _hitStunDuration;
    public GameObject Player => _player;

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

    // NavMeshLink를 건너는 중인지. 건너는 동안은 상태 Update와 피격 경직을 건너뛴다.
    private bool _isJumping;

    private bool IsDead => _currentState == _deathState;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
        _animator = GetComponentInChildren<Animator>();
        _agent.speed = _moveSpeed;
        _agent.autoTraverseOffMeshLink = false;     // 링크는 Jump_Coroutine에서 직접 건넌다.

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
        _health = _maxHealth;
        _isJumping = false;     // 점프 중에 풀로 돌아갔어도 재사용될 때 초기화한다.
        _collider.enabled = true;
        _agent.enabled = true;
        _agent.avoidancePriority = UnityEngine.Random.Range(_minAvoidancePriority, _maxAvoidancePriority + 1);

        // 풀에서 재사용될 때 NavMesh에 붙지 못했다면 가까운 NavMesh 위로 옮긴다.
        if (!_agent.isOnNavMesh)
        {
            _agent.Warp(transform.position);
        }

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = false;
        }

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

    private void Start()
    {
        // OnEnable 시점에 매니저가 아직 없었을 수 있으니 한 번 더 등록한다. (중복 등록은 무시됨)
        RegisterToManager();

        // 게임을 시작하면 플레이어를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");
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
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_isJumping) return;

        if (_agent.enabled && _agent.isOnNavMesh && _agent.isOnOffMeshLink)
        {
            StartCoroutine(Jump_Coroutine());
            return;
        }

        _currentState.Update();
    }

    // NavMeshLink의 시작점에서 도착점까지 포물선으로 뛰어 건넌다.
    private IEnumerator Jump_Coroutine()
    {
        _isJumping = true;

        OffMeshLinkData linkData = _agent.currentOffMeshLinkData;
        Vector3 start = transform.position;
        // 링크 끝점은 NavMesh 표면 위치이므로 피벗 높이(Base Offset)를 더해 준다.
        Vector3 end = linkData.endPos + (Vector3.up * _agent.baseOffset);

        FaceToward(end);

        float elapsed = 0f;
        while (elapsed < _jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _jumpDuration);

            // 직선 이동에 포물선(4h * t * (1 - t))을 더한다.
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += 4f * _jumpHeight * t * (1f - t);
            transform.position = position;

            yield return null;
        }

        transform.position = end;

        // 점프 도중 죽어서 에이전트가 꺼졌다면 건넌 자리에 그대로 둔다.
        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.CompleteOffMeshLink();
        }

        _isJumping = false;
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
            //bloodSpray.transform.SetParent(transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            // 잠깐 멈칫 하고 싶다! (링크를 건너는 중에는 멈추면 경로가 꼬이므로 멈칫하지 않는다.)
            if (!_isJumping)
            {
                ChangeState(_hitState);
            }
        }
        else
        {
            UnregisterFromManager();
            OnDied?.Invoke();
            ChangeState(_deathState);
        }

    }

    // 플레이어를 발견했는지
    public bool IsPlayerInDetectRange()
    {
        return Vector3.Distance(transform.position, _player.transform.position) <= _detectDistance;
    }

    // 추격하던 플레이어가 충분히 멀어졌는지 (추격 포기 거리 밖인지)
    public bool IsPlayerOutOfLoseRange()
    {
        return Vector3.Distance(transform.position, _player.transform.position) > _detectDistance * _loseDistanceRatio;
    }

    // 애니메이터 상태 이름으로 바로 재생한다. (전이나 트리거 없이 현재 모션에서 부드럽게 섞는다.)
    // 그 상태가 컨트롤러에 없으면 fallbackHash 상태로 대신 재생하고, 그것도 없으면 아무것도 하지 않는다.
    private void PlayAnimation(int stateHash, int fallbackHash = 0)
    {
        if (_animator == null || !_animator.isInitialized) return;

        if (!_animator.HasState(0, stateHash))
        {
            if (fallbackHash == 0 || !_animator.HasState(0, fallbackHash)) return;

            stateHash = fallbackHash;
        }

        _animator.CrossFadeInFixedTime(stateHash, _animationFadeDuration);
    }

    public void PlayIdleAnimation() => PlayAnimation(IdleHash);

    // 로밍은 걷는 모션을 쓴다. Walk 상태가 없으면 Trace 모션으로 대신한다.
    public void PlayWalkAnimation() => PlayAnimation(WalkHash, TraceHash);

    public void PlayTraceAnimation() => PlayAnimation(TraceHash);

    public void PlayAttackAnimation() => PlayAnimation(AttackHash);

    // 공격과 공격 사이에 기다리는 모션
    public void PlayAttackDelayAnimation() => PlayAnimation(AttackDelayHash, IdleHash);

    // 피격 모션이 없으면 서 있는 모션으로 대신한다.
    public void PlayHitAnimation() => PlayAnimation(HitHash, IdleHash);

    // 죽는 모션 중 하나를 무작위로 고른다.
    public void PlayDeathAnimation() => PlayAnimation(DeathHashes[UnityEngine.Random.Range(0, DeathHashes.Length)]);

    // 공격 모션을 끝까지 재생했는지. 다른 모션으로 섞이는 중에는 판단하지 않는다.
    public bool IsAttackAnimationFinished()
    {
        if (_animator == null || !_animator.isInitialized || _animator.IsInTransition(0)) return false;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == AttackHash && stateInfo.normalizedTime >= 1f;
    }

    // NavMesh 경로를 계산해 목표 위치로 이동을 시작한다. 경로를 만들 수 없으면 false.
    public bool SetDestination(Vector3 target)
    {
        if (!_agent.isOnNavMesh) return false;

        return _agent.SetDestination(target);
    }

    public void SetStoppingDistance(float distance)
    {
        _agent.stoppingDistance = distance;
    }

    // 걷는 상태에 들어갈 때 부른다. (정지했던 에이전트를 다시 움직일 수 있게 한다.)
    public void ResumeMoving()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = false;
    }

    // 제자리에 서는 상태에 들어갈 때 부른다.
    public void StopMoving()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;     // 남은 속도로 미끄러지며 목표를 지나쳤다 돌아오는 떨림을 막는다.
    }

    // 목적지에 도착했거나 갈 수 없는 경로면 true. 경로 계산 중에는 false.
    public bool HasFinishedPath()
    {
        if (!_agent.isOnNavMesh || _agent.pathPending) return false;

        return _agent.pathStatus == NavMeshPathStatus.PathInvalid || _agent.remainingDistance <= _agent.stoppingDistance;
    }

    // 멈춰 있는 동안에도 높이는 무시하고 목표 쪽을 바라본다.
    public void FaceToward(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < Mathf.Epsilon) return;

        transform.forward = direction.normalized;
    }

    // 죽으면 이동과 피격 판정을 끈다. (다시 살아날 때 OnEnable에서 켠다.)
    public void DisableMovement()
    {
        _agent.enabled = false;
        _collider.enabled = false;
    }

    public void StartDespawn()
    {
        StartCoroutine(Despawn_Coroutine());
    }

    private IEnumerator Despawn_Coroutine()
    {
        yield return new WaitForSeconds(_despawnDelay);
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
}
