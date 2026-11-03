// 몬스터: 플레이어가 맞히면 명중 표시를 내고, 죽였는지에 따라 연출이 달라지는 피격 대상(좀비, 보스 등).
// 총과 칼은 구체 클래스(Zombie)가 아니라 이 인터페이스만 보고 몬스터를 구분한다.
// 드럼통, 차량 같은 환경 오브젝트는 IDamageable만 구현하므로 명중 표시를 내지 않는다.
public interface IMonster : IDamageable
{
    // 죽었는지. (대미지를 받아 죽은 직후에도 true다.)
    bool IsDead { get; }
}
