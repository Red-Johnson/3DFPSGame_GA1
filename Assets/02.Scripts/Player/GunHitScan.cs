using UnityEngine;

// 총알 판정: 레이로 맞은 것을 찾고(플레이어 자신 제외, 약점 우선), 줌 중에는 조준 보정으로 총알 방향을 약점 쪽으로 끌어당긴다.
// 상태가 거의 없는 계산이라 PlayerGunFire가 일반 객체로 들고 쓴다. (씬에 붙일 것이 없다.)
public class GunHitScan
{
    // 조준점 바로 앞에서 끊기지 않도록 조준점까지의 거리에 더하는 여유(m)
    public const float AimPointMargin = 0.1f;

    private const int HitBufferSize = 64;

    private readonly Transform _owner;      // 이 총을 든 플레이어. 이 오브젝트의 콜라이더는 맞지 않는다.

    // 레이가 맞은 것을 담는 버퍼. 한 번 쓰고 바로 결과를 고르므로(안에서 다시 부르지 않는다) 돌려 써도 안전하다.
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];

    // RaycastAllSorted가 거리순으로 정리해 돌려주는 배열. 맞은 수가 더 많으면 그만큼 키운다.
    private RaycastHit[] _sortedHits = new RaycastHit[HitBufferSize];

    public GunHitScan(Transform owner)
    {
        _owner = owner;
    }

    // 가장 가까운 맞은 물체를 찾되 플레이어 자신의 콜라이더는 건너뛴다. (3인칭에서는 카메라가 플레이어 뒤에 있어 레이가 자신부터 맞기 때문)
    // preferWeakPoint가 켜져 있으면, 맞은 대상의 약점 판정(WeakPointHitbox)이 레이 위에 있을 때 몸통 대신 약점을 맞은 것으로 돌려준다.
    // (약점 판정은 몸통 콜라이더 안쪽에 있을 수 있어서, 가장 가까운 것만 고르면 항상 몸통이 먼저 잡히기 때문)
    public bool Raycast(Ray ray, float distance, bool preferWeakPoint, out RaycastHit result)
    {
        result = default;
        float nearest = float.MaxValue;
        bool found = false;

        // 매번 배열을 새로 만들지 않도록 버퍼를 재사용한다. 버퍼가 가득 차면 가까운 대상을 놓치지 않도록 새 배열로 다시 쏜다.
        int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = _hitBuffer;

        if (hitCount == HitBufferSize)
        {
            hits = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            hitCount = hits.Length;
        }

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.transform.root == _owner.root || hit.distance >= nearest) continue;

            nearest = hit.distance;
            result = hit;
            found = true;
        }

        if (!found || !preferWeakPoint) return found;

        IDamageable owner = result.collider.GetComponentInParent<IDamageable>();
        if (owner == null) return true;

        float nearestWeakPoint = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.distance >= nearestWeakPoint || hit.collider.GetComponent<WeakPointHitbox>() == null) continue;
            if (hit.collider.GetComponentInParent<IDamageable>() != owner) continue;

            nearestWeakPoint = hit.distance;
            result = hit;
        }

        return true;
    }

    // 레이가 지나는 모든 맞은 지점을 가까운 순서로 돌려준다. (관통하는 총이 쓴다.) 플레이어 자신의 콜라이더는 건너뛴다.
    // 돌려주는 배열은 다음에 이 메서드를 부를 때까지만 유효하고, 앞에서 count개만 의미가 있다.
    public int RaycastAllSorted(Ray ray, float distance, out RaycastHit[] hits)
    {
        int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit[] source = _hitBuffer;

        // 버퍼가 가득 차면 놓치는 것이 없도록 새 배열로 다시 쏜다.
        if (hitCount == HitBufferSize)
        {
            source = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            hitCount = source.Length;
        }

        if (_sortedHits.Length < hitCount)
        {
            _sortedHits = new RaycastHit[hitCount];
        }

        // 맞은 수가 적어서 삽입 정렬로 충분하다. (정렬하면서 자기 자신은 뺀다.)
        int count = 0;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = source[i];
            if (hit.collider.transform.root == _owner.root) continue;

            int position = count;
            while (position > 0 && _sortedHits[position - 1].distance > hit.distance)
            {
                _sortedHits[position] = _sortedHits[position - 1];
                position--;
            }

            _sortedHits[position] = hit;
            count++;
        }

        hits = _sortedHits;
        return count;
    }

    // 조준 보정: 줌 중에 조준점이 약점 근처로 지나가면 총알이 나가는 방향만 그 약점 쪽으로 끌어당긴다. (화면과 시점은 바뀌지 않는다.)
    // 조준선이 약점 중심에서 얼마나 벗어나 지나가는지를 약점이 있는 거리에서 미터로 재서, 반지름 안에서 가장 가까운 약점 하나를 고른다.
    // 고른 약점 쪽으로 그 어긋남의 세기(0~1)만큼을 지운다. (0.5면 어긋남이 절반으로, 1이면 약점 중심으로 맞춘다.)
    // 다른 물체에 가려진 약점, 사거리 밖의 약점, 뒤쪽의 약점은 고르지 않는다. 줌 중이 아니면 방향을 그대로 돌려준다.
    public Vector3 ApplyAimAssist(Vector3 origin, Vector3 direction, GunSpec spec, bool isZooming)
    {
        if (!spec.HasAimAssist || !isZooming || ZombieManager.Instance == null) return direction;

        float bestMiss = spec.AimAssistRadius;
        Vector3 bestDirection = direction;
        bool found = false;

        foreach (Zombie zombie in ZombieManager.Instance.AliveZombies)
        {
            foreach (WeakPointHitbox weakPoint in zombie.WeakPoints)
            {
                Vector3 center = weakPoint.Center;
                Vector3 toWeakPoint = center - origin;
                float distance = toWeakPoint.magnitude;
                if (distance > spec.Range) continue;

                // 조준선 위에서 약점과 가장 가까운 지점까지의 거리(미터). 약점이 뒤쪽에 있으면 제외한다.
                float along = Vector3.Dot(toWeakPoint, direction);
                if (along <= 0f) continue;

                float miss = (center - (origin + (direction * along))).magnitude;
                if (miss >= bestMiss) continue;

                // 약점으로 쏜 레이가 같은 좀비에게 먼저 닿아야 보이는 것이다. (벽이나 다른 물체에 가리면 제외)
                bool isVisible = Raycast(new Ray(origin, toWeakPoint), distance + AimPointMargin, true, out RaycastHit hit)
                    && hit.collider.GetComponentInParent<Zombie>() == zombie;
                if (!isVisible) continue;

                bestMiss = miss;
                bestDirection = toWeakPoint / distance;
                found = true;
            }
        }

        if (!found) return direction;

        return Vector3.Slerp(direction, bestDirection, spec.AimAssistStrength).normalized;
    }
}
