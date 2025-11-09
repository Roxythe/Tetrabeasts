using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TitleMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Exact name of your gameplay scene (as it appears in Build Settings).")]
    public string gameplaySceneName = "GameplayScene";

    [Header("Panels (optional)")]
    public GameObject highScorePanel;   // set if you have a HS panel in the title scene
    public GameObject settingsPanel;    // reuse your Volume panel here if you like
    public HighScoreUI highScoreUI;
    public CharacterSelectUI characterSelectPanel;

    [Header("Pause While Panels Open?")]
    public bool pauseOnPanels = false;  // usually false for title screen

    [Header("Default Character (optional)")]
    public PlayerCharacterData defaultCharacter;

    void Awake()
    {
        // Ensure panels start hidden
        if (highScorePanel) highScorePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        if (pauseOnPanels) Time.timeScale = 1f;
        HighScoreManager.EnsureInitialized(10);
    }

    // --- Button hooks ---
    public void OnStartGame()
    {
        // Ensure we have a character before loading Gameplay
        if (SelectedCharacterStore.Current == null)
        {
            if (defaultCharacter != null)
                SelectedCharacterStore.Current = defaultCharacter;
            else if (characterSelectPanel != null &&
                     characterSelectPanel.roster != null &&
                     characterSelectPanel.roster.Length > 0)
                SelectedCharacterStore.Current = characterSelectPanel.roster[0];
            else
                Debug.LogWarning("No character selected and no default/roster configured. Gameplay will use its own fallback if set.");
        }

        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);
        if (!string.IsNullOrEmpty(gameplaySceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        else
            Debug.LogError("TitleMenuUI: gameplaySceneName is empty.");
    }

    public void OnToggleHighScore()
    {
        if (!highScorePanel) return;
        bool show = !highScorePanel.activeSelf;

        if (show)
        {
            HighScoreManager.EnsureInitialized(10);
            highScoreUI?.ShowReadOnly();        // build & show table, no input
        }

        highScorePanel.SetActive(show);
        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSelectCharacter()
    {
        if (!characterSelectPanel) return;

        var go = characterSelectPanel.gameObject;   // <- toggle the GO
        bool show = !go.activeSelf;
        go.SetActive(show);

        if (show)
            characterSelectPanel.RefreshPreview();  // show the currently stored pick

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }


    public void OnToggleSettings()
    {
        if (!settingsPanel) return;
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);
        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnQuitGame()
    {
        // Save anything needed, then quit
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Optional: ESC closes panels
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel && settingsPanel.activeSelf) { OnToggleSettings(); return; }
            if (highScorePanel && highScorePanel.activeSelf) { OnToggleHighScore(); return; }
        }
    }
}
