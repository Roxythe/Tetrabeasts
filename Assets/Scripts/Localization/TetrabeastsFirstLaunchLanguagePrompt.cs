using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TetrabeastsFirstLaunchLanguagePrompt : MonoBehaviour
{
    static readonly Vector2 ModalSize = new(560f, 300f);
    static readonly Color OverlayColor = new(0f, 0f, 0f, 0.72f);
    static readonly Color PanelColor = new(0.08f, 0.07f, 0.06f, 0.96f);
    static readonly Color ButtonColor = new(0.95f, 0.58f, 0.16f, 1f);
    static readonly Color ButtonSelectedColor = new(1f, 0.76f, 0.24f, 1f);

    readonly List<Button> languageButtons = new();
    string selectedCode = TetrabeastsLocalization.EnglishCode;
    TMP_Text titleText;
    TMP_Text bodyText;
    TMP_Text confirmText;

    public static bool ShowIfNeeded(Transform titleSceneRoot)
    {
        TetrabeastsLocalization.EnsureInitialized();
        if (!TetrabeastsLocalization.ShouldShowFirstLaunchPrompt)
            return false;

        if (FindFirstObjectByType<TetrabeastsFirstLaunchLanguagePrompt>(FindObjectsInactive.Include))
            return false;

        Canvas canvas = null;
        if (titleSceneRoot)
            canvas = titleSceneRoot.GetComponentInParent<Canvas>();

        if (!canvas)
            canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (!canvas)
        {
            Debug.LogWarning("TetrabeastsFirstLaunchLanguagePrompt: No Canvas found for the language prompt.");
            return false;
        }

        var overlay = new GameObject("FirstLaunchLanguagePrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TetrabeastsFirstLaunchLanguagePrompt));
        overlay.transform.SetParent(canvas.transform, false);
        overlay.transform.SetAsLastSibling();

        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay.GetComponent<Image>().color = OverlayColor;
        overlay.GetComponent<TetrabeastsFirstLaunchLanguagePrompt>().Build();
        return true;
    }

    void Build()
    {
        selectedCode = TetrabeastsLocalization.CurrentLanguageCode;

        var font = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include)?.font;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = ModalSize;
        panel.GetComponent<Image>().color = PanelColor;

        titleText = CreateText(panel.transform, "Title", TetrabeastsLocalization.GetText("language_prompt_title"), 36, FontStyles.Bold, TextAlignmentOptions.Center, font);
        SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(500f, 48f));

        bodyText = CreateText(panel.transform, "Body", TetrabeastsLocalization.GetText("language_prompt_body"), 22, FontStyles.Normal, TextAlignmentOptions.Center, font);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        SetRect(bodyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(480f, 72f));

        var buttonRow = new GameObject("LanguageButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonRow.transform.SetParent(panel.transform, false);
        var rowRect = buttonRow.GetComponent<RectTransform>();
        SetRect(rowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(430f, 48f));
        var layout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.spacing = 12f;

        foreach (var language in TetrabeastsLocalization.SupportedLanguages)
            CreateLanguageButton(buttonRow.transform, language.Code, language.NativeName, font);

        var confirm = CreateButton(panel.transform, "Confirm_Button", TetrabeastsLocalization.GetText("language_prompt_confirm"), font);
        SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(180f, 44f));
        confirm.onClick.AddListener(Confirm);
        confirmText = confirm.GetComponentInChildren<TMP_Text>();

        RefreshText();
        RefreshSelectedButtons();
    }

    void CreateLanguageButton(Transform parent, string code, string label, TMP_FontAsset font)
    {
        var button = CreateButton(parent, $"{label}_Button", label, font);
        button.GetComponent<LayoutElement>().minWidth = 130f;
        button.onClick.AddListener(() =>
        {
            selectedCode = code;
            TetrabeastsLocalization.SetLanguageCode(selectedCode, persist: false);
            RefreshText();
            RefreshSelectedButtons();
        });

        languageButtons.Add(button);
    }

    Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = ButtonColor;

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonSelectedColor;
        colors.pressedColor = new Color(0.8f, 0.35f, 0.08f, 1f);
        colors.selectedColor = ButtonSelectedColor;
        button.colors = colors;

        var text = CreateText(go.transform, "Text", label, 24, FontStyles.Bold, TextAlignmentOptions.Center, font);
        text.color = Color.black;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return button;
    }

    TMP_Text CreateText(Transform parent, string name, string value, int size, FontStyles style, TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        if (font)
            text.font = font;

        return text;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    void RefreshText()
    {
        if (titleText) titleText.text = TetrabeastsLocalization.GetText("language_prompt_title");
        if (bodyText) bodyText.text = TetrabeastsLocalization.GetText("language_prompt_body");
        if (confirmText) confirmText.text = TetrabeastsLocalization.GetText("language_prompt_confirm");
    }

    void RefreshSelectedButtons()
    {
        for (int i = 0; i < languageButtons.Count; i++)
        {
            var button = languageButtons[i];
            if (!button)
                continue;

            bool selected = i < TetrabeastsLocalization.SupportedLanguages.Count &&
                TetrabeastsLocalization.SupportedLanguages[i].Code == selectedCode;
            button.image.color = selected ? ButtonSelectedColor : ButtonColor;
        }
    }

    void Confirm()
    {
        TetrabeastsLocalization.SetLanguageCode(selectedCode, persist: true);
        Destroy(gameObject);
    }
}
