using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private LevelData level;
    private float countdown = 0f;

    [Header("UI Panels")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    public enum GameState { Paused, Running, Won, Lost }
    public GameState State { get; private set; } = GameState.Paused;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // UI setup is not level-dependent, so it can safely happen in Awake.
        if (winScreen == null || loseScreen == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                if (winScreen == null)
                {
                    var win = canvas.transform.Find("WinScreen");
                    if (win != null) winScreen = win.gameObject;
                }
                if (loseScreen == null)
                {
                    var lose = canvas.transform.Find("LoseScreen");
                    if (lose != null) loseScreen = lose.gameObject;
                }
            }
        }

        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
    }

    public void InitializeWithLevel(LevelData newLevel)
    {
        level = newLevel;
        if (level == null)
        {
            Debug.LogError("[GameManager] InitializeWithLevel called with a null level!");
            return;
        }

        countdown = level.GetCountdownTime();
        Time.timeScale = 1f;
        State = GameState.Paused; // Start paused until the first click

        // Ensure UI is hidden at the start of a new level
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        Debug.Log($"[GameManager] Initialized for level '{level.name}' with a countdown of {countdown}s.");
    }

    void Update()
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        if (State != GameState.Running) return;

        if (countdown > 0f)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f)
            {
                countdown = 0f;
                // Time ran out, check for win/loss
                EvaluateGameState();
            }
        }
    }

    public void EvaluateGameState()
    {
        if (State == GameState.Won || State == GameState.Lost) return;

        if (GridSpawner.passengerCount <= 0)
        {
            Win();
        }
        else if (countdown <= 0f)
        {
            Lose("Time ran out");
        }
    }

    public void Win()
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        State = GameState.Won;
        Debug.Log("[GameManager] YOU WIN!");

        //Time.timeScale = 0f;
        if (winScreen != null) winScreen.SetActive(true);

        StartCoroutine(WaitAndComplete());
    }

    public void Lose(string reason = "")
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        State = GameState.Lost;
        Debug.Log($"[GameManager] YOU LOSE! Reason: {reason}");

        //Time.timeScale = 0f;
        if (loseScreen != null) loseScreen.SetActive(true);

        StartCoroutine(WaitAndFail());
    }

    public void SetGameState(GameState newState)
    {
        State = newState;
    }

    public float GetRemainingTime()
    {
        return countdown;
    }

    private IEnumerator WaitAndComplete()
    {
        yield return new WaitForSecondsRealtime(2f); 
        Time.timeScale = 1f;
        LevelManager.Instance?.OnLevelCompleted();
    }

    private IEnumerator WaitAndFail()
    {
        yield return new WaitForSecondsRealtime(2f); 
        Time.timeScale = 1f;
        LevelManager.Instance?.OnLevelFailed();
    }
}
