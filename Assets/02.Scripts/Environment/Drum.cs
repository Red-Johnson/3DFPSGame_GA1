using System.Collections;
using UnityEngine;

// 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour, IDamageable
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _damage = 100f;
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destroyDelay = 3f;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _explosionDelay = 0.2f;
    [SerializeField] private float _explosionEffectScale = 10f;     // 폭발 이펙트 크기 배율

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0)
        {
            return;
        }

        _health -= damageInfo.Value;

        if (_health <= 0)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 0. 폭발하기전에 대기
        yield return new WaitForSeconds(_explosionDelay);

        // 1. 폭발 이펙트를 풀에서 꺼내 재생한다. (끝나면 스스로 돌아간다.)
        EffectPool.For(_explosionEffectPrefab).Play(transform.position, Quaternion.identity, _explosionEffectScale);

        // 2. 위로 튀어오르며 빙글빙글 돈다.
        _rigidbody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 주위에 폭발을 전달한다.(대미지를 준다.)
        ExplosionDamage.Apply(transform.position, _explosionRange, _damage);


        // 4. 딜레이 후 사라진다.
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}
