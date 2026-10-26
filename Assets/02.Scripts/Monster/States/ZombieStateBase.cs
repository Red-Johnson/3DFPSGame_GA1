// 좀비 상태의 공통 틀: 상태마다 진입(Enter), 매 프레임(Update), 종료(Exit) 때 할 일을 정한다.
// 새 상태를 추가하려면 이 클래스를 상속하고 Zombie에서 인스턴스를 만들어 ChangeState로 전이한다.
public abstract class ZombieStateBase
{
    protected readonly Zombie _zombie;

    protected ZombieStateBase(Zombie zombie)
    {
        _zombie = zombie;
    }

    public virtual void Enter() { }

    public abstract void Update();

    public virtual void Exit() { }
}
