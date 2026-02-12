using UnityEngine;
using UnityEngine.UI;

public class UICursorController : MonoBehaviour
{
    public RectTransform cursorRect;   
    public Canvas rootCanvas;
    public float baseSize = 64f;                 // Size at scale 1
    public Vector2 hotspotPixels = Vector2.zero; // (0,0) top-left
    public bool forceLastSibling = true;
    public bool cursorBlocksRaycasts = false; // Keep false so it never blocks dropdown clicks

    float _scale = 1f;

    void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();

        // Make sure the cursor never blocks UI interactions
        if (cursorRect)
        {
            var img = cursorRect.GetComponent<Image>();
            if (img) img.raycastTarget = cursorBlocksRaycasts;

            var cg = cursorRect.GetComponent<CanvasGroup>();
            if (!cg) cg = cursorRect.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = cursorBlocksRaycasts;
            cg.interactable = false;
        }
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

    void LateUpdate()
    {
        if (!forceLastSibling) return;
        if (!gameObject.activeInHierarchy) return;

        transform.SetAsLastSibling();
    }

}
