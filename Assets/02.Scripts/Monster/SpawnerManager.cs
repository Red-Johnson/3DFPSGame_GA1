using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스포너 관리자: 일정 간격마다 Ground 위 무작위 위치에 좀비 스포너를 만든다.
// 플레이어 주변과 다른 스포너 주변은 피하고, 조건을 만족하는 자리를 못 찾으면 이번 생성은 건너뛴다.
public class SpawnerManager : MonoBehaviour
{
    [Header("스포너 생성")]
    [SerializeField] private float _spawnerInterval = 3f;       // 스포너 생성 간격
    [SerializeField] private int _maxSpawnerCount = 10;         // 동시에 존재할 수 있는 최대 스포너 수
    [SerializeField] private float _playerSafeRadius = 10f;     // 플레이어 주변 스폰 금지 반경
    [SerializeField] private float _minSpawnerDistance = 8f;    // 스포너 사이 최소 거리
    [SerializeField] private int _positionTryCount = 20;        // 위치 탐색 시도 횟수

    [Header("스포너 설정")]
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private int _zombiesPerSpawner = 5;        // 스포너 한 개당 좀비 생성 기회
    [SerializeField] private float _zombieSpawnInterval = 0.5f; // 좀비 생성 간격
    [SerializeField] private float _zombieSpawnRadius = 5f;     // 스포너 주변 좀비 생성 반경

    [Header("위치 판정")]
    [SerializeField] private Renderer _ground;                  // 비워두면 Map 아래의 Ground를 찾는다.
    [SerializeField] private Transform _player;                 // 비워두면 Player 태그로 찾는다.
    [SerializeField] private float _navMeshSampleDistance = 2f; // NavMesh 위치 보정 최대 거리
    [SerializeField] private float _maxSpawnHeight = 1.5f;      // 지면에서 이 높이 이내만 스폰할 수 있다. (지붕 위, 건물 안 제외)

    private const int ZombiePoolDefaultCapacity = 10;

    private readonly List<ZombieSpawner> _spawners = new List<ZombieSpawner>();
    private PrefabPool<Zombie> _zombiePool;

    private void Start()
    {
        _ground = SpawnPointValidator.ResolveGround(_ground, nameof(SpawnerManager));
        _player = SpawnPointValidator.ResolvePlayer(_player, nameof(SpawnerManager));

        // Ground나 플레이어를 못 찾으면 스포너 위치를 정할 수 없으니, 오류를 남기고 멈춘다.
        if (_ground == null || _player == null)
        {
            GameLogger.LogError("[SpawnerManager] Ground나 플레이어를 찾지 못했습니다. _ground, _player를 직접 연결하세요. (Ground는 Map 아래에, 플레이어는 Player 태그로 찾습니다.)");
            enabled = false;
            return;
        }

        // 동시에 존재할 수 있는 좀비 수만큼만 풀에 보관한다.
        _zombiePool = new PrefabPool<Zombie>(_zombiePrefab, "ZombiePool", ZombiePoolDefaultCapacity, ZombieManager.Instance.MaxCount);

        StartCoroutine(SpawnerCreate_Coroutine());
    }

    private IEnumerator SpawnerCreate_Coroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_spawnerInterval);

        while (true)
        {
            yield return wait;

            // 게임이 진행 중이 아니면 진행 중이 될 때까지 기다렸다가 만든다.
            yield return GameManager.WaitUntilPlaying;

            if (_spawners.Count >= _maxSpawnerCount) continue;

            if (TryGetSpawnerPosition(out Vector3 position))
            {
                CreateSpawner(position);
            }
        }
    }

    private void CreateSpawner(Vector3 position)
    {
        GameObject spawnerObject = new GameObject("ZombieSpawner");
        spawnerObject.transform.position = position;

        ZombieSpawner spawner = spawnerObject.AddComponent<ZombieSpawner>();
        spawner.OnFinished += OnSpawnerFinished;
        ZombieSpawnSettings settings = new ZombieSpawnSettings()
        {
            SpawnCount = _zombiesPerSpawner,
            SpawnInterval = _zombieSpawnInterval,
            SpawnRadius = _zombieSpawnRadius,
            NavMeshSampleDistance = _navMeshSampleDistance,
            GroundHeight = _ground.bounds.max.y,
            MaxSpawnHeight = _maxSpawnHeight,
        };

        spawner.Init(settings, _zombiePool, _player);

        _spawners.Add(spawner);
    }

    private void OnSpawnerFinished(ZombieSpawner spawner)
    {
        spawner.OnFinished -= OnSpawnerFinished;
        _spawners.Remove(spawner);
    }

    // Ground 안에서 규칙을 만족하는 위치를 찾는다. 시도 횟수 안에 못 찾으면 false.
    private bool TryGetSpawnerPosition(out Vector3 position)
    {
        Bounds bounds = _ground.bounds;

        for (int i = 0; i < _positionTryCount; i++)
        {
            Vector3 candidate = SpawnPointValidator.RandomPointInGround(bounds);

            if (!SpawnPointValidator.TrySnapToGround(candidate, _navMeshSampleDistance, bounds.max.y, _maxSpawnHeight, out Vector3 snapped)) continue;
            if (SpawnPointValidator.GetFlatDistance(snapped, _player.position) < _playerSafeRadius) continue;
            if (IsNearOtherSpawner(snapped)) continue;

            position = snapped;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsNearOtherSpawner(Vector3 position)
    {
        foreach (ZombieSpawner spawner in _spawners)
        {
            if (SpawnPointValidator.GetFlatDistance(position, spawner.transform.position) < _minSpawnerDistance)
            {
                return true;
            }
        }

        return false;
    }
}
