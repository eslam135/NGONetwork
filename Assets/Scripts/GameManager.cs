using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] List<Transform> spawnPoints = new();
    [SerializeField] private GameObject playerPrefab;
    int currIdx = 0;

    private void Start()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
            if(NetworkManager.Singleton.IsHost)
            {
                SceneManager_OnLoadComplete(NetworkManager.Singleton.LocalClientId, SceneManager.GetActiveScene().name, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }

    private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if(sceneName == "GamePlay")
        {
            SpawnPlayer(clientId);
        }
    }

    public void SpawnPlayer(ulong clientId)
    {

        if (currIdx >= spawnPoints.Count)
        {
            currIdx = 0;
        }

        Transform spawnPoint = spawnPoints[currIdx];

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        networkObject.SpawnAsPlayerObject(clientId);
        currIdx++;
    }
}
