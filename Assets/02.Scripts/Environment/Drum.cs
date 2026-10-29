using System.Collections;
using UnityEngine;

// 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
// 생성될 때 외형 프리팹 목록에서 하나를 무작위로 골라 자식으로 붙인다. (목록이 비어 있으면 원래 외형 그대로)
// 충돌과 폭발은 이 오브젝트의 콜라이더와 리지드바디가 맡고, 외형 프리팹은 콜라이더 크기에 맞춰 크기와 위치를 보정한다.
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour, IDamageable
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("외형")]
    [SerializeField] private GameObject[] _visualPrefabs;               // 무작위로 하나가 외형으로 쓰인다.
    [SerializeField] private float _visualScaleMultiplier = 1f;         // 콜라이더 높이에 맞춘 뒤 곱하는 크기 배율

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

        ApplyRandomVisual();
    }

    // 외형 프리팹 중 하나를 무작위로 골라 붙이고, 기본 외형(메시 렌더러)은 숨긴다.
    private void ApplyRandomVisual()
    {
        GameObject prefab = PickVisualPrefab();
        if (prefab == null) return;

        // 콜라이더 범위를 기준으로 삼는다. (원래 외형과 같은 자리에 같은 높이로 맞추기 위해)
        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider == null) return;

        Bounds target = ownCollider.bounds;

        GameObject visual = Instantiate(prefab, transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        // 외형 프리팹에 딸린 콜라이더나 리지드바디는 물리를 방해하므로 지운다.
        foreach (Collider visualCollider in visual.GetComponentsInChildren<Collider>())
        {
            Destroy(visualCollider);
        }

        foreach (Rigidbody visualBody in visual.GetComponentsInChildren<Rigidbody>())
        {
            Destroy(visualBody);
        }

        if (!TryGetRendererBounds(visual, out Bounds bounds))
        {
            GameLogger.LogWarning($"[Drum] 외형 프리팹 '{prefab.name}'에 렌더러가 없어 크기를 맞추지 못했습니다.");
            return;
        }

        // 높이를 콜라이더에 맞춘다. (터지며 튀어오르는 느낌이 같도록)
        float fit = target.size.y / bounds.size.y * _visualScaleMultiplier;
        visual.transform.localScale *= fit;

        // 바닥 높이와 가로세로 중심을 콜라이더에 맞춘다.
        TryGetRendererBounds(visual, out bounds);
        visual.transform.position += new Vector3(
            target.center.x - bounds.center.x,
            target.min.y - bounds.min.y,
            target.center.z - bounds.center.z);

        // 기본 외형은 숨긴다.
        MeshRenderer ownRenderer = GetComponent<MeshRenderer>();
        if (ownRenderer != null)
        {
            ownRenderer.enabled = false;
        }
    }

    // 목록에서 비어 있지 않은 프리팹을 무작위로 하나 고른다. 없으면 null.
    private GameObject PickVisualPrefab()
    {
        if (_visualPrefabs == null || _visualPrefabs.Length == 0) return null;

        GameObject prefab = _visualPrefabs[UnityEngine.Random.Range(0, _visualPrefabs.Length)];
        if (prefab == null)
        {
            GameLogger.LogWarning("[Drum] 외형 프리팹 목록에 빈 칸이 있습니다. 기본 외형을 씁니다.");
        }

        return prefab;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
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
