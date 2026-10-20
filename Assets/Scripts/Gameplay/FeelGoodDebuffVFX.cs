using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Child graphics follow the tile through column settling and are hidden on pool release.
public class FeelGoodDebuffVFX : MonoBehaviour
{
    RectTransform root;
    readonly Image[] edges = new Image[4];
    TMP_Text label;
    int lastStacks = -1;

    public void Show(int stacks, float severity)
    {
        if (!root) Build();
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        float pulse = .5f + .5f * Mathf.Sin(Time.time * Mathf.Lerp(5f, 13f, severity));
        Color tint = Color.Lerp(new Color(.65f, .12f, 1f), new Color(1f, .15f, .65f), severity);
        tint.a = Mathf.Lerp(.55f, 1f, severity) * Mathf.Lerp(.7f, 1f, pulse);
        float thickness = Mathf.Round(Mathf.Lerp(2f, 5f, severity));
        for (int i = 0; i < edges.Length; i++)
        {
            edges[i].color = tint;
            edges[i].rectTransform.sizeDelta = i < 2 ? new Vector2(0, thickness) : new Vector2(thickness, 0);
        }
        if (lastStacks != stacks) { label.text = stacks.ToString(); lastStacks = stacks; }
        label.color = Color.Lerp(new Color(.95f, .65f, 1f), Color.white, severity);
        label.rectTransform.localScale = Vector3.one * (1f + .12f * severity * pulse);
    }

    public void Hide() { if (root) root.gameObject.SetActive(false); }

    void Build()
    {
        root = new GameObject("Feel Good curse", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        for (int i = 0; i < edges.Length; i++)
        {
            var edge = new GameObject("Pixel border", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            edge.transform.SetParent(root, false);
            edge.raycastTarget = false;
            var rect = edge.rectTransform;
            rect.anchorMin = i < 2 ? new Vector2(0, i) : new Vector2(i - 2, 0);
            rect.anchorMax = i < 2 ? new Vector2(1, i) : new Vector2(i - 2, 1);
            rect.pivot = i < 2 ? new Vector2(.5f, i) : new Vector2(i - 2, .5f);
            rect.anchoredPosition = Vector2.zero;
            edges[i] = edge;
        }
        var badge = new GameObject("Curse stack count", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        badge.transform.SetParent(root, false);
        badge.raycastTarget = false;
        badge.color = new Color(.13f, .015f, .2f, .95f);
        var br = badge.rectTransform;
        br.anchorMin = new Vector2(.48f, .68f); br.anchorMax = Vector2.one;
        br.offsetMin = new Vector2(0, -2); br.offsetMax = new Vector2(-2, -2);
        label = new GameObject("Stacks", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(br, false);
        label.raycastTarget = false;
        label.enableAutoSizing = true; label.fontSizeMin = 7; label.fontSizeMax = 20;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
    }
}
