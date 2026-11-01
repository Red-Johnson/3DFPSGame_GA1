using UnityEngine;
using UnityEngine.UI;

// 콤보 유지 시간 바: 랜턴 불빛처럼 일렁이는 얇은 파란 빛줄기가 중앙을 기준으로 남은 시간만큼 이어진다.
// 이미지 없이 UI 메시를 직접 그린다. 세로로는 가운데가 밝은 심지, 위아래는 투명하게 번지는 빛이고,
// 열마다 다른 노이즈로 번지는 높이와 밝기를 흔든다. 시간이 줄면 양쪽 끝이 중앙으로 모이며, 양 끝은 똑같이 가늘게 사라져
// 어느 쪽도 잘린 것처럼 보이지 않는다. 시간이 얼마 안 남을수록 더 빠르고 크게 흔들린다.
// 채움 비율은 UI_KillCombo가 SetFill로 알려 준다.
public class UI_ComboTimeBar : MaskableGraphic
{
    private const int ColumnCount = 40;     // 가로로 나누는 열 수
    private const int VerticesPerColumn = 3;    // 열마다 세로로 찍는 꼭짓점 수 (번짐, 심지, 번짐)

    [SerializeField] private Color _coreColor = new Color(0.75f, 0.9f, 1f, 1f);     // 가운데 심지 색
    [SerializeField] private float _flickerSpeed = 3f;     // 일렁이는 속도
    [SerializeField] private float _flickerScale = 6f;      // 가로로 일렁임이 바뀌는 빈도
    [SerializeField, Range(0f, 1f)] private float _flickerAmount = 0.35f;   // 높이와 밝기가 흔들리는 정도
    [SerializeField] private float _lowTimeBoost = 1.5f;    // 시간이 거의 없을 때 흔들림 속도와 크기에 더하는 비율
    [SerializeField] private float _endTaper = 0.6f;        // 양 끝이 가늘어지는 정도의 지수(작을수록 끝까지 두껍고, 클수록 일찍 가늘어진다)

    private float _fill = 1f;
    private float _seed;

    public void SetFill(float fill)
    {
        _fill = Mathf.Clamp01(fill);
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        _seed = Random.value * 100f;
        raycastTarget = false;
    }

    // 일렁임은 시간에 따라 계속 변하므로 매 프레임 메시를 다시 만든다.
    private void Update()
    {
        if (_fill > 0f)
        {
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_fill <= 0f) return;

        Rect rect = GetPixelAdjustedRect();
        float halfHeight = rect.height * 0.5f;
        float centerY = rect.center.y;
        float width = rect.width * _fill;
        float left = rect.center.x - width * 0.5f;      // 중앙 기준이라 줄어들어도 아이콘과 좌우가 맞는다.

        float urgency = 1f + (1f - _fill) * _lowTimeBoost;      // 남은 시간이 적을수록 커진다.
        float time = Time.time * _flickerSpeed * urgency + _seed;
        float amount = Mathf.Clamp01(_flickerAmount * urgency);

        for (int i = 0; i <= ColumnCount; i++)
        {
            float t = i / (float)ColumnCount;
            float x = left + width * t;

            // 열 위치와 시간으로 얻은 노이즈(0~1)가 높이와 밝기를 같이 흔든다.
            float noise = Mathf.PerlinNoise(t * _flickerScale + _seed, time);
            float shake = 1f - amount + amount * 2f * noise;

            // 양 끝에서 0, 가운데에서 1인 부드러운 곡선으로 높이와 밝기를 모두 줄여 끝이 잘려 보이지 않게 한다.
            float taper = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * t)), _endTaper);
            float height = Mathf.Min(halfHeight * shake * taper, halfHeight);
            float brightness = Mathf.Clamp01((0.6f + 0.4f * noise) * taper);

            Color core = Color.Lerp(color, _coreColor, brightness);
            core.a = color.a * brightness;
            Color glow = color;
            glow.a = 0f;

            vh.AddVert(new Vector3(x, centerY - height), glow, Vector2.zero);
            vh.AddVert(new Vector3(x, centerY), core, Vector2.zero);
            vh.AddVert(new Vector3(x, centerY + height), glow, Vector2.zero);

            if (i == 0) continue;

            int current = i * VerticesPerColumn;
            int previous = current - VerticesPerColumn;
            for (int row = 0; row < VerticesPerColumn - 1; row++)
            {
                vh.AddTriangle(previous + row, previous + row + 1, current + row);
                vh.AddTriangle(current + row, previous + row + 1, current + row + 1);
            }
        }
    }
}
