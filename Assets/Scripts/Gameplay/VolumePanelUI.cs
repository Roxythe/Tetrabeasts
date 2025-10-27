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
    }

    void OnEnable()
    {
        // Snap sliders to current values from AudioManager
        if (AudioManager.I)
        {
            masterSlider?.SetValueWithoutNotify(AudioManager.I.masterVolume);
            musicSlider?.SetValueWithoutNotify(AudioManager.I.musicVolume);
            sfxSlider?.SetValueWithoutNotify(AudioManager.I.sfxVolume);
        }

        if (pauseWhenOpen) Time.timeScale = 0f;
        BringToFront();
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
    }

    void OnDisable()
    {
        if (pauseWhenOpen) Time.timeScale = 1f;
    }

    void SetMaster(float v) { if (AudioManager.I) AudioManager.I.SetMasterVolume(v); }
    void SetMusic(float v) { if (AudioManager.I) AudioManager.I.SetMusicVolume(v); }
    
    void SetSFX(float v)
    {
        if (AudioManager.I) AudioManager.I.SetSFXVolume(v);

        if (!previewOnChange) return;
        // debounce: restart a short timer; play once after the slider settles
        if (sfxPreviewCo != null) StopCoroutine(sfxPreviewCo);
        sfxPreviewCo = StartCoroutine(PreviewAfterDelay());
    }

    System.Collections.IEnumerator PreviewAfterDelay()
    {
        // Works while paused
        yield return new WaitForSecondsRealtime(previewDelay);

        if (AudioManager.I)
            AudioManager.I.PlayPreviewSFX();   // plays a random line-clear (or fallback)
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
