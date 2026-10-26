using UnityEngine;

// 추격: 플레이어를 쫓아간다. 공격 거리 안에 들어오면 공격한다.
public class ZombieTraceState : ZombieStateBase
{
    public ZombieTraceState(Zombie zombie) : base(zombie) { }

    public override void Update()
    {
        // Todo. Run 애니메이션 실행

        Vector3 playerPosition = _zombie.Player.transform.position;

        if (Vector3.Distance(_zombie.transform.position, playerPosition) <= _zombie.AttackRange)
        {
            _zombie.ChangeState(_zombie.AttackState);
            return;
        }

        _zombie.MoveToward(playerPosition);
    }
}
