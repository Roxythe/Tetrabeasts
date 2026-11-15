using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class MarathonRewardUI : MonoBehaviour
{
    public Button[] choices;
    public TMP_Text[] choiceLabels;
    public TMP_Text[] buffLabels, debuffLabels;
    public TMP_Text[] buffTitles, debuffTitles;
    public Image[] buffBackgrounds, debuffBackgrounds;
    public MarathonRewardCard[] cards;
    public MarathonRewardColors colors;
    public Image[] buffIcons, debuffIcons;
    public Outline[] selectionOutlines;
    [HideInInspector] public Color buffTextColor = new Color(0.3f, 0.6f, 1f);
    [HideInInspector] public Color debuffTextColor = new Color(1f, 0.22f, 0.25f);
    public Button confirmButton;
    public TMP_Text title;
    [Header("Stat previews (authored in the prefab)")]
    public RectTransform[] previewPanels;
    public TMP_Text[] previewLabels;
    public Toggle compareAllToggle;
    public RectTransform content;
    public Vector2 contentSize = new Vector2(1120f, 900f);
    GameController previewGame;
    bool[] targeted;
    bool awaitingConfirmation;
    bool deferClose;
    public struct Pair { public RunModifierSO buff, debuff; }
    Pair[] pairs;
    int selected = -1;
    Action<RunModifierSO, RunModifierSO> onConfirm;
    void Awake()
    {
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            choices[i].onClick.AddListener(() => Select(index));
            choices[i].transition = Selectable.Transition.None;
            cards[i].previewOwner = this;
            cards[i].choiceIndex = i;
        }
        confirmButton.onClick.AddListener(Confirm);
        if (compareAllToggle) compareAllToggle.onValueChanged.AddListener(SetCompareAll);
    }
    public static Pair[] RollPairs(RunModifierSO[] buffs, RunModifierSO[] debuffs)
    {
        var available = new List<Pair>();
        var seen = new HashSet<(RunModifierSO, RunModifierSO)>();
        if (buffs != null && debuffs != null)
            foreach (var buff in buffs)
            foreach (var debuff in debuffs)
                if (buff && debuff && Rarity(buff) == Rarity(debuff) && !Conflicts(buff, debuff))
                {
                    var pair = new Pair { buff = buff, debuff = debuff };
                    if (seen.Add((buff, debuff))) available.Add(pair);
                }
        var result = new List<Pair>();
        float[] weights = { 55f, 25f, 12f, 6f, 2f };
        for (int i = 0; i < 3 && available.Count > 0; i++)
        {
            var rarities = new List<RunModRarity>();
            foreach (var pair in available) if (!rarities.Contains(Rarity(pair.buff))) rarities.Add(Rarity(pair.buff));
            float total = 0f;
            foreach (var r in rarities) total += weights[(int)r];
            float roll = UnityEngine.Random.value * total;
            var rarity = rarities[rarities.Count - 1];
            foreach (var r in rarities) { roll -= weights[(int)r]; if (roll <= 0f) { rarity = r; break; } }
            var candidates = available.FindAll(p => Rarity(p.buff) == rarity);
            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            result.Add(chosen); available.Remove(chosen);
        }
        return result.ToArray();
    }
    public static bool Conflicts(RunModifierSO buff, RunModifierSO debuff)
    {
        if (!(buff is RunModifier good) || !(debuff is RunModifier bad)) return false;
        if (good.stat == bad.stat) return true;
        // These affect the same outcome even though their serialized stat keys differ.
        bool SameFamily(RunModStat a, RunModStat b) =>
            (good.stat == a && bad.stat == b) || (good.stat == b && bad.stat == a);
        return SameFamily(RunModStat.ReserveUnitsRestoredOnWinAdd, RunModStat.DisableRoundWinReserveRestore) ||
            SameFamily(RunModStat.SpecialGaugeGainPerSecond, RunModStat.SpecialGaugeDrainPerSecond) ||
            SameFamily(RunModStat.LuckAdd, RunModStat.MisfortuneAdd) ||
            SameFamily(RunModStat.StoneBuffDropChanceAdd, RunModStat.StoneObstacleDropsDebuffsOnly);
    }
    public static RunModRarity Rarity(RunModifierSO modifier) => modifier is RunModifier rm ? rm.rarity : RunModRarity.Common;
    public static Color RarityColor(RunModRarity rarity)
    {
        switch (rarity)
        {
            case RunModRarity.Uncommon: return Color.green;
            case RunModRarity.Rare: return new Color(0.3f, 0.6f, 1f);
            case RunModRarity.Epic: return new Color(0.75f, 0.3f, 1f);
            case RunModRarity.Legendary: return new Color(1f, 0.75f, 0.2f);
            default: return Color.white;
        }
    }
    public void Show(int level, Pair[] options, Action<RunModifierSO, RunModifierSO> callback,
        GameController previewSource = null, bool keepOpenAfterConfirm = false)
    {
        pairs = options; onConfirm = callback; selected = -1;
        previewGame = previewSource ? previewSource : FindFirstObjectByType<GameController>();
        deferClose = keepOpenAfterConfirm;
        awaitingConfirmation = true;
        targeted = new bool[choices.Length];
        if (compareAllToggle)
        {
            compareAllToggle.SetIsOnWithoutNotify(SettingsStore.LoadMarathonCompareRewards());
            compareAllToggle.interactable = true;
        }
        title.text = "Level " + level + " — Choose a Modifier";
        confirmButton.interactable = false;
        for (int i = 0; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(i < pairs.Length);
            choices[i].interactable = true;
            if (i >= pairs.Length) continue;
            var p = pairs[i];
            var color = colors ? colors.GetRarity(Rarity(p.buff)) : RarityColor(Rarity(p.buff));
            choiceLabels[i].text = Rarity(p.buff).ToString();
            choiceLabels[i].color = color;
            buffTitles[i].text = p.buff.displayName;
            debuffTitles[i].text = p.debuff.displayName;
            buffLabels[i].text = p.buff.description;
            debuffLabels[i].text = p.debuff.description;
            buffTitles[i].color = colors.buffTitle; debuffTitles[i].color = colors.debuffTitle;
            buffLabels[i].color = colors.buffDescription; debuffLabels[i].color = colors.debuffDescription;
            buffBackgrounds[i].color = colors.buffBackground; debuffBackgrounds[i].color = colors.debuffBackground;
            buffIcons[i].sprite = p.buff.icon; buffIcons[i].enabled = p.buff.icon;
            debuffIcons[i].sprite = p.debuff.icon; debuffIcons[i].enabled = p.debuff.icon;
            choices[i].GetComponent<Image>().color = Color.clear;
            if (cards[i].rarityBorder) cards[i].rarityBorder.color = color;
            selectionOutlines[i].enabled = false;
            cards[i].ResetState();
            if (previewLabels != null && i < previewLabels.Length && previewLabels[i])
                previewLabels[i].text = Describe(p.buff, null) + "\n\n" + Describe(p.debuff, p.buff as RunModifier);
        }
        gameObject.SetActive(true); transform.SetAsLastSibling();
        FitContent();
        RefreshPreviews();
        foreach (var shadow in GetComponentsInChildren<TMPLabelShadow>(true)) shadow.Sync();
        var navigator = GetComponent<ScopedMenuNavigator>() ?? gameObject.AddComponent<ScopedMenuNavigator>();
        navigator.SetNavigationRoot(gameObject);
        if (EventSystem.current && pairs.Length > 0) EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }
    void Select(int index)
    {
        if (!awaitingConfirmation) return;
        selected = index; confirmButton.interactable = true;
        for (int i = 0; i < choices.Length; i++)
            cards[i].SetSelected(i == selected);
    }
    void Confirm()
    {
        if (!awaitingConfirmation || selected < 0 || selected >= pairs.Length) return;
        awaitingConfirmation = false;
        confirmButton.interactable = false;
        foreach (var choice in choices) choice.interactable = false;
        if (compareAllToggle) compareAllToggle.interactable = false;
        RefreshPreviews();
        var pair = pairs[selected];
        if (!deferClose) Dismiss();
        onConfirm?.Invoke(pair.buff, pair.debuff);
    }

    public void Dismiss() => gameObject.SetActive(false);

    string Describe(RunModifierSO mod, RunModifier pendingBuff) => previewGame && mod is RunModifier generic
        ? previewGame.RewardPreview(generic, pendingBuff, colors.buffDescription, colors.debuffDescription)
        : TetrabeastsLocalization.LocalizeText(mod.description);

    public void TargetReward(int index, bool isTargeted)
    {
        if (targeted == null || index < 0 || index >= targeted.Length || targeted[index] == isTargeted) return;
        targeted[index] = isTargeted;
        RefreshPreviews();
    }

    void SetCompareAll(bool enabled)
    {
        SettingsStore.SaveMarathonCompareRewards(enabled);
        RefreshPreviews();
    }

    void RefreshPreviews()
    {
        if (previewPanels == null) return;
        for (int i = 0; i < previewPanels.Length; i++)
        {
            if (!previewPanels[i]) continue;
            bool visible = awaitingConfirmation && pairs != null && i < pairs.Length &&
                ((compareAllToggle && compareAllToggle.isOn) || (targeted != null && i < targeted.Length && targeted[i]));
            if (previewPanels[i].gameObject.activeSelf != visible) previewPanels[i].gameObject.SetActive(visible);
        }
    }

    void LateUpdate() => FitContent();

    void FitContent()
    {
        if (!content || !(transform is RectTransform root)) return;
        var panel = content.parent as RectTransform;
        if (!panel) return;
        if (panel != root)
        {
            float panelScale = Mathf.Min(1f, (root.rect.width - 48f) / panel.rect.width,
                (root.rect.height - 48f) / panel.rect.height);
            panel.localScale = Vector3.one * Mathf.Max(.1f, panelScale);
        }
        float scale = Mathf.Min(1f, (panel.rect.width - 48f) / contentSize.x, (panel.rect.height - 48f) / contentSize.y);
        content.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }
}
