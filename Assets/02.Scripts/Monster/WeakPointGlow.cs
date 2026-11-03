using UnityEngine;

// 약점 표시 이펙트: WeakPointHitbox의 구 콜라이더 영역만큼 반투명한 붉은 구(자식 Glow)를 맞춰 둔다.
// 약점이 어디 있는지 플레이어에게 알려 주는 용도다. 일렁임과 색은 Glow의 머티리얼(Game/WeakPointGlow)에서 정한다.
// 콜라이더의 반지름이나 중심을 바꾸면 에디터에서도 바로 따라 바뀐다. 콜라이더가 꺼지면(죽으면) 이펙트도 끈다.
// 약점 콜라이더(WeakPointHitbox)가 붙은 오브젝트에 함께 붙인다.
[ExecuteAlways]
[RequireComponent(typeof(SphereCollider))]
public class WeakPointGlow : MonoBehaviour
{
    [SerializeField] private Transform _glow;           // 구 메시를 가진 자식
    [SerializeField] private Renderer _glowRenderer;

    private SphereCollider _collider;

    private void OnEnable()
    {
        _collider = GetComponent<SphereCollider>();
        Sync();
    }

    private void LateUpdate()
    {
        Sync();
    }

    // 에디터(재생 중이 아닐 때)에서는 LateUpdate가 매 프레임 불리지 않으므로, 씬 뷰를 다시 그릴 때 맞춘다.
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            if (_collider == null)
            {
                _collider = GetComponent<SphereCollider>();
            }

            Sync();
        }
    }

    // 콜라이더와 같은 중심, 같은 반지름(지름 = 구 메시의 기본 지름 1 * 스케일)으로 맞춘다.
    private void Sync()
    {
        if (_glow == null || _collider == null) return;

        _glow.localPosition = _collider.center;
        _glow.localScale = Vector3.one * (_collider.radius * 2f);

        if (_glowRenderer != null)
        {
            _glowRenderer.enabled = _collider.enabled;
        }
    }
}
