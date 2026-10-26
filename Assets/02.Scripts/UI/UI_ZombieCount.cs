using TMPro;
using UnityEngine;

// 남은 좀비 수 UI: 시작할 때 씬의 좀비를 세고, 좀비가 죽을 때마다 숫자를 줄인다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;

    private Zombie[] _zombies;
    private int _remainingCount;

    private void Start()
    {
        _zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        _remainingCount = _zombies.Length;

        foreach (Zombie zombie in _zombies)
        {
            zombie.OnDied += OnZombieDied;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        foreach (Zombie zombie in _zombies)
        {
            if (zombie != null)
            {
                zombie.OnDied -= OnZombieDied;
            }
        }
    }

    private void OnZombieDied()
    {
        _remainingCount--;
        Refresh();
    }

    private void Refresh()
    {
        _countText.text = $"남은 좀비 수: {_remainingCount:00}";
    }
}
