using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;
using System.Collections.Generic;



public enum TeamID
{
    Red = 0,
    Blue = 1,
}

public class UIMainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField if_PlayerName;
    [SerializeField] private TMP_Dropdown DD_Team;
    [SerializeField] private Button btn_StartHost;
    [SerializeField] private Button btn_StartClient;
    [SerializeField] private Button btn_StartServer;
    private TeamID teamID;

    void Start()
    {
        ValidateName();

        var teamIDs = System.Enum.GetNames(typeof(TeamID));

        List<TMP_Dropdown.OptionData> ddOptions = new();

        foreach(var teamId in teamIDs)
        {
            ddOptions.Add(new TMP_Dropdown.OptionData(teamId));
        }
        DD_Team.options.Clear();
        DD_Team.options = ddOptions;


    }

    void Update()
    {
        ValidateName();
    }

    public void ValidateName()
    {
        if (string.IsNullOrEmpty(if_PlayerName.text))
        {
            btn_StartHost.interactable = false;
            btn_StartClient.interactable = false;
            btn_StartServer.interactable = false;
        }
        else
        {
            btn_StartHost.interactable = true;
            btn_StartClient.interactable = true;
            btn_StartServer.interactable = true;
        }
    }

    public void OnStartHostPressed()
    {
        teamID = (TeamID)System.Enum.Parse(typeof(TeamID), DD_Team.captionText.text);
        NetworkingManager.Singleton.UpdatePlayerName(if_PlayerName.text);
        NetworkingManager.Singleton.StartHost();
    }

    public void OnStartClientPressed()
    {
        teamID = (TeamID)System.Enum.Parse(typeof(TeamID), DD_Team.captionText.text);
        NetworkingManager.Singleton.UpdatePlayerName(if_PlayerName.text);
        NetworkingManager.Singleton.StartClient();
    }

    public void OnStartServerPressed()
    {
        NetworkingManager.Singleton.StartServer();
    }

}
