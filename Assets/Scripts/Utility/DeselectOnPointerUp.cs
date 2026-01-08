using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectOnPointerUp : MonoBehaviour, IPointerUpHandler, IPointerExitHandler, IPointerDownHandler
{
    private bool isPointerDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;

        // If we released outside, Unity may leave it selected—clear selection.
        if (!eventData.hovered.Contains(gameObject))
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // If user is dragging off while holding click, clear selection immediately.
        if (isPointerDown)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
