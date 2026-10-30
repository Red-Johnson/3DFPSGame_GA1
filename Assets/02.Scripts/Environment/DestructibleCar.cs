using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 부서지는 차량: 체력에 따라 효과가 단계로 바뀌고, 체력이 0이 되면 불이 붙었다가 폭발한 뒤 불이 붙은 잔해로 남는다.
// - 체력이 연기 시작 체력(_smokeStartHealth)을 넘으면(기본 70 초과) 아무 효과가 없다.
// - 체력이 그 이하이고 0보다 크면 검은 연기(VFX_BlackSmoke)가 난다.
// - 체력이 0이 되면 연기가 같은 위치에서 불(VFX_Fire)로 바뀌고, 폭발까지의 시간(_burnTimeBeforeExplosion, 기본 2초)이 지나면
//   폭발 이펙트를 한 번 내고 주변에 폭발 대미지를 준다. 그 뒤로도 불은 계속 탄다.
// - 폭발 대미지는 ExplosionDamage를 통해 다른 차량에도 전달되어 연쇄 폭발이 일어날 수 있다.
// 차량은 리지드바디가 없는 메시 콜라이더라 드럼통처럼 튀어오르지 않고 제자리에 남는다.
// 연기와 불은 보닛(차량 앞부분)에서 나온다. 앞쪽은 앞바퀴와 뒷바퀴 위치로 알아내고, 위치와 크기는 인스펙터에서 보정한다.
// 폭발 반경 하나(_explosionRange)가 폭발 대미지 범위이면서 이펙트 안의 충격파(shockwave) 크기이기도 하다. 이펙트는 충격파 반지름이 폭발 반경과 같아지도록 크기를 맞춘다.
public class DestructibleCar : MonoBehaviour, IDamageable
{
    private const string WheelKeyword = "Wheel";                // 바퀴 오브젝트 이름에 들어 있는 글자
    private const string FrontWheelKeyword = "Front";           // 앞바퀴 오브젝트 이름에 들어 있는 글자
    private const string RearWheelKeyword = "Rear";             // 뒷바퀴 오브젝트 이름에 들어 있는 글자
    private const float ShockwaveSampleStep = 0.02f;            // 충격파가 가장 커지는 크기를 찾을 때 곡선을 보는 간격
    private const float EffectDestroyMargin = 0.5f;             // 폭발 이펙트가 끝난 뒤 지우기 전에 더 기다리는 시간

    // 씬에 있는 차량 목록. 폭발 대미지가 차량에도 닿도록 ExplosionDamage가 이 목록을 쓴다.
    private static readonly List<DestructibleCar> Cars = new List<DestructibleCar>();

    [Header("스탯")]
    [SerializeField] private float _health = 100f;
    [SerializeField] private float _smokeStartHealth = 70f;    // 체력이 이 값 이하(0보다 클 때)가 되면 검은 연기가 난다.
    [SerializeField] private float _explosionDamage = 100f;     // 폭발이 주변에 주는 대미지
    [SerializeField] private float _explosionRange = 8f;        // 폭발 대미지 범위이자 충격파 이펙트의 반지름(m)
    [SerializeField] private float _burnTimeBeforeExplosion = 2f;   // 체력이 0이 되어 불이 붙은 뒤 폭발하기까지의 시간(초)

    [Header("이펙트")]
    [SerializeField] private ParticleSystem _blackSmokePrefab;
    [SerializeField] private ParticleSystem _firePrefab;
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private string _shockwaveName = "Shockwave";   // 폭발 이펙트 안에서 충격파로 쓰는 파티클 이름
    [SerializeField] private float _smokeScale = 3f;
    [SerializeField] private float _fireScale = 3f;

    [Header("연기와 불의 위치 (보닛)")]
    [SerializeField, Range(0f, 1f)] private float _effectForwardRatio = 0.6f;   // 차량 중심에서 앞쪽 끝까지 거리 중 얼마나 앞으로 갈지
    [SerializeField, Range(0f, 1f)] private float _effectHeightRatio = 0.6f;    // 차량 바닥에서 지붕까지 높이 중 어느 높이에 둘지(보닛 높이)
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;              // 위 위치에 더하는 보정(월드 방향)

    private Collider _bodyCollider;
    private Vector3 _effectPosition;     // 연기와 불이 나오는 월드 위치. 파티클이 붙기 전에 한 번만 계산해서 위치가 흔들리지 않게 한다.
    private Vector3 _centerPosition;     // 차량 중심의 월드 위치. 폭발 중심이다.
    private ParticleSystem _smoke;
    private ParticleSystem _fire;
    private bool _isBurning;

    private bool IsDestroyed => _health <= 0f;

    private void Awake()
    {
        _bodyCollider = GetComponent<Collider>();
        CalculateEffectPositions();
    }

    private void OnEnable()
    {
        Cars.Add(this);
    }

    private void OnDisable()
    {
        Cars.Remove(this);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        // 이미 체력이 0이 되어 불이 붙었으면(폭발 전후 모두) 더 이상 대미지를 받지 않는다.
        if (IsDestroyed) return;

        _health = Mathf.Max(0f, _health - damageInfo.Value);

        if (IsDestroyed)
        {
            if (!_isBurning)
            {
                _isBurning = true;
                StartCoroutine(Burn_Coroutine());
            }

            return;
        }

        // 체력이 연기 시작 체력 이하로 내려가면 검은 연기를 낸다. (한 번만)
        if (_health <= _smokeStartHealth && _smoke == null)
        {
            _smoke = SpawnEffect(_blackSmokePrefab, _smokeScale);
        }
    }

    // 폭발 중심 주변의 모든 차량에게 대미지를 준다. 차량은 레이어로 걸러지지 않아서 ExplosionDamage가 이 함수를 따로 부른다.
    // 거리는 폭발 중심에서 차량 콜라이더 범위(박스)까지로 잰다.
    public static void ApplyExplosion(Vector3 center, float range, float damage)
    {
        foreach (DestructibleCar car in Cars)
        {
            if (car.IsDestroyed || car._bodyCollider == null) continue;

            Vector3 closestPoint = car._bodyCollider.bounds.ClosestPoint(center);
            if ((closestPoint - center).sqrMagnitude > range * range) continue;

            Vector3 normal = (center - closestPoint).sqrMagnitude > 0.0001f ? (center - closestPoint).normalized : Vector3.up;
            car.TakeDamage(new DamageInfo(damage, closestPoint, normal));
        }
    }

    // 체력이 0이 되면: 연기를 같은 자리의 불로 바꾸고, 정해진 시간 뒤에 폭발을 한 번 낸다. 불은 폭발 뒤에도 계속 탄다.
    private IEnumerator Burn_Coroutine()
    {
        // 1. 연기는 새로 나오지 않게 멈추고(남은 연기는 자연스럽게 사라진다), 같은 자리에서 불을 붙인다.
        ReplaceSmokeWithFire();

        // 2. 폭발하기 전에 불이 탄다.
        yield return new WaitForSeconds(_burnTimeBeforeExplosion);

        // 3. 폭발 이펙트를 한 번 재생한다. (충격파가 폭발 반경만큼 퍼지도록 크기를 맞춘다.)
        PlayExplosionEffect();

        // 4. 주위에 폭발을 전달한다. (좀비, 드럼통과 다른 차량에 대미지를 준다.)
        ExplosionDamage.Apply(_centerPosition, _explosionRange, _explosionDamage);
    }

    // 폭발 이펙트를 차량마다 한 번만 만들어 쓰고 끝나면 지운다. (풀을 쓰지 않는다.)
    // - 하위 파티클이 반복 재생(Looping)되면 폭발이 계속 되풀이되므로 모두 끈다.
    // - 하위 파티클이 루트 크기 배율을 따르도록(Hierarchy) 바꿔서 이펙트 전체가 같은 비율로 커지게 한다.
    private void PlayExplosionEffect()
    {
        if (_explosionEffectPrefab == null) return;

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, _centerPosition, Quaternion.identity);

        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        effect.transform.localScale = Vector3.one * GetExplosionEffectScale();
        Destroy(effect.gameObject, GetTotalPlayTime(effect));
    }

    // 충격파의 반지름이 폭발 반경과 같아지는 이펙트 크기 배율. 충격파를 찾지 못하면 원래 크기(1)로 낸다.
    private float GetExplosionEffectScale()
    {
        float baseRadius = GetShockwaveBaseRadius();
        if (baseRadius <= 0f)
        {
            GameLogger.LogWarning($"[DestructibleCar] 폭발 이펙트에서 {_shockwaveName} 파티클을 찾지 못해 이펙트 크기를 맞추지 못했습니다.");
            return 1f;
        }

        return _explosionRange / baseRadius;
    }

    // 이펙트 원래 크기(배율 1)에서 충격파가 가장 크게 퍼졌을 때의 반지름. 시작 크기에 시간에 따른 크기 곡선의 최대값을 곱한 지름의 절반이다.
    private float GetShockwaveBaseRadius()
    {
        foreach (ParticleSystem particle in _explosionEffectPrefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particle.name != _shockwaveName) continue;

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

    private void ReplaceSmokeWithFire()
    {
        if (_smoke != null)
        {
            _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // 남은 연기가 다 사라진 뒤에 지운다.
            Destroy(_smoke.gameObject, GetLongestLifetime(_smoke));
            _smoke = null;
        }

        _fire = SpawnEffect(_firePrefab, _fireScale);
    }

    // 연기와 불이 나오는 위치(보닛)와 폭발 중심을 정한다.
    // 차량 본체 메시 범위를 차량 기준 좌표로 구하고, 앞바퀴와 뒷바퀴 위치로 차량이 향하는 앞쪽을 알아낸다.
    private void CalculateEffectPositions()
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null) continue;

            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f));

                Vector3 local = transform.InverseTransformPoint(meshFilter.transform.TransformPoint(corner));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }

        if (float.IsInfinity(min.x))
        {
            // 메시를 찾지 못하면 차량 위치 위에서 낸다.
            _centerPosition = transform.position;
            _effectPosition = transform.position + _effectOffset;
            return;
        }

        Vector3 center = (min + max) * 0.5f;
        Vector3 extents = (max - min) * 0.5f;
        Vector3 forward = GetLocalForward();

        // 차량 앞쪽 끝까지의 거리(차량 기준 박스를 앞쪽 방향으로 잰 반길이)
        float halfLength = (Mathf.Abs(forward.x) * extents.x) + (Mathf.Abs(forward.z) * extents.z);

        Vector3 hood = new Vector3(center.x, min.y + ((max.y - min.y) * _effectHeightRatio), center.z) + (forward * (halfLength * _effectForwardRatio));

        _centerPosition = transform.TransformPoint(center);
        _effectPosition = transform.TransformPoint(hood) + _effectOffset;
    }

    // 차량이 향하는 앞쪽(차량 기준 수평 방향). 뒷바퀴에서 앞바퀴로 향하는 방향이고, 바퀴를 찾지 못하면 차량의 앞(+Z)을 쓴다.
    private Vector3 GetLocalForward()
    {
        Vector3 front = Vector3.zero;
        Vector3 rear = Vector3.zero;
        int frontCount = 0;
        int rearCount = 0;

        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child == transform || !child.name.Contains(WheelKeyword)) continue;

            Vector3 local = transform.InverseTransformPoint(child.position);
            if (child.name.Contains(FrontWheelKeyword))
            {
                front += local;
                frontCount++;
            }
            else if (child.name.Contains(RearWheelKeyword))
            {
                rear += local;
                rearCount++;
            }
        }

        if (frontCount == 0 || rearCount == 0) return Vector3.forward;

        Vector3 direction = (front / frontCount) - (rear / rearCount);
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    // 이펙트 프리팹을 차량의 자식으로 만들어 연기와 불이 차량을 따라다니게 한다. 프리팹이 없으면 아무것도 하지 않는다.
    private ParticleSystem SpawnEffect(ParticleSystem prefab, float scale)
    {
        if (prefab == null) return null;

        ParticleSystem effect = Instantiate(prefab, _effectPosition, Quaternion.identity, transform);
        effect.transform.localScale = Vector3.one * scale;
        return effect;
    }

    // 파티클이 나온 뒤 사라지기까지 가장 오래 걸리는 시간
    private static float GetLongestLifetime(ParticleSystem effect)
    {
        float longest = 0f;
        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>())
        {
            longest = Mathf.Max(longest, particle.main.startLifetime.constantMax);
        }

        return longest;
    }

    // 반복하지 않는 이펙트가 시작해서 완전히 끝나기까지의 시간(시작 지연 + 재생 시간 + 파티클 수명)
    private static float GetTotalPlayTime(ParticleSystem effect)
    {
        float longest = 0f;
        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particle.main;
            longest = Mathf.Max(longest, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
        }

        return longest + EffectDestroyMargin;
    }
}
