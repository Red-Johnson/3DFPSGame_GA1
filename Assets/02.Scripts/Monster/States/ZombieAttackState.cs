using UnityEngine;

// 공격: 일정 시간마다 플레이어를 때린다. 플레이어가 공격 거리 밖으로 나가면 다시 추격한다.
public class ZombieAttackState : ZombieStateBase
{
    private float _timer;

    public ZombieAttackState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _timer = 0f;
    }

    public override void Update()
    {
        GameObject player = _zombie.Player;

        if (Vector3.Distance(_zombie.transform.position, player.transform.position) > _zombie.AttackRange)
        {
            _zombie.ChangeState(_zombie.TraceState);
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _zombie.AttackInterval)
        {
            // Todo. 공격 애니메이션 실행

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
