using UnityEngine;

// 추격: 플레이어를 쫓아간다. 공격 거리 안에 들어오면 공격하고, 추격 포기 거리 밖으로 멀어지면 쉰다.
public class ZombieTraceState : ZombieStateBase
{
    private float _destinationTimer;

    public ZombieTraceState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        // 진입하자마자 목적지를 잡도록 타이머를 가득 채워 둔다.
        _destinationTimer = _zombie.DestinationUpdateInterval;
        _zombie.SetStoppingDistance(_zombie.ChaseStopDistance);
        _zombie.ResumeMoving();
        _zombie.PlayTraceAnimation();
    }

    public override void Update()
    {
        // 플레이어가 충분히 멀어졌으면 추격을 포기하고 쉰다.
        if (_zombie.IsPlayerOutOfLoseRange())
        {
            _zombie.ChangeState(_zombie.IdleState);
            return;
        }

        Vector3 playerPosition = _zombie.Player.transform.position;

        if (Vector3.Distance(_zombie.transform.position, playerPosition) <= _zombie.AttackRange)
        {
            _zombie.ChangeState(_zombie.AttackState);
            return;
        }

        // 경로 계산은 비싸므로 매 프레임이 아니라 일정 간격마다 목적지를 갱신한다.
        _destinationTimer += Time.deltaTime;
        if (_destinationTimer >= _zombie.DestinationUpdateInterval)
        {
            _destinationTimer = 0f;
            _zombie.SetDestination(playerPosition);
        }
    }
}
