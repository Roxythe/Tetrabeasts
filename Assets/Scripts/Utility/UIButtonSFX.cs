using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    [Header("Assigned by GameplayUI_SFXHook")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Header("Options")]
    [Range(0f, 1f)] public float hoverVolume = 1f;
    [Range(0f, 1f)] public float clickVolume = 1f;
    [Min(0f)] public float hoverCooldownSeconds = 0.08f;

    [Tooltip("If true, ignores clicks when the Button is not interactable.")]
    public bool requireInteractable = true;

    [Tooltip("Clears the selected button after mouse clicks while the effective control profile is Keyboard / Mouse.")]
    public bool clearSelectionOnKeyboardMouseClick = true;

    static float s_nextHoverSfxTime;
    static int s_lastPointerUiFrame = -1000;
    static int s_lastClickSfxFrame = -1000;
    static GameObject s_lastClickSfxObject;

    Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        s_lastPointerUiFrame = Time.frameCount;
        PlayHoverSFX();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        s_lastPointerUiFrame = Time.frameCount;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (Time.frameCount - s_lastPointerUiFrame <= 1)
            return;

        PlayHoverSFX();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickSFX();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        s_lastPointerUiFrame = Time.frameCount;

        if (requireInteractable && _btn && !_btn.interactable) return;

        // Left click / primary tap only
        if (eventData.button != PointerEventData.InputButton.Left) return;

        PlayClickSFX();

        ClearSelectionAfterKeyboardMouseClick();
    }

    public void PlayClickSFX()
    {
        if (!AudioManager.I || !clickClip) return;
        if (requireInteractable && _btn && !_btn.interactable) return;
        if (ClickSfxPlayedThisFrame()) return;

        AudioManager.I.PlayUISFX(clickClip, vol: clickVolume);
    }

    bool ClickSfxPlayedThisFrame()
    {
        if (s_lastClickSfxFrame == Time.frameCount && s_lastClickSfxObject == gameObject)
            return true;

        s_lastClickSfxFrame = Time.frameCount;
        s_lastClickSfxObject = gameObject;
        return false;
    }

    void PlayHoverSFX()
    {
        if (!AudioManager.I || !hoverClip) return;
        if (requireInteractable && _btn && !_btn.interactable) return;

        if (hoverCooldownSeconds > 0f)
        {
            float now = Time.unscaledTime;
            if (now < s_nextHoverSfxTime)
                return;

            s_nextHoverSfxTime = now + hoverCooldownSeconds;
        }

        AudioManager.I.PlayUISFX(hoverClip, vol: hoverVolume);
    }

    void ClearSelectionAfterKeyboardMouseClick()
    {
        if (!clearSelectionOnKeyboardMouseClick)
            return;

        if (TetrabeastsControls.EffectiveProfile != TetrabeastsControlProfile.KeyboardMouse)
            return;

        ClearCurrentSelection();

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        StartCoroutine(ClearSelectionAgainNextFrame());
    }

    System.Collections.IEnumerator ClearSelectionAgainNextFrame()
    {
        yield return null;

        ClearCurrentSelection();
    }

    static void ClearCurrentSelection()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem)
            eventSystem.SetSelectedGameObject(null);
    }
}
