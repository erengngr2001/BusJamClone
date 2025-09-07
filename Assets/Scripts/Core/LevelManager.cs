using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string PLAYER_PREF_KEY = "CurrentLevelIndex";
    [Tooltip("Optional: explicitly assign LevelData assets here. If empty, Resources/Levels/Level_i is used.")]
    public List<LevelData> levels = new List<LevelData>();

    [Header("Scene Names")]
    public string gameplaySceneName = "Gameplay";
    public string mainMenuSceneName = "MainMenu";

    //[Header("Level Progression Display")]
    //public TextMeshProUGUI levelDisplayText;

    public int CurrentLevelIndex { get; private set; } = 1;
    public int MaxLevelCount => (levels != null && levels.Count > 0) ? levels.Count : Resources.LoadAll<LevelData>("Levels").Length;

    private void Awake()
    {
        //levelDisplayText?.SetText($"Level {CurrentLevelIndex}");
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLevelIndex = PlayerPrefs.GetInt(PLAYER_PREF_KEY, 1);
        if (CurrentLevelIndex < 1) CurrentLevelIndex = 1;
    }

    public void PlayCurrentLevel()
    {
        StartCoroutine(LoadGameplayAndAssignLevelCoroutine(CurrentLevelIndex));
    }

    public void OnLevelCompleted()
    {
        // Always unfreeze the game BEFORE doing anything else.
        Time.timeScale = 1f;

        int max = MaxLevelCount;
        if (max > 0)
            CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex + 1, 1, max);
        else
            CurrentLevelIndex++;

        PlayerPrefs.SetInt(PLAYER_PREF_KEY, CurrentLevelIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    public void OnLevelFailed()
    {
        // Always unfreeze the game BEFORE doing anything else.
        Time.timeScale = 1f;

        PlayerPrefs.SetInt(PLAYER_PREF_KEY, CurrentLevelIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    public LevelData GetLevelData(int levelIndex)
    {
        if (levels != null && levels.Count >= levelIndex && levelIndex > 0)
        {
            return levels[levelIndex - 1];
        }

        string path = $"Levels/Level_{levelIndex}";
        var ld = Resources.Load<LevelData>(path);
        if (ld != null) return ld;

        Debug.LogWarning($"LevelManager: LevelData not found for index {levelIndex}.");
        return null;
    }

    private IEnumerator LoadGameplayAndAssignLevelCoroutine(int levelIndex)
    {
        Debug.Log($"[LevelManager] Loading scene '{gameplaySceneName}' for Level {levelIndex}...");
        AsyncOperation op = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);

        while (!op.isDone)
        {
            yield return null;
        }

        // Wait one frame for all Awake() methods in the new scene to run.
        yield return null;

        LevelData ld = GetLevelData(levelIndex);
        if (ld == null)
        {
            Debug.LogError($"[LevelManager] LevelData for index {levelIndex} is null. Aborting assignment.");
            yield break;
        }

        if (GridSpawner.Instance == null)
        {
            Debug.LogError("[LevelManager] GridSpawner.Instance not found after loading scene. Cannot initialize level.");
            yield break;
        }

        Debug.Log($"[LevelManager] Scene loaded. Initializing GridSpawner with Level '{ld.name}'.");
        GridSpawner.Instance.InitializeWithLevel(ld);
    }

    [ContextMenu("Reset Saved Progress")]
    public void ResetSavedProgress()
    {
        PlayerPrefs.DeleteKey(PLAYER_PREF_KEY);
        CurrentLevelIndex = 1;
        Debug.Log("LevelManager: Saved progress reset to Level 1.");
    }
}
