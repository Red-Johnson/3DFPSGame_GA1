using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 버튼의 글자 연출. 마우스를 올리면 글자가 가로등 같은 호박색으로 밝아지고, 누르는 동안 살짝 눌린다.
// 버튼 배경 그림은 Button의 Sprite Swap이 바꾸므로 이 컴포넌트는 글자만 만진다.
[RequireComponent(typeof(Button))]
public class UI_TitleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Color _normalColor = new Color(0.78f, 0.84f, 0.96f, 1f);
    [SerializeField] private Color _hoverColor = new Color(1f, 0.85f, 0.55f, 1f);
    [SerializeField] private Color _disabledColor = new Color(0.36f, 0.4f, 0.5f, 1f);
    [SerializeField] private float _pressOffset = 2f; // 눌렀을 때 글자가 내려가는 거리

    private Button _button;
    private RectTransform _labelRect;
    private Vector2 _labelBasePos;
    private bool _hover;
    private bool _pressed;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_label != null)
        {
            _labelRect = _label.rectTransform;
            _labelBasePos = _labelRect.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        _hover = false;
        _pressed = false;
        Refresh();
    }

    private void Update()
    {
        // 비활성 상태가 바뀌었을 때(회원가입 버튼 등) 색을 맞춘다.
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) { _hover = true; }
    public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { _pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { _pressed = false; }

    private void Refresh()
    {
        if (_label == null || _button == null) return;

        bool usable = _button.IsInteractable();
        _label.color = !usable ? _disabledColor : (_hover || _pressed) ? _hoverColor : _normalColor;
        _labelRect.anchoredPosition = _labelBasePos + (usable && _pressed ? new Vector2(0f, -_pressOffset) : Vector2.zero);
    }
}
