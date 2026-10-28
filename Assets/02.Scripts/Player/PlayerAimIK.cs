using UnityEngine;

// 상체 조준 보정: 사격 중에는 상체가 카메라가 보는 곳을 향하게 한다.
// 달릴 때 골반이 좌우로 흔들리고 기울어도 그 위의 상체와 총이 같이 흔들리지 않게 잡아 준다.
// Animator가 붙은 모델 오브젝트에 있어야 OnAnimatorIK가 호출되고, 사격 레이어의 IK Pass가 켜져 있어야 한다.
// PlayerGunFire가 모델에 자동으로 붙여 준다.
public class PlayerAimIK : MonoBehaviour
{
    private const string FireLayerName = "Fire Layer";      // 애니메이터 컨트롤러의 레이어 이름과 똑같아야 한다.
    private const string FireParameter = "Fire";            // 사격 중인지 알려 주는 애니메이터 파라미터

    [SerializeField] private float _aimDistance = 30f;                      // 카메라 앞 이 거리의 점을 조준점으로 삼는다.
    [SerializeField, Range(0f, 1f)] private float _bodyWeight = 1f;         // 상체(몸통)를 얼마나 돌릴지
    [SerializeField, Range(0f, 1f)] private float _headWeight = 0.3f;       // 머리를 얼마나 돌릴지
    [SerializeField, Range(0f, 1f)] private float _clampWeight = 0.5f;      // 클수록 돌아가는 각도를 제한한다.
    [SerializeField] private float _blendSpeed = 8f;                        // 보정이 켜지고 꺼지는 속도(초당)

    private Animator _animator;
    private Camera _camera;
    private int _fireLayerIndex;
    private int _fireHash;
    private float _weight;      // 현재 보정 강도 0 ~ 1. 사격 중일 때만 1을 향해 올라간다.

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _camera = Camera.main;
        _fireHash = Animator.StringToHash(FireParameter);
        _fireLayerIndex = _animator.GetLayerIndex(FireLayerName);

        if (_fireLayerIndex < 0)
        {
            GameLogger.LogError($"[PlayerAimIK] 애니메이터에 '{FireLayerName}' 레이어가 없어 조준 보정을 끕니다.");
            enabled = false;
        }
    }

    // IK Pass가 켜진 레이어마다 불린다. 사격 레이어일 때만 처리한다.
    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != _fireLayerIndex || _camera == null) return;

        // 사격 중에는 보정을 서서히 켜고, 끝나면 서서히 끈다.
        float targetWeight = _animator.GetBool(_fireHash) ? 1f : 0f;
        _weight = Mathf.MoveTowards(_weight, targetWeight, _blendSpeed * Time.deltaTime);

        if (_weight <= 0f)
        {
            _animator.SetLookAtWeight(0f);
            return;
        }

        Transform cameraTransform = _camera.transform;
        Vector3 aimPoint = cameraTransform.position + (cameraTransform.forward * _aimDistance);

        _animator.SetLookAtWeight(_weight, _bodyWeight, _headWeight, 0f, _clampWeight);
        _animator.SetLookAtPosition(aimPoint);
    }
}
