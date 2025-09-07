using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI levelDisplayText;

    private void Start()
    {
        SetupButtons();
        UpdateLevelText();
    }

    public void PlayGame()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[MainMenu] LevelManager.Instance is null. Make sure LevelManager exists and is initialized.");
            return;
        }

        Debug.Log("[MainMenu] PlayGame clicked -> LevelManager.PlayCurrentLevel()");
        LevelManager.Instance.PlayCurrentLevel();
    }

    private void UpdateLevelText()
    {
        if (levelDisplayText != null)
        {
            if (LevelManager.Instance != null)
            {
                levelDisplayText.text = $"Level {LevelManager.Instance.CurrentLevelIndex}";
            }
            else
            {
                Debug.LogError("[MainMenu] LevelManager not found! Cannot display level text.");
                levelDisplayText.text = "Level ?";
            }
        }
        else
        {
            Debug.LogError("[MainMenu] Level Display Text is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Call this from a "Reset Progress" button in your main menu for easy testing.
    /// </summary>
    public void ResetProgress()
    {
        if (LevelManager.Instance != null)
        {
            Debug.Log("[MainMenu] Reset Progress button clicked.");
            LevelManager.Instance.ResetSavedProgress();

            if (levelDisplayText != null)
            {
                levelDisplayText.text = $"Level {LevelManager.Instance.CurrentLevelIndex}";
            }
        }
    }

    private void SetupButtons()
    {
        // Ensure the button reference is not null
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(PlayGame);
        }
        else
        {
            Debug.LogError("[MainMenu] Start Button is not assigned in the Inspector!");
        }

        // Ensure the button reference is not null
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetProgress);
        }
        else
        {
            Debug.LogError("[MainMenu] Reset Button is not assigned in the Inspector!");
        }

        // Ensure the text reference is not null
        if (levelDisplayText != null)
        {
            if (LevelManager.Instance != null)
            {
                levelDisplayText.text = $"Level {LevelManager.Instance.CurrentLevelIndex}";
            }
            else
            {
                Debug.LogError("[MainMenu] LevelManager not found! Cannot display level text.");
                levelDisplayText.text = "Level ?";
            }
        }
        else
        {
            Debug.LogError("[MainMenu] Level Display Text is not assigned in the Inspector!");
        }
    }
}
