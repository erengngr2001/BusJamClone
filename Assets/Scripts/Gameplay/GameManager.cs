using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    LevelData level;
    [SerializeField] float countdown = 0f;

    [Header("UI Panels")]
    [Tooltip("Panel under Canvas named 'WinScreen'")]
    [SerializeField] private GameObject winScreen;
    [Tooltip("Panel under Canvas named 'LoseScreen'")]
    [SerializeField] private GameObject loseScreen;

    public enum GameState { Running, Paused, Won, Lost }
    public GameState State { get; private set; } = GameState.Running;

    private void Start()
    {
        Instance = this;
        level = GridSpawner.Instance?.level;
        countdown = level.GetCountdownTime();

        // Try to auto-find Win/Lose panels if not assigned in inspector
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

        // Ensure both are initially hidden
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        Time.timeScale = 1f;
        State = GameState.Paused;
    }

    // Update is called once per frame
    void Update()
    {
        PassTime();

        if (State != GameState.Running) return;
        if (level == null) return;

        Debug.Log("Remanining Passengers: " + GridSpawner.passengerCount);
        if (countdown <= 0f)
        {
            int remaining = GridSpawner.passengerCount;
            Debug.Log($"Time ran out! Remaining passengers: {remaining}");
            if (remaining > 0)
            {
                Lose("Time ran out");
            }
            else
            {
                Win();
            }
        }

    }

    public void EvaluateGameState()
    {
        if (State != GameState.Running) return;
        if (GridSpawner.passengerCount == 0)
        {
            Win();
        }
    }

    public void Win()
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        State = GameState.Won;
        Time.timeScale = 0f; // freeze game

        Debug.Log("[GameManager] YOU WIN! All passengers processed.");

        ShowWinUI();
    }

    public void Lose(string reason = null)
    {
        if (State == GameState.Won || State == GameState.Lost) return;
        State = GameState.Lost;
        Time.timeScale = 0f; // freeze game

        Debug.Log($"[GameManager] YOU LOSE! {reason}");

        // Make all passengers unclickable
        DisableAllPassengerColliders();

        ShowLoseUI();
    }

    private void ShowWinUI()
    {
        // e.g. WinCanvas.SetActive(true);
        winScreen.SetActive(true);
        Debug.Log("WIN UI");
    }

    private void ShowLoseUI()
    {
        loseScreen.SetActive(true);
        Debug.Log("LOSE UI");
    }

    /// For editor or restart logic: unfreeze and reset (not fully implemented here).
    public void UnfreezeForTesting()
    {
        Time.timeScale = 1f;
        State = GameState.Running;
    }

    public void SetGameState(GameState newState)
    {
        State = newState;
        if (newState == GameState.Running)
            Time.timeScale = 1f;
        else
            Time.timeScale = 0f;
    }

    public float GetRemainingTime()
    {
        return countdown;
    }   

    void PassTime()
    {
        if (State != GameState.Running) return;
        if (countdown > 0f)
        {
            countdown -= Time.deltaTime;
            if (countdown < 0f) countdown = 0f;
        }
    }

    void DisableAllPassengerColliders()
    {
        // Find all Passenger components in scene (include inactive to be safe)
        Passenger[] allPassengers = FindObjectsOfType<Passenger>(true);
        for (int i = 0; i < allPassengers.Length; i++)
        {
            var p = allPassengers[i];
            if (p == null) continue;

            // mark non-interactable (prevents input action handler from firing)
            p.SetInteractable(false);

            //// disable any colliders on the passenger or its children
            //Collider[] cols = p.GetComponentsInChildren<Collider>(true);
            //if (cols != null)
            //{
            //    foreach (var c in cols)
            //    {
            //        if (c != null) c.enabled = false;
            //    }
            //}

            Collider c = p.GetComponent<Collider>();
            c.enabled = false;
            Debug.Log(GetRemainingTime() + " Disabling collider on " + p.name);

        }
    }
}
