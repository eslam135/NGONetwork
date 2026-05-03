using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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
    [SerializeField] private RectTransform _healthBarFill;


    [Header("Player Info")]
    [SerializeField] private TMP_Text _nameText;


    private Rigidbody _rb;
    private bool IsDead;

    private NetworkVariable<PlayerData> _playerData;
    private NetworkVariable<int> _health = new();

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        RefreshNameText();
    }

    public override void OnNetworkSpawn()
    {
        _playerData.OnValueChanged += OnPlayerDataChanged;
        _health.OnValueChanged += OnHealthChanged;

        RefreshNameText();

        if (IsOwner)
        {
            string localName = NetworkingManager.Singleton.LocalPlayerName;
            SubmitPlayerNameServerRpc(localName);
        }
    }

    public override void OnNetworkDespawn()
    {
        _playerData.OnValueChanged -= OnPlayerDataChanged;
    }

    private void Update()
    {
        if (IsLocalPlayer && !IsDead)
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

        Vector3 moveInput = new Vector3(moveZ * -1, 0, moveX) * _moveSpeed * Time.deltaTime;
        _rb.MovePosition(transform.position + moveInput);


        if (right || left)
        {
            float rotationDirection = right ? 1 : -1;
            _rotationPivot.transform.Rotate(Vector3.up, rotationDirection * _rotationSpeed * Time.deltaTime * 10);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootBulletRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void SubmitPlayerNameServerRpc(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            newName = "Player";
        }

        _playerData.Value = new();
        _health.Value = _maxHealth;
    }
    [Rpc(SendTo.Everyone)]
    private void ShootBulletRpc()
    {
        BulletManager bullet = Instantiate(_bulletPrefab, _bulletSpawnPoint.position, _bulletSpawnPoint.rotation).GetComponent<BulletManager>();
        bullet.ShooterID = OwnerClientId;

    }
    private void OnPlayerDataChanged(PlayerData oldData, PlayerData newData)
    {
        RefreshNameText();
    }
    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (_healthBarFill != null)
        {
            float healthPercent = (float)newHealth / _maxHealth;
            _healthBarFill.localScale = new Vector3(0.09657639f * healthPercent, 0.03809938f, 0.09657639f);
        }
    }
    private void RefreshNameText()
    {
        if (_nameText != null)
        {
            _nameText.text = _playerData.Value.PlayerName.ToString();
        }
    }
    public void TakeDamage(int damage, ulong killerID)
    {
        if (!IsServer) return;
        _health.Value -= damage;
        if (_health.Value <= 0)
        {
            MarkAsDeadRpc(killerID);
            _health.Value = 0;
        }
    }
    [Rpc(SendTo.Everyone)]
    public void MarkAsDeadRpc(ulong killerID)
    {
        IsDead = true;

        NetworkPlayer killer = NetworkingManager.Singleton.GetPlayer(killerID);
        if (killer != null)
        {
            Debug.Log($"Player {_playerData.Value.PlayerName.ToString()} was killed by player: {killer._playerData.Value.PlayerName.ToString()}");
        }
        else
        {
            Debug.Log($"Player {_playerData.Value.PlayerName.ToString()} was killed by an unknown player.");
        }
    }
}