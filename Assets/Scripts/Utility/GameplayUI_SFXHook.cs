using UnityEngine;
using UnityEngine.UI;

public class GameplayUI_SFXHook : MonoBehaviour
{
    [Header("UI SFX Clips")]
    public AudioClip uiHoverSFX;
    public AudioClip uiClickSFX;

    [Header("Options")]
    public bool includeInactive = true;

    void Awake()
    {
        HookAllButtonsForSFX();
    }

    [ContextMenu("Rehook All Buttons")]
    public void HookAllButtonsForSFX()
    {
        var buttons = GetComponentsInChildren<Button>(includeInactive);

        foreach (var b in buttons)
        {
            if (!b) continue;

            // If button already has UIButtonSFX, just update clips
            var sfx = b.GetComponent<UIButtonSFX>();
            if (!sfx) sfx = b.gameObject.AddComponent<UIButtonSFX>();

            sfx.hoverClip = uiHoverSFX;
            sfx.clickClip = uiClickSFX;
        }
    }

    public void HookButton(Button b)
    {
        if (!b) return;

        var sfx = b.GetComponent<UIButtonSFX>();
        if (!sfx) sfx = b.gameObject.AddComponent<UIButtonSFX>();

        sfx.hoverClip = uiHoverSFX;
        sfx.clickClip = uiClickSFX;
    }
}
