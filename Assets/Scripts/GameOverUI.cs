using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _gameOverText;

    private void Awake()
    {
        Instance = this;

        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }
    }

    public void ShowGameOver(TeamID winningTeam)
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }

        if (_gameOverText != null)
        {
            _gameOverText.text = $"{winningTeam} Team Wins!";
        }
    }
}