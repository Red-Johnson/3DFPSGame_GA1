// 대기: 제자리에서 일정 시간 쉰다. 시간이 지나면 로밍하고, 플레이어를 발견하면 추격한다.
public class ZombieIdleState : ZombieStateBase
{
    private float _timer;

    public ZombieIdleState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _timer = 0f;
        _zombie.StopMoving();
        _zombie.PlayIdleAnimation();
    }

    public override void Update()
    {
        if (_zombie.IsPlayerInDetectRange())
        {
            _zombie.ChangeState(_zombie.TraceState);
            return;
        }

        _timer += UnityEngine.Time.deltaTime;
        if (_timer >= _zombie.IdleDuration)
        {
            _zombie.ChangeState(_zombie.RoamingState);
        }
    }
}
