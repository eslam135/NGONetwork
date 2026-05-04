using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<Transform> spawnPoints = new();
    [SerializeField] private GameObject playerPrefab;

    private int _currentSpawnIndex = 0;
    private bool _gameOver;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;

            if (NetworkManager.Singleton.IsHost)
            {
                SceneManager_OnLoadComplete(
                    NetworkManager.Singleton.LocalClientId,
                    SceneManager.GetActiveScene().name,
                    LoadSceneMode.Single
                );
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= SceneManager_OnLoadComplete;
        }
    }

    private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (sceneName == "GamePlay")
        {
            SpawnPlayer(clientId);
        }
    }

    public void SpawnPlayer(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (playerPrefab == null || spawnPoints.Count == 0)
        {
            Debug.LogError("GameManager is missing player prefab or spawn points.");
            return;
        }

        if (_currentSpawnIndex >= spawnPoints.Count)
        {
            _currentSpawnIndex = 0;
        }

        Transform spawnPoint = spawnPoints[_currentSpawnIndex];

        GameObject playerInstance = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("Player prefab is missing NetworkObject.");
            Destroy(playerInstance);
            return;
        }

        networkObject.SpawnAsPlayerObject(clientId);

        _currentSpawnIndex++;
    }

    public void CheckForGameOver()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (_gameOver)
        {
            return;
        }

        int redPlayers = 0;
        int bluePlayers = 0;

        int redDeadPlayers = 0;
        int blueDeadPlayers = 0;

        foreach (KeyValuePair<ulong, NetworkClient> connectedClient in NetworkManager.Singleton.ConnectedClients)
        {
            if (connectedClient.Value.PlayerObject == null)
            {
                continue;
            }

            NetworkPlayer player = connectedClient.Value.PlayerObject.GetComponent<NetworkPlayer>();

            if (player == null)
            {
                continue;
            }

            switch (player.TeamID)
            {
                case TeamID.Red:
                    redPlayers++;

                    if (player.IsDead)
                    {
                        redDeadPlayers++;
                    }

                    break;

                case TeamID.Blue:
                    bluePlayers++;

                    if (player.IsDead)
                    {
                        blueDeadPlayers++;
                    }

                    break;
            }
        }

        bool redTeamLost = redPlayers > 0 && redDeadPlayers >= redPlayers;
        bool blueTeamLost = bluePlayers > 0 && blueDeadPlayers >= bluePlayers;

        if (redTeamLost)
        {
            EndGame(TeamID.Blue);
            return;
        }

        if (blueTeamLost)
        {
            EndGame(TeamID.Red);
        }
    }

    private void EndGame(TeamID winningTeam)
    {
        _gameOver = true;

        ShowGameOverRpc(winningTeam);

        if (KillFeedUI.Instance != null)
        {
            KillFeedUI.Instance.ShowMessage($"{winningTeam} Team Wins!");
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShowGameOverRpc(TeamID winningTeam)
    {
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver(winningTeam);
        }
    }

    public bool IsGameOver()
    {
        return _gameOver;
    }
}