using UnityEngine;
using UnityEngine.UI;

// Small pooled-by-lifetime UI burst: square pixels, scaled game clock, no input interception.
public sealed class PixelSprayVFX : MonoBehaviour
{
    RectTransform[] dots;
    Vector2[] velocities;
    Image[] images;
    float elapsed;
    const float Lifetime = 0.45f;
    public static void Play(Transform parent, Vector3 worldPosition, Color color, float radius = 35f)
    {
        var root = new GameObject("Pixel impact", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false); root.position = worldPosition;
        var effect = root.gameObject.AddComponent<PixelSprayVFX>();
        const int count = 16;
        effect.dots = new RectTransform[count]; effect.velocities = new Vector2[count]; effect.images = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var image = new GameObject("Pixel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root, false); image.raycastTarget = false;
            image.color = i % 3 == 0 ? Color.white : color;
            image.rectTransform.sizeDelta = Vector2.one * Random.Range(4f, 8f);
            effect.images[i] = image; effect.dots[i] = image.rectTransform;
            effect.velocities[i] = (Vector2)(Quaternion.Euler(0, 0, i * 360f / count) * Vector2.up) * radius * Random.Range(3f, 6f);
        }
    }
    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= Lifetime) { Destroy(gameObject); return; }
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].anchoredPosition += velocities[i] * Time.deltaTime;
            var color = images[i].color; color.a = 1f - elapsed / Lifetime; images[i].color = color;
            dots[i].localScale = Vector3.one * (1f - elapsed / Lifetime * .65f);
        }
    }
}
