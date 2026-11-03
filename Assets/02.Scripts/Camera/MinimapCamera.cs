using UnityEngine;
using UnityEngine.Rendering.Universal;


// 미니맵 카메라: 플레이어 머리 위에서 아래를 내려다보며 따라다니고, 확대/축소를 담당한다.
// 비용을 줄이려고 그림자, 후처리, HDR, MSAA를 끄고 렌더 범위를 줄이며, 일정 간격으로만 그린다.
public class MinimapCamera : MonoBehaviour
{
    private Camera _camera;
    private float _nextRenderTime;

    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;

    [Header("확대/축소")]
    [SerializeField] private float _zoomStep = 2f;
    [SerializeField] private float _minSize = 4f;
    [SerializeField] private float _maxSize = 16f;

    [Header("최적화")]
    [SerializeField] private float _renderInterval = 0.05f;     // 미니맵을 다시 그리는 간격(초). 0이면 매 프레임 그린다.
    [SerializeField] private float _farClipMargin = 20f;        // 카메라 높이에 더해 먼 클립 평면으로 쓰는 여유 거리(m)


    private void Awake()
    {
        _camera = GetComponent<Camera>();

        // 시작할 때는 가장 멀리(최소 배율) 보이게 한다.
        SetZoom(_maxSize);

        ApplyLightweightSettings();
    }

    // 미니맵에는 그림자, 후처리, 고해상도 색상 버퍼가 필요 없다.
    private void ApplyLightweightSettings()
    {
        _camera.allowHDR = false;
        _camera.allowMSAA = false;
        _camera.farClipPlane = _height + _farClipMargin;

        if (_camera.TryGetComponent(out UniversalAdditionalCameraData data))
        {
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
        }
        else
        {
            GameLogger.LogWarning("[MinimapCamera] UniversalAdditionalCameraData가 없어 그림자와 후처리를 끄지 못했습니다.");
        }

        // 직접 Render()를 불러 정해진 간격으로만 그린다. 간격이 0이면 카메라가 알아서 매 프레임 그린다.
        _camera.enabled = _renderInterval <= 0f;
    }


    private void Update()
    {
        if (GameInput.MinimapZoomInPressed)
        {
            ZoomIn();
        }
        else if (GameInput.MinimapZoomOutPressed)
        {
            ZoomOut();
        }
    }

    private void LateUpdate()
    {
        // 1. 대상의 머리 위로 위치를 옮긴다.
        transform.position = _target.position + (Vector3.up * _height);

        // 2. 아래를 보되, 대상이 쳐다보는 방향으로 카메라도 회전한다.
        transform.eulerAngles = new Vector3(90, _target.eulerAngles.y, 0f);

        RenderIfDue();
    }

    // 간격이 지났을 때만 미니맵을 그린다. (일시정지 중에도 흐르도록 unscaledTime을 쓴다.)
    private void RenderIfDue()
    {
        if (_renderInterval <= 0f) return;
        if (Time.unscaledTime < _nextRenderTime) return;

        _nextRenderTime = Time.unscaledTime + _renderInterval;
        _camera.Render();
    }

    public void ZoomIn()
    {
        SetZoom(_camera.orthographicSize - _zoomStep);
    }

    public void ZoomOut()
    {
        SetZoom(_camera.orthographicSize + _zoomStep);
    }

    private void SetZoom(float size)
    {
        _camera.orthographicSize = Mathf.Clamp(size, _minSize, _maxSize);

        // 확대/축소는 바로 보이게 한다.
        _nextRenderTime = 0f;
    }
}
