using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TitleMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    public string gameplaySceneName = "GameplayScene";

    [Header("Panels (optional)")]
    public GameObject highScorePanel;
    public GameObject settingsPanel;
    public GameObject shopPanel;
    public GameObject helpPanel;
    public GameObject achievementPanel;
    public HelpMenuUI helpMenuUI;
    public AchievementPanelUI achievementPanelUI;
    public ShopPanelUI shopPanelUI;
    public VolumePanelUI volumePanelUI;
    public MonsterSelectUI monsterSelectPanel;
    public HighScoreUI highScoreUI;
    public CharacterSelectUI characterSelectPanel;

    [SerializeField] CharacterSelectUI characterSelectUI;
    [SerializeField] MonsterSelectUI monsterSelectUI;

    [Header("Pause While Panels Open?")]
    public bool pauseOnPanels = false;

    [Header("Defaults")]
    public PlayerCharacterData defaultCharacter;
    public MonsterData defaultA;
    public MonsterData defaultB;

    [Header("Audio (Title)")]
    public AudioClip titleBGM;
    [Range(0f, 1f)] public float titleBGMVolume = 0.5f;

    public AudioClip startGameSFX;
    public AudioClip uiClickSFX;
    public AudioClip uiHoverSFX;

    [Header("Cursor (Menu)")]
    public UICursorController uiCursorController;

    void Awake()
    {
        // --- Character ---
        if (characterSelectUI && characterSelectUI.roster != null && characterSelectUI.roster.Length > 0)
        {
            var savedChar = SelectedCharacterStore.ResolveFromRoster(characterSelectUI.roster);
            if (savedChar != null && UnlockStore.IsUnlocked(savedChar))
            {
                SelectedCharacterStore.Current = savedChar;
            }
            else
            {
                var fallback = GetFirstUnlockedCharacter(characterSelectUI.roster) ?? defaultCharacter ?? characterSelectUI.roster[0];
                SelectedCharacterStore.Current = fallback;
                SelectedCharacterStore.Save(fallback); // Persist so first launch has a stable default
            }

            characterSelectUI.RefreshPreview();
        }

        // --- Monsters (deck) ---
        if (monsterSelectUI && monsterSelectUI.roster != null && monsterSelectUI.roster.Length > 0)
        {
            var resolved = SelectedMonstersStore.ResolveFromRoster(monsterSelectUI.roster);
            var sanitized = SanitizeMonsterSelection(resolved, monsterSelectUI.roster);

            SelectedMonstersStore.Active = sanitized;
            SelectedMonstersStore.SaveNames(sanitized); // Persist sanitized defaults / locked removals

            monsterSelectUI.RefreshAllUI();
        }
    }

    void Start()
    {
        SettingsStore.ApplySavedVolumesToAudio();

        // Start title BGM
        if (AudioManager.I)
            AudioManager.I.PlayTitleMusic();

        if (volumePanelUI && volumePanelUI.uiCursor)
            volumePanelUI.uiCursor.SetScale(SettingsStore.LoadCursorScale());

        // Setup cursor
        volumePanelUI.uiCursor.SetScale(SettingsStore.LoadCursorScale());
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        HookAllButtonsForSFX(); // Auto-hook all buttons under this menu for click/hover sounds
    }

    // --- Button hooks ---
    public void OnStartGame()
    {
        // Stop title BGM
        if (AudioManager.I)
        {
            var clip = startGameSFX ? startGameSFX : AudioManager.I.sfxRestart;
            AudioManager.I.PlaySFX(clip);
            AudioManager.I.StopLevelMusic(); // Safe even if not playing level music
            AudioManager.I.StopMusic();      // Stops title music source
        }

        // Ensure character exists (and is unlocked) before loading Gameplay
        if (SelectedCharacterStore.Current == null || !UnlockStore.IsUnlocked(SelectedCharacterStore.Current))
        {
            PlayerCharacterData fallback = null;

            if (defaultCharacter != null && UnlockStore.IsUnlocked(defaultCharacter))
                fallback = defaultCharacter;
            else if (characterSelectPanel != null && characterSelectPanel.roster != null && characterSelectPanel.roster.Length > 0)
                fallback = GetFirstUnlockedCharacter(characterSelectPanel.roster) ?? characterSelectPanel.roster[0];
            else if (characterSelectUI != null && characterSelectUI.roster != null && characterSelectUI.roster.Length > 0)
                fallback = GetFirstUnlockedCharacter(characterSelectUI.roster) ?? characterSelectUI.roster[0];

            if (fallback != null)
            {
                SelectedCharacterStore.Current = fallback;
                SelectedCharacterStore.Save(fallback);
            }
            else
            {
                Debug.LogWarning("No character selected and no roster configured. Gameplay will use its own fallback if set.");
            }
        }

        // Ensure monster deck is valid, unlocked, and within [2 to 4].
        var rosterRef = (monsterSelectPanel && monsterSelectPanel.roster != null && monsterSelectPanel.roster.Length > 0)
            ? monsterSelectPanel.roster
            : (monsterSelectUI && monsterSelectUI.roster != null && monsterSelectUI.roster.Length > 0 ? monsterSelectUI.roster : null);

        SelectedMonstersStore.Active = SanitizeMonsterSelection(SelectedMonstersStore.Active, rosterRef);
        SelectedMonstersStore.SaveNames(SelectedMonstersStore.Active);

        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);

        RunModsStore.ResetAll();

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
            highScoreUI?.ShowReadOnly();
        }

        highScorePanel.SetActive(show);
        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSelectCharacter()
    {
        if (!characterSelectPanel) return;

        var go = characterSelectPanel.gameObject;
        bool show = !go.activeSelf;
        go.SetActive(show);

        if (show)
            characterSelectPanel.RefreshPreview();  // Show the currently stored pick

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSelectMonster()
    {
        if (!monsterSelectPanel) return;

        var go = monsterSelectPanel.gameObject;
        bool show = !go.activeSelf;
        go.SetActive(show);

        if (show)
            monsterSelectPanel.RefreshAllUI();

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSettings()
    {
        if (!settingsPanel) return;
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleShop()
    {
        if (!shopPanel) return;

        bool show = !shopPanel.activeSelf;
        shopPanel.SetActive(show);

        if (show)
            shopPanelUI?.RefreshAll();

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleHelp()
    {
        if (!helpPanel) return;

        bool show = !helpPanel.activeSelf;
        helpPanel.SetActive(show);

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleAchievement()
    {
        if (!achievementPanel) return;

        bool show = !achievementPanel.activeSelf;
        achievementPanel.SetActive(show);

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnQuitGame()
    {
        PlayerProgress.I?.EndRun();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void DEV_ClearAchievementProgress()
    {
        if (PlayerProgress.I != null)
            PlayerProgress.I.DEV_ClearAllProgress();
    }

    public void DEV_ClearAllPrefsAndProgress()
    {
        // Wipe PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Wipe achievement/stat file 
        DEV_ClearAchievementProgress();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload to reflect cleared progress
    }

    public void DEV_TestAchievementToast()
    {
        AchievementToastManager.I?.DEV_ShowToast(AchievementSystem.Ach.Gold_100);
    }

    // ESC closes panels
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel && settingsPanel.activeSelf) { OnToggleSettings(); return; }
            if (highScorePanel && highScorePanel.activeSelf) { OnToggleHighScore(); return; }
        }
    }


    PlayerCharacterData GetFirstUnlockedCharacter(PlayerCharacterData[] roster)
    {
        if (roster == null) return null;
        foreach (var c in roster)
            if (c != null && UnlockStore.IsUnlocked(c))
                return c;
        return null;
    }

    List<MonsterData> SanitizeMonsterSelection(List<MonsterData> resolved, MonsterData[] roster)
    {
        var outList = new List<MonsterData>();

        // Keep only unlocked, unique, and cap at 4.
        if (resolved != null)
        {
            foreach (var md in resolved)
            {
                if (md == null) continue;
                if (!UnlockStore.IsUnlocked(md)) continue;
                if (outList.Contains(md)) continue;
                outList.Add(md);
                if (outList.Count >= 4) break;
            }
        }

        // Ensure minimum of 2 selections.
        if (outList.Count < 2)
        {
            // Prefer configured defaults if unlocked
            if (defaultA != null && UnlockStore.IsUnlocked(defaultA) && !outList.Contains(defaultA))
                outList.Add(defaultA);
            if (outList.Count < 2 && defaultB != null && UnlockStore.IsUnlocked(defaultB) && !outList.Contains(defaultB))
                outList.Add(defaultB);

            // Fill remaining from roster in order (unlocked only)
            if (roster != null)
            {
                for (int i = 0; i < roster.Length && outList.Count < 2; i++)
                {
                    var md = roster[i];
                    if (md == null) continue;
                    if (!UnlockStore.IsUnlocked(md)) continue;
                    if (!outList.Contains(md)) outList.Add(md);
                }
            }
        }

        // If nothing is unlocked
        if (outList.Count < 2 && roster != null)
        {
            for (int i = 0; i < roster.Length && outList.Count < 2; i++)
            {
                var md = roster[i];
                if (md == null) continue;
                if (!outList.Contains(md)) outList.Add(md);
            }
        }

        return outList;
    }

    // --- SFX hooking ---
    void HookAllButtonsForSFX()
    {
        var buttons = GetComponentsInChildren<Button>(includeInactive: true);
        foreach (var b in buttons)
        {
            b.onClick.AddListener(() => { if (AudioManager.I) AudioManager.I.PlaySFX(uiClickSFX); });

            // Hover
            var hover = b.gameObject.GetComponent<global::UIButtonSFX>();
            if (!hover) hover = b.gameObject.AddComponent<global::UIButtonSFX>();
            hover.hoverClip = uiHoverSFX;
            hover.clickClip = uiClickSFX;
        }
    }

    // --- Helper Functions ---
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Clear any leftover hardware cursor

        if (uiCursorController)
            uiCursorController.SetVisible(true);
    }

}
