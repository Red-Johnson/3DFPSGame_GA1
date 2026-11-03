using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 보스 스포너: 좀비를 일정 수 처치할 때마다 Ground 안의 무작위 위치에 보스를 한 마리 생성한다.
// 씬에 하나만 두며 위치는 상관없다. 보스가 이미 살아 있어도 처치 수를 채우면 또 생성한다.
// 건물 안이나 지붕 위처럼 스폰할 수 없는 곳이면 성공할 때까지 다른 무작위 위치로 계속 재시도한다.
// (스폰 가능 여부는 SpawnPointValidator가 판정)
// 보스 자신을 처치한 것은 처치 수에 세지 않는다. (보스 프리팹의 ZombieStats로 구분)
// 보스의 체력, 공격력, 이동속도 같은 수치는 보스 프리팹이 연결한 ZombieStats 에셋에서 정한다.
public class BossSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _bossPrefab;
    [SerializeField] private int _killsPerBoss = 20;                // 보스가 한 마리 나오는 데 필요한 좀비 처치 수
    [SerializeField] private Transform _player;                     // 비워두면 Player 태그로 찾는다.

    [Header("위치 판정")]
    [SerializeField] private Renderer _ground;                      // 비워두면 Map 아래의 Ground를 찾는다.
    [SerializeField] private float _navMeshSampleDistance = 5f;     // 후보 위치를 NavMesh 위로 보정하는 최대 거리
    [SerializeField] private float _maxSpawnHeight = 1.5f;          // 지면에서 이 높이 이내만 스폰할 수 있다. (지붕 위, 건물 안 제외)
    [SerializeField] private float _playerSafeRadius = 20f;         // 플레이어 주변 이 반경 안은 피한다.
    [SerializeField] private int _positionTryCountPerFrame = 20;    // 위치를 찾을 때 한 프레임에 시도하는 횟수

    private int _killCount;
    private int _pendingBossCount;      // 아직 생성하지 못한 보스 수
    private Coroutine _spawnRoutine;

    private void Awake()
    {
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _player = playerObject.transform;
                GameLogger.LogWarning("[BossSpawner] _player가 연결되지 않아 Player 태그로 찾았습니다. 인스펙터에서 연결하세요.");
            }
        }

        if (_ground == null)
        {
            _ground = SpawnPointValidator.FindGround();
            GameLogger.LogWarning("[BossSpawner] _ground가 연결되지 않아 씬에서 찾았습니다. 인스펙터에서 연결하세요.");
        }
    }

    private void OnEnable()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.OnZombieKilled += OnZombieKilled;
        }
    }

    private void OnDisable()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.OnZombieKilled -= OnZombieKilled;
        }

        _spawnRoutine = null;   // 비활성화되면 코루틴도 멈춘다.
    }

    private void OnZombieKilled(Zombie zombie)
    {
        if (_bossPrefab == null || _killsPerBoss <= 0) return;
        if (zombie.Stats == _bossPrefab.Stats) return;      // 보스 처치는 세지 않는다.

        _killCount++;

        if (_killCount >= _killsPerBoss)
        {
            _killCount -= _killsPerBoss;
            _pendingBossCount++;

            if (_spawnRoutine == null)
            {
                _spawnRoutine = StartCoroutine(SpawnBoss_Coroutine());
            }
        }
    }

    // 밀린 보스를 한 마리씩 생성한다. 스폰 가능한 무작위 위치를 못 찾으면 다음 프레임에 다시 시도한다.
    private IEnumerator SpawnBoss_Coroutine()
    {
        while (_pendingBossCount > 0)
        {
            // 게임이 진행 중이 아니면 진행 중이 될 때까지 기다린다.
            yield return new WaitUntil(IsGamePlaying);

            if (TryGetRandomPosition(out Vector3 position))
            {
                SpawnBoss(position);
                _pendingBossCount--;
            }
            else
            {
                yield return null;      // 이번 프레임에는 못 찾았다. 다음 프레임에 다른 위치로 다시 시도한다.
            }
        }

        _spawnRoutine = null;
    }

    private bool IsGamePlaying()
    {
        return GameManager.Instance.IsPlaying;
    }

    // Ground 안의 무작위 위치를 시도 횟수만큼 찾아본다. 스폰 가능한 곳을 찾으면 true.
    private bool TryGetRandomPosition(out Vector3 position)
    {
        position = Vector3.zero;

        if (_ground == null)
        {
            GameLogger.LogError("[BossSpawner] Ground를 찾지 못해 스폰 위치를 고를 수 없습니다. _ground를 연결하세요.");
            return false;
        }

        Bounds bounds = _ground.bounds;

        for (int i = 0; i < _positionTryCountPerFrame; i++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                Random.Range(bounds.min.z, bounds.max.z));

            if (!TryGetSpawnPosition(candidate, out position)) continue;
            if (_player != null && GetFlatDistance(position, _player.position) < _playerSafeRadius) continue;

            return true;
        }

        return false;
    }

    // 후보 위치를 NavMesh 위로 보정하고, 도로나 땅 위인지 확인한다.
    private bool TryGetSpawnPosition(Vector3 candidate, out Vector3 position)
    {
        position = Vector3.zero;

        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas)) return false;

        if (!SpawnPointValidator.IsGroundLevel(hit.position, _ground.bounds.max.y, _maxSpawnHeight)) return false;

        position = hit.position;
        return true;
    }

    private void SpawnBoss(Vector3 position)
    {
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Zombie boss = Instantiate(_bossPrefab, position, rotation);
        if (_player != null)
        {
            boss.SetTarget(_player);
        }
    }

    // 높이를 무시한 평면 거리
    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
