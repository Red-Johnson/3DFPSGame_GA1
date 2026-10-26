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

    // 다른 콜라이더와 부딛힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        // 같은 프레임에 여러 번 부딛혀도 한 번만 터진다.
        if (_hasExploded) return;
        _hasExploded = true;

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        ApplyAreaDamage();

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

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다.
        // Physics.OverlapSpere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        // 1. 드럼통과 좀비들에게 대미지를 준다.
        foreach (Collider col in colliders)
        {
            DamageInfo damageInfo = new()
            {
                Value = _damage,
                HitPoint = col.ClosestPoint(transform.position),  // 폭발 중심에서 가장 가까운 콜라이더 표면 위치
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized
            };


            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}
