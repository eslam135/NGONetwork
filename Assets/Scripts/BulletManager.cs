using Unity.Netcode;
using UnityEngine;

public class BulletManager : NetworkBehaviour
{
    [Header("Bullet Data")]
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _lifeTime = 3f;

    [Header("Hit VFX")]
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private float _hitEffectLifetime = 2f;
    private Rigidbody _rb;
    private int _damage;

    public ulong ShooterID { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Initialize(ulong shooterID, int damage)
    {
        ShooterID = shooterID;
        _damage = damage;

        IgnoreShooterCollision();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            if (_rb != null)
            {
                _rb.isKinematic = true;
            }

            return;
        }

        if (_rb != null)
        {
            _rb.linearVelocity = transform.forward * _bulletSpeed;
        }

        Invoke(nameof(DespawnBullet), _lifeTime);
    }

    private void IgnoreShooterCollision()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(ShooterID, out NetworkClient shooterClient))
        {
            return;
        }

        if (shooterClient.PlayerObject == null)
        {
            return;
        }

        Collider[] shooterColliders = shooterClient.PlayerObject.GetComponentsInChildren<Collider>();
        Collider[] bulletColliders = GetComponentsInChildren<Collider>();

        foreach (Collider shooterCollider in shooterColliders)
        {
            foreach (Collider bulletCollider in bulletColliders)
            {
                Physics.IgnoreCollision(bulletCollider, shooterCollider, true);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        ContactPoint contactPoint = collision.GetContact(0);

        SpawnHitEffectRpc(
            contactPoint.point,
            Quaternion.LookRotation(contactPoint.normal)
        );

        NetworkPlayer attackedPlayer = collision.gameObject.GetComponentInParent<NetworkPlayer>();

        if (attackedPlayer == null)
        {
            DespawnBullet();
            return;
        }

        NetworkPlayer shooterPlayer = NetworkingManager.Singleton.GetPlayer(ShooterID);

        if (shooterPlayer == null)
        {
            DespawnBullet();
            return;
        }

        if (attackedPlayer.OwnerClientId == ShooterID)
        {
            DespawnBullet();
            return;
        }

        if (attackedPlayer.TeamID == shooterPlayer.TeamID)
        {
            DespawnBullet();
            return;
        }

        attackedPlayer.TakeDamage(_damage, ShooterID);

        DespawnBullet();
    }
    private void DespawnBullet()
    {
        if (!IsServer)
        {
            return;
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnHitEffectRpc(Vector3 position, Quaternion rotation)
    {
        if (_hitEffectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(_hitEffectPrefab, position, rotation);
        Destroy(effect, _hitEffectLifetime);
    }
}