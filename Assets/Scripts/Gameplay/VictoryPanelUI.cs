using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] GameObject root;

    [Header("Stats")]
    [SerializeField] TMP_Text statsCategoryText;
    [SerializeField] TMP_Text statsEarnedText;
    [SerializeField] TMP_Text unlockedDifficultyText;

    [Header("Modifier Lists")]
    [SerializeField] Transform buffsContent;
    [SerializeField] Transform debuffsContent;
    [SerializeField] GameObject modifierRowPrefab;

    [Header("Continue")]
    [SerializeField] Button continueButton;

    [Header("Rarity Colors")]
    [SerializeField] Color commonTitleColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] Color uncommonTitleColor = Color.green;
    [SerializeField] Color rareTitleColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] Color epicTitleColor = new Color(0.75f, 0.3f, 1f, 1f);
    [SerializeField] Color legendaryTitleColor = new Color(1f, 0.75f, 0.2f, 1f);

    const string DefaultCategoryText =
        "Lines Cleared:\n" +
        "Special Used:\n" +
        "Obstacles Destroyed:\n" +
        "Highest Combo:\n" +
        "Highest Single Attack:\n" +
        "Units Died:\n" +
        "Units Healed:\n" +
        "Total Damage Dealt:\n" +
        "Clear Time:\n\n" +
        "Final Score:";

    readonly struct ModKey
    {
        public readonly RunModifierSO mod;
        public readonly RunModRarity rarity;

        public ModKey(RunModifierSO mod, RunModRarity rarity)
        {
            this.mod = mod;
            this.rarity = rarity;
        }
    }

    sealed class ModKeyComparer : IEqualityComparer<ModKey>
    {
        public bool Equals(ModKey a, ModKey b)
        {
            return ReferenceEquals(a.mod, b.mod) && a.rarity == b.rarity;
        }

        public int GetHashCode(ModKey key)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (key.mod ? key.mod.GetInstanceID() : 0);
                hash = (hash * 31) + (int)key.rarity;
                return hash;
            }
        }
    }

    void Awake()
    {
        AutoWire();
        Hide();
    }

    void OnValidate()
    {
        AutoWire();
    }

    public void SetModifierRowPrefab(GameObject prefab)
    {
        if (prefab)
            modifierRowPrefab = prefab;
    }

    public void Show(RunSummaryStats.Snapshot stats, IReadOnlyList<RunModifierSO> buffs,
                 IReadOnlyList<RunModifierSO> debuffs, Action onContinue)
    {
        AutoWire();

        if (!root)
            root = gameObject;

        RefreshStats(stats);
        RebuildModifierList(buffsContent, buffs);
        RebuildModifierList(debuffsContent, debuffs);

        if (continueButton)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }

        UIPanelTransition.Show(root);
        Canvas.ForceUpdateCanvases();
    }

    public void SetRootActive(bool active)
    {
        if (!root)
            root = gameObject;

        if (root)
            UIPanelTransition.SetVisible(root, active);
    }

    public void Hide()
    {
        if (!root)
            root = gameObject;

        if (root)
            UIPanelTransition.Hide(root, true);
    }

    void RefreshStats(RunSummaryStats.Snapshot stats)
    {
        if (statsCategoryText && string.IsNullOrWhiteSpace(statsCategoryText.text))
            statsCategoryText.text = DefaultCategoryText;

        if (!statsEarnedText)
            return;

        statsEarnedText.text =
            $"{stats.linesCleared} Lines\n" +
            $"{stats.specialUsedCount} Times\n" +
            $"{stats.obstaclesDestroyed} Obstacles\n" +
            $"{stats.highestCombo}\n" +
            $"{stats.highestSingleAttackDamage} Damage\n" +
            $"{stats.unitsDied} Units\n" +
            $"{Mathf.RoundToInt(stats.totalHealingDone)} Health\n" +
            $"{stats.totalDamageDealt} Damage\n" +
            $"{FormatDuration(stats.clearTimeSeconds)}\n\n" +
            $"{stats.finalScore}";
    }

    void RebuildModifierList(Transform contentRoot, IReadOnlyList<RunModifierSO> source)
    {
        if (!contentRoot)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        if (!modifierRowPrefab || source == null)
            return;

        var comparer = new ModKeyComparer();
        var counts = new Dictionary<ModKey, int>(comparer);
        var order = new List<ModKey>();

        for (int i = 0; i < source.Count; i++)
        {
            var mod = source[i];
            if (!mod) continue;

            var key = new ModKey(mod, GetRarity(mod));
            if (counts.TryGetValue(key, out var currentCount))
            {
                counts[key] = currentCount + 1;
            }
            else
            {
                counts[key] = 1;
                order.Add(key);
            }
        }

        for (int i = 0; i < order.Count; i++)
        {
            var key = order[i];
            if (!key.mod) continue;

            var row = Instantiate(modifierRowPrefab, contentRoot);
            row.name = $"{key.mod.displayName}_VictoryRow";
            BindRow(row.transform, key.mod, key.rarity, counts[key]);
        }
    }

    void BindRow(Transform rowRoot, RunModifierSO mod, RunModRarity rarity, int copies)
    {
        if (!rowRoot || !mod)
            return;

        string titleValue = string.IsNullOrWhiteSpace(mod.displayName) ? mod.name : mod.displayName;
        string counterValue = $"x{Mathf.Max(1, copies)}";
        Color titleColor = GetTitleColor(rarity);

        var binder = rowRoot.GetComponent<VictoryModifierRowUI>();
        if (binder)
        {
            binder.Bind(mod.icon, titleValue, counterValue, titleColor);
            return;
        }

        var icon = FindImage(rowRoot, "iconimage");
        if (icon)
        {
            icon.sprite = mod.icon;
            icon.enabled = mod.icon != null;
        }

        var title = FindText(rowRoot, "titletext");
        var titleShadow = FindText(rowRoot, "shadowtitletext");
        var counter = FindText(rowRoot, "countertext");
        var counterShadow = FindText(rowRoot, "shadowcountertext");

        if (title)
        {
            title.text = titleValue;
            title.color = titleColor;
        }

        if (titleShadow)
            titleShadow.text = titleValue;

        if (counter)
            counter.text = counterValue;

        if (counterShadow)
            counterShadow.text = counterValue;
    }

    void AutoWire()
    {
        if (!root)
            root = gameObject;

        var rootTransform = root ? root.transform : transform;
        if (!rootTransform)
            return;

        if (!statsCategoryText)
            statsCategoryText = FindText(rootTransform, "StatsCategory_Text");

        if (!statsEarnedText)
            statsEarnedText = FindText(rootTransform, "StatsEarned_Text");

        if (!unlockedDifficultyText)
            unlockedDifficultyText = FindText(rootTransform, "UnlockedDifficulty_Text");

        if (!continueButton)
        {
            var continueTransform = FindDescendant(rootTransform, "Continue_Button", exactMatch: true);
            if (continueTransform)
                continueButton = continueTransform.GetComponent<Button>();
        }

        if (!buffsContent)
            buffsContent = ResolveScrollContent(rootTransform, "Buff_ScrollView");

        if (!debuffsContent)
            debuffsContent = ResolveScrollContent(rootTransform, "Debuff_ScrollView");
    }

    Transform ResolveScrollContent(Transform rootTransform, string scrollViewName)
    {
        var scrollView = FindDescendant(rootTransform, scrollViewName, exactMatch: true);
        if (!scrollView)
            return null;

        var scrollRect = scrollView.GetComponent<ScrollRect>();
        if (scrollRect && scrollRect.content)
            return scrollRect.content;

        var content = FindDescendant(scrollView, "content", exactMatch: true);
        return content;
    }

    static TMP_Text FindText(Transform rootTransform, string normalizedName)
    {
        var found = FindDescendant(rootTransform, normalizedName, exactMatch: false);
        return found ? found.GetComponent<TMP_Text>() : null;
    }

    static Image FindImage(Transform rootTransform, string normalizedName)
    {
        var found = FindDescendant(rootTransform, normalizedName, exactMatch: false);
        return found ? found.GetComponent<Image>() : null;
    }

    static Transform FindDescendant(Transform current, string normalizedName, bool exactMatch)
    {
        if (!current)
            return null;

        string currentName = NormalizeName(current.name);
        bool matches = exactMatch
            ? currentName == normalizedName
            : currentName.Contains(normalizedName);

        if (matches)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            var found = FindDescendant(current.GetChild(i), normalizedName, exactMatch);
            if (found)
                return found;
        }

        return null;
    }

    static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var buffer = new char[value.Length];
        int length = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (!char.IsLetterOrDigit(c))
                continue;

            buffer[length++] = char.ToLowerInvariant(c);
        }

        return new string(buffer, 0, length);
    }

    static RunModRarity GetRarity(RunModifierSO mod)
    {
        return mod is RunModifier runModifier ? runModifier.rarity : RunModRarity.Common;
    }

    Color GetTitleColor(RunModRarity rarity)
    {
        return rarity switch
        {
            RunModRarity.Uncommon => uncommonTitleColor,
            RunModRarity.Rare => rareTitleColor,
            RunModRarity.Epic => epicTitleColor,
            RunModRarity.Legendary => legendaryTitleColor,
            _ => commonTitleColor
        };
    }

    static string FormatDuration(float totalSeconds)
    {
        totalSeconds = Mathf.Max(0f, totalSeconds);
        int roundedSeconds = Mathf.RoundToInt(totalSeconds);

        int hours = roundedSeconds / 3600;
        int minutes = (roundedSeconds % 3600) / 60;
        int seconds = roundedSeconds % 60;

        return $"{hours}H {minutes}M {seconds}S";
    }

    public void SetUnlockedDifficultyText(string message)
    {
        if (!unlockedDifficultyText)
            return;

        bool hasMessage = !string.IsNullOrWhiteSpace(message);
        unlockedDifficultyText.gameObject.SetActive(hasMessage);
        unlockedDifficultyText.text = hasMessage ? message : string.Empty;
    }
}
