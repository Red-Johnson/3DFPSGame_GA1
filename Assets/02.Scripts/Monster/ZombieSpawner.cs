using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 좀비 스포너 한 개의 설정. SpawnerManager가 채워서 Init으로 넘긴다.
public struct ZombieSpawnSettings
{
    public int SpawnCount;                  // 좀비 생성 기회 횟수
    public float SpawnInterval;             // 좀비 생성 간격
    public float SpawnRadius;               // 스포너 주변 좀비 생성 반경
    public float NavMeshSampleDistance;     // NavMesh 위치 보정 최대 거리
}

// 좀비 스포너: 일정 간격으로 주변 무작위 위치에 좀비를 한 마리씩 생성한다.
// 생성 기회를 모두 쓰면(건너뛴 것 포함) 스스로 사라진다. SpawnerManager가 만들고 Init으로 설정을 넘긴다.
public class ZombieSpawner : MonoBehaviour
{
    private ZombieSpawnSettings _settings;
    private PrefabPool<Zombie> _zombiePool;
    private Transform _target;

    // 스포너가 사라질 때 구독자(SpawnerManager)에게 알린다.
    public event Action<ZombieSpawner> OnFinished;

    // target: 좀비가 쫓아갈 대상(플레이어)
    public void Init(ZombieSpawnSettings settings, PrefabPool<Zombie> zombiePool, Transform target)
    {
        _settings = settings;
        _zombiePool = zombiePool;
        _target = target;

        StartCoroutine(Spawn_Coroutine());
    }

    private IEnumerator Spawn_Coroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_settings.SpawnInterval);

        for (int i = 0; i < _settings.SpawnCount; i++)
        {
            yield return wait;

            // 게임이 진행 중이 아니면 기회를 쓰지 않고 진행 중이 될 때까지 기다린다.
            yield return new WaitUntil(IsGamePlaying);

            TrySpawnZombie();
        }

        OnFinished?.Invoke(this);
        Destroy(gameObject);
    }

    private bool IsGamePlaying()
    {
        return GameManager.Instance.IsPlaying;
    }

    // 생성 기회 한 번. 상한에 도달했거나 자리를 못 찾으면 건너뛴다.
    private void TrySpawnZombie()
    {
        if (!ZombieManager.Instance.HasRoom) return;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * _settings.SpawnRadius;
        Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

        // NavMesh 위의 지면 높이로 맞춘다. 높이가 어긋나 허공/바닥에 끼는 것을 막는다.
        // 피벗과 바닥 사이 높이는 NavMeshAgent의 Base Offset이 맞춰 준다.
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _settings.NavMeshSampleDistance, NavMesh.AllAreas)) return;

        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        CreateZombie(hit.position, rotation);
    }

    // 풀에서 좀비를 꺼내 쫓아갈 대상을 알려 준다. 위치를 잡은 뒤 활성화되므로 NavMeshAgent가 NavMesh에 정상적으로 붙는다.
    private void CreateZombie(Vector3 position, Quaternion rotation)
    {
        Zombie zombie = _zombiePool.Get(position, rotation);
        zombie.SetTarget(_target);
    }
}
