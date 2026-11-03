using UnityEngine;

// 좀비 설정 데이터: 에셋(.asset)으로 만들어 좀비 프리팹이 참조한다. 좀비 종류를 늘릴 때는 에셋을 복제해서 값만 바꾼다.
// 현재 체력처럼 런타임에 바뀌는 값은 여기에 두지 않고 Zombie가 따로 가진다.
[CreateAssetMenu(fileName = "ZombieStats", menuName = "Game/Zombie Stats")]
public class ZombieStats : ScriptableObject
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _attackDamage = 10f;     // 공격 대미지
    [SerializeField] private float _attackRange = 1.5f;     // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f;  // 공격 간격
    [SerializeField] private float _firstAttackDelay = 0.5f;    // 공격 거리에 들어와 멈춘 뒤 첫 공격까지 걸리는 시간. 공격 간격 이상이면 간격만큼 기다린다.
    [SerializeField] private float _moveSpeed = 5f;         // 이동속도
    [SerializeField] private float _angularSpeed = 540f;    // 회전속도(초당 각도). 낮으면 방향을 바꿀 때 큰 반원을 그리며 돈다.

    [Header("길찾기")]
    [SerializeField] private float _destinationUpdateInterval = 0.2f;   // 추격 중 목적지를 다시 계산하는 간격
    [SerializeField] private float _destinationRepathDistance = 0.5f;   // 플레이어가 이만큼 움직였을 때만 추격 경로를 다시 계산한다.
    [SerializeField] private float _chaseStopDistanceRatio = 0.8f;      // 추격 정지 거리 = 공격 범위 * 이 비율 (경계에서 떨리지 않게 공격 범위보다 짧게)
    [SerializeField] private int _minAvoidancePriority = 30;            // 좀비끼리 길이 막히지 않도록 회피 우선순위를 무작위로 준다.
    [SerializeField] private int _maxAvoidancePriority = 60;

    [Header("점프")]
    [SerializeField] private float _jumpDuration = 0.6f;    // NavMeshLink를 건너는 데 걸리는 시간
    [SerializeField] private float _jumpHeight = 1.5f;      // 시작점과 도착점을 잇는 직선보다 위로 솟는 최대 높이

    [Header("처치 보상")]
    [SerializeField] private int _comboValue = 1;           // 처치했을 때 올라가는 콤보 수. (보스는 크게 준다.)

    [Header("애니메이션")]
    [SerializeField] private float _animationFadeDuration = 0.15f;  // 애니메이션이 서로 섞이는 시간

    [Header("피격/죽음")]
    [SerializeField] private bool _canBeStaggered = true;   // 맞으면 멈칫하는지. 보스처럼 끄면 맞아도 하던 행동을 계속한다.
    [SerializeField] private float _hitStunDuration = 0.3f; // 맞고 멈칫하는 시간
    [SerializeField] private float _despawnDelay = 1.5f;    // 죽고 사라지기까지 걸리는 시간

    public float MaxHealth => _maxHealth;
    public float AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float FirstAttackDelay => _firstAttackDelay;
    public float MoveSpeed => _moveSpeed;
    public float AngularSpeed => _angularSpeed;

    public float DestinationUpdateInterval => _destinationUpdateInterval;
    public float DestinationRepathDistance => _destinationRepathDistance;
    public float ChaseStopDistance => _attackRange * _chaseStopDistanceRatio;
    public int MinAvoidancePriority => _minAvoidancePriority;
    public int MaxAvoidancePriority => _maxAvoidancePriority;

    public float JumpDuration => _jumpDuration;
    public float JumpHeight => _jumpHeight;

    public int ComboValue => _comboValue;

    public float AnimationFadeDuration => _animationFadeDuration;

    public bool CanBeStaggered => _canBeStaggered;
    public float HitStunDuration => _hitStunDuration;
    public float DespawnDelay => _despawnDelay;
}
