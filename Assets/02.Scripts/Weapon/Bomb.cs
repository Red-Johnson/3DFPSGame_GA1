using UnityEngine;
using UnityEngine.Pool;

// 무언가에 부딛히면 폭발 이펙트를 남기고 사라지는 폭탄
public class Bomb : MonoBehaviour, IPoolable<Bomb>
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _damage = 100;

    private Rigidbody _rigidbody;

    // 풀에서 꺼낸 폭탄이면 값이 있다. 직접 배치한 폭탄은 null이라 Destroy로 없앤다.
    private IObjectPool<Bomb> _pool;
    private bool _hasExploded;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 꺼냈을 때마다 이전에 던지던 속도와 폭발 상태를 지운다.
    private void OnEnable()
    {
        _hasExploded = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void SetPool(IObjectPool<Bomb> pool)
    {
        _pool = pool;
    }

    // 힘을 한 번에 가해서 던진다.
    public void Launch(Vector3 impulse)
    {
        _rigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    // 다른 콜라이더와 부딛힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        // 같은 프레임에 여러 번 부딛혀도 한 번만 터진다.
        if (_hasExploded) return;
        _hasExploded = true;

        // 폭발 이펙트는 풀에서 꺼내 쓰고, 끝나면 스스로 돌아간다.
        EffectPool.For(_explosionEffectPrefab).Play(transform.position, Quaternion.identity);

        ExplosionDamage.Apply(transform.position, _explosionRange, _damage);

        // 폭탄을 풀로 돌려보낸다.
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
