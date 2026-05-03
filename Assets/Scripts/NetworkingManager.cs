using UnityEngine;
using Unity.Netcode;

public class NetworkingManager : NetworkManager
{
    public static new NetworkingManager Singleton { get; private set; }

    const string GAME_SCENE_NAME = "GamePlay";

    private string PlayerName = "Player";
    public string LocalPlayerName => PlayerName;

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

    public void UpdatePlayerName(string newName)
    {
        PlayerName = string.IsNullOrWhiteSpace(newName) ? "Player" : newName;
    }
    public NetworkPlayer GetPlayer(ulong clientID)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientID, out NetworkClient networkClient))
        {
            NetworkPlayer player = networkClient.PlayerObject.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                return player;
            }
        }
        return null;
    }
}