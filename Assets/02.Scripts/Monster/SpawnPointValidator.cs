using UnityEngine;
using UnityEngine.AI;

// 스폰 위치 도우미: 스포너들이 같이 쓰는 위치 판정과 계산을 모았다.
// NavMesh는 추격할 때 웬만한 길을 다 건너도록 지붕, 건물 안까지 덮고 있어서, 스폰만 따로 걸러 낸다.
// - 높이가 지면(Ground) 높이에서 허용치 이내여야 한다. (높은 곳 위 제외)
// - 머리 위가 건물 지붕 등으로 막혀 있지 않아야 한다. (건물 안이나 건물 중간 제외)
public static class SpawnPointValidator
{
    private const float RayStartHeight = 100f;      // 점 위 이 높이에서 아래로 쏜다.
    private const float SurfaceTolerance = 0.5f;    // 이 높이까지 솟은 것은 바닥(연석 등)으로 본다.
    private const int HitBufferSize = 32;
    private const string MapName = "Map";
    private const string GroundName = "Ground";

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];

    // 후보 위치를 NavMesh 위로 보정하고, 도로나 땅 위인지 확인한다. 통과하면 보정된 위치를 돌려준다.
    // sampleDistance: NavMesh 위로 보정하는 최대 거리, groundY: 지면 높이, maxHeightAboveGround: 지면에서 허용하는 최대 높이
    public static bool TrySnapToGround(Vector3 candidate, float sampleDistance, float groundY, float maxHeightAboveGround, out Vector3 position)
    {
        position = Vector3.zero;

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas)) return false;
        if (!IsGroundLevel(hit.position, groundY, maxHeightAboveGround)) return false;

        position = hit.position;
        return true;
    }

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

    // Ground 범위 안의 무작위 위치. 높이는 Ground 윗면이다. (NavMesh 위로 보정하기 전의 후보)
    public static Vector3 RandomPointInGround(Bounds groundBounds)
    {
        return new Vector3(
            Random.Range(groundBounds.min.x, groundBounds.max.x),
            groundBounds.max.y,
            Random.Range(groundBounds.min.z, groundBounds.max.z));
    }

    // 높이를 무시한 평면 거리
    public static float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // 인스펙터에 연결된 Ground를 그대로 쓰고, 비어 있으면 Map 아래에서 찾아 경고를 남긴다. 못 찾으면 null.
    public static Renderer ResolveGround(Renderer assigned, string ownerName)
    {
        if (assigned != null) return assigned;

        Renderer found = FindGround();
        GameLogger.LogWarning($"[{ownerName}] _ground가 연결되지 않아 씬에서 찾았습니다. 인스펙터에서 연결하세요.");
        return found;
    }

    // 인스펙터에 연결된 플레이어를 그대로 쓰고, 비어 있으면 Player 태그로 찾아 경고를 남긴다. 못 찾으면 null.
    public static Transform ResolvePlayer(Transform assigned, string ownerName)
    {
        if (assigned != null) return assigned;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return null;

        GameLogger.LogWarning($"[{ownerName}] _player가 연결되지 않아 Player 태그로 찾았습니다. 인스펙터에서 연결하세요.");
        return playerObject.transform;
    }

    // Map 아래의 Ground 렌더러를 찾는다. 없으면 null.
    public static Renderer FindGround()
    {
        GameObject map = GameObject.Find(MapName);
        if (map == null) return null;

        foreach (Renderer candidate in map.GetComponentsInChildren<Renderer>())
        {
            if (candidate.name == GroundName)
            {
                return candidate;
            }
        }

        return null;
    }
}
