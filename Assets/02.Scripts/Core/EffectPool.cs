using System.Collections.Generic;
using UnityEngine;

// 이펙트 풀: 이펙트 프리팹(ParticleSystem)마다 풀을 하나씩 만들어 공유한다.
// 총알 자국이나 폭발처럼 자주 터지는 이펙트가 씬에 쌓이지 않고, 끝나면 재사용된다.
public class EffectPool
{
    private const int DefaultCapacity = 10;
    private const int MaxSize = 50;

    private static readonly Dictionary<ParticleSystem, EffectPool> Pools = new Dictionary<ParticleSystem, EffectPool>();

    private readonly PrefabPool<PooledEffect> _pool;

    private EffectPool(ParticleSystem prefab)
    {
        _pool = new PrefabPool<PooledEffect>(parent => CreateEffect(prefab, parent), $"EffectPool_{prefab.name}", DefaultCapacity, MaxSize);
    }

    // 이 프리팹의 풀을 돌려준다. 없거나 씬이 바뀌어 보관함이 사라졌다면 새로 만든다.
    public static EffectPool For(ParticleSystem prefab)
    {
        if (!Pools.TryGetValue(prefab, out EffectPool pool) || !pool._pool.IsAlive)
        {
            pool = new EffectPool(prefab);
            Pools[prefab] = pool;
        }

        return pool;
    }

    // 이펙트를 꺼내 그 자리에서 재생한다. 재생이 끝나면 스스로 풀에 돌아간다.
    public void Play(Vector3 position, Quaternion rotation, float scale = 1f)
    {
        PooledEffect effect = _pool.Get(position, rotation);
        effect.transform.localScale = Vector3.one * scale;
    }

    // 프리팹을 복제하고 풀링에 필요한 컴포넌트를 붙인다. (보관함 아래에 만들어 꺼낼 때 활성화된다)
    private static PooledEffect CreateEffect(ParticleSystem prefab, Transform parent)
    {
        ParticleSystem instance = Object.Instantiate(prefab, parent);
        return instance.gameObject.AddComponent<PooledEffect>();
    }
}
