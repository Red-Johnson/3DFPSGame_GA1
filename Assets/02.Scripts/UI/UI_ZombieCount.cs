using TMPro;
using UnityEngine;

// 남은 좀비 수 UI: ZombieManager의 수 변경 알림을 구독해서 "남은 좀비 수: 08/50" 형식으로 표시한다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;

    private void Start()
    {
        ZombieManager.Instance.OnCountChanged += Refresh;

        Refresh(ZombieManager.Instance.AliveCount, ZombieManager.Instance.MaxCount);
    }

    private void OnDestroy()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.OnCountChanged -= Refresh;
        }
    }

    private void Refresh(int aliveCount, int maxCount)
    {
        _countText.text = $"남은 좀비 수: {aliveCount:00}/{maxCount:00}";
    }
}
