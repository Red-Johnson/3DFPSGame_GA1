using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    // Todo. ZombieHealthBar를 이용해서 체력과 스태미나 UI 구현
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    [SerializeField] private Player _player;
    [SerializeField] private PlayerMove _playerMove;

    private void Start()
    {
        _player.OnDamaged += RefreshHealth; // OnDamaged 함수에 RefreshHealth 함수를 등록!

        RefreshHealth();    // 처음 표시할 체력을 맞춘다.
    }

    private void OnDestroy()
    {
        _player.OnDamaged -= RefreshHealth;
    }

    private void Update()
    {
        // RefreshHealth(); // 폴링 방식

        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }

    private void RefreshHealth()
    {
        // 문제: 체력값이 변하지 않았음에도 매 프레임마다 캔버스를 불필요하게 다시 그린다.
        // 이로 인해 성능 낭비가 발생. 불필요한 연산이 누적될수록 부하 증가.
        // 해결: 값이 변경될 때만 캔버스 갱신을 실시한다.

        // 해결 방법: <옵저버 패턴>을 사용한다!
        // 옵저버 패턴은 한 객체(주체)의 상태가 바뀔 때 이를 구독 중인 여러 객체(관찰자)에게 자동으로 알려주는 패턴
        // 유튜브 구독과 비슷함 : 유튜버가 영상을 올릴 때 구독자에게 '영상 올렸다!'라는 알림이 가게 함
        // C#에서는 이것을 Action으로 구현할 수 있음.

        _healthSlider.value = _player.Health / _player.MaxHealth;

    }

}
