using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 좀비 이동: NavMeshAgent를 감싸서 길찾기 이동, 정지, 바라보기, NavMeshLink 점프를 맡는다.
// 컴포넌트가 아닌 일반 클래스라 Zombie가 만들어 쓴다. 점프 코루틴은 Zombie가 실행해 준다.
public class ZombieMotor
{
    private readonly NavMeshAgent _agent;
    private readonly Transform _transform;
    private readonly ZombieStats _stats;

    // NavMeshLink를 건너는 중인지. 건너는 동안은 Zombie가 상태 Update를 건너뛴다.
    public bool IsJumping { get; private set; }

    public ZombieMotor(NavMeshAgent agent, ZombieStats stats)
    {
        _agent = agent;
        _transform = agent.transform;
        _stats = stats;

        _agent.speed = stats.MoveSpeed;
        _agent.angularSpeed = stats.AngularSpeed;
        _agent.autoTraverseOffMeshLink = false;     // 링크는 Jump_Coroutine에서 직접 건넌다.
    }

    // 활성화될 때마다 에이전트를 처음 상태로 되돌린다. (오브젝트 풀링으로 재사용해도 같은 경로)
    public void Enable()
    {
        IsJumping = false;     // 점프 중에 풀로 돌아갔어도 재사용될 때 초기화한다.

        _agent.enabled = true;
        _agent.avoidancePriority = Random.Range(_stats.MinAvoidancePriority, _stats.MaxAvoidancePriority + 1);

        // 풀에서 재사용될 때 NavMesh에 붙지 못했다면 가까운 NavMesh 위로 옮긴다.
        if (!_agent.isOnNavMesh)
        {
            _agent.Warp(_transform.position);
        }

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = false;
        }
    }

    public void Disable()
    {
        _agent.enabled = false;
    }

    public bool IsOnOffMeshLink => _agent.enabled && _agent.isOnNavMesh && _agent.isOnOffMeshLink;

    // NavMesh 경로를 계산해 목표 위치로 이동을 시작한다. 경로를 만들 수 없으면 false.
    public bool SetDestination(Vector3 target)
    {
        if (!_agent.isOnNavMesh) return false;

        return _agent.SetDestination(target);
    }

    public void SetStoppingDistance(float distance)
    {
        _agent.stoppingDistance = distance;
    }

    // 걷는 상태에 들어갈 때 부른다. (정지했던 에이전트를 다시 움직일 수 있게 한다.)
    public void ResumeMoving()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = false;
    }

    // 제자리에 서는 상태에 들어갈 때 부른다.
    public void StopMoving()
    {
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;     // 남은 속도로 미끄러지며 목표를 지나쳤다 돌아오는 떨림을 막는다.
    }

    // 목적지에 도착했거나 갈 수 없는 경로면 true. 경로 계산 중에는 false.
    public bool HasFinishedPath()
    {
        if (!_agent.isOnNavMesh || _agent.pathPending) return false;

        return _agent.pathStatus == NavMeshPathStatus.PathInvalid || _agent.remainingDistance <= _agent.stoppingDistance;
    }

    // 에이전트가 회전을 맡지 않을 때(멈춤, 점프) 매 프레임 불러서, 높이는 무시하고 목표 쪽으로 회전 속도에 맞춰 돈다.
    public void FaceToward(Vector3 target)
    {
        Vector3 direction = target - _transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < Mathf.Epsilon) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation, _stats.AngularSpeed * Time.deltaTime);
    }

    // NavMeshLink의 시작점에서 도착점까지 포물선으로 뛰어 건넌다.
    public IEnumerator Jump_Coroutine()
    {
        IsJumping = true;

        OffMeshLinkData linkData = _agent.currentOffMeshLinkData;
        Vector3 start = _transform.position;
        // 링크 끝점은 NavMesh 표면 위치이므로 피벗 높이(Base Offset)를 더해 준다.
        Vector3 end = linkData.endPos + (Vector3.up * _agent.baseOffset);

        float elapsed = 0f;
        while (elapsed < _stats.JumpDuration)
        {
            elapsed += Time.deltaTime;
            FaceToward(end);
            float t = Mathf.Clamp01(elapsed / _stats.JumpDuration);

            // 직선 이동에 포물선(4h * t * (1 - t))을 더한다.
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += 4f * _stats.JumpHeight * t * (1f - t);
            _transform.position = position;

            yield return null;
        }

        _transform.position = end;

        // 점프 도중 죽어서 에이전트가 꺼졌다면 건넌 자리에 그대로 둔다.
        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.CompleteOffMeshLink();
        }

        IsJumping = false;
    }
}
