using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TitleMenuUI : MonoBehaviour
{
    [Header("Demo Build Guard Rails")]
    [SerializeField] bool demoBuildGuardRailsEnabled = false;
    [SerializeField, TextArea(2, 4)] string demoPurchaseBlockedMessage =
        "Purchases are disabled in the demo. Your earned progress will still carry into the full game.";

    [Header("Scenes")]
    public string gameplaySceneName = "GameplayScene";

    [Header("Panels (optional)")]
    public GameObject highScorePanel;
    public GameObject settingsPanel;
    public GameObject shopPanel;
    public GameObject helpPanel;
    public GameObject achievementPanel;
    public GameObject codexPanel;
    public HelpMenuUI helpMenuUI;
    public AchievementPanelUI achievementPanelUI;
    public CodexPanelUI codexPanelUI;
    public ShopPanelUI shopPanelUI;
    public VolumePanelUI volumePanelUI;
    public MonsterSelectUI monsterSelectPanel;
    public HighScoreUI highScoreUI;
    public SteamLeaderboardUI steamLeaderboardUI;
    public CharacterSelectUI characterSelectPanel;

    [SerializeField] CharacterSelectUI characterSelectUI;
    [SerializeField] MonsterSelectUI monsterSelectUI;

    [Header("Steam Leaderboard")]
    [SerializeField] GameObject steamLeaderboardPanel;
    [SerializeField] GameObject steamLeaderboardPrefab;
    [SerializeField] Transform steamLeaderboardParent;

    [Header("Temp Run Buttons")]
    public Button continueRunButton;
    public Button deleteTempRunButton;

    [Header("Locked While Temp Run Exists")]
    public Button characterSelectButton;
    public Button monsterSelectButton;
    public Button shopButton;

    [Header("Selected Commander UI")]
    public Transform selectedCommanderParent;
    public GameObject selectedCommanderPrefab;

    [Header("Selected Monsters UI")]
    public Transform selectedMonstersParent;
    public GameObject selectedMonsterIconPrefab;

    [Header("Selected Monster Role Backgrounds")]
    public Sprite attackRoleBackgroundSprite;
    public Sprite defenseRoleBackgroundSprite;
    public Sprite healerRoleBackgroundSprite;

    [Header("Tutorial Popups")]
    [SerializeField] TriggeredTutorialPopupController triggeredTutorialPopups;

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

    TitleStarDifficultyUI _starDifficultyUI;
    readonly Dictionary<Selectable, Navigation> automaticNavigationScope = new();
    GameObject automaticNavigationRoot;
    Vector2 heldNavigationDirection;
    float nextNavigationTime;
    bool eventSystemNavigationSuppressed;

    const string TutorialIdFirstShopOpen = "title_shop_intro";
    const float NavigationRepeatDelay = 0.28f;
    const float NavigationRepeatInterval = 0.12f;
    const string SavedRunLockedMessage =
        "A saved run is waiting. Continue that run or delete the temp save before changing your commander, squad, or shop buffs.";
    const string DeleteTempRunWarningMessage =
    "Deleting the current temp run will permanently erase that saved run. After deleting it, you will be able to change your commander, monsters, and access the shop again. Continue?";


    void Awake()
    {
        ApplyDemoBuildGuardRailsSetting();
        UnlockDeferredDemoAchievementsIfRetail();

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

        if (TryGetValidTempRun(out var tempRun))
            ApplyTempRunSelectionsToStores(tempRun);

        RefreshSelectedLoadoutUI();
    }

    void Start()
    {
        TetrabeastsLocalization.EnsureInitialized();

        ApplyDemoBuildGuardRailsSetting();
        UnlockDeferredDemoAchievementsIfRetail();

        SettingsStore.ApplySavedVolumesToAudio();

        // Start title BGM
        if (AudioManager.I)
            AudioManager.I.PlayTitleMusic();

        if (volumePanelUI && volumePanelUI.uiCursor)
            volumePanelUI.uiCursor.SetScale(SettingsStore.LoadCursorScale());

        // Setup cursor
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        EnsureStarDifficultyUI();
        RefreshTempRunUI();
        HideSteamLeaderboardIfUnavailable();
        RefreshSteamLeaderboardIfVisible();
        HookAllButtonsForSFX(); // Auto-hook all buttons under this menu for click/hover sounds
        TetrabeastsFirstLaunchLanguagePrompt.ShowIfNeeded(transform);
    }

    void OnDisable()
    {
        RestoreAutomaticNavigationScope();
    }

    void ApplyDemoBuildGuardRailsSetting()
    {
        DemoBuildGuardRails.Configure(
            demoBuildGuardRailsEnabled,
            DemoBuildGuardRails.DefaultMaxCompletedLevel,
            demoPurchaseBlockedMessage);
    }

    void UnlockDeferredDemoAchievementsIfRetail()
    {
        if (DemoBuildGuardRails.IsDemoBuild || PlayerProgress.I == null)
            return;

        PlayerProgress.I.UnlockDeferredDemoAchievements();
        AchievementSystem.EvaluateStoredProgressForUnlocks();
        SteamAchievementService.Ensure().SyncPendingExternalUnlocks();
    }

    // --- Button hooks ---
    public void OnStartGame()
    {
        if (TryGetValidTempRun(out _))
        {
            ShowConfirmationPopup(
                "Starting a new game will erase the saved run. Continue?",
                StartFreshGameFromTitle);
            return;
        }

        StartFreshGameFromTitle();
    }

    void StartFreshGameFromTitle()
    {
        TempRunSaveStore.Delete();

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

        LoadGameplayScene();
    }

    public void OnContinueRun()
    {
        if (!TryGetValidTempRun(out var tempRun))
        {
            RefreshTempRunUI();
            return;
        }

        ApplyTempRunSelectionsToStores(tempRun);
        TempRunSaveStore.PrepareResume();

        if (AudioManager.I)
        {
            var clip = startGameSFX ? startGameSFX : AudioManager.I.sfxRestart;
            AudioManager.I.PlaySFX(clip);
            AudioManager.I.StopLevelMusic();
            AudioManager.I.StopMusic();
        }

        if (!LoadGameplayScene())
            TempRunSaveStore.CancelPendingResume();
    }

    public void OnDeleteTempRun()
    {
        if (!TryGetValidTempRun(out _))
        {
            RefreshTempRunUI();
            return;
        }

        ShowConfirmationPopup(
            DeleteTempRunWarningMessage,
            ConfirmDeleteTempRun);
    }

    void ConfirmDeleteTempRun()
    {
        TempRunSaveStore.Delete();
        RunModsStore.ResetAll();
        RefreshTempRunUI();
        RefreshSelectedLoadoutUI();
        characterSelectUI?.RefreshPreview();
        monsterSelectUI?.RefreshAllUI();
        monsterSelectPanel?.RefreshAllUI();
        shopPanelUI?.RefreshAll();
    }

    public void OnToggleHighScore()
    {
        if (!highScorePanel) return;
        bool show = !UIPanelTransition.IsVisible(highScorePanel);

        if (show)
        {
            HighScoreManager.EnsureInitialized(10);
            highScoreUI?.ShowReadOnly();
            highScorePanel.transform.SetAsLastSibling();
        }

        UIPanelTransition.SetVisible(highScorePanel, show);
        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSteamLeaderboard()
    {
        TryToggleSteamLeaderboard();
    }

    public void OnToggleSelectCharacter()
    {
        if (TryGetValidTempRun(out _))
        {
            ShowSavedRunLockedPopup();
            return;
        }

        if (!characterSelectPanel) return;

        var go = characterSelectPanel.gameObject;
        bool show = !UIPanelTransition.IsVisible(go);
        UIPanelTransition.SetVisible(go, show);

        if (show)
            characterSelectPanel.RefreshPreview();  // Show the currently stored pick

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSelectMonster()
    {
        if (TryGetValidTempRun(out _))
        {
            ShowSavedRunLockedPopup();
            return;
        }

        if (!monsterSelectPanel) return;

        var go = monsterSelectPanel.gameObject;
        bool show = !UIPanelTransition.IsVisible(go);
        UIPanelTransition.SetVisible(go, show);

        if (show)
            monsterSelectPanel.RefreshAllUI();

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSettings()
    {
        if (!settingsPanel) return;
        bool show = !UIPanelTransition.IsVisible(settingsPanel);
        UIPanelTransition.SetVisible(settingsPanel, show);

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleShop()
    {
        if (TryGetValidTempRun(out _))
        {
            ShowSavedRunLockedPopup();
            return;
        }

        if (!shopPanel) return;

        bool show = !UIPanelTransition.IsVisible(shopPanel);
        UIPanelTransition.SetVisible(shopPanel, show);

        if (show)
        {
            shopPanelUI?.RefreshAll();

            EnsureTriggeredTutorialPopups();
            if (triggeredTutorialPopups)
            {
                triggeredTutorialPopups.QueueShowOnce(
                    TutorialIdFirstShopOpen,
                    "Welcome to the the shop, here you can purchase permanent buffs to help improve your monster " +
                    "units or other aspects of battle. (Press [F] to Continue)",
                    TutorialPopupView.PopupAnchorPreset.Top,
                    popupAlpha: 1f,
                    pauseGameplay: false,
                    freezePieceGravity: false,
                    allowSkip: true,
                    highlightTarget: null,
                    highlightPadding: default);
            }
        }

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleHelp()
    {
        if (!helpPanel) return;

        bool show = !UIPanelTransition.IsVisible(helpPanel);
        UIPanelTransition.SetVisible(helpPanel, show);

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleAchievement()
    {
        if (!achievementPanel) return;

        bool show = !UIPanelTransition.IsVisible(achievementPanel);
        UIPanelTransition.SetVisible(achievementPanel, show);

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleCodex()
    {
        if (!codexPanel) return;

        bool show = !UIPanelTransition.IsVisible(codexPanel);
        UIPanelTransition.SetVisible(codexPanel, show);

        if (show)
            codexPanelUI?.RebuildAll();

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

        _starDifficultyUI?.RefreshNow();
    }

    public void DEV_ClearAllPrefsAndProgress()
    {
        // Wipe PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SteamCloudSaveService.QueueUpload();

        DEV_ClearAchievementProgress(); // Wipe achievement/stat file 

        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload to reflect cleared progress
    }

    public void DEV_TestAchievementToast()
    {
        AchievementToastManager.I?.DEV_ShowToast(AchievementSystem.Ach.Skins_10);
    }

    public void DEV_ResetAllMonsterXp()
    {
        MonsterProgressStore.ClearAll();

        RunMonsterProgress.BeginRun(SelectedMonstersStore.Active);

        // Refresh monster UI
        monsterSelectUI?.RefreshAllUI();
        monsterSelectPanel?.RefreshAllUI();
    }

    void EnsureStarDifficultyUI()
    {
        if (!_starDifficultyUI)
            _starDifficultyUI = GetComponentInChildren<TitleStarDifficultyUI>(true);

        if (!_starDifficultyUI)
        {
            Debug.LogWarning("TitleMenuUI: No TitleStarDifficultyUI found in children. Add one to your title canvas and wire its references in the inspector.");
            return;
        }

        _starDifficultyUI.Initialize();
    }

    // ESC closes panels
    void Update()
    {
        TetrabeastsControls.RefreshActiveInputProfile();

        GameObject root = GetCurrentNavigationRoot();
        RefreshAutomaticNavigationScope(root);

        if (volumePanelUI && settingsPanel && UIPanelTransition.IsVisible(settingsPanel))
            return;

        if (WasCancelPressedThisFrame() && TryCloseVisiblePanelFromCancelInput(root))
            return;

        if (TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuSubmit) &&
            UINavigationUtility.TrySubmitSelected())
            return;

        HandleSpatialNavigation(root);
    }

    void HandleSpatialNavigation(GameObject root)
    {
        if (!root)
            return;

        bool tabPressed = WasTabPressedThisFrame();
        bool navigationInput = tabPressed ||
            TetrabeastsControls.WasButtonNavigationPressedThisFrame() ||
            TetrabeastsControls.IsButtonNavigationHeld();

        if (!navigationInput)
        {
            ResetNavigationRepeat();
            return;
        }

        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!UINavigationUtility.IsSelectionUsableInside(current, root))
        {
            UINavigationUtility.SelectFirstUsable(root);
            ResetNavigationRepeat();
            return;
        }

        if (tabPressed)
        {
            UINavigationUtility.SelectNextInOrder(root, IsShiftHeld() ? -1 : 1);
            ResetNavigationRepeat();
            return;
        }

        if (!TryConsumeDirectionalNavigation(out Vector2 direction))
            return;

        UINavigationUtility.SelectInDirection(root, direction);
    }

    GameObject GetCurrentNavigationRoot()
    {
        if (ConfirmationPopupUI.TryGetShowingRoot(out var popupRoot))
            return popupRoot;

        if (IsVisiblePanel(settingsPanel)) return settingsPanel;
        if (characterSelectPanel && IsVisiblePanel(characterSelectPanel.gameObject)) return characterSelectPanel.gameObject;
        if (monsterSelectPanel && IsVisiblePanel(monsterSelectPanel.gameObject)) return monsterSelectPanel.gameObject;
        if (IsVisiblePanel(shopPanel)) return shopPanel;
        if (IsVisiblePanel(helpPanel)) return helpPanel;
        if (IsVisiblePanel(achievementPanel)) return achievementPanel;
        if (IsVisiblePanel(codexPanel)) return codexPanel;
        if (IsVisiblePanel(highScorePanel)) return highScorePanel;

        return gameObject;
    }

    bool TryConsumeDirectionalNavigation(out Vector2 direction)
    {
        direction = UINavigationUtility.GetPrimaryDirection(TetrabeastsControls.GetButtonNavigationDirection());
        if (direction.sqrMagnitude < 0.5f)
        {
            ResetNavigationRepeat();
            return false;
        }

        float now = Time.unscaledTime;
        if (direction != heldNavigationDirection)
        {
            heldNavigationDirection = direction;
            nextNavigationTime = now + NavigationRepeatDelay;
            return true;
        }

        if (now < nextNavigationTime)
            return false;

        nextNavigationTime = now + NavigationRepeatInterval;
        return true;
    }

    void ResetNavigationRepeat()
    {
        heldNavigationDirection = Vector2.zero;
        nextNavigationTime = 0f;
    }

    void RefreshAutomaticNavigationScope(GameObject root)
    {
        if (root != automaticNavigationRoot)
        {
            RestoreAutomaticNavigationScope();
            automaticNavigationRoot = root;
            UINavigationUtility.SuppressEventSystemNavigation();
            eventSystemNavigationSuppressed = true;
        }

        UINavigationUtility.SuppressAutomaticNavigation(root, automaticNavigationScope);
    }

    void RestoreAutomaticNavigationScope()
    {
        UINavigationUtility.RestoreAutomaticNavigation(automaticNavigationScope);
        if (eventSystemNavigationSuppressed)
        {
            UINavigationUtility.RestoreEventSystemNavigation();
            eventSystemNavigationSuppressed = false;
        }

        automaticNavigationRoot = null;
    }

    bool TryCloseVisiblePanelFromCancelInput(GameObject root)
    {
        if (ConfirmationPopupUI.TryCancelShowingPopup())
            return true;

        if (root && root != gameObject && TryPressPanelBackOrCloseButton(root))
            return true;

        if (settingsPanel && UIPanelTransition.IsVisible(settingsPanel))
        {
            if (volumePanelUI)
                volumePanelUI.Close();
            else
                HideTitlePanel(settingsPanel);

            return true;
        }

        if (codexPanel && UIPanelTransition.IsVisible(codexPanel)) { HideTitlePanel(codexPanel); return true; }
        if (helpPanel && UIPanelTransition.IsVisible(helpPanel)) { HideTitlePanel(helpPanel); return true; }
        if (achievementPanel && UIPanelTransition.IsVisible(achievementPanel)) { HideTitlePanel(achievementPanel); return true; }
        if (shopPanel && UIPanelTransition.IsVisible(shopPanel)) { HideTitlePanel(shopPanel); return true; }

        if (monsterSelectPanel && UIPanelTransition.IsVisible(monsterSelectPanel.gameObject))
        {
            if (monsterSelectPanel.TryCloseFromInput())
                SetPanelPause(false);

            return true;
        }

        if (characterSelectPanel && UIPanelTransition.IsVisible(characterSelectPanel.gameObject))
        {
            HideTitlePanel(characterSelectPanel.gameObject);
            return true;
        }

        if (highScorePanel && UIPanelTransition.IsVisible(highScorePanel)) { HideTitlePanel(highScorePanel); return true; }

        return false;
    }

    bool TryPressPanelBackOrCloseButton(GameObject root)
    {
        if (!root)
            return false;

        if (monsterSelectPanel && root == monsterSelectPanel.gameObject)
            return false;

        return UINavigationUtility.TryPressNamedButton(root, "Back", "Close", "Cancel");
    }

    void HideTitlePanel(GameObject panel)
    {
        if (!panel)
            return;

        UIPanelTransition.Hide(panel);
        SetPanelPause(false);
    }

    void SetPanelPause(bool paused)
    {
        if (pauseOnPanels)
            Time.timeScale = paused ? 0f : 1f;
    }

    static bool WasCancelPressedThisFrame()
    {
        return Input.GetKeyDown(KeyCode.Escape) ||
               TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuCancel);
    }

    static bool WasTabPressedThisFrame()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        pressed |= keyboard != null && keyboard.tabKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Tab);
#endif

        return pressed;
    }

    static bool IsShiftHeld()
    {
        bool held = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        held |= keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        held |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

        return held;
    }

    static bool IsVisiblePanel(GameObject panel)
    {
        return panel && UIPanelTransition.IsVisible(panel);
    }

    bool TryGetValidTempRun(out TempRunSaveStore.SaveData saveData)
    {
        saveData = null;

        if (!TempRunSaveStore.Exists())
            return false;

        if (TempRunSaveStore.TryLoad(out saveData) && saveData != null)
            return true;

        TempRunSaveStore.Delete();
        return false;
    }

    void ApplyTempRunSelectionsToStores(TempRunSaveStore.SaveData saveData)
    {
        if (saveData == null)
            return;

        var characterRoster = (characterSelectUI && characterSelectUI.roster != null && characterSelectUI.roster.Length > 0)
            ? characterSelectUI.roster
            : (characterSelectPanel ? characterSelectPanel.roster : null);

        if (!string.IsNullOrWhiteSpace(saveData.selectedCharacterName) && characterRoster != null)
        {
            for (int i = 0; i < characterRoster.Length; i++)
            {
                var candidate = characterRoster[i];
                if (!candidate || candidate.displayName != saveData.selectedCharacterName)
                    continue;

                SelectedCharacterStore.Current = candidate;
                SelectedCharacterStore.Save(candidate);
                break;
            }
        }

        var monsterRoster = (monsterSelectUI && monsterSelectUI.roster != null && monsterSelectUI.roster.Length > 0)
            ? monsterSelectUI.roster
            : (monsterSelectPanel ? monsterSelectPanel.roster : null);

        if (monsterRoster != null && saveData.selectedMonsterNames != null && saveData.selectedMonsterNames.Count > 0)
        {
            var restored = new List<MonsterData>();

            for (int i = 0; i < saveData.selectedMonsterNames.Count; i++)
            {
                string wantedName = saveData.selectedMonsterNames[i];
                if (string.IsNullOrWhiteSpace(wantedName))
                    continue;

                for (int j = 0; j < monsterRoster.Length; j++)
                {
                    var candidate = monsterRoster[j];
                    if (!candidate || candidate.monsterName != wantedName || restored.Contains(candidate))
                        continue;

                    restored.Add(candidate);
                    break;
                }
            }

            if (restored.Count > 0)
            {
                SelectedMonstersStore.Active = restored;
                SelectedMonstersStore.SaveNames(restored);
            }
        }
    }

    void RefreshTempRunUI()
    {
        bool hasTempRun = TryGetValidTempRun(out var saveData);
        if (hasTempRun)
            ApplyTempRunSelectionsToStores(saveData);

        if (continueRunButton)
            continueRunButton.interactable = hasTempRun;

        if (deleteTempRunButton)
            deleteTempRunButton.interactable = hasTempRun;

        if (characterSelectButton)
            characterSelectButton.interactable = !hasTempRun;

        if (monsterSelectButton)
            monsterSelectButton.interactable = !hasTempRun;

        if (shopButton)
            shopButton.interactable = !hasTempRun;

        if (hasTempRun)
        {
            if (shopPanel) UIPanelTransition.Hide(shopPanel, true);
            if (monsterSelectPanel) UIPanelTransition.Hide(monsterSelectPanel.gameObject, true);
            if (characterSelectPanel) UIPanelTransition.Hide(characterSelectPanel.gameObject, true);
        }
    }

    bool LoadGameplayScene()
    {
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
            return true;
        }

        Debug.LogError("TitleMenuUI: gameplaySceneName is empty.");
        return false;
    }

    void ShowSavedRunLockedPopup()
    {
        ShowAlertPopup(SavedRunLockedMessage);
    }

    void ShowConfirmationPopup(string body, System.Action onConfirm, System.Action onCancel = null)
    {
        var popup = ConfirmationPopupUI.FindOrCreate();
        if (popup)
        {
            popup.ShowConfirmation(body, onConfirm, onCancel ?? (() => { }), showWarningVisual: true);
            return;
        }

        onConfirm?.Invoke();
    }

    void ShowAlertPopup(string body)
    {
        var popup = ConfirmationPopupUI.FindOrCreate();
        if (popup)
        {
            popup.ShowAlert(body, showWarningVisual: true);
            return;
        }

        Debug.LogWarning(body);
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

    // --- Selected Loadout UI ---

    public void RefreshSelectedLoadoutUI()
    {
        RefreshSelectedCommanderUI();
        RefreshSelectedMonstersUI();
        RefreshSteamLeaderboardIfVisible();
    }

    bool TryToggleSteamLeaderboard()
    {
        if (!IsSteamLeaderboardAvailable())
        {
            HideSteamLeaderboardIfUnavailable();
            if (pauseOnPanels) Time.timeScale = 1f;
            return false;
        }

        var leaderboard = EnsureSteamLeaderboardUI();
        if (!leaderboard)
            return false;

        var panel = steamLeaderboardPanel ? steamLeaderboardPanel : leaderboard.gameObject;
        bool show = !UIPanelTransition.IsVisible(panel);

        if (highScorePanel && UIPanelTransition.IsVisible(highScorePanel))
            UIPanelTransition.Hide(highScorePanel);

        UIPanelTransition.SetVisible(panel, show);

        if (show)
        {
            leaderboard.SetCommanderRoster(GetCommanderRosterForLeaderboard());
            leaderboard.RefreshLeaderboard();
        }

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
        return true;
    }

    SteamLeaderboardUI EnsureSteamLeaderboardUI()
    {
        if (!steamLeaderboardUI)
            steamLeaderboardUI = FindFirstObjectByType<SteamLeaderboardUI>(FindObjectsInactive.Include);

        if (steamLeaderboardUI)
        {
            if (!steamLeaderboardPanel)
                steamLeaderboardPanel = steamLeaderboardUI.gameObject;

            return steamLeaderboardUI;
        }

        var prefab = steamLeaderboardPrefab;
        if (!prefab)
            prefab = Resources.Load<GameObject>("Prefabs/Panels/SteamLeaderboard_Panel");

        if (!prefab)
            prefab = Resources.Load<GameObject>("UI/SteamLeaderboard_Panel");

        if (!prefab)
            return null;

        var parent = steamLeaderboardParent ? steamLeaderboardParent : transform;
        var instance = Instantiate(prefab, parent);
        instance.name = prefab.name;
        UIPanelTransition.Hide(instance, true);

        steamLeaderboardPanel = instance;
        steamLeaderboardUI = instance.GetComponentInChildren<SteamLeaderboardUI>(true);
        return steamLeaderboardUI;
    }

    void RefreshSteamLeaderboardIfVisible()
    {
        if (!IsSteamLeaderboardAvailable())
        {
            HideSteamLeaderboardIfUnavailable();
            return;
        }

        if (!steamLeaderboardUI)
            steamLeaderboardUI = FindFirstObjectByType<SteamLeaderboardUI>(FindObjectsInactive.Include);

        if (!steamLeaderboardUI || !steamLeaderboardUI.gameObject.activeInHierarchy)
            return;

        steamLeaderboardUI.SetCommanderRoster(GetCommanderRosterForLeaderboard());
        steamLeaderboardUI.RefreshLeaderboard();
    }

    bool IsSteamLeaderboardAvailable()
    {
        var service = SteamLeaderboardService.Ensure();
        return service && service.IsAvailable;
    }

    void HideSteamLeaderboardIfUnavailable()
    {
        if (IsSteamLeaderboardAvailable())
            return;

        GameObject panel = steamLeaderboardPanel ? steamLeaderboardPanel : (steamLeaderboardUI ? steamLeaderboardUI.gameObject : null);
        if (panel)
            UIPanelTransition.Hide(panel, true);
    }

    PlayerCharacterData[] GetCommanderRosterForLeaderboard()
    {
        if (characterSelectUI && characterSelectUI.roster != null && characterSelectUI.roster.Length > 0)
            return characterSelectUI.roster;

        if (characterSelectPanel && characterSelectPanel.roster != null && characterSelectPanel.roster.Length > 0)
            return characterSelectPanel.roster;

        return null;
    }

    void RefreshSelectedCommanderUI()
    {
        if (!selectedCommanderParent || !selectedCommanderPrefab)
            return;

        for (int i = selectedCommanderParent.childCount - 1; i >= 0; i--)
            Destroy(selectedCommanderParent.GetChild(i).gameObject);

        var current = SelectedCharacterStore.Current;
        if (!current)
            return;

        var entry = Instantiate(selectedCommanderPrefab, selectedCommanderParent);
        var refs = entry.GetComponent<CommanderIconUIRefs>();

        if (!refs)
        {
            Debug.LogWarning("Selected commander prefab is missing CommanderIconUIRefs.", entry);
            return;
        }

        if (refs.iconImage)
            refs.iconImage.sprite = current.portrait;

        if (refs.borderImage)
            refs.borderImage.sprite = current.defaultBorder;
    }

    void RefreshSelectedMonstersUI()
    {
        if (!selectedMonstersParent || !selectedMonsterIconPrefab)
            return;

        for (int i = selectedMonstersParent.childCount - 1; i >= 0; i--)
            Destroy(selectedMonstersParent.GetChild(i).gameObject);

        if (SelectedMonstersStore.Active == null)
            return;

        foreach (var monster in SelectedMonstersStore.Active)
        {
            if (!monster)
                continue;

            var entry = Instantiate(selectedMonsterIconPrefab, selectedMonstersParent);
            var refs = entry.GetComponent<MonsterIconUIRefs>();

            if (!refs)
            {
                Debug.LogWarning("Selected monster prefab is missing MonsterIconUIRefs.", entry);
                continue;
            }

            int skinIndex = MonsterSkinStore.GetValidSelected(monster);

            if (refs.iconImage)
                refs.iconImage.sprite = MonsterSkinStore.GetPortrait(monster, skinIndex);

            if (refs.roleBackgroundImage)
            {
                var roleSprite = GetRoleBackgroundSprite(monster.role);
                refs.roleBackgroundImage.sprite = roleSprite;
                refs.roleBackgroundImage.enabled = roleSprite != null;
            }
        }
    }

    Sprite GetRoleBackgroundSprite(MonsterRole role)
    {
        return role switch
        {
            MonsterRole.Attack => attackRoleBackgroundSprite,
            MonsterRole.Defense => defenseRoleBackgroundSprite,
            MonsterRole.Healer => healerRoleBackgroundSprite,
            _ => null
        };
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

    void EnsureTriggeredTutorialPopups()
    {
        if (!triggeredTutorialPopups)
            triggeredTutorialPopups = FindFirstObjectByType<TriggeredTutorialPopupController>(FindObjectsInactive.Include);
    }

}
