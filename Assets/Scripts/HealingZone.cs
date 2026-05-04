using Unity.Netcode;
using UnityEngine;

public class HealingZone : NetworkBehaviour
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private float _duration = 6f;
    [SerializeField] private int _healPerTick = 5;
    [SerializeField] private float _tickRate = 1f;

    private TeamID _ownerTeam;
    private float _lifeTimer;
    private float _tickTimer;

    public void Initialize(TeamID ownerTeam)
    {
        _ownerTeam = ownerTeam;
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
            HealPlayersInRange();
        }

        if (_lifeTimer <= 0f)
        {
            DespawnZone();
        }
    }

    private void HealPlayersInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius);

        foreach (Collider hit in hits)
        {
            NetworkPlayer player = hit.GetComponentInParent<NetworkPlayer>();

            if (player == null)
            {
                continue;
            }

            if (player.TeamID != _ownerTeam)
            {
                continue;
            }

            player.Heal(_healPerTick);
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