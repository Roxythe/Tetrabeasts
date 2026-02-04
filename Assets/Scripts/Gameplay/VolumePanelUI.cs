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

        if (pauseWhenOpen) Time.timeScale = 0f;
        //BringToFront();
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
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
