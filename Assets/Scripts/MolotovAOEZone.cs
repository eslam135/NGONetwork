using Unity.Netcode;
using UnityEngine;

public class MolotovAOEZone : NetworkBehaviour
{
    [SerializeField] private float _radius = 4f;
    [SerializeField] private float _duration = 5f;
    [SerializeField] private float _tickRate = 1f;

    private ulong _shooterID;
    private int _damagePerTick;

    private float _lifeTimer;
    private float _tickTimer;

    public void Initialize(ulong shooterID, int damagePerTick)
    {
        _shooterID = shooterID;
        _damagePerTick = damagePerTick;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        _lifeTimer = _duration;
        _tickTimer = _tickRate;
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        _lifeTimer -= Time.deltaTime;
        _tickTimer -= Time.deltaTime;

        if (_tickTimer <= 0f)
        {
            _tickTimer = _tickRate;
            DamagePlayersInRange();
        }

        if (_lifeTimer <= 0f)
        {
            DespawnZone();
        }
    }

    private void DamagePlayersInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius);

        foreach (Collider hit in hits)
        {
            NetworkPlayer player = hit.GetComponentInParent<NetworkPlayer>();

            if (player == null)
            {
                continue;
            }

            player.TakeDamage(_damagePerTick, _shooterID);
        }
    }

    private void DespawnZone()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}