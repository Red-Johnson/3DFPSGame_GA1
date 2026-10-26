using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 미니맵 UI: 지역 이름을 보여 주고, +/- 버튼 클릭을 미니맵 카메라의 확대/축소로 전달한다.
public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _minimapCamera;

    [SerializeField] private TextMeshProUGUI _mapNameText;

    private void Awake()
    {
        _mapNameText.text = SceneManager.GetActiveScene().name;
    }

    // 버튼 onClick에서 호출한다.
    public void ZoomIn()
    {
        _minimapCamera.ZoomIn();
    }

    public void ZoomOut()
    {
        _minimapCamera.ZoomOut();
    }
}
