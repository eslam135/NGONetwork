using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Tank Movement Settings")]
    [SerializeField] private float _maxForwardSpeed = 5f;
    [SerializeField] private float _maxReverseSpeed = 2.5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 14f;
    [SerializeField] private float _bodyRotationSpeed = 120f;

    [Header("Turret Settings")]
    [SerializeField] private float _turretRotationSpeed = 140f;
    [SerializeField] private GameObject _rotationPivot;

    [Header("Bullet Settings")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _bulletSpawnPoint;

    [Header("Ability Settings")]
    [SerializeField] private float _abilityCooldown = 8f;
    [SerializeField] private GameObject _healingZonePrefab;
    [SerializeField] private GameObject _molotovProjectilePrefab;
    [SerializeField] private Transform _abilitySpawnPoint;
    private KeyCode _abilityKey = KeyCode.E;

    [Header("Tank Class Stats")]
    [SerializeField] private int _tankMaxHealth = 180;
    [SerializeField] private float _tankForwardSpeed = 3f;
    [SerializeField] private float _tankReverseSpeed = 1.5f;
    [SerializeField] private int _tankBulletDamage = 10;
    [SerializeField] private int _tankAbilityHealPerTick = 5;

    [Header("DPS Class Stats")]
    [SerializeField] private int _dpsMaxHealth = 70;
    [SerializeField] private float _dpsForwardSpeed = 7f;
    [SerializeField] private float _dpsReverseSpeed = 3.5f;
    [SerializeField] private int _dpsBulletDamage = 30;
    [SerializeField] private int _dpsMolotovDamagePerTick = 8;

    [Header("Runtime Stats")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _bulletDamage = 20;

    [Header("Health Settings")]
    [SerializeField] private Image _healthBarFill;

    [Header("Player Info")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _classText;
    [SerializeField] private Image _teamCircleImage;

    [Header("Local Camera")]
    [SerializeField] private GameObject _localCameraRig;
    [SerializeField] private Camera _localCamera;
    [SerializeField] private AudioListener _localAudioListener;

    private Rigidbody _rb;
    private bool _isDead;

    private float _moveInput;
    private float _turnInput;
    private float _turretTurnInput;
    private float _currentMoveSpeed;
    private float _nextAbilityTime;

    private NetworkVariable<PlayerData> _playerData = new NetworkVariable<PlayerData>(
        new PlayerData
        {
            PlayerName = "Player",
            TeamID = TeamID.Red,
            ClassID = PlayerClassID.Tank
        },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public PlayerData PlayerData => _playerData.Value;
    public string PlayerName => _playerData.Value.PlayerName.ToString();
    public TeamID TeamID => _playerData.Value.TeamID;
    public PlayerClassID ClassID => _playerData.Value.ClassID;
    public bool IsDead => _isDead;

    private NetworkVariable<int> _health = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    public override void OnNetworkSpawn()
    {
        SetupLocalCamera();

        _playerData.OnValueChanged += OnPlayerDataChanged;
        _health.OnValueChanged += OnHealthChanged;

        ApplyClassStats();
        RefreshAllPlayerInfo();
        RefreshHealthBar();

        if (IsOwner)
        {
            string localName = NetworkingManager.Singleton.LocalPlayerName;
            TeamID localTeam = NetworkingManager.Singleton.LocalPlayerTeam;
            PlayerClassID localClass = NetworkingManager.Singleton.LocalPlayerClass;

            SubmitPlayerDataServerRpc(localName, localTeam, localClass);
        }
    }

    public override void OnNetworkDespawn()
    {
        _playerData.OnValueChanged -= OnPlayerDataChanged;
        _health.OnValueChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (!IsLocalPlayer || _isDead)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            return;
        }

        ReadInput();
        HandleTurretRotation();
        HandleShooting();
        HandleAbilityInput();
    }

    private void FixedUpdate()
    {
        if (!IsLocalPlayer || _isDead)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            return;
        }

        HandleTankMovement();
    }

    private void ReadInput()
    {
        _moveInput = Input.GetAxisRaw("Vertical");
        _turnInput = Input.GetAxisRaw("Horizontal");

        bool turretRight = Input.GetKey(KeyCode.RightArrow);
        bool turretLeft = Input.GetKey(KeyCode.LeftArrow);

        if (turretRight)
        {
            _turretTurnInput = 1f;
        }
        else if (turretLeft)
        {
            _turretTurnInput = -1f;
        }
        else
        {
            _turretTurnInput = 0f;
        }
    }

    private void HandleTankMovement()
    {
        if (_rb == null)
        {
            return;
        }

        float targetSpeed = 0f;

        if (_moveInput > 0f)
        {
            targetSpeed = _moveInput * _maxForwardSpeed;
        }
        else if (_moveInput < 0f)
        {
            targetSpeed = _moveInput * _maxReverseSpeed;
        }

        float speedChangeRate = Mathf.Abs(targetSpeed) > 0.01f ? _acceleration : _deceleration;

        _currentMoveSpeed = Mathf.MoveTowards(
            _currentMoveSpeed,
            targetSpeed,
            speedChangeRate * Time.fixedDeltaTime
        );

        Vector3 movement = transform.forward * _currentMoveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + movement);

        if (Mathf.Abs(_turnInput) > 0.01f)
        {
            float turnDirection = _turnInput;

            if (_moveInput < -0.01f)
            {
                turnDirection *= -1f;
            }

            float rotationAmount = turnDirection * _bodyRotationSpeed * Time.fixedDeltaTime;
            Quaternion rotationDelta = Quaternion.Euler(0f, rotationAmount, 0f);

            _rb.MoveRotation(_rb.rotation * rotationDelta);
        }
    }

    private void HandleTurretRotation()
    {
        if (_rotationPivot == null)
        {
            return;
        }

        if (Mathf.Abs(_turretTurnInput) <= 0.01f)
        {
            return;
        }

        float rotationAmount = _turretTurnInput * _turretRotationSpeed * Time.deltaTime;
        _rotationPivot.transform.Rotate(Vector3.up, rotationAmount, Space.Self);
    }

    private void HandleShooting()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (_bulletSpawnPoint == null)
        {
            return;
        }

        RequestShootServerRpc(_bulletSpawnPoint.position, _bulletSpawnPoint.rotation);
    }

    private void HandleAbilityInput()
    {
        if (!Input.GetKeyDown(_abilityKey))
        {
            return;
        }

        if (Time.time < _nextAbilityTime)
        {
            return;
        }

        _nextAbilityTime = Time.time + _abilityCooldown;

        Vector3 spawnPosition = transform.position;

        if (_abilitySpawnPoint != null)
        {
            spawnPosition = _abilitySpawnPoint.position;
        }

        RequestUseAbilityServerRpc(spawnPosition, transform.rotation, _bulletSpawnPoint.rotation);
    }

    private void SetupLocalCamera()
    {
        bool shouldEnableCamera = IsOwner;

        if (_localCameraRig != null)
        {
            _localCameraRig.SetActive(shouldEnableCamera);
        }

        if (_localCamera != null)
        {
            _localCamera.enabled = shouldEnableCamera;
        }

        if (_localAudioListener != null)
        {
            _localAudioListener.enabled = shouldEnableCamera;
        }
    }

    private void ApplyClassStats()
    {
        switch (ClassID)
        {
            case PlayerClassID.Tank:
                _maxHealth = _tankMaxHealth;
                _maxForwardSpeed = _tankForwardSpeed;
                _maxReverseSpeed = _tankReverseSpeed;
                _bulletDamage = _tankBulletDamage;
                break;

            case PlayerClassID.DPS:
                _maxHealth = _dpsMaxHealth;
                _maxForwardSpeed = _dpsForwardSpeed;
                _maxReverseSpeed = _dpsReverseSpeed;
                _bulletDamage = _dpsBulletDamage;
                break;
        }
    }

    private void RefreshAllPlayerInfo()
    {
        RefreshNameText();
        RefreshClassText();
        RefreshTeamCircle();
    }

    private void RefreshNameText()
    {
        if (_nameText == null)
        {
            return;
        }

        string playerName = PlayerName;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        _nameText.text = playerName;
    }

    private void RefreshClassText()
    {
        if (_classText == null)
        {
            return;
        }

        _classText.text = ClassID.ToString();
    }

    private void RefreshTeamCircle()
    {
        if (_teamCircleImage == null)
        {
            return;
        }

        switch (TeamID)
        {
            case TeamID.Red:
                _teamCircleImage.color = Color.red;
                break;

            case TeamID.Blue:
                _teamCircleImage.color = Color.blue;
                break;

            default:
                _teamCircleImage.color = Color.white;
                break;
        }
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

    private void OnPlayerDataChanged(PlayerData oldData, PlayerData newData)
    {
        ApplyClassStats();
        RefreshAllPlayerInfo();
        RefreshHealthBar();
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        RefreshHealthBar();
    }

    [Rpc(SendTo.Server)]
    private void SubmitPlayerDataServerRpc(string newName, TeamID teamID, PlayerClassID classID)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            newName = "Player";
        }

        _playerData.Value = new PlayerData
        {
            PlayerName = new FixedString32Bytes(newName),
            TeamID = teamID,
            ClassID = classID
        };

        ApplyClassStats();

        _health.Value = _maxHealth;
    }

    [Rpc(SendTo.Server)]
    private void RequestShootServerRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (_bulletPrefab == null)
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
            bulletManager.Initialize(OwnerClientId, _bulletDamage);
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

    [Rpc(SendTo.Server)]
    private void RequestUseAbilityServerRpc(Vector3 spawnPosition, Quaternion playerRotation, Quaternion turretRotation)
    {
        switch (ClassID)
        {
            case PlayerClassID.Tank:
                SpawnHealingZone(spawnPosition);
                break;

            case PlayerClassID.DPS:
                SpawnMolotov(spawnPosition, turretRotation);
                break;
        }
    }

    private void SpawnHealingZone(Vector3 spawnPosition)
    {
        if (_healingZonePrefab == null)
        {
            return;
        }

        GameObject zoneObject = Instantiate(
            _healingZonePrefab,
            spawnPosition,
            Quaternion.identity
        );

        HealingZone healingZone = zoneObject.GetComponent<HealingZone>();

        if (healingZone != null)
        {
            healingZone.Initialize(TeamID);
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

    private void SpawnMolotov(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (_molotovProjectilePrefab == null)
        {
            return;
        }

        GameObject molotovObject = Instantiate(
            _molotovProjectilePrefab,
            spawnPosition,
            spawnRotation
        );

        MolotovProjectile molotov = molotovObject.GetComponent<MolotovProjectile>();

        if (molotov != null)
        {
            molotov.Initialize(OwnerClientId, _dpsMolotovDamagePerTick);
        }

        NetworkObject networkObject = molotovObject.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Spawn(true);
        }
        else
        {
            Destroy(molotovObject);
        }
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

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            return;
        }

        _health.Value -= damage;

        if (_health.Value <= 0)
        {
            _health.Value = 0;

            _isDead = true;
            _currentMoveSpeed = 0f;

            MarkAsDeadRpc(killerID);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckForGameOver();
            }
        }
    }
    public void Heal(int healAmount)
    {
        if (!IsServer)
        {
            return;
        }

        if (healAmount <= 0)
        {
            return;
        }

        bool shouldRevive = _isDead || _health.Value <= 0;

        _health.Value = Mathf.Min(_health.Value + healAmount, _maxHealth);

        if (shouldRevive && _health.Value > 0)
        {
            ReviveRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void MarkAsDeadRpc(ulong killerID)
    {
        _isDead = true;
        _currentMoveSpeed = 0f;

        NetworkPlayer killer = NetworkingManager.Singleton.GetPlayer(killerID);

        string deadPlayerName = PlayerName;

        string message;

        if (killer != null)
        {
            string killerName = killer.PlayerName;
            message = $"{killerName} destroyed {deadPlayerName}";
        }
        else
        {
            message = $"{deadPlayerName} was destroyed";
        }

        if (KillFeedUI.Instance != null)
        {
            KillFeedUI.Instance.ShowMessage(message);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ReviveRpc()
    {
        _isDead = false;
        _currentMoveSpeed = 0f;

        RefreshHealthBar();

        if (KillFeedUI.Instance != null)
        {
            KillFeedUI.Instance.ShowMessage($"{PlayerName} was revived");
        }
    }
}