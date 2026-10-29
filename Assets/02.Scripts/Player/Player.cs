using UnityEngine;
using System;

[RequireComponent(typeof(PlayerAnimator))]
public class Player : MonoBehaviour, IDamageable
{
    private const float InjuredHealthRatio = 0.5f;     // 체력이 이 비율 아래로 내려가면 절뚝거리기 시작하고, 체력이 0이면 최대가 된다.

    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;
    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // Action 자료형: 함수를 저장할 수 있다.
    public event Action OnDamaged = null;

    // 체력이 0이 되면 구독자(GameManager 등)에게 알린다. 게임 오버 처리는 구독한 쪽이 한다.
    public event Action OnDied = null;

    private PlayerAnimator _playerAnimator;


    private void Awake()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();

        // UI가 Start에서 체력을 읽어도 되도록 Awake에서 채운다.
        _health = _maxHealth;

        RefreshInjured();
    }

    // 체력이 줄수록 하체가 절뚝거리는 정도를 올린다. 체력이 InjuredHealthRatio 이상이면 0, 0이면 1.
    private void RefreshInjured()
    {
        float healthPercent = _health / _maxHealth; // 0 ~ 1
        float injured = Mathf.InverseLerp(InjuredHealthRatio, 0f, healthPercent);
        _playerAnimator.SetInjured(injured);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;
        RefreshInjured();

        OnDamaged?.Invoke();

        GameLogger.Log($"플레이어가 맞았다. 대미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            GameLogger.Log("플레이어가 쓰러졌다!");
            OnDied?.Invoke();
        }
    }
}
