using UnityEngine;
using UnityEngine.UI;

// 좀비 체력바: 체력이 바뀔 때만 갱신하고, 카메라를 쳐다보며 좀비의 머리 위를 따라다닌다.
// 몸체는 애니메이션에 따라 루트에서 벗어나므로, 루트가 아니라 머리 뼈를 기준으로 위치를 잡는다.
public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _headOffset = 0.7f;  // 머리 뼈에서 체력바까지 띄우는 높이

    private Zombie _zombie;
    private Camera _camera;
    private Transform _barRoot;     // 체력바 캔버스. 머리 뼈를 따라 움직인다.
    private Transform _head;        // 머리 뼈. 못 찾으면 프리팹에 정해 둔 위치를 그대로 쓴다.

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
        _barRoot = _slider.GetComponentInParent<Canvas>().transform;
    }

    private void Start()
    {
        _camera = Camera.main;

        // 모델이 Humanoid 아바타라서 머리 뼈를 바로 얻을 수 있다.
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        _zombie.OnHealthChanged += RefreshHealth;
        RefreshHealth();
    }

    private void OnDestroy()
    {
        _zombie.OnHealthChanged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        // 애니메이션이 반영된 뒤의 머리 위치를 따라간다. 위치만 따라가고 회전은 따르지 않아 머리가 흔들려도 기울지 않는다.
        if (_head != null)
        {
            _barRoot.position = _head.position + (Vector3.up * _headOffset);
        }

        // 빌보드: 체력바가 카메라를 쳐다보게한다.
        if (_camera == null) return;

        _slider.transform.forward = _camera.transform.forward;
    }

    // 체력이 바뀔 때만 호출된다.
    private void RefreshHealth()
    {
        // 0 ~ 100% -> 0 ~ 1
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
