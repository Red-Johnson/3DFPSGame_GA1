using System.Collections.Generic;
using UnityEngine;

// 이펙트 풀: 이펙트 프리팹(ParticleSystem)마다 풀을 하나씩 만들어 공유한다.
// 총알 자국이나 폭발처럼 자주 터지는 이펙트가 씬에 쌓이지 않고, 끝나면 재사용된다.
// 프리팹을 연결하지 않았으면(null) 이펙트를 재생하지 않고 경고만 남긴다. (호출한 쪽이 예외로 멈추지 않게)
public class EffectPool
{
    private const int DefaultCapacity = 10;
    private const int MaxSize = 50;

    // 같은 프리팹이라도 반복 재생 설정을 다르게 쓸 수 있도록 (프리팹, 설정)으로 풀을 나눈다.
    private static readonly Dictionary<(ParticleSystem prefab, bool oneShotUniformScale), EffectPool> Pools
        = new Dictionary<(ParticleSystem, bool), EffectPool>();

    private static readonly EffectPool Empty = new EffectPool();    // 프리팹이 없을 때 돌려주는, 아무것도 하지 않는 풀
    private static bool _hasWarnedNullPrefab;

    private readonly PrefabPool<PooledEffect> _pool;

    private EffectPool()
    {
    }

    private EffectPool(ParticleSystem prefab, bool oneShotUniformScale)
    {
        _pool = new PrefabPool<PooledEffect>(parent => CreateEffect(prefab, parent, oneShotUniformScale), $"EffectPool_{prefab.name}", DefaultCapacity, MaxSize);
    }

    // 이 프리팹의 풀을 돌려준다. 없거나 씬이 바뀌어 보관함이 사라졌다면 새로 만든다.
    // oneShotUniformScale: 켜면 하위 파티클의 반복 재생을 끄고, 크기 배율이 이펙트 전체에 같은 비율로 적용되게 한다. (폭발처럼 한 번만 터지고 크기를 맞춰 쓰는 이펙트용)
    public static EffectPool For(ParticleSystem prefab, bool oneShotUniformScale = false)
    {
        if (prefab == null)
        {
            if (!_hasWarnedNullPrefab)
            {
                _hasWarnedNullPrefab = true;
                GameLogger.LogWarning("[EffectPool] 이펙트 프리팹이 연결되지 않아 이펙트를 재생하지 않습니다.");
            }

            return Empty;
        }

        (ParticleSystem, bool) key = (prefab, oneShotUniformScale);
        if (!Pools.TryGetValue(key, out EffectPool pool) || !pool._pool.IsAlive)
        {
            pool = new EffectPool(prefab, oneShotUniformScale);
            Pools[key] = pool;
        }

        return pool;
    }

    // 이펙트를 꺼내 그 자리에서 재생한다. 재생이 끝나면 스스로 풀에 돌아간다.
    public void Play(Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (_pool == null) return;

        PooledEffect effect = _pool.Get(position, rotation);
        effect.transform.localScale = Vector3.one * scale;
    }

    // 프리팹을 복제하고 풀링에 필요한 컴포넌트를 붙인다. (보관함 아래에 만들어 꺼낼 때 활성화된다)
    private static PooledEffect CreateEffect(ParticleSystem prefab, Transform parent, bool oneShotUniformScale)
    {
        ParticleSystem instance = Object.Instantiate(prefab, parent);

        if (oneShotUniformScale)
        {
            foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particle.main;
                main.loop = false;                                          // 반복 재생되면 끝나지 않아 풀로 돌아가지 못한다.
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;     // 루트 크기 배율을 하위 파티클도 따르게 한다.
            }
        }

        return instance.gameObject.AddComponent<PooledEffect>();
    }
}
