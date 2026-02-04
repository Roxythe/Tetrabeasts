using UnityEngine;
using UnityEngine.UI;

public class UICursorController : MonoBehaviour
{
    public RectTransform cursorRect;   
    public Canvas rootCanvas;
    public float baseSize = 64f;                 // Size at scale 1
    public Vector2 hotspotPixels = Vector2.zero; // (0,0) top-left

    float _scale = 1f;

    void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (!cursorRect || !rootCanvas) return;

        Vector2 screen = Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            screen,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out var localPoint
        );

        // Apply hotspot offset
        float s = _scale;
        Vector2 hotspot = hotspotPixels * s;

        cursorRect.anchoredPosition = localPoint - hotspot;
    }

    public void SetScale(float scale)
    {
        _scale = Mathf.Clamp(scale, 0.25f, 4f);
        if (cursorRect)
            cursorRect.sizeDelta = Vector2.one * (baseSize * _scale);
    }

    public void SetVisible(bool visible)
    {
        if (cursorRect) cursorRect.gameObject.SetActive(visible);
    }
}
