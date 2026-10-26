// 죽음: 움직임을 멈추고 잠시 뒤 사라진다.
public class ZombieDeathState : ZombieStateBase
{
    public ZombieDeathState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _zombie.DisableController();
        _zombie.StartDespawn();
    }

    public override void Update() { }
}
