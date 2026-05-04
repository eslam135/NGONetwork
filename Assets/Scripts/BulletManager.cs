using Unity.Netcode;
using UnityEngine;

public class BulletManager : NetworkBehaviour
{
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private int _bulletDamage = 20;
    [SerializeField] private float _lifeTime = 3f;

    private Rigidbody _rb;

    public ulong ShooterID { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Initialize(ulong shooterID)
    {
        ShooterID = shooterID;
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
            Debug.Log("Shooting Myself, Abort!");
            DespawnBullet();
            return;
        }

        if (attackedPlayer.TeamID == shooterPlayer.TeamID)
        {
            Debug.Log("FriendlyFire, Abort!");
            DespawnBullet();
            return;
        }

        attackedPlayer.TakeDamage(_bulletDamage, ShooterID);

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
}