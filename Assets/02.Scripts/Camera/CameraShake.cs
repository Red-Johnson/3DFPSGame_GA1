using UnityEngine;

// 카메라 흔들림: 근접 공격이 몬스터에 맞으면 카메라를 짧게 흔들어 타격 성공을 알린다.
// 메인 카메라에 붙인다. 1인칭과 3인칭 모두 같은 방식이다.
// - 위치가 아니라 회전만 흔든다. (1인칭에서 벽이나 몸에 파묻히지 않고, 3인칭 카메라의 충돌 판정과도 겹치지 않는다.)
// - 시점 컴포넌트(CameraFollow, CameraRotate, ThirdPersonCamera)가 매 프레임 카메라를 원래 값으로 다시 정하므로,
//   그 뒤에(LateUpdate, 실행 순서 100) 흔들림만 얹는다. 흔들리지 않을 때는 아무것도 하지 않아 기존 시점에 영향이 없다.
[DefaultExecutionOrder(100)]
public class CameraShake : MonoBehaviour
{
    private const float RollRatio = 0.5f;       // 좌우 기울어짐(롤)은 위아래, 좌우 흔들림보다 약하게 한다.

    [SerializeField] private PlayerMeleeAttack _meleeAttack;

    [Header("근접 타격")]
    [SerializeField] private float _hitStrength = 2f;       // 일반 타격의 흔들림 각도(도)
    [SerializeField] private float _killStrength = 3.5f;    // 몬스터를 죽였을 때의 흔들림 각도(도)
    [SerializeField] private float _duration = 0.18f;       // 흔들리는 시간(초). 점점 약해지며 끝난다.
    [SerializeField] private float _frequency = 30f;        // 흔들리는 빠르기

    private float _strength;
    private float _totalTime;
    private float _remainingTime;

    private void Awake()
    {
        if (_meleeAttack == null)
        {
            GameLogger.LogWarning("[CameraShake] _meleeAttack이 연결되지 않아 근접 타격 흔들림이 나오지 않습니다.");
        }
    }

    private void OnEnable()
    {
        if (_meleeAttack != null)
        {
            _meleeAttack.OnHit += OnMeleeHit;
        }
    }

    private void OnDisable()
    {
        if (_meleeAttack != null)
        {
            _meleeAttack.OnHit -= OnMeleeHit;
        }

        _remainingTime = 0f;
    }

    private void OnMeleeHit(bool killedMonster)
    {
        Shake(killedMonster ? _killStrength : _hitStrength, _duration);
    }

    // 카메라를 strength도만큼 duration초 동안 흔든다. (이미 더 세게 흔들리는 중이면 무시한다.)
    public void Shake(float strength, float duration)
    {
        if (duration <= 0f || strength < CurrentStrength()) return;

        _strength = strength;
        _totalTime = duration;
        _remainingTime = duration;
    }

    private float CurrentStrength()
    {
        return _remainingTime > 0f ? _strength * (_remainingTime / _totalTime) : 0f;
    }

    private void LateUpdate()
    {
        if (_remainingTime <= 0f) return;

        _remainingTime -= Time.deltaTime;
        float amplitude = CurrentStrength();

        // 부드럽게 변하는 무작위 값으로 흔든다. (프레임마다 완전히 무작위면 떨림이 거칠다.)
        float t = Time.time * _frequency;
        float pitch = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * amplitude;
        float yaw = (Mathf.PerlinNoise(10f, t) - 0.5f) * 2f * amplitude;
        float roll = (Mathf.PerlinNoise(20f, t) - 0.5f) * 2f * amplitude * RollRatio;

        transform.rotation *= Quaternion.Euler(pitch, yaw, roll);
    }
}
