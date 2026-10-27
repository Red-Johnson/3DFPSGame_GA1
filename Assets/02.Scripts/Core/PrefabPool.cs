using System;
using UnityEngine;
using UnityEngine.Pool;

// 풀에서 꺼낸 오브젝트가 스스로 풀에 돌아갈 수 있도록 풀을 알려 주는 약속
public interface IPoolable<T> where T : Component
{
    void SetPool(IObjectPool<T> pool);
}

// 오브젝트 풀: 만들어 둔 오브젝트를 껐다 켜며 재사용한다.
// 꺼진 오브젝트는 비활성 상태인 보관함 아래에 둔다. 그래서 만들 때는 Awake/OnEnable이 실행되지 않고,
// Get에서 위치를 잡은 뒤 보관함 밖으로 꺼내는 순간 활성화(OnEnable)된다. (CharacterController, NavMeshAgent 위치 어긋남 방지)
public class PrefabPool<T> where T : Component, IPoolable<T>
{
    private readonly ObjectPool<T> _pool;
    private readonly Transform _inactiveRoot;

    // 프리팹을 복제해서 만드는 풀
    public PrefabPool(T prefab, string name, int defaultCapacity, int maxSize)
        : this(parent => UnityEngine.Object.Instantiate(prefab, parent), name, defaultCapacity, maxSize)
    {
    }

    // 프리팹 없이 코드로 만드는 풀. create는 받은 보관함을 부모로 해서 오브젝트를 만들어 돌려준다.
    public PrefabPool(Func<Transform, T> create, string name, int defaultCapacity, int maxSize)
    {
        GameObject root = new GameObject(name);
        root.SetActive(false);
        _inactiveRoot = root.transform;

        _pool = new ObjectPool<T>(
            createFunc: () =>
            {
                T item = create(_inactiveRoot);
                item.SetPool(_pool);
                return item;
            },
            actionOnRelease: item => item.transform.SetParent(_inactiveRoot),
            actionOnDestroy: item => UnityEngine.Object.Destroy(item.gameObject),
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }

    // 위치와 회전을 잡은 뒤 활성화해서 돌려준다.
    public T Get(Vector3 position, Quaternion rotation)
    {
        T item = _pool.Get();
        item.transform.SetPositionAndRotation(position, rotation);
        item.transform.SetParent(null);

        return item;
    }
}
