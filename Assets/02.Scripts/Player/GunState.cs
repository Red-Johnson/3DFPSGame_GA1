// 총 한 자루의 사용 중 상태: 남은 총알과 다음에 쏠 수 있는 시각. PlayerGunFire가 총마다 하나씩 들고 있다.
// 무기를 바꿨다가 돌아와도 총마다 이 상태가 그대로 남는다.
public class GunState
{
    public int BulletCount;         // 남은 총알
    public float NextFireTime;      // 다음에 쏠 수 있는 시각(Time.time 기준)

    public GunState(int bulletCount)
    {
        BulletCount = bulletCount;
        NextFireTime = 0f;
    }
}
