using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 칼을 휘둘러 범위를 베는 근접 공격 (총 사격 PlayerGunFire를 대신한다.)
// 휘두르는 모션을 시작하고 타격 시점이 되면 범위 안의 피격 대상(IDamageable)에게 대미지를 주고 이펙트를 낸다.
// 이펙트 프리팹을 연결하면 이펙트가 그려지는 구 범위를, 비워두면 앞쪽 부채꼴 범위를 벤다.
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("공격")]
    [SerializeField] private float _damage = 15f;
    [SerializeField] private float _range = 3.5f;                   // 부채꼴 반지름
    [SerializeField, Range(10f, 360f)] private float _angle = 120f; // 부채꼴 각도
    [SerializeField] private float _attackInterval = 0.6f;          // 공격 간격. 휘두르는 모션 길이에 맞춘다.
    [SerializeField] private float _hitDelay = 0.2f;                // 휘두르기 시작부터 타격이 들어가기까지의 시간
    [SerializeField] private Vector3 _originOffset = new Vector3(0f, -0.1f, 0f);    // 부채꼴 중심(플레이어 위치 기준)
    [SerializeField] private LayerMask _hitLayers = ~0;

    [Header("이펙트")]
    [SerializeField] private ParticleSystem _slashEffectPrefab;     // 베기 이펙트 프리팹(에셋). 연결하면 이것을 쓰고, 비워두면 코드로 만든 부채꼴을 쓴다.
    [SerializeField] private float _slashEffectScale = 1.2f;                                // 프리팹 이펙트의 크기 배율
    [SerializeField] private Vector3 _slashEffectOffset = new Vector3(0f, 0.2f, 0.8f);      // 이펙트 위치(부채꼴 중심 기준, 바라보는 방향이 +Z)
    [SerializeField] private Vector3 _slashEffectRotation = Vector3.zero;                   // 이펙트 회전 보정(오일러 각도)
    [SerializeField, Range(0.1f, 2f)] private float _effectHitRadiusScale = 1f;             // 이펙트가 그려지는 범위에 곱해 판정 반경을 줄이거나 늘리는 보정
    [SerializeField] private SlashEffect _slashEffect;      // 프리팹이 없을 때 쓰는 부채꼴. 비워두면 자식으로 하나 만든다.

    private const float MeasureStep = 0.03f;            // 이펙트 범위를 잴 때 시뮬레이션하는 시간 간격
    private const float MeasureMaxTime = 3f;            // 이 시간까지만 잰다.

    private float _effectRadius;                        // 이펙트 중심에서 이펙트가 그려지는 가장 먼 거리(판정 반경)
    private float _attackTimer;
    private PlayerAnimator _playerAnimator;
    private PlayerBombFire _bombFire;
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();

    // 한 번 벤 결과 몬스터(좀비)가 하나라도 맞으면 알린다. 인자는 그중 죽은 몬스터가 있는지다.
    // (카메라 흔들림, 히트 스톱, 명중 표시가 구독한다. 이 클래스는 그 연출들을 모른다.)
    public event System.Action<bool> OnHit;

    // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 벨 수 없다.
    private bool IsBlockedByBomb => _bombFire != null && (_bombFire.IsCharging || _bombFire.IsThrowing);

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _bombFire = GetComponent<PlayerBombFire>();

        if (_slashEffectPrefab != null)
        {
            // 프리팹 이펙트를 쓰면 이펙트가 그려지는 구 범위를 재서 판정 반경으로 쓴다.
            _effectRadius = MeasureEffectRadius() * _effectHitRadiusScale;
            GameLogger.Log($"[PlayerMeleeAttack] 이펙트 판정 반경: {_effectRadius:F2}m");
        }
        else if (_slashEffect == null)
        {
            // 프리팹이 없으면 코드로 만든 부채꼴을 쓴다.
            GameObject effectObject = new GameObject("SlashEffect");
            effectObject.transform.SetParent(transform, false);
            _slashEffect = effectObject.AddComponent<SlashEffect>();
        }
    }

    // 이펙트를 임시로 하나 만들어 끝까지 시뮬레이션하면서, 이펙트 중심에서 가장 멀리 그려진 거리를 잰다.
    // 크기 배율과 회전 보정을 실제 재생과 똑같이 적용한다. 재지 못하면 부채꼴 반지름을 대신 쓴다.
    private float MeasureEffectRadius()
    {
        ParticleSystem instance = Instantiate(_slashEffectPrefab, Vector3.zero, Quaternion.Euler(_slashEffectRotation));
        instance.transform.localScale = Vector3.one * _slashEffectScale;

        ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>();
        float radius = 0f;

        instance.Simulate(0f, true, true, true);
        for (float time = 0f; time <= MeasureMaxTime; time += MeasureStep)
        {
            instance.Simulate(MeasureStep, true, false, true);

            foreach (ParticleSystemRenderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                if (bounds.size == Vector3.zero) continue;

                // 중심(원점)에서 경계 상자의 각 축 방향으로 가장 먼 거리
                Vector3 far = Vector3.Max(bounds.max, -bounds.min);
                radius = Mathf.Max(radius, far.x, far.y, far.z);
            }

            if (time > 0f && !instance.IsAlive(true)) break;
        }

        Destroy(instance.gameObject);

        if (radius <= 0f)
        {
            GameLogger.LogWarning("[PlayerMeleeAttack] 이펙트 범위를 재지 못해 부채꼴 반지름을 판정 반경으로 씁니다.");
            return _range;
        }

        return radius;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        _attackTimer -= Time.deltaTime;

        // 커서가 풀려 있는 동안은 GameInput이 입력 없음으로 돌려준다. 누르고 있으면 간격마다 계속 벤다.
        if (GameInput.FireHeld && _attackTimer <= 0f && !IsBlockedByBomb)
        {
            _attackTimer = _attackInterval;
            _playerAnimator.TriggerSlash();
            StartCoroutine(Slash_Coroutine());
        }
    }

    private IEnumerator Slash_Coroutine()
    {
        yield return new WaitForSeconds(_hitDelay);

        // 기다리는 사이 게임이 끝났으면 타격하지 않는다.
        if (!GameManager.Instance.IsPlaying) yield break;

        Slash();
    }

    // 이펙트가 그려지는 범위에 있는 대상에게 대미지를 주고 이펙트를 낸다.
    // - 프리팹 이펙트: 이펙트 중심에서 잰 구 범위 전체를 벤다. (이펙트가 플레이어를 감싸므로 방향을 따지지 않는다.)
    // - 부채꼴 이펙트: 바라보는 방향의 부채꼴 범위만 벤다.
    private void Slash()
    {
        Vector3 origin = transform.position + _originOffset;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion facing = Quaternion.LookRotation(forward);
        bool isSphere = _slashEffectPrefab != null;

        Vector3 center = origin;
        float radius = _range;

        if (isSphere)
        {
            center = origin + (facing * _slashEffectOffset);
            radius = _effectRadius;
            EffectPool.For(_slashEffectPrefab).Play(center, facing * Quaternion.Euler(_slashEffectRotation), _slashEffectScale);
        }
        else
        {
            _slashEffect.Play(origin, facing, _range, _angle);
        }

        _hitTargets.Clear();
        float halfAngle = _angle * 0.5f;
        bool hitMonster = false;
        bool killedMonster = false;

        foreach (Collider collider in Physics.OverlapSphere(center, radius, _hitLayers, QueryTriggerInteraction.Ignore))
        {
            IDamageable target = collider.GetComponentInParent<IDamageable>();

            // 자기 자신과 이미 벤 대상은 건너뛴다. (콜라이더가 여러 개인 대상이 한 번만 맞게)
            if (target == null || (target is Component component && component.transform.root == transform.root)) continue;
            if (_hitTargets.Contains(target)) continue;

            Vector3 closest = collider.ClosestPoint(center);

            // 부채꼴이면 수평으로 본 부채꼴 안에 있는지 확인한다. (아주 가까운 대상은 방향과 상관없이 맞는다.)
            if (!isSphere)
            {
                Vector3 toTarget = closest - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.01f && Vector3.Angle(forward, toTarget) > halfAngle) continue;
            }

            // 벽 같은 것에 가려진 대상은 맞지 않는다. (이펙트 중심이 벽 너머에 있을 수 있어 플레이어 몸에서 확인한다.)
            if (IsBlocked(origin, closest, collider)) continue;

            _hitTargets.Add(target);
            Vector3 normal = (center - closest).sqrMagnitude > 0.0001f ? (center - closest).normalized : -forward;
            target.TakeDamage(new DamageInfo(_damage, closest, normal));

            // 몬스터(좀비)만 명중으로 친다. 드럼통, 박스는 연출을 내지 않는다.
            if (target is Zombie zombie)
            {
                hitMonster = true;
                killedMonster |= zombie.Health <= 0f;
            }
        }

        if (hitMonster)
        {
            OnHit?.Invoke(killedMonster);
        }
    }

    // 중심에서 대상까지 사이에 다른 물체가 있어 막혀 있는지
    private bool IsBlocked(Vector3 origin, Vector3 point, Collider targetCollider)
    {
        if (!Physics.Linecast(origin, point, out RaycastHit blockHit, _hitLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // 막은 것이 대상 자신이거나 플레이어 자신이면 막힌 것이 아니다.
        return blockHit.collider != targetCollider && blockHit.collider.transform.root != transform.root;
    }
}
