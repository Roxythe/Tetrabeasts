#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameModeFollowupAuthoring
{
    const string Modes = "Assets/Prefabs/GameModes/";
    public static void ApplyAndPreview()
    {
        Apply();
        GameModeAssetBuilder.RenderPreviews();
        GameModeAssetBuilder.RenderGameplayPreview();
    }
    [MenuItem("Tetrabeasts/Apply Marathon Follow-up Assets")]
    public static void Apply()
    {
        Edit(Modes + "GameModeMenu.prefab", root =>
        {
            var menu = root.GetComponent<GameModeSelectionUI>();
            menu.descriptions = root.GetComponent<GameModeDescriptions>() ?? root.AddComponent<GameModeDescriptions>();
            menu.tooltipTitle.fontStyle = FontStyles.Bold;
            menu.tooltipTitle.color = new Color(1f, 0.8f, 0.15f);
            menu.tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 420);
            menu.tooltipTitle.rectTransform.anchoredPosition = new Vector2(0, 174);
            menu.tooltipDescription.rectTransform.anchoredPosition = new Vector2(0, 25);
            menu.tooltipDescription.rectTransform.sizeDelta = new Vector2(274, 242);
            menu.tooltipDescription.enableAutoSizing = true;
            menu.tooltipDescription.fontSizeMin = 18; menu.tooltipDescription.fontSizeMax = 24;
            menu.tooltipLockNote.rectTransform.anchoredPosition = new Vector2(0, -163);
            menu.tooltipLockNote.rectTransform.sizeDelta = new Vector2(274, 65);
            Shadow(menu.tooltipTitle);
        });
        Edit(Modes + "MarathonRewardPanel.prefab", PolishRewards);
        Edit(Modes + "MarathonLevelGauge.prefab", root => ((RectTransform)root.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f));
        Edit("Assets/Prefabs/SpecialGauge_UI.prefab", root => ConfigureGauge(root.GetComponentInChildren<Slider>(true), "Special", "Assets/UI_LiquidFill_EnemyHP_Mat.mat"));
        Edit("Assets/Resources/Prefabs/Panels/SteamLeaderboard_Panel.prefab", root =>
        {
            root.GetComponent<SteamLeaderboardUI>().modeDropdown = AddModeDropdown(root, false);
        });
        Edit("Assets/Prefabs/Panels/HighScore_Panel.prefab", root => root.GetComponent<HighScoreUI>().modeDropdown = AddModeDropdown(root, true));

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Single);
        var gc = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
        ConfigureGauge(gc.specialSlider, "Special", "Assets/UI_LiquidFill_EnemyHP_Mat.mat");
        ConfigureGauge(gc.enemyCastleUI.healthBarSlider, "EnemyHealth", "Assets/UI_LiquidFill_EnemyHP_Mat.mat");
        ConfigureGauge(gc.unitLivesSlider, "Reserves", "Assets/UI_LiquidFill_ReserveUnit_Mat.mat");
        // Existing scene instances can have overrides; bind their dropdown explicitly too.
        foreach (var local in Object.FindObjectsByType<HighScoreUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!local.modeDropdown) local.modeDropdown = AddModeDropdown(local.gameObject, true);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Marathon follow-up prefabs, gauges and leaderboard dropdowns saved.");
    }

    public static void PolishRewards(GameObject root)
    {
        var ui = root.GetComponent<MarathonRewardUI>();
        ui.colors = root.GetComponent<MarathonRewardColors>() ?? root.AddComponent<MarathonRewardColors>();
        ui.colors.buffTitle = new Color(0.12f, 0.42f, 0.9f);
        ui.colors.debuffTitle = new Color(0.8f, 0.1f, 0.12f);
        ui.cards = new MarathonRewardCard[3]; ui.buffTitles = new TMP_Text[3]; ui.debuffTitles = new TMP_Text[3];
        ui.buffBackgrounds = new Image[3]; ui.debuffBackgrounds = new Image[3];
        var buffStyle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buttons/Buff_RunModOption_Button.prefab").GetComponent<RunModOptionButton>();
        var debuffStyle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buttons/Debuff_RunModOption_Button.prefab").GetComponent<RunModOptionButton>();
        var iconFrame = buffStyle.GetComponentsInChildren<Image>(true).First(i => i.name == "IconBorder_Image");
        // The prefab root is the full-screen dimmer; frame the dialog body instead.
        var oldFrame = root.transform.Find("Panel Border");
        if (oldFrame) oldFrame.SetParent(ui.title.transform.parent, false);
        var panelFrame = AddFrame(ui.title.transform.parent, "Panel Border", iconFrame);
        panelFrame.rectTransform.anchorMin = Vector2.zero; panelFrame.rectTransform.anchorMax = Vector2.one;
        panelFrame.rectTransform.offsetMin = new Vector2(-12, -12); panelFrame.rectTransform.offsetMax = new Vector2(12, 12);
        panelFrame.pixelsPerUnitMultiplier = 4;
        panelFrame.transform.SetAsLastSibling();
        var fire = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Animations/FireBorder_Animation.prefab");
        for (int i = 0; i < 3; i++)
        {
            var button = ui.choices[i];
            button.transition = Selectable.Transition.None;
            ui.selectionOutlines[i].enabled = false;
            var card = button.GetComponent<MarathonRewardCard>() ?? button.gameObject.AddComponent<MarathonRewardCard>();
            ui.cards[i] = card; card.colors = ui.colors;
            if (!card.selectedFlames)
            {
                var flame = (GameObject)PrefabUtility.InstantiatePrefab(fire);
                flame.transform.SetParent(button.transform, false);
                var rect = (RectTransform)flame.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-20, -20); rect.offsetMax = new Vector2(20, 20);
                foreach (var image in flame.GetComponentsInChildren<Graphic>(true)) image.raycastTarget = false;
                foreach (var animator in flame.GetComponentsInChildren<Animator>(true)) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                card.selectedFlames = flame;
            }
            var flameRect = (RectTransform)card.selectedFlames.transform;
            flameRect.offsetMin = new Vector2(-22, -22); flameRect.offsetMax = new Vector2(22, 22);
            var borderFit = card.selectedFlames.GetComponent<AnimatedBorderFit>() ?? card.selectedFlames.AddComponent<AnimatedBorderFit>();
            borderFit.edgeWidth = 22f;
            card.selectedFlames.transform.SetAsLastSibling(); card.selectedFlames.SetActive(false);
            ConfigureSection(ui.buffLabels[i], buffStyle.backgroundImage, "Buff Title", ui.colors.buffTitle, ui.colors.buffDescription, ui.colors.buffBackground, out ui.buffTitles[i], out ui.buffBackgrounds[i]);
            ConfigureSection(ui.debuffLabels[i], debuffStyle.backgroundImage, "Debuff Title", ui.colors.debuffTitle, ui.colors.debuffDescription, ui.colors.debuffBackground, out ui.debuffTitles[i], out ui.debuffBackgrounds[i]);
            foreach (var icon in new[] { ui.buffIcons[i], ui.debuffIcons[i] })
            {
                icon.color = Color.white; icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(90, 90);
                icon.rectTransform.anchoredPosition = new Vector2(-99, -20);
                var frame = AddFrame(icon.transform.parent, "Icon Border", iconFrame);
                frame.rectTransform.anchoredPosition = icon.rectTransform.anchoredPosition;
                frame.rectTransform.sizeDelta = new Vector2(96, 96);
            }
            card.ResetState();
        }
        Shadow(ui.title);
        var buttonArt = AssetDatabase.LoadAssetAtPath<GameModeSelectionUI>(Modes + "GameModeMenu.prefab").standardButton.GetComponent<Image>();
        ui.confirmButton.GetComponent<Image>().sprite = buttonArt.sprite;
        ui.confirmButton.GetComponent<Image>().type = buttonArt.type;
    }

    static void ConfigureSection(TMP_Text description, Image source, string name, Color titleColor, Color descriptionColor, Color backgroundColor, out TMP_Text title, out Image background)
    {
        var section = description.transform.parent;
        background = section.GetComponent<Image>(); background.sprite = source.sprite; background.type = source.type; background.color = backgroundColor;
        description.name = "Description";
        description.rectTransform.anchoredPosition = new Vector2(51, -25);
        description.rectTransform.sizeDelta = new Vector2(184, 126);
        description.fontSizeMin = 17; description.fontSizeMax = 25;
        description.alignment = TextAlignmentOptions.Center; description.color = descriptionColor;
        var existing = section.Find(name);
        title = existing ? existing.GetComponent<TMP_Text>() : Object.Instantiate(description, section);
        title.name = name; title.text = name; title.color = titleColor;
        title.rectTransform.anchoredPosition = new Vector2(0, 66); title.rectTransform.sizeDelta = new Vector2(276, 56);
        title.fontSizeMin = 20; title.fontSizeMax = 28; title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Top;
        Shadow(description); Shadow(title);
    }

    static void Shadow(TMP_Text text)
    {
        var old = text.GetComponent<Shadow>(); if (old) Object.DestroyImmediate(old);
        var binding = text.GetComponent<TMPLabelShadow>();
        if (binding && binding.shadow)
        {
            binding.shadow.rectTransform.anchoredPosition = text.rectTransform.anchoredPosition + new Vector2(2, -2);
            binding.shadow.rectTransform.sizeDelta = text.rectTransform.sizeDelta;
            binding.Sync(); return;
        }
        var shadow = Object.Instantiate(text, text.transform.parent);
        shadow.name = text.name + " Black Shadow";
        var cloneBinding = shadow.GetComponent<TMPLabelShadow>(); if (cloneBinding) Object.DestroyImmediate(cloneBinding);
        shadow.rectTransform.anchoredPosition += new Vector2(2, -2);
        shadow.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        binding = binding ? binding : text.gameObject.AddComponent<TMPLabelShadow>();
        binding.shadow = shadow; binding.Sync();
    }

    static Image AddFrame(Transform parent, string name, Image source)
    {
        var existing = parent.Find(name);
        var frame = existing ? existing.GetComponent<Image>() : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        frame.transform.SetParent(parent, false);
        frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        frame.sprite = source.sprite; frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        frame.fillCenter = false; frame.raycastTarget = false;
        return frame;
    }

    public static void ConfigureGauge(Slider slider, string name, string materialSource)
    {
        if (!slider || !slider.fillRect) return;
        var fill = slider.fillRect.GetComponent<Image>();
        if (name == "Special")
        {
            // The built-in rounded slider sprite tapers inside the tall tube.
            // Use the same opaque pixel as Marathon; the frame/glass supplies the shape.
            fill.sprite = AssetDatabase.LoadAssetAtPath<GameObject>(Modes + "MarathonLevelGauge.prefab")
                .GetComponent<MarathonGaugeUI>().gauge.fillRect.GetComponent<Image>().sprite;
            fill.overrideSprite = null;
            fill.preserveAspect = false;
        }
        fill.type = Image.Type.Filled;
        bool vertical = slider.direction == Slider.Direction.BottomToTop || slider.direction == Slider.Direction.TopToBottom;
        fill.fillMethod = vertical ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        if (!fill.GetComponent<MarathonLiquidSlosh>()) fill.gameObject.AddComponent<MarathonLiquidSlosh>();
        if (!slider.GetComponent<GaugeLiquidMotion>()) slider.gameObject.AddComponent<GaugeLiquidMotion>();
        var materialPath = Modes + name + "Liquid.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>(materialSource));
            material.shader = Shader.Find("Tetrabeasts/UI/Marathon Liquid");
            material.SetFloat("_Aspect", vertical ? 0.08f : 1f);
            material.SetVector("_BubbleSpeed", vertical ? new Vector4(0, -0.2f, 0, 0) : new Vector4(-0.1f, -0.2f, 0, 0));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        fill.material = material;
        if (vertical)
        {
            // Horizontal gauge settings hide bubbles in the narrow vertical UV strip.
            material.SetFloat("_BubbleScale", 6f);
            material.SetColor("_BubbleColor", new Color(1f, 0.8f, 0.55f));
            EditorUtility.SetDirty(material);
        }
        var scroll = fill.GetComponent<UILiquidMaterialScroll>() ?? fill.gameObject.AddComponent<UILiquidMaterialScroll>();
        var serializedScroll = new SerializedObject(scroll);
        serializedScroll.FindProperty("bubbleSpeed").vector2Value = new Vector2(vertical ? 0 : -0.1f, -0.2f);
        serializedScroll.ApplyModifiedPropertiesWithoutUndo();
        slider.fillRect.anchorMin = Vector2.zero; slider.fillRect.anchorMax = Vector2.one;
        fill.fillAmount = slider.normalizedValue;
        EditorUtility.SetDirty(slider); EditorUtility.SetDirty(fill);
    }

    public static TMP_Dropdown AddModeDropdown(GameObject root, bool local)
    {
        var existing = root.GetComponentsInChildren<TMP_Dropdown>(true).FirstOrDefault(d => d.name == "Game Mode Dropdown");
        // TMP's built-in authoring factory provides a complete keyboard/mouse accessible template.
        var dropdownRoot = existing ? existing.gameObject : TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdownRoot.name = "Game Mode Dropdown";
        dropdownRoot.transform.SetParent(root.transform, false);
        var rect = (RectTransform)dropdownRoot.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(local ? 210 : 180, 42);
        rect.anchoredPosition = new Vector2(local ? 430 : 115, local ? -100 : -39);
        var dropdown = dropdownRoot.GetComponent<TMP_Dropdown>();
        dropdown.options = new System.Collections.Generic.List<TMP_Dropdown.OptionData> {
            new("Standard"), new("Chaos"), new("Marathon") };
        dropdown.value = 0;
        var title = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Title_Text");
        foreach (var text in dropdownRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (title) text.font = title.font;
            text.color = new Color(1f, 0.88f, 0.4f); text.fontSize = 25;
        }
        dropdownRoot.GetComponent<Image>().color = new Color(0.57f, 0.28f, 0.68f);
        var dropdownColors = dropdown.colors;
        dropdownColors.highlightedColor = dropdownColors.selectedColor = new Color(1f, 0.8f, 0.6f);
        dropdownColors.pressedColor = new Color(0.8f, 0.55f, 0.6f); dropdown.colors = dropdownColors;
        dropdownRoot.transform.Find("Arrow").GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        dropdown.template.GetComponent<Image>().color = new Color(0.35f, 0.13f, 0.43f);
        var item = dropdown.template.GetComponentInChildren<Toggle>(true);
        var itemColors = item.colors;
        itemColors.normalColor = new Color(0.48f, 0.22f, 0.59f);
        itemColors.highlightedColor = itemColors.selectedColor = new Color(0.9f, 0.43f, 0.19f);
        item.colors = itemColors;
        item.graphic.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        ((RectTransform)item.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 36);
        var scrollRect = dropdown.template.GetComponent<ScrollRect>();
        if (scrollRect.verticalScrollbar) scrollRect.verticalScrollbar.gameObject.SetActive(false);
        scrollRect.verticalScrollbar = null;
        scrollRect.viewport.offsetMax = Vector2.zero;
        scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 38);
        dropdown.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 114);
        dropdown.RefreshShownValue();
        if (!local)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name == "Refresh_Text" || t.name == "RefreshShadow_Text"))
            {
                label.rectTransform.SetParent(root.transform, false);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0);
                label.rectTransform.pivot = Vector2.one * 0.5f;
                label.rectTransform.sizeDelta = new Vector2(500, 30);
                label.rectTransform.anchoredPosition = label.name.Contains("Shadow") ? new Vector2(2, -2) : Vector2.zero;
                if (label.name.Contains("Shadow")) label.color = Color.black;
                label.alignment = TextAlignmentOptions.Center; label.fontSize = 22;
                label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 22;
            }
        }
        if (title)
        {
            // Keep the title and mode choice together, clear of the close button and score columns.
            var titles = root.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name == "Title_Text" || t.name == "TitleShadow_Text" || t.name == "Title_TextShadow");
            foreach (var label in titles)
            {
                label.rectTransform.SetParent(root.transform, true);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                label.rectTransform.pivot = Vector2.one * 0.5f;
                label.rectTransform.localScale = Vector3.one;
                label.rectTransform.sizeDelta = local ? new Vector2(600, 110) : new Vector2(250, 46);
                label.rectTransform.anchoredPosition = new Vector2(local ? 0 : -116, local ? -100 : -39) + (label == title ? Vector2.zero : new Vector2(2, -2));
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = label.fontSizeMax = local ? 75 : 35; label.enableAutoSizing = true; label.fontSizeMin = local ? 60 : 23;
            }
        }
        return dropdown;
    }

    static void Edit(string path, Action<GameObject> action)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { action(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
