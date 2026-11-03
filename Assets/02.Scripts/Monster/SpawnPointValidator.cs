using UnityEngine;

// 스폰 위치 검사: NavMesh 위의 한 점이 "도로나 땅 위"인지 판정한다.
// NavMesh는 추격할 때 웬만한 길을 다 건너도록 지붕, 건물 안까지 덮고 있어서, 스폰만 따로 걸러 낸다.
// - 높이가 지면(Ground) 높이에서 허용치 이내여야 한다. (높은 곳 위 제외)
// - 머리 위가 건물 지붕 등으로 막혀 있지 않아야 한다. (건물 안이나 건물 중간 제외)
public static class SpawnPointValidator
{
    private const float RayStartHeight = 100f;      // 점 위 이 높이에서 아래로 쏜다.
    private const float SurfaceTolerance = 0.5f;    // 이 높이까지 솟은 것은 바닥(연석 등)으로 본다.
    private const int HitBufferSize = 32;

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];

    // navPoint: NavMesh 위의 후보 위치, groundY: 지면 높이, maxHeightAboveGround: 지면에서 허용하는 최대 높이
    public static bool IsGroundLevel(Vector3 navPoint, float groundY, float maxHeightAboveGround)
    {
        if (navPoint.y > groundY + maxHeightAboveGround) return false;

        Vector3 origin = navPoint + (Vector3.up * RayStartHeight);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, HitBuffer, RayStartHeight + 1f, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = HitBuffer[i];

            // 좀비와 플레이어는 지나다니는 것이라 막힌 것으로 보지 않는다.
            if (hit.collider.GetComponentInParent<Zombie>() != null) continue;
            if (hit.collider.transform.root.CompareTag("Player")) continue;

            // 후보 위치보다 위에 무언가 있으면 지붕 밑(건물 안)이거나 구조물 아래다.
            if (hit.point.y > navPoint.y + SurfaceTolerance) return false;
        }

        return true;
    }

    // Map 아래의 Ground 렌더러를 찾는다. 없으면 null.
    public static Renderer FindGround()
    {
        GameObject map = GameObject.Find("Map");
        if (map == null) return null;

        foreach (Renderer candidate in map.GetComponentsInChildren<Renderer>())
        {
            if (candidate.name == "Ground")
            {
                return candidate;
            }
        }

        return null;
    }
}
