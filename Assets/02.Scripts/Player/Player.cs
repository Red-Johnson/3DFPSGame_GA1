using UnityEngine;
using System;

public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;
    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // Action 자료형: 함수를 저장할 수 있다.
    public event Action OnDamaged = null;

    // 체력이 0이 되면 구독자(GameManager 등)에게 알린다. 게임 오버 처리는 구독한 쪽이 한다.
    public event Action OnDied = null;

    private const string InjuredLayerName = "Injured Layer";   // 애니메이터 컨트롤러의 레이어 이름과 똑같아야 한다.

    private Animator _animator;
    private int _injuredLayerIndex = -1;


    private void Awake()
    {
        // UI가 Start에서 체력을 읽어도 되도록 Awake에서 채운다.
        _health = _maxHealth;

        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            _injuredLayerIndex = _animator.GetLayerIndex(InjuredLayerName);
            if (_injuredLayerIndex < 0)
            {
                GameLogger.LogError($"[Player] 애니메이터에 '{InjuredLayerName}' 레이어가 없어 부상 애니메이션을 쓸 수 없습니다.");
            }
        }

        RefreshInjuredLayer();
    }

    // 체력이 줄수록 부상 레이어(하체 절뚝거림)의 가중치를 올린다. 체력이 가득이면 0, 0이면 1.
    private void RefreshInjuredLayer()
    {
        if (_animator == null || _injuredLayerIndex < 0) return;

        float healthPercent = _health / _maxHealth; // 0 ~ 1
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);
        _animator.SetLayerWeight(_injuredLayerIndex, injured);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;
        RefreshInjuredLayer();

        OnDamaged?.Invoke();

        GameLogger.Log($"플레이어가 맞았다. 대미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            GameLogger.Log("플레이어가 쓰러졌다!");
            OnDied?.Invoke();
        }
    }
}
