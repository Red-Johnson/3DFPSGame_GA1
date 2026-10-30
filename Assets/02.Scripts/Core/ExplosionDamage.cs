using UnityEngine;

// 폭발 대미지: 폭발 중심 주변의 드럼통, 좀비, 차량에게 대미지를 준다. (폭탄, 드럼통, 차량이 같이 쓴다.)
public static class ExplosionDamage
{
    public static void Apply(Vector3 center, float range, float damage)
    {
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(center, range, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider col in colliders)
        {
            Vector3 closestPoint = col.ClosestPoint(center);   // 폭발 중심에서 가장 가까운 콜라이더 표면 위치

            DamageInfo damageInfo = new DamageInfo(damage, closestPoint, (center - closestPoint).normalized);

            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }

        // 차량은 별도 레이어가 없어서(카메라와 미니맵이 레이어로 그리는 범위를 바꾸지 않으려고) 목록으로 따로 전달한다.
        DestructibleCar.ApplyExplosion(center, range, damage);
    }
}
