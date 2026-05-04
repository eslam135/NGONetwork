using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIMainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField if_PlayerName;
    [SerializeField] private TMP_Dropdown DD_Team;
    [SerializeField] private TMP_Dropdown DD_Class;

    [SerializeField] private Button btn_StartHost;
    [SerializeField] private Button btn_StartClient;
    [SerializeField] private Button btn_StartServer;

    private void Start()
    {
        SetupTeamDropdown();
        SetupClassDropdown();
        ValidateName();
    }

    private void Update()
    {
        ValidateName();
    }

    private void SetupTeamDropdown()
    {
        string[] teamIDs = System.Enum.GetNames(typeof(TeamID));

        List<TMP_Dropdown.OptionData> options = new();

        foreach (string teamID in teamIDs)
        {
            options.Add(new TMP_Dropdown.OptionData(teamID));
        }

        DD_Team.options.Clear();
        DD_Team.options = options;
        DD_Team.value = 0;
        DD_Team.RefreshShownValue();
    }

    private void SetupClassDropdown()
    {
        string[] classIDs = System.Enum.GetNames(typeof(PlayerClassID));

        List<TMP_Dropdown.OptionData> options = new();

        foreach (string classID in classIDs)
        {
            options.Add(new TMP_Dropdown.OptionData(classID));
        }

        DD_Class.options.Clear();
        DD_Class.options = options;
        DD_Class.value = 0;
        DD_Class.RefreshShownValue();
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
        PlayerClassID selectedClass = (PlayerClassID)DD_Class.value;

        NetworkingManager.Singleton.UpdateLocalPlayerData(
            if_PlayerName.text,
            selectedTeam,
            selectedClass
        );
    }
}