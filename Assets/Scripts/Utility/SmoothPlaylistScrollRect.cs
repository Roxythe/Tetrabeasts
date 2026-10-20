using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Wheel impulses ease over unscaled time so settings remain smooth while paused.
public class SmoothPlaylistScrollRect : ScrollRect
{
    float targetY;
    float lastY;
    bool easing;

    public override void OnScroll(PointerEventData eventData)
    {
        if (!IsActive() || !content || !viewport) return;
        float range = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        if (!easing || Mathf.Abs(content.anchoredPosition.y - lastY) > .1f)
            targetY = content.anchoredPosition.y;
        targetY = Mathf.Clamp(targetY - eventData.scrollDelta.y * scrollSensitivity, 0f, range);
        lastY = content.anchoredPosition.y;
        velocity = Vector2.zero;
        easing = true;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        easing = false;
        base.OnBeginDrag(eventData);
    }

    protected override void OnDisable()
    {
        easing = false;
        base.OnDisable();
    }

    protected override void LateUpdate()
    {
        if (easing && content && viewport)
        {
            // A scrollbar/controller/selection move takes control immediately.
            if (Mathf.Abs(content.anchoredPosition.y - lastY) > .1f) easing = false;
            else
            {
                targetY = Mathf.Clamp(targetY, 0f, Mathf.Max(0f, content.rect.height - viewport.rect.height));
                var position = content.anchoredPosition;
                position.y = Mathf.Lerp(position.y, targetY, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                if (Mathf.Abs(position.y - targetY) < .1f) { position.y = targetY; easing = false; }
                content.anchoredPosition = position;
                velocity = Vector2.zero;
            }
        }
        base.LateUpdate();
        if (content) lastY = content.anchoredPosition.y;
    }
}
