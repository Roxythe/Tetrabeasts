using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class VolumePanelUI : MonoBehaviour
{
    static readonly string[] MusicModeOptionLabels = { "EDM", "Metal", "Random" };

    [Header("Refs")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider cursorSizeSlider;
    public UICursorController uiCursor;
    public Button closeButton;
    public TMP_Dropdown musicModeDropdown; // 0=EDM, 1=Metal, 2=Both
    public TMP_Dropdown languageDropdown;
    public TMP_Text languageLabel;
    public Toggle combatLogToggle;
    public GameObject combatLogRoot;
    public Toggle skipIntroToggle;
    public GameController gameplayController;

    [Header("SFX Preview")]
    public bool previewOnChange = true;
    [Range(0.05f, 0.5f)] public float previewDelay = 0.15f;
    Coroutine sfxPreviewCo;

    [Header("Pause")]
    public bool pauseWhenOpen = true;

    CanvasGroup cg;

    void Awake()
    {
        TetrabeastsLocalization.EnsureInitialized();
        EnsureLanguageDropdown();
        RefreshMusicModeDropdown();

        cg = GetComponent<CanvasGroup>();
        if (closeButton) closeButton.onClick.AddListener(Close);

        // Wire slider callbacks once
        if (masterSlider) masterSlider.onValueChanged.AddListener(SetMaster);
        if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusic);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFX);
        if (cursorSizeSlider) cursorSizeSlider.onValueChanged.AddListener(SetCursorSize);
        if (musicModeDropdown) musicModeDropdown.onValueChanged.AddListener(SetMusicMode);
        if (languageDropdown) languageDropdown.onValueChanged.AddListener(SetLanguage);
        if (combatLogToggle) combatLogToggle.onValueChanged.AddListener(SetCombatLogVisible);
        if (skipIntroToggle) skipIntroToggle.onValueChanged.AddListener(SetSkipIntroEnabled);
    }

    void OnEnable()
    {
        // Apply saved settings first then snap sliders
        SettingsStore.ApplySavedVolumesToAudio();

        if (AudioManager.I)
        {
            masterSlider?.SetValueWithoutNotify(AudioManager.I.masterVolume);
            musicSlider?.SetValueWithoutNotify(AudioManager.I.musicVolume);
            sfxSlider?.SetValueWithoutNotify(AudioManager.I.sfxVolume);
        }

        // Cursor size uses SettingsStore
        cursorSizeSlider?.SetValueWithoutNotify(SettingsStore.LoadCursorScale());

        if (musicModeDropdown && AudioManager.I)
            musicModeDropdown.SetValueWithoutNotify((int)AudioManager.I.GetMusicMode());

        RefreshLanguageControls();
        TetrabeastsLocalization.LanguageChanged += RefreshLanguageControls;

        if (pauseWhenOpen) Time.timeScale = 0f;

        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

        if (combatLogRoot)
            combatLogRoot.SetActive(SettingsStore.LoadCombatLogEnabled());

        if (!combatLogRoot)
        {
            var log = FindFirstObjectByType<BattleLogUI>(FindObjectsInactive.Include);
            combatLogRoot = log ? log.gameObject : null;
        }

        if (combatLogToggle)
            combatLogToggle.SetIsOnWithoutNotify(SettingsStore.LoadCombatLogEnabled());

        if (skipIntroToggle)
            skipIntroToggle.SetIsOnWithoutNotify(SettingsStore.LoadSkipIntroEnabled());
    }

    void OnDisable()
    {
        TetrabeastsLocalization.LanguageChanged -= RefreshLanguageControls;
        if (pauseWhenOpen) Time.timeScale = 1f;
    }

    void SetMaster(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMasterVolume(v);
        SettingsStore.SaveVolumes(v, SettingsStore.LoadMusic(), SettingsStore.LoadSFX());
    }

    void SetMusic(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMusicVolume(v);
        SettingsStore.SaveVolumes(SettingsStore.LoadMaster(), v, SettingsStore.LoadSFX());
    }

    void SetSFX(float v)
    {
        if (AudioManager.I) AudioManager.I.SetSFXVolume(v);
        SettingsStore.SaveVolumes(SettingsStore.LoadMaster(), SettingsStore.LoadMusic(), v);

        if (!previewOnChange) return;

        // Play once after the slider settles
        if (sfxPreviewCo != null) StopCoroutine(sfxPreviewCo);
        sfxPreviewCo = StartCoroutine(PreviewAfterDelay());
    }

    void SetCursorSize(float v)
    {
        uiCursor?.SetScale(v);
        SettingsStore.SaveCursorScale(v);
    }

    void SetMusicMode(int index)
    {
        if (!AudioManager.I) return;

        var mode = (AudioManager.MusicMode)Mathf.Clamp(index, 0, 2);
        AudioManager.I.SetMusicMode(mode);
    }

    void SetLanguage(int index)
    {
        TetrabeastsLocalization.SetLanguageByDropdownIndex(index, persist: true);
        RefreshLanguageControls();
    }

    void RefreshLanguageControls()
    {
        if (languageLabel)
            languageLabel.text = TetrabeastsLocalization.GetText("settings_language_label");

        RefreshMusicModeDropdown();

        if (languageDropdown)
            TetrabeastsLocalization.PopulateLanguageDropdown(languageDropdown);
    }

    void RefreshMusicModeDropdown()
    {
        if (!musicModeDropdown)
            return;

        int selectedIndex = AudioManager.I ? (int)AudioManager.I.GetMusicMode() : musicModeDropdown.value;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, MusicModeOptionLabels.Length - 1);

        var options = new List<TMP_Dropdown.OptionData>(MusicModeOptionLabels.Length);
        for (int i = 0; i < MusicModeOptionLabels.Length; i++)
            options.Add(new TMP_Dropdown.OptionData(TetrabeastsLocalization.LocalizeText(MusicModeOptionLabels[i])));

        musicModeDropdown.ClearOptions();
        musicModeDropdown.AddOptions(options);
        musicModeDropdown.SetValueWithoutNotify(selectedIndex);
        musicModeDropdown.RefreshShownValue();
    }

    void EnsureLanguageDropdown()
    {
        if (languageDropdown || !musicModeDropdown)
            return;

        var parent = musicModeDropdown.transform.parent;
        if (!parent)
            return;

        var languageDropdownInstance = Instantiate(musicModeDropdown, parent);
        languageDropdownInstance.name = "Language_Dropdown";
        languageDropdown = languageDropdownInstance;

        var dropdownRect = languageDropdown.GetComponent<RectTransform>();
        var sourceRect = musicModeDropdown.GetComponent<RectTransform>();
        if (dropdownRect && sourceRect)
            dropdownRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -40f);

        var musicLabel = parent.Find("MusicGenre_Text")?.GetComponent<TMP_Text>();
        if (musicLabel)
        {
            languageLabel = Instantiate(musicLabel, parent);
            languageLabel.name = "Language_Text";
            languageLabel.text = TetrabeastsLocalization.GetText("settings_language_label");

            var labelRect = languageLabel.GetComponent<RectTransform>();
            var musicLabelRect = musicLabel.GetComponent<RectTransform>();
            if (labelRect && musicLabelRect)
                labelRect.anchoredPosition = musicLabelRect.anchoredPosition + new Vector2(0f, -40f);
        }

        TetrabeastsLocalization.PopulateLanguageDropdown(languageDropdown);
    }

    System.Collections.IEnumerator PreviewAfterDelay()
    {
        // Works while paused
        yield return new WaitForSecondsRealtime(previewDelay);

        if (AudioManager.I)
            AudioManager.I.PlayPreviewSFX();   // Plays a random line-clear 
    }

    void BringToFront() => transform.SetAsLastSibling();

    public void Open() { UIPanelTransition.Show(gameObject); }
    public void Close() { Close(false); }
    public void Close(bool instant) { UIPanelTransition.Hide(gameObject, instant); }

    public void Toggle()
    {
        if (UIPanelTransition.IsVisible(gameObject)) Close();
        else Open();
    }

    void SetCombatLogVisible(bool isOn)
    {
        SettingsStore.SaveCombatLogEnabled(isOn);

        var log = FindFirstObjectByType<BattleLogUI>(FindObjectsInactive.Include);
        if (log) log.SetVisible(isOn);
    }

    void SetSkipIntroEnabled(bool isOn)
    {
        SettingsStore.SaveSkipIntroEnabled(isOn);
    }

    public void OnSaveAndQuitPressed()
    {
        var controller = GetGameplayController();
        if (!controller)
        {
            Debug.LogWarning("VolumePanelUI: No GameController found for Save and Quit.");
            return;
        }

        controller.RequestSaveAndQuit();
    }

    GameController GetGameplayController()
    {
        if (!gameplayController)
            gameplayController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);

        return gameplayController;
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (!UIPanelTransition.IsVisible(gameObject))
            return;

        if (ShouldLetGameplayPauseHandleEscape())
            return;

        if (!ConfirmationPopupUI.TryCancelShowingPopup())
            Toggle();
    }
#else
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (!UIPanelTransition.IsVisible(gameObject))
            return;

        if (ShouldLetGameplayPauseHandleEscape())
            return;

        if (!ConfirmationPopupUI.TryCancelShowingPopup())
            Toggle();
    }
#endif

    bool ShouldLetGameplayPauseHandleEscape()
    {
        return !pauseWhenOpen && GetGameplayController() && GetGameplayController().IsPaused;
    }
}
