using TMPro;
using UnityEngine;

// 시작 연출 문구: GameManager가 알리는 "준비", "시작!"을 화면 가운데 글자(_text)에 보여 주고, null이 오면 숨긴다.
// 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙이고, 보여 주고 숨길 글자는 _text로 연결한다. (글자 오브젝트 자신에 붙이면 숨겼을 때 구독이 끊긴다.)
public class UI_StartMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    // 씬에는 글자가 켜진 채로 배치되어 있을 수 있으므로, 처음에는 숨긴다.
    // GameManager가 첫 문구를 알리는 것은 모든 OnEnable이 끝난 뒤(Start)라서 여기서 숨겨도 첫 문구가 가려지지 않는다.
    private void OnEnable()
    {
        _text.gameObject.SetActive(false);

        if (GameManager.Instance == null)
        {
            GameLogger.LogError("[UI_StartMessage] 씬에 GameManager가 없어 시작 문구를 보여 줄 수 없습니다.");
            return;
        }

        GameManager.Instance.OnStartMessage += Show;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStartMessage -= Show;
        }
    }

    // message가 null이면 글자를 숨긴다.
    private void Show(string message)
    {
        bool hasMessage = !string.IsNullOrEmpty(message);

        _text.gameObject.SetActive(hasMessage);
        if (hasMessage)
        {
            _text.text = message;
        }
    }
}
