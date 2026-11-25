using TMPro;
using UnityEngine;

// All typography and artwork are authored on GameModeTooltip.prefab.
[RequireComponent(typeof(GameModeDescriptions))]
public sealed class GameModeTooltip : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public TMP_Text lockNote;
    public GameModeDescriptions descriptions;
    [Min(200f)] public float fixedWidth = 350f;
    [Min(0f)] public float padding = 18f;
    [Min(0f)] public float textGap = 12f;
    [TextArea] public string lockedMessage = "Reach the final level in Standard to unlock this game mode.";

    public void ShowMode(RunGameMode mode, string modeName, bool locked)
    {
        if (!descriptions) descriptions = GetComponent<GameModeDescriptions>();
        if (title) title.text = TetrabeastsLocalization.LocalizeText(modeName);
        if (description && descriptions) description.text = TetrabeastsLocalization.LocalizeText(descriptions.Get(mode));
        if (lockNote)
        {
            lockNote.text = TetrabeastsLocalization.LocalizeText(lockedMessage);
            lockNote.gameObject.SetActive(locked);
        }
        title?.GetComponent<TMPLabelShadow>()?.Sync();
        FitText();
    }
    public void FitText()
    {
        var panel = (RectTransform)transform;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fixedWidth);
        float y = padding;
        void Place(TMP_Text text)
        {
            if (!text) return;
            if (text.enableAutoSizing) text.fontSize = text.fontSizeMax;
            text.enableAutoSizing = false;
            float height = text.GetPreferredValues(text.text, fixedWidth - padding * 2, Mathf.Infinity).y;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(fixedWidth - padding * 2, height);
            rect.anchoredPosition = new Vector2(0, -y);
            y += height + textGap;
        }
        Place(title); Place(description);
        if (lockNote && lockNote.gameObject.activeSelf) Place(lockNote);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y - textGap + padding);
        var shadow = title ? title.GetComponent<TMPLabelShadow>() : null;
        if (shadow && shadow.shadow)
        {
            var rect = shadow.shadow.rectTransform;
            rect.anchorMin = title.rectTransform.anchorMin; rect.anchorMax = title.rectTransform.anchorMax;
            rect.pivot = title.rectTransform.pivot; rect.sizeDelta = title.rectTransform.sizeDelta;
            rect.anchoredPosition = title.rectTransform.anchoredPosition + new Vector2(2, -2);
            shadow.Sync();
        }
    }
}
