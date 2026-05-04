using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;
using System.Collections.Generic;

public class UIMainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField if_PlayerName;
    [SerializeField] private TMP_Dropdown DD_Team;

    [SerializeField] private Button btn_StartHost;
    [SerializeField] private Button btn_StartClient;
    [SerializeField] private Button btn_StartServer;

    private void Start()
    {
        SetupTeamDropdown();
        ValidateName();
    }

    private void Update()
    {
        ValidateName();
    }

    private void SetupTeamDropdown()
    {
        string[] teamIDs = System.Enum.GetNames(typeof(TeamID));

        List<TMP_Dropdown.OptionData> ddOptions = new();

        foreach (string teamID in teamIDs)
        {
            ddOptions.Add(new TMP_Dropdown.OptionData(teamID));
        }

        DD_Team.options.Clear();
        DD_Team.options = ddOptions;
        DD_Team.value = 0;
        DD_Team.RefreshShownValue();
    }

    public void ValidateName()
    {
        bool hasName = !string.IsNullOrWhiteSpace(if_PlayerName.text);

        btn_StartHost.interactable = hasName;
        btn_StartClient.interactable = hasName;
        btn_StartServer.interactable = hasName;
    }

    public void OnStartHostPressed()
    {
        SaveLocalPlayerData();
        NetworkingManager.Singleton.StartHost();
    }

    public void OnStartClientPressed()
    {
        SaveLocalPlayerData();
        NetworkingManager.Singleton.StartClient();
    }

    public void OnStartServerPressed()
    {
        NetworkingManager.Singleton.StartServer();
    }

    private void SaveLocalPlayerData()
    {
        TeamID selectedTeam = (TeamID)DD_Team.value;
        NetworkingManager.Singleton.UpdateLocalPlayerData(if_PlayerName.text, selectedTeam);
    }
}
