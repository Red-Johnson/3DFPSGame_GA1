using UnityEngine;

// 시점 전환 연출: 1인칭과 3인칭이 바뀔 때 카메라가 순간이동하지 않고 새 위치로 쑤욱 이동하게 한다.
// CameraViewManager가 전환 순간에 직전 카메라 위치와 회전을 넘기면, 그 뒤 시점 컴포넌트가 정한 새 위치와 회전으로 부드럽게 섞는다.
// 시점 컴포넌트(CameraFollow, ThirdPersonCamera)의 LateUpdate 뒤, 카메라 흔들림(CameraShake) 앞에서 실행된다.
[DefaultExecutionOrder(50)]
public class CameraViewTransition : MonoBehaviour
{
    private Vector3 _fromPosition;
    private Quaternion _fromRotation;
    private float _duration;
    private float _elapsed;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    // 지금 카메라 위치와 회전에서 시작해 duration 초 동안 새 시점의 위치와 회전으로 옮겨 간다.
    public void Begin(Vector3 fromPosition, Quaternion fromRotation, float duration)
    {
        if (duration <= 0f)
        {
            _isPlaying = false;
            return;
        }

        _fromPosition = fromPosition;
        _fromRotation = fromRotation;
        _duration = duration;
        _elapsed = 0f;
        _isPlaying = true;
    }

    private void LateUpdate()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float eased = 1f - ((1f - t) * (1f - t) * (1f - t));    // 처음엔 빠르게 쑤욱, 끝에서 부드럽게 멈춘다.

        // 이 시점의 transform은 새 시점 컴포넌트가 이번 프레임에 정한 목표 위치와 회전이다.
        transform.SetPositionAndRotation(
            Vector3.Lerp(_fromPosition, transform.position, eased),
            Quaternion.Slerp(_fromRotation, transform.rotation, eased));

        if (t >= 1f)
        {
            _isPlaying = false;
        }
    }
}
