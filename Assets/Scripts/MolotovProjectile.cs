using Unity.Netcode;
using UnityEngine;

public class MolotovProjectile : NetworkBehaviour
{
    [SerializeField] private float _throwForce = 12f;
    [SerializeField] private GameObject _aoeZonePrefab;

    private Rigidbody _rb;
    private ulong _shooterID;
    private int _damagePerTick;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Initialize(ulong shooterID, int damagePerTick)
    {
        _shooterID = shooterID;
        _damagePerTick = damagePerTick;
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
            Vector3 throwVelocity = transform.forward * _throwForce;
            _rb.linearVelocity = throwVelocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        SpawnAOEZone();
        DespawnProjectile();
    }

    private void SpawnAOEZone()
    {
        if (_aoeZonePrefab == null)
        {
            return;
        }

        GameObject zoneObject = Instantiate(
            _aoeZonePrefab,
            transform.position,
            Quaternion.identity
        );

        MolotovAOEZone zone = zoneObject.GetComponent<MolotovAOEZone>();

        if (zone != null)
        {
            zone.Initialize(_shooterID, _damagePerTick);
        }

        NetworkObject networkObject = zoneObject.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(true);
        }
        else
        {
            Destroy(zoneObject);
        }
    }

    private void DespawnProjectile()
    {
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