using UnityEngine;
using UnityEngine.AI;

// 로밍: 현재 위치에서 일정 거리 안의 무작위 위치로 걸어간다. 도착하면 대기하고, 플레이어를 발견하면 추격한다.
public class ZombieRoamingState : ZombieStateBase
{
    private const float NavMeshSampleDistance = 2f;     // 목표 위치를 NavMesh 위로 보정하는 최대 거리
    private const float ArriveDistance = 0.3f;          // 이 거리 안이면 도착으로 본다.
    private const float TimeoutMultiplier = 2f;         // 장애물에 막혀도 영원히 걷지 않도록 걷는 시간에 두는 여유 배율

    private Vector3 _target;
    private bool _hasTarget;
    private float _timer;
    private float _timeout;

    public ZombieRoamingState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _timer = 0f;
        _timeout = (_zombie.RoamDistance / _zombie.MoveSpeed) * TimeoutMultiplier;

        Vector2 offset = Random.insideUnitCircle * _zombie.RoamDistance;
        Vector3 candidate = _zombie.transform.position + new Vector3(offset.x, 0f, offset.y);

        _zombie.SetStoppingDistance(ArriveDistance);
        _zombie.ResumeMoving();
        _zombie.PlayWalkAnimation();

        _hasTarget = NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas);
        _target = hit.position;

        // 목적지까지 갈 수 있는 경로를 못 만들면 곧바로 쉬러 간다.
        if (_hasTarget)
        {
            _hasTarget = _zombie.SetDestination(_target);
        }
    }

    public override void Update()
    {
        if (_zombie.IsPlayerInDetectRange())
        {
            _zombie.ChangeState(_zombie.TraceState);
            return;
        }

        // 갈 수 있는 자리를 못 찾았거나, 도착했거나, 너무 오래 걸으면 다시 쉰다.
        _timer += Time.deltaTime;
        if (!_hasTarget || _timer >= _timeout || _zombie.HasFinishedPath())
        {
            _zombie.ChangeState(_zombie.IdleState);
        }
    }
}
