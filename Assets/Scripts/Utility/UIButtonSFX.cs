using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
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

    static float s_nextHoverSfxTime;

    Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!AudioManager.I || !clickClip) return;
        if (requireInteractable && _btn && !_btn.interactable) return;

        // Left click / primary tap only
        if (eventData.button != PointerEventData.InputButton.Left) return;

        AudioManager.I.PlayUISFX(clickClip, vol: clickVolume);
    }
}
