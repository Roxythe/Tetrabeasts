using UnityEngine;
using UnityEngine.UI;

public sealed class SpriteCrossfade : MonoBehaviour
{
    Image target, previous;
    Color tint;
    float elapsed;
    public static void Swap(Image image, Sprite sprite, Color color, Sprite oldSprite = null)
    {
        if (!image || (!oldSprite && image.sprite == sprite)) return;
        var fade = image.GetComponent<SpriteCrossfade>();
        if (!fade) fade = image.gameObject.AddComponent<SpriteCrossfade>();
        fade.Begin(image, sprite, color, oldSprite ? oldSprite : image.sprite);
    }
    void Begin(Image image, Sprite next, Color color, Sprite old)
    {
        if (previous) Destroy(previous.gameObject);
        target = image; tint = color; elapsed = 0;
        previous = new GameObject("Previous growth stage", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        previous.transform.SetParent(image.transform, false);
        previous.rectTransform.anchorMin = Vector2.zero; previous.rectTransform.anchorMax = Vector2.one;
        previous.rectTransform.sizeDelta = Vector2.zero;
        previous.sprite = old; previous.color = color; previous.raycastTarget = false;
        target.sprite = next; target.color = new Color(tint.r, tint.g, tint.b, 0);
    }
    void Update()
        => Advance(Time.deltaTime);
    void Advance(float delta)
    {
        if (!previous || !target) return;
        elapsed += delta;
        float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .45f));
        target.color = new Color(tint.r, tint.g, tint.b, tint.a * p);
        previous.color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - p));
        if (p >= 1) Destroy(previous.gameObject);
    }
    void OnDisable()
    {
        if (target) target.color = tint;
        if (previous) { if (Application.isPlaying) Destroy(previous.gameObject); else DestroyImmediate(previous.gameObject); }
    }
}
