using UnityEngine;
using UnityEngine.UI;

// Keeps a destroyed unit's final impact visible without retaining gameplay state.
public sealed class ImpactEchoVFX : MonoBehaviour
{
    float elapsed;
    Image image;
    void Awake() => image = GetComponent<Image>();
    void Update()
    {
        elapsed += Time.deltaTime;
        if (!image || elapsed >= .25f) { Destroy(gameObject); return; }
        var color = image.color; color.a = 1f - elapsed / .25f; image.color = color;
    }
}
