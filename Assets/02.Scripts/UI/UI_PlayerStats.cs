using UnityEngine;
using UnityEngine.UI;

// 플레이어 체력과 기력 UI: 체력은 피격 이벤트(OnDamaged)가 올 때만 갱신하고, 기력은 매 프레임 변하므로 계속 갱신한다.
public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    [SerializeField] private Player _player;
    [SerializeField] private PlayerMove _playerMove;

    private void OnEnable()
    {
        _player.OnDamaged += RefreshHealth;
    }

    private void OnDisable()
    {
        _player.OnDamaged -= RefreshHealth;
    }

    private void Start()
    {
        // 모든 컴포넌트의 Awake가 끝난 뒤 한 번 전부 갱신해서, 처음부터 정확한 값을 보여 준다.
        RefreshAll();
    }

    private void Update()
    {
        RefreshStamina();
    }

    private void RefreshAll()
    {
        RefreshHealth();
        RefreshStamina();
    }

    private void RefreshStamina()
    {
        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }

    private void RefreshHealth()
    {
        _healthSlider.value = _player.Health / _player.MaxHealth;
    }
}
