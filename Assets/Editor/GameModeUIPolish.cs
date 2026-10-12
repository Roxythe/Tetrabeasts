#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicit authoring migration. Runtime uses the saved, editable prefabs.
public static class GameModeUIPolish
{
    const string Folder = "Assets/Prefabs/GameModes/";
    static readonly Color Green = new Color(0.49411765f, 1f, 0.18431373f);
    static readonly Color Yellow = new Color(1f, 0.91764706f, 0f);
    static TMP_FontAsset font;

    [MenuItem("Tetrabeasts/Apply Marathon UI Polish")]
    public static void Apply()
    {
        Edit("GameModeMenu.prefab", Menu);
        Edit("MarathonRewardPanel.prefab", Rewards);
        Edit("MarathonLevelGauge.prefab", Gauge);
        AssetDatabase.SaveAssets();
        Debug.Log("Saved Marathon rewards, mode tooltips and liquid gauge UI polish.");
    }

    public static void PolishNewPrefab(GameObject root)
    {
        font = root.GetComponentInChildren<TMP_Text>(true).font;
        if (root.GetComponent<GameModeSelectionUI>()) Menu(root);
        else if (root.GetComponent<MarathonRewardUI>()) Rewards(root);
        else if (root.GetComponent<MarathonGaugeUI>()) Gauge(root);
    }

    static void Edit(string file, System.Action<GameObject> edit)
    {
        string path = Folder + file;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            font = root.GetComponentInChildren<TMP_Text>(true).font;
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Sound(Button button, bool lockedHover = false)
    {
        var sound = button.GetComponent<UIButtonSFX>() ?? button.gameObject.AddComponent<UIButtonSFX>();
        sound.hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("dae9f1364bb247044be20a7795e60123"));
        sound.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("731b97b241116d84aa62d03a949ca0e8"));
        sound.hoverWhenDisabled = lockedHover;
    }

    static void Style(Button button, Color normal)
    {
        button.GetComponent<Image>().color = Color.white;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = normal; colors.highlightedColor = colors.selectedColor = Yellow;
        colors.pressedColor = new Color(normal.r * 0.75f, normal.g * 0.75f, normal.b * 0.75f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.colorMultiplier = 1f; button.colors = colors;
        Sound(button, true);
    }

    static void Menu(GameObject root)
    {
        var ui = root.GetComponent<GameModeSelectionUI>();
        string[] descriptions = {
            "Battle through the original castle campaign. Defeat each castle to advance and conquer the final level to win.",
            "Play the Standard campaign with a new modifier every other level. All selected modifiers stay active for the rest of the run. Earn an additional 20% monster experience.",
            "An endless battle. Deal damage to level up and choose a buff and debuff of matching rarity. Face a boss every third level; the final boss stays indefinitely."
        };
        var buttons = new[] { ui.standardButton, ui.chaosButton, ui.marathonButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            Style(buttons[i], Green);
            buttons[i].GetComponentInChildren<TMP_Text>().text = new[] { "Standard", "Chaos", "Marathon" }[i];
            var hover = buttons[i].GetComponent<GameModeButtonTooltip>() ?? buttons[i].gameObject.AddComponent<GameModeButtonTooltip>();
            hover.menu = ui; hover.description = descriptions[i];
        }
        Style(ui.closeButton, new Color(1f, 0.22f, 0.18f));
        if (ui.lockDescription) ui.lockDescription.gameObject.SetActive(false);
        var panel = ui.standardButton.transform.parent;
        Remove(panel, "Mode Tooltip");
        ui.tooltip = Rect("Mode Tooltip", panel, Vector2.zero, new Vector2(310, 340));
        ui.tooltip.pivot = new Vector2(0, 0.5f);
        var background = ui.tooltip.gameObject.AddComponent<Image>(); background.color = new Color(0.04f, 0.04f, 0.065f, 0.98f); background.raycastTarget = false;
        var border = ui.tooltip.gameObject.AddComponent<Outline>(); border.effectColor = new Color(0.55f, 0.44f, 0.25f); border.effectDistance = new Vector2(2, -2);
        ui.tooltipTitle = Label(ui.tooltip, "Mode Name", "Chaos", new Vector2(0, 137), new Vector2(276, 44), 31);
        ui.tooltipTitle.color = Green;
        ui.tooltipDescription = Label(ui.tooltip, "Description", descriptions[1], new Vector2(0, 12), new Vector2(274, 190), 25);
        ui.tooltipDescription.alignment = TextAlignmentOptions.TopLeft;
        ui.tooltipLockNote = Label(ui.tooltip, "Unlock Note", "Reach the final level in Standard to unlock this game mode.", new Vector2(0, -125), new Vector2(274, 72), 22);
        ui.tooltipLockNote.color = new Color(1f, 0.8f, 0.32f);
        ui.tooltip.gameObject.SetActive(false);
    }

    static void Rewards(GameObject root)
    {
        var ui = root.GetComponent<MarathonRewardUI>();
        ui.buffTextColor = new Color(0.3f, 0.6f, 1f);
        ui.title.text = "Choose a Modifier";
        ui.buffLabels = new TMP_Text[3]; ui.debuffLabels = new TMP_Text[3];
        ui.buffIcons = new Image[3]; ui.debuffIcons = new Image[3]; ui.selectionOutlines = new Outline[3];
        for (int i = 0; i < 3; i++)
        {
            var button = ui.choices[i];
            var rect = (RectTransform)button.transform;
            Sound(button);
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 0.92f, 0.6f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f); button.colors = colors;
            button.GetComponent<Image>().color = Color.white;
            ui.selectionOutlines[i] = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            ui.selectionOutlines[i].effectColor = Yellow;
            ui.selectionOutlines[i].effectDistance = new Vector2(6, -6);
            ui.selectionOutlines[i].enabled = false;
            Remove(rect, "Rarity Backing"); Remove(rect, "Buff"); Remove(rect, "Debuff");
            var header = Rect("Rarity Backing", rect, new Vector2(0, 209), new Vector2(306, 48));
            header.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.03f, 0.05f);
            header.GetComponent<Image>().raycastTarget = false;
            var rarity = ui.choiceLabels[i];
            rarity.rectTransform.anchoredPosition = new Vector2(0, 209); rarity.rectTransform.sizeDelta = new Vector2(294, 44);
            rarity.enableAutoSizing = false; rarity.fontSize = 29; rarity.text = "Common"; rarity.color = Color.white;
            rarity.transform.SetAsLastSibling();
            Section("Buff", 79, out ui.buffIcons[i], out ui.buffLabels[i]);
            Section("Debuff", -132, out ui.debuffIcons[i], out ui.debuffLabels[i]);
            ui.buffLabels[i].color = ui.buffTextColor; ui.debuffLabels[i].color = ui.debuffTextColor;

            void Section(string name, float y, out Image icon, out TMP_Text label)
            {
                var section = Rect(name, rect, new Vector2(0, y), new Vector2(306, 200));
                var backing = section.gameObject.AddComponent<Image>(); backing.color = new Color(0.03f, 0.035f, 0.055f, 0.985f); backing.raycastTarget = false;
                var iconRect = Rect("Icon", section, Vector2.zero, new Vector2(180, 180));
                icon = iconRect.gameObject.AddComponent<Image>(); icon.color = new Color(1, 1, 1, 0.19f); icon.preserveAspect = true; icon.raycastTarget = false;
                label = Label(section, "Title and Description", name, Vector2.zero, new Vector2(278, 180), 27);
                label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 27;
            }
        }
        Style(ui.confirmButton, Green);
    }

    static void Gauge(GameObject root)
    {
        var ui = root.GetComponent<MarathonGaugeUI>();
        var rect = (RectTransform)root.transform;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64f);
        ui.horizontalOffset = 28f;
        ui.gauge.transition = Selectable.Transition.None;
        ui.gauge.navigation = new Navigation { mode = Navigation.Mode.None };
        ui.gauge.targetGraphic = null;
        if (root.GetComponent<Image>()) Object.DestroyImmediate(root.GetComponent<Image>());
        Remove(rect, "Background");
        var backgroundRect = Rect("Background", rect, Vector2.zero, Vector2.zero);
        backgroundRect.SetAsFirstSibling();
        backgroundRect.anchorMin = new Vector2(0.25f, 0); backgroundRect.anchorMax = new Vector2(0.75f, 1);
        var background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.018f, 0.008f, 0.03f, 1f); background.raycastTarget = false;
        var fillArea = (RectTransform)ui.gauge.fillRect.parent;
        fillArea.anchorMin = new Vector2(0.25f, 0); fillArea.anchorMax = new Vector2(0.75f, 1);
        fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
        if (ui.levelLabel) Object.DestroyImmediate(ui.levelLabel.gameObject);
        ui.levelLabel = null;
        ui.damageLabel.rectTransform.anchoredPosition = Vector2.zero;
        ui.damageLabel.rectTransform.anchorMin = ui.damageLabel.rectTransform.anchorMax = Vector2.one * 0.5f;
        ui.damageLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
        ui.damageLabel.rectTransform.sizeDelta = new Vector2(rect.rect.height - 40, 36);
        ui.damageLabel.fontSize = 23;
        var shadow = ui.damageLabel.GetComponent<Shadow>() ?? ui.damageLabel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -1);
        ui.liquidSlosh = ui.gauge.fillRect.GetComponent<MarathonLiquidSlosh>() ?? ui.gauge.fillRect.gameObject.AddComponent<MarathonLiquidSlosh>();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab");
        var images = source.GetComponentsInChildren<Image>(true);
        Remove(rect, "Border_Image"); Remove(rect, "GlassOverlay");
        // Copy the existing art with proportional anchors, so runtime height adjustments stretch it correctly.
        var borderSource = images.First(i => i.name == "Border_Image");
        var borderRect = Rect("Border_Image", rect, new Vector2(0, -5.53f), Vector2.zero);
        borderRect.anchorMin = new Vector2(0.5f - 4.4341f / 2, 0.5f - 1.295945f / 2);
        borderRect.anchorMax = Vector2.one - borderRect.anchorMin;
        var border = borderRect.gameObject.AddComponent<Image>(); border.sprite = borderSource.sprite; border.raycastTarget = false;
        borderRect.localScale = new Vector3(-1f, 1f, 1f);
        var glassSource = images.First(i => i.name == "GlassOverlay");
        var glassRect = Rect("GlassOverlay", rect, Vector2.zero, Vector2.zero);
        glassRect.anchorMin = Vector2.zero; glassRect.anchorMax = Vector2.one;
        // Source glass is horizontal artwork rotated -90 degrees.
        var glassImage = glassRect.gameObject.AddComponent<Image>(); glassImage.sprite = glassSource.sprite;
        glassImage.color = glassSource.color; glassImage.raycastTarget = false;
        glassRect.anchorMin = glassRect.anchorMax = Vector2.one * 0.5f;
        glassRect.localRotation = Quaternion.Euler(0, 0, -90);
        glassRect.sizeDelta = new Vector2(rect.rect.height * 1.049158f, rect.rect.width * 0.577755f);
        ui.glassOverlay = glassRect;
        ui.damageLabel.transform.SetAsLastSibling();
    }

    static void Remove(Transform parent, string name)
    {
        var child = parent.Find(name); if (child) Object.DestroyImmediate(child.gameObject);
    }
    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.gameObject.layer = 5;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static TMP_Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
}
#endif
