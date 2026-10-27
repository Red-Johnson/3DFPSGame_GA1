using UnityEngine;
using UnityEngine.Pool;

// 파티클 이펙트: 재생이 끝나면 스스로 풀에 돌아간다. EffectPool이 이펙트 프리팹에 붙여서 만든다.
[RequireComponent(typeof(ParticleSystem))]
public class PooledEffect : MonoBehaviour, IPoolable<PooledEffect>
{
    private ParticleSystem _particle;
    private IObjectPool<PooledEffect> _pool;

    private void Awake()
    {
        _particle = GetComponent<ParticleSystem>();

        // 프리팹이 끝나면 스스로 파괴되게(Stop Action: Destroy) 설정돼 있어도, 풀에서 재사용해야 하므로 끈다.
        // 끄지 않으면 풀에 돌려보낸 오브젝트가 파괴되어 다음에 꺼낼 때 MissingReferenceException이 난다.
        ParticleSystem.MainModule main = _particle.main;
        main.stopAction = ParticleSystemStopAction.None;
    }

    // 꺼냈을 때마다 처음부터 다시 재생한다.
    private void OnEnable()
    {
        _particle.Clear(true);
        _particle.Play(true);
    }

    public void SetPool(IObjectPool<PooledEffect> pool)
    {
        _pool = pool;
    }

    private void Update()
    {
        // 자식 파티클까지 모두 끝났을 때만 돌려보낸다.
        if (_particle.IsAlive(true)) return;

        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
