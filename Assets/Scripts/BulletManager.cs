using UnityEngine;

public class BulletManager : MonoBehaviour
{
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private int _bulletDamage = 20;
    private Rigidbody _rb;
    public ulong ShooterID { get; set; }
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = transform.forward * _bulletSpeed;
        Destroy(gameObject, 3f);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            NetworkPlayer player = collision.gameObject.GetComponent<NetworkPlayer>();
            if(player != null)
            {
                player.TakeDamage(_bulletDamage, ShooterID);
            }
        }
        Destroy(gameObject);
    }
}
