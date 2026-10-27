using UnityEngine;

// 공격: 일정 시간마다 플레이어를 때린다. 플레이어가 공격 거리 밖으로 나가면 다시 추격한다.
public class ZombieAttackState : ZombieStateBase
{
    private float _timer;
    private bool _isAttackAnimating;    // 공격 모션을 재생 중인지. 끝나면 대기 모션으로 돌아간다.

    public ZombieAttackState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _timer = 0f;
        _isAttackAnimating = false;
        _zombie.StopMoving();
        _zombie.PlayAttackDelayAnimation();
    }

    public override void Update()
    {
        GameObject player = _zombie.Player;

        if (Vector3.Distance(_zombie.transform.position, player.transform.position) > _zombie.AttackRange)
        {
            _zombie.ChangeState(_zombie.TraceState);
            return;
        }

        // 멈춰 있어도 플레이어 쪽을 바라본다.
        _zombie.FaceToward(player.transform.position);

        // 공격 모션이 끝나면 다음 공격까지 기다리는 모션으로 돌아간다.
        if (_isAttackAnimating && _zombie.IsAttackAnimationFinished())
        {
            _isAttackAnimating = false;
            _zombie.PlayAttackDelayAnimation();
        }

        _timer += Time.deltaTime;
        if (_timer >= _zombie.AttackInterval)
        {
            _zombie.PlayAttackAnimation();
            _isAttackAnimating = true;

            DamageInfo info = new DamageInfo()
            {
                Value = _zombie.AttackDamage,
                HitPoint = player.transform.position,
                HitNormal = (_zombie.transform.position - player.transform.position).normalized,
            };

            player.GetComponent<IDamageable>().TakeDamage(info);

            _timer = 0f;
        }
    }
}
