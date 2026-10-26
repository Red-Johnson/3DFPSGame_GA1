using UnityEngine;
using UnityEngine.Pool;

// 총알 궤적: 꼬리가 달린 짧은 선이 총구에서 목표 지점까지 빠르게 날아가고, 끝에 닿으면 풀로 돌아간다.
[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour, IPoolable<BulletTracer>
{
    private LineRenderer _line;
    private IObjectPool<BulletTracer> _pool;

    private Vector3 _start;
    private Vector3 _direction;
    private float _totalLength;     // 시작점에서 목표 지점까지의 거리
    private float _speed;
    private float _tailLength;      // 선의 길이(꼬리)
    private float _traveled;        // 선의 머리가 날아간 거리

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
    }

    public void SetPool(IObjectPool<BulletTracer> pool)
    {
        _pool = pool;
    }

    public void Fire(Vector3 start, Vector3 end, float speed, float tailLength)
    {
        Vector3 toEnd = end - start;

        _start = start;
        _totalLength = toEnd.magnitude;
        _direction = _totalLength > Mathf.Epsilon ? toEnd / _totalLength : Vector3.forward;
        _speed = speed;
        _tailLength = tailLength;
        _traveled = 0f;

        Refresh();
    }

    private void Update()
    {
        _traveled += _speed * Time.deltaTime;

        // 꼬리까지 목표 지점을 지나면 사라진다.
        if (_traveled - _tailLength >= _totalLength)
        {
            Release();
            return;
        }

        Refresh();
    }

    // 머리와 꼬리 위치를 목표 지점을 넘지 않게 맞춘다.
    private void Refresh()
    {
        float headDistance = Mathf.Min(_traveled, _totalLength);
        float tailDistance = Mathf.Clamp(_traveled - _tailLength, 0f, _totalLength);

        _line.SetPosition(0, _start + (_direction * tailDistance));
        _line.SetPosition(1, _start + (_direction * headDistance));
    }

    private void Release()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
