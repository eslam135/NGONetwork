using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private GameObject _rotationPivot;

    [Header("Bullet Settings")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _bulletSpawnPoint;

    [Header("Health Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _damageAmount = 20;
    [SerializeField] private Image _healthBarFill;

    [Header("Player Info")]
    [SerializeField] private TMP_Text _nameText;

    private Rigidbody _rb;
    private bool _isDead;

    private NetworkVariable<PlayerData> _playerData = new NetworkVariable<PlayerData>(
        new PlayerData
        {
            PlayerName = "Player",
            TeamID = TeamID.Red
        },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public PlayerData PlayerData => _playerData.Value;
    public string PlayerName => _playerData.Value.PlayerName.ToString();
    public TeamID TeamID => _playerData.Value.TeamID;

    private NetworkVariable<int> _health = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        _playerData.OnValueChanged += OnPlayerDataChanged;
        _health.OnValueChanged += OnHealthChanged;

        RefreshNameText();
        RefreshHealthBar();

        if (IsOwner)
        {
            string localName = NetworkingManager.Singleton.LocalPlayerName;
            TeamID localTeam = NetworkingManager.Singleton.LocalPlayerTeam;

            SubmitPlayerDataServerRpc(localName, localTeam);
        }
    }

    public override void OnNetworkDespawn()
    {
        _playerData.OnValueChanged -= OnPlayerDataChanged;
        _health.OnValueChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (IsLocalPlayer && !_isDead)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool right = Input.GetKey(KeyCode.RightArrow);
        bool left = Input.GetKey(KeyCode.LeftArrow);

        Vector3 moveInput = new Vector3(moveX * 1f, 0f, moveZ) * _moveSpeed * Time.deltaTime;

        _rb.MovePosition(transform.position + moveInput);

        if (_rotationPivot != null && (right || left))
        {
            float rotationDirection = right ? 1f : -1f;
            _rotationPivot.transform.Rotate(Vector3.up, rotationDirection * _rotationSpeed * Time.deltaTime * 10f);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestShootServerRpc(_bulletSpawnPoint.position, _bulletSpawnPoint.rotation);
        }
    }

    [Rpc(SendTo.Server)]
    private void SubmitPlayerDataServerRpc(string newName, TeamID teamID)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            newName = "Player";
        }

        _playerData.Value = new PlayerData
        {
            PlayerName = new FixedString32Bytes(newName),
            TeamID = teamID
        };

        _health.Value = _maxHealth;
    }

    [Rpc(SendTo.Server)]
    private void RequestShootServerRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (_bulletPrefab == null || _bulletSpawnPoint == null)
        {
            return;
        }

        GameObject bulletObject = Instantiate(
            _bulletPrefab,
            spawnPosition,
            spawnRotation
        );

        BulletManager bulletManager = bulletObject.GetComponent<BulletManager>();

        if (bulletManager != null)
        {
            bulletManager.Initialize(OwnerClientId);
        }

        NetworkObject bulletNetworkObject = bulletObject.GetComponent<NetworkObject>();

        if (bulletNetworkObject != null)
        {
            bulletNetworkObject.Spawn(true);
        }
        else
        {
            Destroy(bulletObject);
        }
    }
    private void OnPlayerDataChanged(PlayerData oldData, PlayerData newData)
    {
        RefreshNameText();
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        RefreshHealthBar();
    }

    private void RefreshNameText()
    {
        if (_nameText == null)
        {
            return;
        }

        string playerName = _playerData.Value.PlayerName.ToString();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        _nameText.text = playerName;
    }

    private void RefreshHealthBar()
    {
        if (_healthBarFill == null)
        {
            return;
        }

        float healthPercent = Mathf.Clamp01((float)_health.Value / _maxHealth);
        _healthBarFill.fillAmount = healthPercent;
    }

    public void TakeDamage(int damage, ulong killerID)
    {
        if (!IsServer)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }

        _health.Value -= damage;

        if (_health.Value <= 0)
        {
            _health.Value = 0;
            MarkAsDeadRpc(killerID);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void MarkAsDeadRpc(ulong killerID)
    {
        _isDead = true;

        NetworkPlayer killer = NetworkingManager.Singleton.GetPlayer(killerID);

        string deadPlayerName = _playerData.Value.PlayerName.ToString();
        string deadPlayerTeam = _playerData.Value.TeamID.ToString();

        if (killer != null)
        {
            string killerName = killer._playerData.Value.PlayerName.ToString();
            string killerTeam = killer._playerData.Value.TeamID.ToString();

            Debug.Log($"Player {deadPlayerName} from team {deadPlayerTeam} was killed by player {killerName} from team {killerTeam}");
        }
        else
        {
            Debug.Log($"Player {deadPlayerName} from team {deadPlayerTeam} was killed by an unknown player.");
        }
    }
}