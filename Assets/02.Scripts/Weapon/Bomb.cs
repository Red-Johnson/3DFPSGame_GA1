using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

// 던지면 회전하며 날아가고, 처음 부딛힌 뒤 일정 시간이 지나면 폭발 이펙트를 남기고 사라지는 폭탄
// 부딛힌 뒤에는 폭발할 때까지 물리 법칙에 따라 구른다.
public class Bomb : MonoBehaviour, IPoolable<Bomb>
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _damage = 100;

    [Header("신관")]
    [SerializeField] private float _fuseTime = 1.5f;            // 처음 부딛힌 뒤 폭발까지 걸리는 시간

    [Header("회전")]
    [SerializeField] private float _spinSpeed = 12f;            // 던질 때 주는 회전 속도(rad/s)
    [SerializeField, Range(0f, 1f)] private float _spinRandomness = 0.3f;   // 회전축이 흔들리는 정도
    [SerializeField] private float _landedAngularDamping = 1f;  // 부딛힌 뒤 구르는 회전이 줄어드는 정도

    [Header("구르기")]
    [SerializeField] private float _landedLinearDamping = 2.5f; // 부딛힌 뒤 이동 속도가 줄어드는 정도. 클수록 빨리 멈춘다.

    private Rigidbody _rigidbody;

    // 풀에서 꺼낸 폭탄이면 값이 있다. 직접 배치한 폭탄은 null이라 Destroy로 없앤다.
    private IObjectPool<Bomb> _pool;
    private bool _fuseStarted;
    private float _flyingAngularDamping;
    private float _flyingLinearDamping;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _flyingAngularDamping = _rigidbody.angularDamping;
        _flyingLinearDamping = _rigidbody.linearDamping;
    }

    // 꺼냈을 때마다 이전에 던지던 속도와 신관 상태를 지운다.
    private void OnEnable()
    {
        _fuseStarted = false;
        _rigidbody.angularDamping = _flyingAngularDamping;
        _rigidbody.linearDamping = _flyingLinearDamping;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void SetPool(IObjectPool<Bomb> pool)
    {
        _pool = pool;
    }

    // 힘을 한 번에 가해서 던지고, 날아가는 방향에 수직인 축으로 회전시킨다.
    public void Launch(Vector3 impulse)
    {
        _rigidbody.AddForce(impulse, ForceMode.Impulse);

        // 앞으로 뒤집히며 날아가는 회전축에 약간의 무작위를 섞는다.
        Vector3 axis = Vector3.Cross(Vector3.up, impulse);
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Random.onUnitSphere;
        }

        axis = (axis.normalized + Random.insideUnitSphere * _spinRandomness).normalized;
        _rigidbody.AddTorque(axis * _spinSpeed, ForceMode.VelocityChange);
    }

    // 다른 콜라이더와 부딛힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        // 처음 부딛힌 순간에만 신관에 불을 붙인다. 이후에는 폭발할 때까지 굴러간다.
        if (_fuseStarted) return;
        _fuseStarted = true;

        // 공중에서 얻은 속도와 회전이 땅에서 서서히 줄어 멈추도록 한다.
        _rigidbody.angularDamping = _landedAngularDamping;
        _rigidbody.linearDamping = _landedLinearDamping;

        StartCoroutine(Fuse_Coroutine());
    }

    private IEnumerator Fuse_Coroutine()
    {
        yield return new WaitForSeconds(_fuseTime);
        Explode();
    }

    private void Explode()
    {
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
