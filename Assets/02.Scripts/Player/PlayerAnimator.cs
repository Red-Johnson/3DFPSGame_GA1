using UnityEngine;

// 플레이어 애니메이터: 애니메이터 컨트롤러의 파라미터, 레이어, 상태 이름을 한곳에 모으고 의미 있는 메서드로 감싼다.
// 컨트롤러의 이름을 바꾸면 이 파일의 상수만 고치면 된다. Player 오브젝트에 붙이고, 모델의 Animator는 자식에서 찾는다.
[DefaultExecutionOrder(-50)]    // 다른 플레이어 컴포넌트가 Awake에서 쓰기 전에 먼저 준비한다.
public class PlayerAnimator : MonoBehaviour
{
    // 애니메이터 컨트롤러의 이름과 똑같아야 한다.
    private const string MoveParameter = "MoveMotion";          // 걷기/달리기 블렌드 (0~1)
    private const string SlashParameter = "Slash";              // 칼 휘두르기 모션을 시작하는 트리거
    private const string ThrowParameter = "Throw";              // 던지는 모션을 시작하는 트리거
    private const string ThrowSpeedParameter = "ThrowSpeed";    // Throw 상태의 재생 속도(0이면 멈춤, 1이면 재생)
    private const string MeleeIdleLayerName = "Melee Idle Layer";   // 근접 모드에서 서 있을 때 온몸이 battle stance가 되는 레이어
    private const string StanceLayerName = "Stance Layer";          // 근접 모드에서 움직일 때 상체만 battle stance가 되는 레이어
    private const string InjuredLayerName = "Injured Layer";    // 체력이 낮을 때 하체가 절뚝거리는 레이어
    private const string ThrowLayerName = "Throw Layer";        // 던지는 모션(상체만)을 재생하는 레이어
    private const string ThrowStateName = "Throw";
    private const string EmptyStateName = "Empty";

    private const float CancelThrowFadeDuration = 0.15f;        // 던지기를 취소했을 때 모션이 풀리는 시간

    private static readonly int MoveHash = Animator.StringToHash(MoveParameter);
    private static readonly int SlashHash = Animator.StringToHash(SlashParameter);
    private static readonly int ThrowHash = Animator.StringToHash(ThrowParameter);
    private static readonly int ThrowSpeedHash = Animator.StringToHash(ThrowSpeedParameter);
    private static readonly int ThrowStateHash = Animator.StringToHash(ThrowStateName);
    private static readonly int EmptyStateHash = Animator.StringToHash(EmptyStateName);

    private Animator _animator;
    private int _meleeIdleLayerIndex = -1;
    private int _stanceLayerIndex = -1;
    private bool _isMeleeStance;
    private float _moveAmount;
    private int _injuredLayerIndex = -1;
    private int _throwLayerIndex = -1;

    // 던지는 모션을 쓸 수 있는지. 컨트롤러에 Throw Layer가 없으면 모션 없이 던진다.
    public bool HasThrowAnimation => _animator != null && _throwLayerIndex >= 0;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            GameLogger.LogError("[PlayerAnimator] Animator를 찾지 못했습니다. 플레이어 모델에 Animator가 있어야 합니다.");
            return;
        }

        _meleeIdleLayerIndex = FindLayer(MeleeIdleLayerName, "근접 서 있는 자세");
        _stanceLayerIndex = FindLayer(StanceLayerName, "근접 상체 자세");
        _injuredLayerIndex = FindLayer(InjuredLayerName, "부상 애니메이션");
        _throwLayerIndex = FindLayer(ThrowLayerName, "던지는 모션");
    }

    // 걷기/달리기 정도 (0: 서 있음, 1: 달림)
    public void SetMove(float amount)
    {
        if (_animator == null) return;

        _moveAmount = amount;
        _animator.SetFloat(MoveHash, amount);
        RefreshMeleeStanceWeights();
    }

    // 근접 자세 여부. 켜면 서 있을 때는 온몸이, 움직일 때는 상체만 battle stance가 된다. (총 모드에서는 끈다.)
    public void SetMeleeStance(bool isMelee)
    {
        _isMeleeStance = isMelee;
        RefreshMeleeStanceWeights();
    }

    // 서 있을수록 온몸 자세 레이어를, 근접 모드인 동안은 상체 자세 레이어를 항상 켠다.
    private void RefreshMeleeStanceWeights()
    {
        if (_animator == null) return;

        if (_meleeIdleLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_meleeIdleLayerIndex, _isMeleeStance ? 1f - Mathf.Clamp01(_moveAmount) : 0f);
        }

        if (_stanceLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_stanceLayerIndex, _isMeleeStance ? 1f : 0f);
        }
    }

    // 칼을 한 번 휘두르는 모션(상체만)을 처음부터 재생한다. 휘두르는 중에 다시 불러도 처음부터 다시 시작한다.
    public void TriggerSlash()
    {
        if (_animator == null) return;

        _animator.SetTrigger(SlashHash);
    }

    // 부상 정도 (0: 정상, 1: 최대). 하체가 절뚝거리는 레이어의 가중치로 쓴다.
    public void SetInjured(float weight)
    {
        if (_animator == null || _injuredLayerIndex < 0) return;

        _animator.SetLayerWeight(_injuredLayerIndex, weight);
    }

    // 던지는 모션을 시작하고, 첫 자세에서 멈춘 채 기다린다. (속도를 0으로 둔 채 트리거를 켠다.)
    public void StartThrow()
    {
        if (!HasThrowAnimation) return;

        _animator.SetFloat(ThrowSpeedHash, 0f);
        _animator.SetTrigger(ThrowHash);
    }

    // 멈춰 있던 던지는 모션을 주어진 속도로 이어서 재생한다.
    public void ReleaseThrow(float speed)
    {
        if (!HasThrowAnimation) return;

        _animator.SetFloat(ThrowSpeedHash, speed);
    }

    // 던지지 않고 모션만 취소한다. 멈춰 있던 모션을 풀어서 원래 자세로 돌린다.
    public void CancelThrow()
    {
        if (!HasThrowAnimation) return;

        _animator.SetFloat(ThrowSpeedHash, 1f);
        _animator.ResetTrigger(ThrowHash);     // 모션이 시작되기 전에 취소해도 트리거가 남아 나중에 재생되지 않게 한다.
        _animator.CrossFadeInFixedTime(EmptyStateHash, CancelThrowFadeDuration, _throwLayerIndex);
    }

    // 던지는 모션이 아직 재생 중(또는 재생을 시작하는 중)인지
    public bool IsThrowPlaying()
    {
        if (!HasThrowAnimation) return false;

        AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(_throwLayerIndex);
        if (current.shortNameHash == ThrowStateHash) return true;

        // 상태가 바뀌는 중에는 다음 상태가 Throw인지도 본다.
        return _animator.IsInTransition(_throwLayerIndex)
            && _animator.GetNextAnimatorStateInfo(_throwLayerIndex).shortNameHash == ThrowStateHash;
    }

    private int FindLayer(string layerName, string featureName)
    {
        int index = _animator.GetLayerIndex(layerName);
        if (index < 0)
        {
            GameLogger.LogError($"[PlayerAnimator] 애니메이터에 '{layerName}' 레이어가 없어 {featureName}을(를) 쓸 수 없습니다.");
        }

        return index;
    }
}
