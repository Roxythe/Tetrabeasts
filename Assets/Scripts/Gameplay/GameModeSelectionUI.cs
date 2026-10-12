using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class GameModeSelectionUI : MonoBehaviour
{
    public Button standardButton, chaosButton, marathonButton, closeButton;
    public TMP_Text lockDescription;
    public RectTransform tooltip;
    public TMP_Text tooltipTitle, tooltipDescription, tooltipLockNote;
    public float tooltipGap = 18f;
    Button tooltipOwner;
    Action<RunGameMode> selected;
    void Awake()
    {
        standardButton.onClick.AddListener(() => Pick(RunGameMode.Standard));
        chaosButton.onClick.AddListener(() => Pick(RunGameMode.Chaos));
        marathonButton.onClick.AddListener(() => Pick(RunGameMode.Marathon));
        closeButton.onClick.AddListener(Close);
    }
    public void Show(int standardLevelCount, Action<RunGameMode> onSelected)
    {
        selected = onSelected;
        bool unlocked = GameModeStore.ExtraModesUnlocked(standardLevelCount);
        chaosButton.interactable = marathonButton.interactable = unlocked;
        if (lockDescription) lockDescription.gameObject.SetActive(false);
        HideTooltip();
        transform.SetAsLastSibling();
        UIPanelTransition.Show(gameObject, true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(standardButton.gameObject);
    }
    void Pick(RunGameMode mode)
    {
        if (mode == RunGameMode.Chaos && !chaosButton.interactable ||
            mode == RunGameMode.Marathon && !marathonButton.interactable) return;
        var callback = selected;
        Close(); callback?.Invoke(mode);
    }
    public void ShowTooltip(Button button, string description)
    {
        if (!tooltip) return;
        tooltipOwner = button;
        tooltipTitle.text = button.GetComponentInChildren<TMP_Text>().text;
        tooltipDescription.text = description;
        tooltipLockNote.text = "Reach the final level in Standard to unlock this game mode.";
        tooltipLockNote.gameObject.SetActive(!button.interactable);
        var rect = (RectTransform)button.transform;
        // Place relative to the button, independent of canvas scale and menu transitions.
        tooltip.position = rect.TransformPoint(new Vector3(rect.rect.xMax + tooltipGap, rect.rect.center.y));
        tooltip.gameObject.SetActive(true);
        tooltip.SetAsLastSibling();
    }
    public void HideTooltip(Button owner = null)
    {
        if (owner && owner != tooltipOwner) return;
        tooltipOwner = null;
        if (tooltip) tooltip.gameObject.SetActive(false);
    }
    void OnDisable() => HideTooltip();
    public void Close() { HideTooltip(); UIPanelTransition.Hide(gameObject, true); }
}
