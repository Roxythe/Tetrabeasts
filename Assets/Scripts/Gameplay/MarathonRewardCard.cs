using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public sealed class MarathonRewardCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public MarathonRewardColors colors;
    public GameObject selectedFlames;
    bool selected, hovered, focused;
    CanvasGroup group;
    public void SetSelected(bool value) { selected = value; Refresh(); }
    public void ResetState() { selected = hovered = focused = false; Refresh(); }
    void OnEnable() => Refresh();
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
        group.alpha = selected || targeted ? 1f : (colors ? colors.inactiveAlpha : 0.65f);
        if (selectedFlames && selectedFlames.activeSelf != selected) selectedFlames.SetActive(selected);
    }
}
