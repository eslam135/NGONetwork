using UnityEngine;
using Unity.Netcode;

public class NetworkingManager : NetworkManager
{
    public static new NetworkingManager Singleton { get; private set; }

    private const string GAME_SCENE_NAME = "GamePlay";

    private string _localPlayerName = "Player";
    private TeamID _localPlayerTeam = TeamID.Red;

    public string LocalPlayerName => _localPlayerName;
    public TeamID LocalPlayerTeam => _localPlayerTeam;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        OnServerStarted += () =>
        {
            Debug.Log("Server started.");
            SceneManager.LoadScene(GAME_SCENE_NAME, UnityEngine.SceneManagement.LoadSceneMode.Single);
        };
    }

    public void UpdateLocalPlayerData(string newName, TeamID teamID)
    {
        _localPlayerName = string.IsNullOrWhiteSpace(newName) ? "Player" : newName;
        _localPlayerTeam = teamID;
    }

    public NetworkPlayer GetPlayer(ulong clientID)
    {
        if (NetworkManager.Singleton == null)
        {
            return null;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientID, out NetworkClient networkClient))
        {
            if (networkClient.PlayerObject == null)
            {
                return null;
            }

            NetworkPlayer player = networkClient.PlayerObject.GetComponent<NetworkPlayer>();

            if (player != null)
            {
                return player;
            }
        }

        return null;
    }
}