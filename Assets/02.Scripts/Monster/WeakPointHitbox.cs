using UnityEngine;

// 약점 판정: 피격 대상의 약점 위치(좀비는 머리 뼈 아래)에 둔 콜라이더에 붙이는 표시 컴포넌트다.
// 총알이 이 콜라이더에 맞으면 PlayerGunFire가 약점 공격으로 처리한다. (대미지를 받는 쪽은 부모의 IDamageable이다.)
// 몬스터마다 약점 위치가 다르므로(머리가 없는 몬스터 등) 콜라이더를 약점에 맞게 두면 된다.
[RequireComponent(typeof(Collider))]
public class WeakPointHitbox : MonoBehaviour
{
}
