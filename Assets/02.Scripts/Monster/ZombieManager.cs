using System;
using UnityEngine;

// 좀비 수 관리자: 좀비가 등록되면 세고, 죽으면 줄인다. 수가 바뀔 때마다 구독자(UI 등)에게 알린다.
// 좀비는 미리 배치되었든 스포너가 만들었든 Start에서 Register만 호출하면 집계된다.
public class ZombieManager : MonoBehaviour
{
    private static ZombieManager _instance = null;
    public static ZombieManager Instance => _instance;

    [SerializeField] private int _maxCount = 50;    // 동시에 존재할 수 있는 최대 좀비 수

    private int _aliveCount = 0;

    public int AliveCount => _aliveCount;
    public int MaxCount => _maxCount;
    public bool HasRoom => _aliveCount < _maxCount;

    // 남은 좀비 수, 최대 좀비 수
    public event Action<int, int> OnCountChanged;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public void Register(Zombie zombie)
    {
        _aliveCount++;
        zombie.OnDied += OnZombieDied;

        NotifyCountChanged();
    }

    private void OnZombieDied()
    {
        _aliveCount--;

        NotifyCountChanged();
    }

    private void NotifyCountChanged()
    {
        OnCountChanged?.Invoke(_aliveCount, _maxCount);
    }
}
