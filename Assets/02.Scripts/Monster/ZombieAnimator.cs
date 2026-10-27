using UnityEngine;

// 좀비 애니메이션: 애니메이터 상태 이름으로 바로 재생한다. (전이나 트리거 없이 현재 모션에서 부드럽게 섞는다.)
// 컴포넌트가 아닌 일반 클래스라 Zombie가 만들어 쓴다.
public class ZombieAnimator
{
    // 애니메이터 상태 이름. 컨트롤러의 상태 이름과 똑같아야 한다.
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int TraceHash = Animator.StringToHash("Trace");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackDelayHash = Animator.StringToHash("AttackDelay");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int[] DeathHashes = { Animator.StringToHash("Death1"), Animator.StringToHash("Death2") };

    private readonly Animator _animator;
    private readonly float _fadeDuration;

    public ZombieAnimator(Animator animator, float fadeDuration)
    {
        _animator = animator;
        _fadeDuration = fadeDuration;
    }

    public void PlayIdle() => Play(IdleHash);

    // 로밍은 걷는 모션을 쓴다. Walk 상태가 없으면 Trace 모션으로 대신한다.
    public void PlayWalk() => Play(WalkHash, TraceHash);

    public void PlayTrace() => Play(TraceHash);

    public void PlayAttack() => Play(AttackHash);

    // 공격과 공격 사이에 기다리는 모션
    public void PlayAttackDelay() => Play(AttackDelayHash, IdleHash);

    // 피격 모션이 없으면 서 있는 모션으로 대신한다.
    public void PlayHit() => Play(HitHash, IdleHash);

    // 죽는 모션 중 하나를 무작위로 고른다.
    public void PlayDeath() => Play(DeathHashes[Random.Range(0, DeathHashes.Length)]);

    // 공격 모션을 끝까지 재생했는지. 다른 모션으로 섞이는 중에는 판단하지 않는다.
    public bool IsAttackFinished()
    {
        if (_animator == null || !_animator.isInitialized || _animator.IsInTransition(0)) return false;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == AttackHash && stateInfo.normalizedTime >= 1f;
    }

    // 그 상태가 컨트롤러에 없으면 fallbackHash 상태로 대신 재생하고, 그것도 없으면 아무것도 하지 않는다.
    private void Play(int stateHash, int fallbackHash = 0)
    {
        if (_animator == null || !_animator.isInitialized) return;

        if (!_animator.HasState(0, stateHash))
        {
            if (fallbackHash == 0 || !_animator.HasState(0, fallbackHash)) return;

            stateHash = fallbackHash;
        }

        _animator.CrossFadeInFixedTime(stateHash, _fadeDuration);
    }
}
