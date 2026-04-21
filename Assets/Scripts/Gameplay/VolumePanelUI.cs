using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class VolumePanelUI : MonoBehaviour
{
    [Header("Refs")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider cursorSizeSlider;
    public UICursorController uiCursor;
    public Button closeButton;
    public TMP_Dropdown musicModeDropdown; // 0=EDM, 1=Metal, 2=Both
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
        cg = GetComponent<CanvasGroup>();
        if (closeButton) closeButton.onClick.AddListener(Close);

        // Wire slider callbacks once
        if (masterSlider) masterSlider.onValueChanged.AddListener(SetMaster);
        if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusic);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFX);
        if (cursorSizeSlider) cursorSizeSlider.onValueChanged.AddListener(SetCursorSize);
        if (musicModeDropdown) musicModeDropdown.onValueChanged.AddListener(SetMusicMode);
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

    System.Collections.IEnumerator PreviewAfterDelay()
    {
        // Works while paused
        yield return new WaitForSecondsRealtime(previewDelay);

        if (AudioManager.I)
            AudioManager.I.PlayPreviewSFX();   // Plays a random line-clear 
    }

    void BringToFront() => transform.SetAsLastSibling();

    public void Open() { gameObject.SetActive(true); }
    public void Close() { gameObject.SetActive(false); }

    public void Toggle()
    {
        if (gameObject.activeSelf) Close();
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
        if (Keyboard.current.escapeKey.wasPressedThisFrame) Toggle();
    }
#else
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }
#endif
}
