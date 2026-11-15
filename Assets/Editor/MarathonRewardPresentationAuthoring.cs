#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicit, repeatable prefab migration. All artwork, previews and controls remain editable.
public static class MarathonRewardPresentationAuthoring
{
    public const string RewardPath = "Assets/Prefabs/GameModes/MarathonRewardPanel.prefab";
    const string CityPath = "Assets/Prefabs/Animations/BoardBorder/ModernCityBorder_Animation.prefab";

    [MenuItem("Tetrabeasts/Update Marathon Reward Presentation")]
    public static void Apply()
    {
        Edit(RewardPath, ConfigureRewards);
        Edit(CityPath, ConfigureCityBacking);
        Edit("Assets/Prefabs/BuffPopUpText_Prefab.prefab", root =>
        {
            var group = root.GetComponent<CanvasGroup>();
            if (!group) group = root.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
        });
        AssetDatabase.SaveAssets();
    }

    static void Edit(string path, System.Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void ConfigureRewards(GameObject root)
    {
        var ui = root.GetComponent<MarathonRewardUI>();
        var rootRect = (RectTransform)root.transform;
        Stretch(rootRect, 0f);
        var canvas = root.GetComponent<Canvas>();
        if (!canvas) canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;
        if (!root.GetComponent<GraphicRaycaster>()) root.AddComponent<GraphicRaycaster>();
        var rootImage = root.GetComponent<Image>();
        // Keep the background sprite the designer chose, above a solid black base.
        if (rootImage.sprite && !root.transform.Find("Background Art"))
        {
            var art = Rect("Background Art", root.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            EditorUtility.CopySerialized(rootImage, art);
            art.raycastTarget = false;
            Stretch(art.rectTransform, 0f);
            art.transform.SetAsFirstSibling();
        }
        rootImage.sprite = null;
        rootImage.color = Color.black;
        rootImage.raycastTarget = true;

        var panel = (RectTransform)root.transform.Find("Panel");
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * .5f;
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(1120, 740);
        var content = (root.transform.Find("Content") ?? panel.Find("Content")) as RectTransform;
        if (!content) content = Rect("Content", panel, Vector2.zero, new Vector2(1120, 900));
        content.SetParent(panel, false);
        content.localScale = Vector3.one * (692f / 900f);
        ui.content = content;
        ui.contentSize = content.sizeDelta = new Vector2(1120, 900);
        // Preserve the user's full-screen frame, but do not let its center tint the cards.
        var outerFrame = root.transform.Find("Image");
        if (outerFrame)
        {
            Stretch((RectTransform)outerFrame, 0f);
            var image = outerFrame.GetComponent<Image>();
            if (image) { image.fillCenter = false; image.raycastTarget = false; }
        }
        var border = panel.Find("Panel Border") ?? root.transform.Find("Panel Border") ?? content.Find("Panel Border");
        if (border)
        {
            border.SetParent(panel, false);
            Stretch((RectTransform)border, -12f);
            border.GetComponent<Image>().fillCenter = false;
            border.GetComponent<Image>().raycastTarget = false;
        }
        var shadow = ui.title.GetComponent<TMPLabelShadow>();
        if (shadow && shadow.shadow) shadow.shadow.transform.SetParent(content, false);
        ui.title.transform.SetParent(content, false);
        ui.title.rectTransform.anchoredPosition = new Vector2(0, 410);
        if (shadow && shadow.shadow) shadow.shadow.rectTransform.anchoredPosition = new Vector2(2, 408);
        ui.colors.inactiveAlpha = 185f / 255f;
        ui.previewPanels = new RectTransform[3];
        ui.previewLabels = new TMP_Text[3];

        for (int i = 0; i < ui.choices.Length; i++)
        {
            float x = (i - 1) * 352f;
            var choice = ui.choices[i];
            choice.transform.SetParent(content, false);
            ((RectTransform)choice.transform).anchoredPosition = new Vector2(x, -95);
            choice.transition = Selectable.Transition.None;
            var card = ui.cards[i];
            card.colors = ui.colors;
            card.previewOwner = ui;
            card.choiceIndex = i;
            choice.GetComponent<Image>().color = Color.clear;
            var oldFrame = card.transform.Find("Rarity Border");
            if (oldFrame) Object.DestroyImmediate(oldFrame.gameObject);
            var frameRect = Rect("Rarity Border", card.transform, Vector2.zero, Vector2.zero);
            Stretch(frameRect, 0f);
            var frame = frameRect.gameObject.AddComponent<MarathonSelectionBorder>();
            frame.thickness = 12f;
            frame.pixelsPerSecond = 0f;
            frame.raycastTarget = false;
            frameRect.SetAsFirstSibling();
            card.rarityBorder = frame;
            ReplaceSelectionAnimation(card);
            card.ResetState();

            var oldPreview = content.Find("Stat Preview " + (i + 1));
            if (oldPreview) Object.DestroyImmediate(oldPreview.gameObject);
            var preview = Rect("Stat Preview " + (i + 1), content, new Vector2(x, 260), new Vector2(330, 180));
            var backing = preview.gameObject.AddComponent<Image>();
            backing.color = new Color(.035f, .035f, .055f, 1f);
            backing.raycastTarget = false;
            var outline = preview.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.7f, .5f, .2f, 1f);
            outline.effectDistance = new Vector2(2, -2);
            var label = Label("Stat Changes", preview, ui.title);
            Stretch(label.rectTransform, 12f);
            label.text = "Monster attack power\n1 -> 1.1\n\nEnemy attack power\n1 -> 1.1";
            label.fontSize = 24;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18;
            label.fontSizeMax = 24;
            label.alignment = TextAlignmentOptions.Center;
            ui.previewPanels[i] = preview;
            ui.previewLabels[i] = label;
            preview.gameObject.SetActive(false);
        }

        ui.confirmButton.transform.SetParent(content, false);
        ((RectTransform)ui.confirmButton.transform).anchoredPosition = new Vector2(0, -402);
        var confirmStyle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameModes/GameModeMenu.prefab")
            .GetComponent<GameModeSelectionUI>().standardButton;
        foreach (var label in ui.confirmButton.GetComponentsInChildren<TMP_Text>(true))
        {
            var source = confirmStyle.GetComponentInChildren<TMP_Text>(true);
            if (source) { label.font = source.font; label.fontSharedMaterial = source.fontSharedMaterial; }
            label.color = Color.black;
        }
        var oldToggle = root.transform.Find("Compare All Rewards");
        if (oldToggle) Object.DestroyImmediate(oldToggle.gameObject);
        var toggleRect = Rect("Compare All Rewards", root.transform, new Vector2(-36, 52), new Vector2(300, 40));
        toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1, 0);
        toggleRect.pivot = new Vector2(1, 0);
        var hit = toggleRect.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        var toggle = toggleRect.gameObject.AddComponent<Toggle>();
        var box = Rect("Checkbox", toggleRect, new Vector2(-130, 0), new Vector2(30, 30)).gameObject.AddComponent<Image>();
        box.color = new Color(.7f, .5f, .2f);
        box.raycastTarget = false;
        var check = Rect("Checkmark", box.transform, Vector2.zero, new Vector2(22, 22)).gameObject.AddComponent<TextMeshProUGUI>();
        check.font = ui.title.font;
        check.text = "X";
        check.fontSize = 24;
        check.color = Color.black;
        check.alignment = TextAlignmentOptions.Center;
        check.raycastTarget = false;
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = false;
        var toggleLabel = Label("Label", toggleRect, ui.title);
        toggleLabel.rectTransform.anchoredPosition = new Vector2(24, 0);
        toggleLabel.rectTransform.sizeDelta = new Vector2(248, 40);
        toggleLabel.text = "Compare all rewards";
        toggleLabel.fontSize = 24;
        toggleLabel.alignment = TextAlignmentOptions.Left;
        ui.compareAllToggle = toggle;
        content.SetAsLastSibling();
        toggleRect.SetAsLastSibling();
    }

    public static void ReplaceSelectionAnimation(MarathonRewardCard card)
    {
        if (card.selectionAnimation) Object.DestroyImmediate(card.selectionAnimation);
        var rect = Rect("Selection Animation", card.transform, Vector2.zero, Vector2.zero);
        Stretch(rect, -7f);
        var border = rect.gameObject.AddComponent<MarathonSelectionBorder>();
        border.color = new Color(1f, .68f, .18f, 1f);
        border.raycastTarget = false;
        card.selectionAnimation = rect.gameObject;
        rect.gameObject.SetActive(false);
    }

    static void ConfigureCityBacking(GameObject root)
    {
        var sourceImage = root.GetComponent<Image>();
        var animation = root.transform.Find("Animation") as RectTransform;
        if (!animation)
        {
            var sourceAnimator = root.GetComponent<Animator>();
            animation = Rect("Animation", root.transform, Vector2.zero, Vector2.zero);
            Stretch(animation, 0f);
            EditorUtility.CopySerialized(sourceImage, animation.gameObject.AddComponent<Image>());
            animation.GetComponent<Image>().raycastTarget = false;
            EditorUtility.CopySerialized(sourceAnimator, animation.gameObject.AddComponent<Animator>());
            Object.DestroyImmediate(sourceAnimator);
        }
        var fit = root.GetComponent<BoardBorderBackingFit>();
        if (!fit) fit = root.AddComponent<BoardBorderBackingFit>();
        fit.artwork = animation.GetComponent<Image>();
        // The parent draws first; the mesh effect excludes the sprite's transparent margins.
        sourceImage.sprite = null;
        sourceImage.color = Color.black;
        sourceImage.preserveAspect = false;
        sourceImage.raycastTarget = false;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.gameObject.layer = 5;
        r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.anchoredPosition = position;
        r.sizeDelta = size;
        return r;
    }
    static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset; rect.offsetMax = -Vector2.one * inset;
    }
    static TMP_Text Label(string name, Transform parent, TMP_Text style)
    {
        var label = Rect(name, parent, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = style.font; label.fontSharedMaterial = style.fontSharedMaterial;
        label.color = Color.white; label.raycastTarget = false;
        return label;
    }
}
#endif
