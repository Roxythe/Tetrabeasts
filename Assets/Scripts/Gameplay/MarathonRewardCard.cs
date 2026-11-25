using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

[RequireComponent(typeof(CanvasGroup))]
public sealed class MarathonRewardCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public MarathonRewardColors colors;
    [FormerlySerializedAs("selectedFlames")] public GameObject selectionAnimation;
    public MarathonRewardUI previewOwner;
    public MarathonSelectionBorder rarityBorder;
    public int choiceIndex;
    bool selected, hovered, focused;
    CanvasGroup group;
    RewardIconBackdrop[] iconEffects;
    public void SetSelected(bool value) { selected = value; Refresh(); }
    public void ResetState() { selected = hovered = focused = false; Refresh(); }
    void OnEnable() => Refresh();
    void OnDisable()
    {
        hovered = focused = false;
        if (previewOwner) previewOwner.TargetReward(choiceIndex, false);
    }
    void Update() => Refresh();
    public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
    public void OnSelect(BaseEventData data) { focused = true; Refresh(); }
    public void OnDeselect(BaseEventData data) { focused = false; Refresh(); }
    void Refresh()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        bool targeted = (hovered && UICursorController.IsPointerTargetMode) ||
            (focused && UICursorController.IsButtonNavigationMode);
        group.alpha = selected || targeted ? 1f : 215f / 255f;
        if (iconEffects == null || iconEffects.Length == 0) iconEffects = GetComponentsInChildren<RewardIconBackdrop>(true);
        foreach (var effect in iconEffects) effect.SetHighlighted(selected || targeted);
        if (selectionAnimation && selectionAnimation.activeSelf != selected) selectionAnimation.SetActive(selected);
        if (previewOwner) previewOwner.TargetReward(choiceIndex, targeted);
    }
}
