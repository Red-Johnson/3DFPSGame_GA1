using UnityEngine;

// 폭발 대미지: 폭발 중심 주변의 드럼통, 좀비, 차량에게 대미지를 준다. (폭탄, 드럼통, 차량이 같이 쓴다.)
public static class ExplosionDamage
{
    private const int ColliderBufferSize = 64;

    private static readonly int HitLayers = LayerMask.GetMask("Drum", "Zombie");
    private static readonly Collider[] ColliderBuffer = new Collider[ColliderBufferSize];

    public static void Apply(Vector3 center, float range, float damage)
    {
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        // 매번 배열을 새로 만들지 않도록 버퍼를 재사용한다. 버퍼가 가득 차면 놓치는 대상이 없도록 새 배열로 다시 찾는다.
        // (TakeDamage는 다시 Apply를 부르지 않는다. 연쇄 폭발은 드럼통, 차량의 코루틴이 나중에 낸다. 그래서 버퍼를 돌려 써도 안전하다.)
        int count = Physics.OverlapSphereNonAlloc(center, range, ColliderBuffer, HitLayers);
        Collider[] colliders = ColliderBuffer;

        if (count == ColliderBufferSize)
        {
            colliders = Physics.OverlapSphere(center, range, HitLayers);
            count = colliders.Length;
        }

        for (int i = 0; i < count; i++)
        {
            Collider col = colliders[i];
            Vector3 closestPoint = col.ClosestPoint(center);   // 폭발 중심에서 가장 가까운 콜라이더 표면 위치

            DamageInfo damageInfo = new DamageInfo(damage, closestPoint, (center - closestPoint).normalized);

            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }

        // 차량은 별도 레이어가 없어서(카메라와 미니맵이 레이어로 그리는 범위를 바꾸지 않으려고) 목록으로 따로 전달한다.
        DestructibleCar.ApplyExplosion(center, range, damage);
    }
}
