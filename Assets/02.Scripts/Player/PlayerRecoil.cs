using UnityEngine;

// 사격 반동: 총알이 나가는 바로 그 프레임에 화면(카메라)과 총을 든 몸을 위로 튀게 하고, 부드럽게 원래대로 돌아오게 한다.
// 애니메이션 클립 대신 코드로 만들어서, 연사해도 총알마다 정확히 같은 타이밍에 반동이 나오고 쌓였다가 풀린다.
// 반동 세기는 총마다 GunSpec(카메라 반동, 몸 반동)에서 정한다. Player 오브젝트에 붙인다.
//
// 화면과 몸만 움직이는 연출이고 조준은 바뀌지 않는다.
// - 카메라: 시점 컴포넌트가 모두 끝난 뒤(렌더링 직전)에 회전을 얹고, 다음 프레임 시작에 원래 회전으로 되돌린다.
//   그래서 CameraRotate, ThirdPersonCamera가 쓰는 값과 사격 조준 레이는 반동의 영향을 받지 않는다.
// - 몸: 애니메이터가 포즈를 만든 뒤(LateUpdate)에 가슴과 오른팔 뼈의 회전을 얹는다. 애니메이터가 매 프레임 포즈를 다시 만들어 쌓이지 않는다.
[DefaultExecutionOrder(-1000)]   // Update에서 다른 컴포넌트(사격 조준 등)보다 먼저 카메라를 원래 회전으로 되돌린다.
[RequireComponent(typeof(PlayerGunFire))]
public class PlayerRecoil : MonoBehaviour
{
    [Header("회복")]
    [SerializeField] private float _recoverySpeed = 12f;        // 클수록 빨리 원래대로 돌아온다.

    [Header("한계 (연사해도 이 이상은 튀지 않는다)")]
    [SerializeField] private float _maxCameraPitch = 6f;
    [SerializeField] private float _maxCameraSide = 2f;
    [SerializeField] private float _maxBodyAngle = 12f;

    [Header("몸")]
    [SerializeField, Range(0f, 1f)] private float _chestRatio = 0.4f;   // 팔이 젖혀지는 각도에 대한 가슴의 비율

    [Header("줌")]
    [SerializeField] private float _zoomRecoilMultiplier = 1.5f;        // 줌 중 화면 반동에 곱하는 배율. 줌 중에는 시야각에 맞춰 줄어든 반동이 더 약하게 느껴질 때 키운다.

    private PlayerGunFire _gunFire;
    private PlayerRotate _playerRotate;
    private PlayerMarksmanZoom _zoom;
    private Camera _camera;
    private Transform _chest;
    private Transform _rightUpperArm;

    private float _cameraPitch;     // 지금 화면이 위로 튄 각도
    private float _cameraSide;      // 지금 화면이 옆으로 튄 각도
    private float _bodyAngle;       // 지금 팔이 젖혀진 각도

    private Quaternion _cameraRotationWithoutRecoil;
    private bool _isCameraRecoilApplied;

    private void Awake()
    {
        _gunFire = GetComponent<PlayerGunFire>();
        _playerRotate = GetComponent<PlayerRotate>();
        _zoom = GetComponent<PlayerMarksmanZoom>();
        _camera = Camera.main;

        // 모델이 Humanoid 아바타라서 뼈를 바로 얻을 수 있다. 없으면 몸 반동은 나오지 않는다.
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            _chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            _rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        }
    }

    private void OnEnable()
    {
        _gunFire.OnShot += OnShot;
        Application.onBeforeRender += ApplyCameraRecoil;
    }

    private void OnDisable()
    {
        _gunFire.OnShot -= OnShot;
        Application.onBeforeRender -= ApplyCameraRecoil;

        RemoveCameraRecoil();
        _cameraPitch = 0f;
        _cameraSide = 0f;
        _bodyAngle = 0f;
    }

    private void OnShot(GunSpec spec)
    {
        _cameraPitch = Mathf.Min(_maxCameraPitch, _cameraPitch + spec.CameraRecoilPitch);
        _cameraSide = Mathf.Clamp(_cameraSide + (Random.Range(-1f, 1f) * spec.CameraRecoilSide), -_maxCameraSide, _maxCameraSide);
        _bodyAngle = Mathf.Min(_maxBodyAngle, _bodyAngle + spec.BodyRecoilAngle);
    }

    private void Update()
    {
        // 지난 프레임에 얹은 카메라 반동을 먼저 걷어 낸다. (조준 레이와 시점 입력이 반동 없는 회전을 쓰게)
        RemoveCameraRecoil();

        // 매 프레임 일정 비율로 줄어서 처음엔 빠르게, 끝은 부드럽게 돌아온다.
        float decay = Mathf.Exp(-_recoverySpeed * Time.deltaTime);
        _cameraPitch *= decay;
        _cameraSide *= decay;
        _bodyAngle *= decay;
    }

    private void LateUpdate()
    {
        ApplyBodyRecoil();
    }

    // 렌더링 직전, 모든 시점 컴포넌트가 카메라를 정한 뒤에 반동을 얹는다.
    private void ApplyCameraRecoil()
    {
        if (_camera == null || _isCameraRecoilApplied) return;

        // 줌 중에는 같은 각도가 화면에서 훨씬 크게 보이므로 시점 감도와 같은 비율로 줄인다.
        float scale = _playerRotate != null ? _playerRotate.LookSensitivityScale : 1f;

        // 줄어든 반동이 너무 약하게 느껴질 수 있어서, 줌 중에만 따로 배율을 곱해 키운다.
        if (_zoom != null && _zoom.IsZooming)
        {
            scale *= _zoomRecoilMultiplier;
        }

        if (Mathf.Abs(_cameraPitch) < 0.001f && Mathf.Abs(_cameraSide) < 0.001f) return;

        Transform cameraTransform = _camera.transform;
        _cameraRotationWithoutRecoil = cameraTransform.rotation;
        cameraTransform.rotation *= Quaternion.Euler(-_cameraPitch * scale, _cameraSide * scale, 0f);
        _isCameraRecoilApplied = true;
    }

    private void RemoveCameraRecoil()
    {
        if (!_isCameraRecoilApplied) return;

        _isCameraRecoilApplied = false;

        if (_camera != null)
        {
            _camera.transform.rotation = _cameraRotationWithoutRecoil;
        }
    }

    // 플레이어의 오른쪽 축을 기준으로 뼈를 뒤로 젖힌다. (뼈마다 축 방향이 달라도 같은 방향으로 움직이도록 월드 기준 회전을 쓴다.)
    private void ApplyBodyRecoil()
    {
        if (_bodyAngle < 0.001f) return;

        Quaternion kick = Quaternion.AngleAxis(-_bodyAngle, transform.right);
        Quaternion chestKick = Quaternion.AngleAxis(-_bodyAngle * _chestRatio, transform.right);

        if (_chest != null)
        {
            _chest.rotation = chestKick * _chest.rotation;
        }

        if (_rightUpperArm != null)
        {
            _rightUpperArm.rotation = kick * _rightUpperArm.rotation;
        }
    }
}
