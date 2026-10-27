using System;
using UnityEngine;

// G키를 누르면 시선이 바라보는 방향으로 수류탄을 던지고 싶다.
public class PlayerBombFire : MonoBehaviour
{
    private const int BombPoolDefaultCapacity = 5;
    private const int BombPoolMaxSize = 20;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;

    private PrefabPool<Bomb> _bombPool;
    private Camera _camera;

    // 폭탄을 던지면 구독자(총기의 재장전 취소 등)에게 알린다.
    public event Action OnBombThrown;

    private void Awake()
    {
        _camera = Camera.main;
        _bombPool = new PrefabPool<Bomb>(_bombPrefab, "BombPool", BombPoolDefaultCapacity, BombPoolMaxSize);
    }

    private void Update()
    {
        // 1. G키 입력 받기 (게임 중이 아니거나 커서가 풀려 있으면 GameInput이 입력 없음으로 돌려준다.)
        if (GameInput.BombPressed)
        {
            Shoot();
        }

    }

    private void Shoot()
    {
        // 2. 발사 위치에서 수류탄 꺼내기 (풀에서 재사용)
        Bomb bomb = _bombPool.Get(_firePoint.position, Quaternion.identity);

        // 3. 꺼낸 수류탄을 시선 방향으로 던지기
        bomb.Launch(_camera.transform.forward * _throwPower);

        OnBombThrown?.Invoke();
    }

}
