using UnityEngine;

// 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다. 끝나면 플레이어를 쫓아온다.
public class ZombieHitState : ZombieStateBase
{
    private float _timer;

    public ZombieHitState(Zombie zombie) : base(zombie) { }

    // 멈칫하는 도중 또 맞아서 다시 진입하면 시간이 처음부터 시작된다.
    public override void Enter()
    {
        _timer = 0f;
        _zombie.StopMoving();
        _zombie.PlayHitAnimation();
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _zombie.HitStunDuration)
        {
            _zombie.ChangeState(_zombie.TraceState);
        }
    }
}
