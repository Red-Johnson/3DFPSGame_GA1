using TMPro;
using UnityEngine;

// 남은 좀비 수 UI: ZombieManager의 수 변경 알림을 구독해서 "남은 좀비 수: 08/50" 형식으로 표시한다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;

    // ZombieManager는 DefaultExecutionOrder(-100)으로 먼저 초기화되므로 OnEnable에서 Instance를 써도 된다.
    private void OnEnable()
    {
        ZombieManager.Instance.OnCountChanged += Refresh;
    }

    private void OnDisable()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.OnCountChanged -= Refresh;
        }
    }

    private void Start()
    {
        // 모든 컴포넌트의 Awake가 끝난 뒤 한 번 갱신해서, 처음부터 정확한 값을 보여 준다.
        Refresh(ZombieManager.Instance.AliveCount, ZombieManager.Instance.MaxCount);
    }

    private void Refresh(int aliveCount, int maxCount)
    {
        _countText.text = $"남은 좀비 수: {aliveCount:00}/{maxCount:00}";
    }
}
