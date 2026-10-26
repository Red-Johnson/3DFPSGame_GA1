using UnityEngine;

// 마우스 올느쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던지고 싶다.
public class PlayerBombFire : MonoBehaviour
{
    private const int BombPoolDefaultCapacity = 5;
    private const int BombPoolMaxSize = 20;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;

    private PrefabPool<Bomb> _bombPool;

    private void Awake()
    {
        _bombPool = new PrefabPool<Bomb>(_bombPrefab, "BombPool", BombPoolDefaultCapacity, BombPoolMaxSize);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            Shoot();
        }

    }

    private void Shoot()
    {
        // 2. 발사 위치에서 수류탄 꺼내기 (풀에서 재사용)
        Bomb bomb = _bombPool.Get(_firePoint.position, Quaternion.identity);

        // 3. 꺼낸 수류탄을 시선 방향으로 던지기
        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);
    }

}
