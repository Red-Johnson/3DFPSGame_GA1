using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 앞쪽 부채꼴 범위를 칼로 베는 근접 공격 (총 사격 PlayerGunFire를 대신한다.)
// 휘두르는 모션을 시작하고 타격 시점이 되면 범위 안의 피격 대상(IDamageable)에게 대미지를 주고 이펙트를 낸다.
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
    [SerializeField] private SlashEffect _slashEffect;      // 비워두면 자식으로 하나 만든다.

    private float _attackTimer;
    private PlayerAnimator _playerAnimator;
    private PlayerBombFire _bombFire;
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();

    // 폭탄을 충전하거나 던지는 모션이 재생되는 동안은 벨 수 없다.
    private bool IsBlockedByBomb => _bombFire != null && (_bombFire.IsCharging || _bombFire.IsThrowing);

    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _bombFire = GetComponent<PlayerBombFire>();

        if (_slashEffect == null)
        {
            GameObject effectObject = new GameObject("SlashEffect");
            effectObject.transform.SetParent(transform, false);
            _slashEffect = effectObject.AddComponent<SlashEffect>();
        }
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

    // 지금 바라보는 방향의 부채꼴 범위에 있는 대상에게 대미지를 주고 이펙트를 낸다.
    private void Slash()
    {
        Vector3 origin = transform.position + _originOffset;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        _slashEffect.Play(origin, Quaternion.LookRotation(forward), _range, _angle);

        _hitTargets.Clear();
        float halfAngle = _angle * 0.5f;

        foreach (Collider collider in Physics.OverlapSphere(origin, _range, _hitLayers, QueryTriggerInteraction.Ignore))
        {
            IDamageable target = collider.GetComponentInParent<IDamageable>();

            // 자기 자신과 이미 벤 대상은 건너뛴다. (콜라이더가 여러 개인 대상이 한 번만 맞게)
            if (target == null || (target is Component component && component.transform.root == transform.root)) continue;
            if (_hitTargets.Contains(target)) continue;

            // 수평으로 본 부채꼴 안에 있는지 확인한다. (아주 가까운 대상은 방향과 상관없이 맞는다.)
            Vector3 closest = collider.ClosestPoint(origin);
            Vector3 toTarget = closest - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f && Vector3.Angle(forward, toTarget) > halfAngle) continue;

            // 벽 같은 것에 가려진 대상은 맞지 않는다.
            if (IsBlocked(origin, closest, collider)) continue;

            _hitTargets.Add(target);
            Vector3 normal = (origin - closest).sqrMagnitude > 0.0001f ? (origin - closest).normalized : -forward;
            target.TakeDamage(new DamageInfo(_damage, closest, normal));
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
