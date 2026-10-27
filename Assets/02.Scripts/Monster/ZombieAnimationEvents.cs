using UnityEngine;

// 애니메이션 이벤트를 받아 Zombie에 전달한다.
// 이벤트 함수는 Animator가 붙은 오브젝트에서 찾으므로, Zombie가 아니라 모델(자식) 오브젝트에 붙여야 한다.
public class ZombieAnimationEvents : MonoBehaviour
{
    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    // Z_Attack 클립에서 팔이 플레이어에게 맞는 프레임에 호출한다.
    public void OnAttackHit()
    {
        _zombie.OnAttackHit();
    }
}
