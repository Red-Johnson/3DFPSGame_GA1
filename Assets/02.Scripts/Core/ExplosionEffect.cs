using System.Collections.Generic;
using UnityEngine;

// 폭발 이펙트: 폭발 반경에 이펙트 크기를 맞춰 풀(EffectPool)에서 꺼내 재생한다.
// 이펙트 안의 충격파(shockwave) 파티클이 가장 크게 퍼졌을 때의 반지름이 폭발 반경과 같아지도록 크기 배율을 정한다.
// (폭발 대미지는 ExplosionDamage가 맡는다. 이 클래스는 보이는 연출만 맡는다.)
public static class ExplosionEffect
{
    private const float ShockwaveSampleStep = 0.02f;    // 충격파가 가장 커지는 크기를 찾을 때 곡선을 보는 간격

    // 프리팹마다 한 번만 재서 기억해 둔다. 충격파를 찾지 못한 프리팹은 0으로 기억한다.
    private static readonly Dictionary<(ParticleSystem prefab, string shockwaveName), float> BaseRadiusCache
        = new Dictionary<(ParticleSystem, string), float>();

    // position에서 충격파가 radius까지 퍼지는 크기로 폭발 이펙트를 재생한다. 충격파를 찾지 못하면 원래 크기(1)로 낸다.
    public static void PlayFitted(ParticleSystem prefab, Vector3 position, float radius, string shockwaveName)
    {
        if (prefab == null)
        {
            GameLogger.LogWarning("[ExplosionEffect] 폭발 이펙트 프리팹이 연결되지 않아 이펙트를 재생하지 않습니다.");
            return;
        }

        EffectPool.For(prefab, true).Play(position, Quaternion.identity, GetScale(prefab, radius, shockwaveName));
    }

    // 충격파의 반지름이 radius와 같아지는 이펙트 크기 배율
    private static float GetScale(ParticleSystem prefab, float radius, string shockwaveName)
    {
        (ParticleSystem, string) key = (prefab, shockwaveName);
        if (!BaseRadiusCache.TryGetValue(key, out float baseRadius))
        {
            baseRadius = MeasureShockwaveBaseRadius(prefab, shockwaveName);
            BaseRadiusCache[key] = baseRadius;
        }

        if (baseRadius <= 0f)
        {
            GameLogger.LogWarning($"[ExplosionEffect] 폭발 이펙트 '{prefab.name}'에서 {shockwaveName} 파티클을 찾지 못해 이펙트 크기를 맞추지 못했습니다.");
            return 1f;
        }

        return radius / baseRadius;
    }

    // 이펙트 원래 크기(배율 1)에서 충격파가 가장 크게 퍼졌을 때의 반지름. 시작 크기에 시간에 따른 크기 곡선의 최대값을 곱한 지름의 절반이다.
    private static float MeasureShockwaveBaseRadius(ParticleSystem prefab, string shockwaveName)
    {
        foreach (ParticleSystem particle in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particle.name != shockwaveName) continue;

            float size = particle.main.startSize.constantMax;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particle.sizeOverLifetime;
            if (sizeOverLifetime.enabled)
            {
                float peak = 0f;
                for (float t = 0f; t <= 1f; t += ShockwaveSampleStep)
                {
                    peak = Mathf.Max(peak, sizeOverLifetime.size.Evaluate(t));
                }

                size *= peak;
            }

            return size * 0.5f;
        }

        return 0f;
    }
}
