using UnityEngine;
using UnityEngine.UI;

public class GameplayUI_SFXHook : MonoBehaviour
{
    [Header("UI SFX Clips")]
    public AudioClip uiHoverSFX;
    public AudioClip uiClickSFX;

    [Header("Options")]
    public bool includeInactive = true;

    [Tooltip("If true, hooks again when this object is enabled")]
    public bool rehookOnEnable = true;

    void Awake()
    {
        HookAllButtonsForSFX();
    }

    void OnEnable()
    {
        if (rehookOnEnable)
            HookAllButtonsForSFX();
    }

    [ContextMenu("Rehook All Buttons")]
    public void HookAllButtonsForSFX()
    {
        var buttons = GetComponentsInChildren<Button>(includeInactive);

        foreach (var b in buttons)
        {
            if (!b) continue;
            UIButtonTargetVisual.Ensure(b.gameObject);
            if (b.GetComponent<IgnoreGameplayUIButtonSFX>()) continue;

            var sfx = b.GetComponent<UIButtonSFX>();
            if (!sfx) sfx = b.gameObject.AddComponent<UIButtonSFX>();

            sfx.hoverClip = uiHoverSFX;
            sfx.clickClip = uiClickSFX;
        }
    }

    public void HookButton(Button b)
    {
        if (!b) return;
        UIButtonTargetVisual.Ensure(b.gameObject);
        if (b.GetComponent<IgnoreGameplayUIButtonSFX>()) return;

        var sfx = b.GetComponent<UIButtonSFX>();
        if (!sfx) sfx = b.gameObject.AddComponent<UIButtonSFX>();

        sfx.hoverClip = uiHoverSFX;
        sfx.clickClip = uiClickSFX;
    }
}
