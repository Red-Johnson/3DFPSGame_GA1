using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    private const int ZombiePoolDefaultCapacity = 10;

    private readonly List<ZombieSpawner> _spawners = new List<ZombieSpawner>();
    private PrefabPool<Zombie> _zombiePool;

    private void Start()
    {
        // 동시에 존재할 수 있는 좀비 수만큼만 풀에 보관한다.
        _zombiePool = new PrefabPool<Zombie>(_zombiePrefab, "ZombiePool", ZombiePoolDefaultCapacity, ZombieManager.Instance.MaxCount);

        if (_ground == null)
        {
            _ground = FindGround();
            GameLogger.LogWarning("[SpawnerManager] _ground가 연결되지 않아 씬에서 찾았습니다. 인스펙터에서 연결하세요.");
        }

        // Ground를 못 찾으면 스포너 위치를 정할 수 없으니, 오류를 남기고 멈춘다.
        if (_ground == null)
        {
            GameLogger.LogError("[SpawnerManager] Ground를 찾지 못했습니다. _ground를 직접 연결하거나 Map 아래에 Ground를 두세요.");
            enabled = false;
            return;
        }

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            GameLogger.LogWarning("[SpawnerManager] _player가 연결되지 않아 Player 태그로 찾았습니다. 인스펙터에서 연결하세요.");
        }

        StartCoroutine(SpawnerCreate_Coroutine());
    }

    private IEnumerator SpawnerCreate_Coroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_spawnerInterval);

        while (true)
        {
            yield return wait;

            // 게임이 진행 중이 아니면 진행 중이 될 때까지 기다렸다가 만든다.
            yield return new WaitUntil(IsGamePlaying);

            if (_spawners.Count >= _maxSpawnerCount) continue;

            if (TryGetSpawnerPosition(out Vector3 position))
            {
                CreateSpawner(position);
            }
        }
    }

    private bool IsGamePlaying()
    {
        return GameManager.Instance.IsPlaying;
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
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                Random.Range(bounds.min.z, bounds.max.z));

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas)) continue;

            if (GetFlatDistance(hit.position, _player.position) < _playerSafeRadius) continue;
            if (IsNearOtherSpawner(hit.position)) continue;

            position = hit.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsNearOtherSpawner(Vector3 position)
    {
        foreach (ZombieSpawner spawner in _spawners)
        {
            if (GetFlatDistance(position, spawner.transform.position) < _minSpawnerDistance)
            {
                return true;
            }
        }

        return false;
    }

    // 높이를 무시한 평면 거리
    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Renderer FindGround()
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
