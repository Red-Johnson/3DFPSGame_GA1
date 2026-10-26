using UnityEngine;

// Ground 가장자리에 보이지 않는 벽을 만들어 플레이어와 좀비가 밖으로 나가지 못하게 한다.
// Ground 오브젝트에 붙인다. (Renderer 경계 기준, 축 정렬된 평면을 가정)
[RequireComponent(typeof(Renderer))]
public class GroundBoundary : MonoBehaviour
{
    private const int IgnoreRaycastLayer = 2;   // 총알 Raycast가 벽에 맞지 않도록 한다.

    [SerializeField] private float _wallHeight = 10f;
    [SerializeField] private float _wallThickness = 1f;

    private void Awake()
    {
        Bounds bounds = GetComponent<Renderer>().bounds;

        GameObject walls = new GameObject("GroundBoundary_Walls");
        walls.layer = IgnoreRaycastLayer;

        float centerY = bounds.max.y + (_wallHeight * 0.5f);
        float widthX = bounds.size.x + (_wallThickness * 2f);
        float depthZ = bounds.size.z + (_wallThickness * 2f);

        // 왼쪽, 오른쪽
        AddWall(walls, new Vector3(bounds.min.x - (_wallThickness * 0.5f), centerY, bounds.center.z), new Vector3(_wallThickness, _wallHeight, depthZ));
        AddWall(walls, new Vector3(bounds.max.x + (_wallThickness * 0.5f), centerY, bounds.center.z), new Vector3(_wallThickness, _wallHeight, depthZ));

        // 앞, 뒤
        AddWall(walls, new Vector3(bounds.center.x, centerY, bounds.min.z - (_wallThickness * 0.5f)), new Vector3(widthX, _wallHeight, _wallThickness));
        AddWall(walls, new Vector3(bounds.center.x, centerY, bounds.max.z + (_wallThickness * 0.5f)), new Vector3(widthX, _wallHeight, _wallThickness));
    }

    // walls 오브젝트는 월드 원점(회전·크기 없음)에 두고, 콜라이더의 center로 위치를 잡는다.
    private void AddWall(GameObject walls, Vector3 center, Vector3 size)
    {
        BoxCollider wall = walls.AddComponent<BoxCollider>();
        wall.center = center;
        wall.size = size;
    }
}
