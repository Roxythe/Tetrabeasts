using TMPro;
using UnityEngine;

// Separate TMP geometry gives an opaque shadow even on fonts without an underlay material.
public sealed class TMPLabelShadow : MonoBehaviour
{
    public TMP_Text shadow;
    TMP_Text source;
    void OnEnable() => Sync();
    void LateUpdate() => Sync();
    public void Sync()
    {
        if (!source) source = GetComponent<TMP_Text>();
        if (!source || !shadow) return;
        shadow.text = source.text; shadow.font = source.font;
        shadow.fontSize = source.fontSize; shadow.fontStyle = source.fontStyle;
        shadow.enableAutoSizing = source.enableAutoSizing;
        shadow.fontSizeMin = source.fontSizeMin; shadow.fontSizeMax = source.fontSizeMax;
        shadow.alignment = source.alignment; shadow.margin = source.margin;
        shadow.color = Color.black; shadow.overrideColorTags = true; shadow.raycastTarget = false;
        shadow.gameObject.SetActive(source.gameObject.activeSelf);
    }
}
