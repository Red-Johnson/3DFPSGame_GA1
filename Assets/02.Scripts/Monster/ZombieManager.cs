using System;
using System.Collections.Generic;
using UnityEngine;

// 좀비 수 관리자: 살아 있는 좀비를 등록해서 세고, 수가 바뀔 때마다 구독자(UI 등)에게 알린다.
// 좀비는 미리 배치되었든 스포너가 만들었든(풀에서 꺼냈든) 활성화될 때 Register, 죽거나 꺼질 때 Unregister를 호출한다.
[DefaultExecutionOrder(-100)]   // 다른 컴포넌트가 OnEnable에서 Instance를 써도 되도록 가장 먼저 초기화한다.
public class ZombieManager : MonoBehaviour
{
    private static ZombieManager _instance = null;
    public static ZombieManager Instance => _instance;

    [SerializeField] private int _maxCount = 50;    // 동시에 존재할 수 있는 최대 좀비 수

    // 같은 좀비가 여러 번 등록/해제되어도 한 번만 세도록 집합으로 관리한다.
    private readonly HashSet<Zombie> _aliveZombies = new HashSet<Zombie>();

    public int AliveCount => _aliveZombies.Count;
    public int MaxCount => _maxCount;
    public bool HasRoom => _aliveZombies.Count < _maxCount;

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
        if (_aliveZombies.Add(zombie))
        {
            NotifyCountChanged();
        }
    }

    public void Unregister(Zombie zombie)
    {
        if (_aliveZombies.Remove(zombie))
        {
            NotifyCountChanged();
        }
    }

    private void NotifyCountChanged()
    {
        OnCountChanged?.Invoke(_aliveZombies.Count, _maxCount);
    }
}
