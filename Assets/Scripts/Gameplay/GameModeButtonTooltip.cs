using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Separate from Button so locked modes can still explain their unlock requirement.
public sealed class GameModeButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public GameModeSelectionUI menu;
    [TextArea] public string description;
    public void OnPointerEnter(PointerEventData data) => menu.ShowTooltip(GetComponent<Button>(), description);
    public void OnPointerExit(PointerEventData data) => menu.HideTooltip(GetComponent<Button>());
    public void OnSelect(BaseEventData data) => menu.ShowTooltip(GetComponent<Button>(), description);
    public void OnDeselect(BaseEventData data) => menu.HideTooltip(GetComponent<Button>());
}
