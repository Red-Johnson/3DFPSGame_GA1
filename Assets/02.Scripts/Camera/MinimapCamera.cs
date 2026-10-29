using UnityEngine;


// 미니맵 카메라: 플레이어 머리 위에서 아래를 내려다보며 따라다니고, 확대/축소를 담당한다.
public class MinimapCamera : MonoBehaviour
{
    private Camera _camera;

    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;

    [Header("확대/축소")]
    [SerializeField] private float _zoomStep = 2f;
    [SerializeField] private float _minSize = 4f;
    [SerializeField] private float _maxSize = 16f;


    private void Awake()
    {
        _camera = GetComponent<Camera>();
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
    }
}
