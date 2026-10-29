using UnityEngine;

// 공격: 일정 시간마다 플레이어를 때린다. 플레이어가 공격 거리 밖으로 나가면 다시 추격한다.
public class ZombieAttackState : ZombieStateBase
{
    private float _timer;
    private bool _isAttackAnimating;    // 공격 모션을 재생 중인지. 끝나면 대기 모션으로 돌아간다.
    private bool _hasHit;               // 이번 공격의 타격을 이미 처리했는지. (이벤트가 두 번 와도 대미지는 한 번만)

    public ZombieAttackState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _timer = 0f;
        _isAttackAnimating = false;
        _hasHit = true;     // 휘두르기 전에는 타격할 것이 없다.
        _zombie.Motor.StopMoving();
        _zombie.Anim.PlayAttackDelay();
    }

    public override void Update()
    {
        Vector3 targetPosition = _zombie.Target.position;

        if (Vector3.Distance(_zombie.transform.position, targetPosition) > _zombie.Stats.AttackRange)
        {
            _zombie.ChangeState(_zombie.TraceState);
            return;
        }

        // 멈춰 있어도 플레이어 쪽을 바라본다.
        _zombie.Motor.FaceToward(targetPosition);

        // 공격 모션이 끝나면 다음 공격까지 기다리는 모션으로 돌아간다.
        if (_isAttackAnimating && _zombie.Anim.IsAttackFinished())
        {
            _isAttackAnimating = false;
            _zombie.Anim.PlayAttackDelay();
        }

        _timer += Time.deltaTime;
        if (_timer >= _zombie.Stats.AttackInterval)
        {
            // 대미지는 여기서 주지 않고, 애니메이션의 타격 프레임(OnAttackHit)에서 준다.
            _zombie.Anim.PlayAttack();
            _isAttackAnimating = true;
            _hasHit = false;

            _timer = 0f;
        }
    }

    // 공격 애니메이션의 타격 프레임에서 불린다. 휘두르는 중일 때만 대미지를 준다.
    public override void OnAttackHit()
    {
        if (_hasHit) return;

        _hasHit = true;

        Vector3 targetPosition = _zombie.Target.position;

        DamageInfo info = new DamageInfo(
            _zombie.Stats.AttackDamage,
            targetPosition,
            (_zombie.transform.position - targetPosition).normalized);

        _zombie.TargetDamageable.TakeDamage(info);
    }
}
