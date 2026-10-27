using UnityEngine;

// 폭발 대미지: 폭발 중심 주변의 드럼통과 좀비에게 대미지를 준다. (폭탄과 드럼통이 같이 쓴다.)
public static class ExplosionDamage
{
    public static void Apply(Vector3 center, float range, float damage)
    {
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(center, range, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider col in colliders)
        {
            Vector3 closestPoint = col.ClosestPoint(center);   // 폭발 중심에서 가장 가까운 콜라이더 표면 위치

            DamageInfo damageInfo = new DamageInfo()
            {
                Value = damage,
                HitPoint = closestPoint,
                HitNormal = (center - closestPoint).normalized
            };

            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}
