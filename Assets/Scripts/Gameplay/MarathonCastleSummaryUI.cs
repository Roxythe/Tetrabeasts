using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A brief receipt above the closed curtain. All timing is independent of gameplay time.
public sealed class MarathonCastleSummaryUI : MonoBehaviour
{
    CanvasGroup group;
    RectTransform panel;
    TMP_FontAsset font;
    readonly List<CanvasGroup> rows = new List<CanvasGroup>();
    readonly List<TMP_Text> labels = new List<TMP_Text>();
    readonly List<TMP_Text> levelUps = new List<TMP_Text>();
    IReadOnlyList<RewardLine> shownLines;
    float presentationElapsed, rollSeconds;
    bool presenting;
    public sealed class RewardLine
    {
        public string text, unitName;
        public Color roleColor;
        public float beforeXp, afterXp;
        public bool IsXp => !string.IsNullOrEmpty(unitName);
        public int Level(float total) => Mathf.Min(100, 1 + Mathf.FloorToInt(total / 100f));
        public string Format(float total, bool levelUpSpace = false)
        {
            if (!IsXp) return text;
            float into = total - (Level(total) - 1) * 100;
            string space = levelUpSpace ? "<space=3.5em>" : "";
            return $"<color=#{ColorUtility.ToHtmlStringRGB(roleColor)}>{unitName}</color>   <color=#FFFFFF>+{afterXp - beforeXp:0.#}</color>    <color=#BD5CFF>XP:</color> <color=#FFFFFF><mspace=0.35em>{into:0.0}/100</mspace>   (Lv. {Level(total)}){space}</color>";
        }
    }

    public static MarathonCastleSummaryUI Create(Canvas parent, TMP_FontAsset font)
    {
        var root = new GameObject("Marathon Castle Clear Summary", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        root.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = 32761;
        var view = root.AddComponent<MarathonCastleSummaryUI>();
        view.font = font;
        view.group = root.GetComponent<CanvasGroup>();
        view.group.interactable = view.group.blocksRaycasts = false;
        var card = new GameObject("Summary Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        card.transform.SetParent(root.transform, false);
        view.panel = (RectTransform)card.transform;
        view.panel.anchorMin = new Vector2(.12f, .16f); view.panel.anchorMax = new Vector2(.88f, .84f);
        view.panel.sizeDelta = Vector2.zero;
        card.GetComponent<Image>().color = new Color(.035f, .025f, .055f, 1f);
        card.GetComponent<Image>().raycastTarget = false;
        card.GetComponent<Outline>().effectColor = new Color(.9f, .67f, .2f);
        card.GetComponent<Outline>().effectDistance = new Vector2(3, -3);
        root.SetActive(false);
        return view;
    }

    public IEnumerator Show(int completedLevel, IReadOnlyList<RewardLine> lines)
    {
        foreach (Transform child in panel) Destroy(child.gameObject);
        rows.Clear();
        labels.Clear(); levelUps.Clear();
        shownLines = lines; presentationElapsed = 0; rollSeconds = 3.2f;
        group.alpha = 1;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        var title = AddText("Title", TetrabeastsLocalization.LocalizeFormat("Level {0} - Castle Destroyed", completedLevel), .8f, .96f, 56);
        title.color = new Color(1f, .82f, .3f);
        title.alignment = TextAlignmentOptions.Center;
        title.GetComponent<TMPLabelShadow>().Sync();
        float height = .69f / Mathf.Max(1, lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            var text = AddText("Reward " + (i + 1), lines[i].Format(lines[i].beforeXp), .76f - height * (i + 1), .76f - height * i, 42);
            var row = text.transform.parent.gameObject.AddComponent<CanvasGroup>();
            row.alpha = 0;
            rows.Add(row);
            labels.Add(text);
            TMP_Text badge = null;
            if (lines[i].IsXp)
            {
                badge = AddText("Level Up " + i, "Level Up!", .76f - height * (i + 1), .76f - height * i, 34);
                // Share the row fade; position the badge from the rendered XP counter below.
                var badgeHolder = (RectTransform)badge.transform.parent;
                badgeHolder.SetParent(row.transform, false);
                badgeHolder.anchorMin = Vector2.zero; badgeHolder.anchorMax = new Vector2(0, 1);
                badgeHolder.pivot = new Vector2(0, .5f);
                badgeHolder.anchoredPosition = Vector2.zero; badgeHolder.sizeDelta = new Vector2(140, 0);
                badgeHolder.gameObject.SetActive(false);
                rollSeconds = Mathf.Max(rollSeconds, Mathf.Min(5f, 3.2f + .35f * (lines[i].Level(lines[i].afterXp) - lines[i].Level(lines[i].beforeXp))));
            }
            levelUps.Add(badge);
        }
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < lines.Count; i++)
        {
            if (!lines[i].IsXp) continue;
            // Fit once using the widest expected counter; changing digits must not resize the text.
            var label = labels[i];
            label.text = lines[i].Format((lines[i].Level(lines[i].afterXp) - 1) * 100f + 99.9f, true);
            label.ForceMeshUpdate();
            float fittedSize = label.fontSize;
            label.enableAutoSizing = false; label.fontSize = fittedSize;
        }
        presenting = true;
        AdvancePresentation(0);
        while (presentationElapsed < .4f + rollSeconds + 1.8f) yield return null;
    }

    void Update() { if (presenting) AdvancePresentation(Time.unscaledDeltaTime); }
    void OnDisable() { presenting = false; }
    void AdvancePresentation(float delta)
    {
        presentationElapsed += delta;
        float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((presentationElapsed - .4f) / rollSeconds));
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(presentationElapsed / .4f));
            var line = shownLines[i];
            if (line.IsXp)
            {
                float total = Mathf.Lerp(line.beforeXp, line.afterXp, progress);
                bool leveled = line.Level(total) > line.Level(line.beforeXp);
                labels[i].text = line.Format(total, leveled);
                var badge = levelUps[i];
                if (badge && leveled)
                {
                    badge.transform.parent.gameObject.SetActive(true);
                    badge.color = Color.Lerp(new Color(1, .78f, .15f), Color.white, .5f + .5f * Mathf.Sin(presentationElapsed * Mathf.PI * 3f));
                    PositionLevelUp(labels[i], badge);
                    badge.GetComponent<TMPLabelShadow>().Sync();
                }
                labels[i].GetComponent<TMPLabelShadow>().Sync();
            }
        }
    }

    static void PositionLevelUp(TMP_Text label, TMP_Text badge)
    {
        label.ForceMeshUpdate();
        int lastDigit = label.text.LastIndexOf(')');
        for (int i = 0; i < label.textInfo.characterCount; i++)
        {
            var character = label.textInfo.characterInfo[i];
            if (character.index != lastDigit) continue;
            var holder = (RectTransform)badge.transform.parent;
            var row = (RectTransform)holder.parent;
            Vector3 position = row.InverseTransformPoint(label.transform.TransformPoint(new Vector3(character.xAdvance + 10, 0)));
            holder.anchoredPosition = new Vector2(position.x - row.rect.xMin, 0);
            holder.sizeDelta = new Vector2(label.fontSize * 3.2f, 0);
            badge.enableAutoSizing = false; badge.fontSize = label.fontSize * .8f;
            break;
        }
    }

    TMP_Text AddText(string name, string value, float bottom, float top, float size)
    {
        var holder = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        holder.SetParent(panel, false);
        holder.anchorMin = new Vector2(.05f, bottom); holder.anchorMax = new Vector2(.95f, top); holder.sizeDelta = Vector2.zero;
        var shadow = new GameObject("Shadow", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        shadow.transform.SetParent(holder, false);
        var obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(holder, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.color = Color.white;
        text.raycastTarget = false; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true; text.fontSizeMin = 20; text.fontSizeMax = text.fontSize = size;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.sizeDelta = Vector2.zero;
        shadow.rectTransform.anchorMin = Vector2.zero; shadow.rectTransform.anchorMax = Vector2.one;
        shadow.rectTransform.sizeDelta = Vector2.zero; shadow.rectTransform.anchoredPosition = new Vector2(3, -3);
        var link = obj.AddComponent<TMPLabelShadow>(); link.shadow = shadow; link.Sync();
        return text;
    }

    public IEnumerator FadeOut(float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - Mathf.SmoothStep(0, 1, t / duration);
            yield return null;
        }
        group.alpha = 0;
        gameObject.SetActive(false);
    }
}
