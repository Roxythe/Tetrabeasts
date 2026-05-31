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
    [SerializeField] ScrollRect buffsScrollRect;
    [SerializeField] ScrollRect debuffsScrollRect;
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

    bool _showRequested;
    ScopedMenuNavigator _navigator;

    public GameObject RootPanel => root ? root : gameObject;

    void Awake()
    {
        AutoWire();

        if (!_showRequested)
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
        _showRequested = true;
        AutoWire();

        if (!root)
            root = gameObject;

        RefreshStats(stats);
        RebuildModifierList(buffsContent, buffs);
        RebuildModifierList(debuffsContent, debuffs);

        if (root)
            root.transform.SetAsLastSibling();

        if (continueButton)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }

        UIPanelTransition.Show(root);
        Canvas.ForceUpdateCanvases();
        RefreshModifierScrollAreas();
        Canvas.ForceUpdateCanvases();
        EnableNavigation();
        _showRequested = false;
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

        DisableNavigation();

        if (root)
            UIPanelTransition.Hide(root, true);
    }

    void OnDisable()
    {
        DisableNavigation();
    }

    void EnableNavigation()
    {
        if (!root)
            root = gameObject;

        if (!root)
            return;

        if (!_navigator)
            _navigator = ScopedMenuNavigator.Attach(root, root);

        if (_navigator)
        {
            _navigator.enabled = true;
            _navigator.SetNavigationRoot(root, selectFirst: true);
        }
    }

    void DisableNavigation()
    {
        if (_navigator)
            _navigator.enabled = false;
    }

    void RefreshStats(RunSummaryStats.Snapshot stats)
    {
        if (statsCategoryText)
            statsCategoryText.text = BuildStatsCategoryText();

        if (!statsEarnedText)
            return;

        statsEarnedText.text =
            $"{FormatLocalizedCount(stats.linesCleared, "Lines")}\n" +
            $"{FormatLocalizedCount(stats.specialUsedCount, "Times")}\n" +
            $"{FormatLocalizedCount(stats.obstaclesDestroyed, "Obstacles")}\n" +
            $"{stats.highestCombo}\n" +
            $"{FormatLocalizedCount(stats.highestSingleAttackDamage, "Damage")}\n" +
            $"{FormatLocalizedCount(stats.unitsDied, "Units")}\n" +
            $"{FormatLocalizedCount(Mathf.RoundToInt(stats.totalHealingDone), "Health")}\n" +
            $"{FormatLocalizedCount(stats.totalDamageDealt, "Damage")}\n" +
            $"{FormatDuration(stats.clearTimeSeconds)}\n\n" +
            $"{stats.finalScore}";
    }

    void RebuildModifierList(Transform contentRoot, IReadOnlyList<RunModifierSO> source)
    {
        if (!contentRoot)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = contentRoot.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        if (!modifierRowPrefab || source == null)
            return;

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var representatives = new Dictionary<string, RunModifierSO>(StringComparer.OrdinalIgnoreCase);
        var rarities = new Dictionary<string, RunModRarity>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();

        for (int i = 0; i < source.Count; i++)
        {
            var mod = source[i];
            if (!mod) continue;

            string key = GetModifierGroupKey(mod);
            RunModRarity rarity = GetRarity(mod);
            if (counts.TryGetValue(key, out var currentCount))
            {
                counts[key] = currentCount + 1;
                if (rarity > rarities[key])
                {
                    representatives[key] = mod;
                    rarities[key] = rarity;
                }
            }
            else
            {
                counts[key] = 1;
                representatives[key] = mod;
                rarities[key] = rarity;
                order.Add(key);
            }
        }

        for (int i = 0; i < order.Count; i++)
        {
            var key = order[i];
            if (!representatives.TryGetValue(key, out RunModifierSO mod) || !mod)
                continue;

            var row = Instantiate(modifierRowPrefab, contentRoot);
            row.name = $"{key}_VictoryRow";
            BindRow(row.transform, mod, rarities[key], counts[key]);
        }
    }

    void RefreshModifierScrollAreas()
    {
        RefreshModifierScrollArea(buffsScrollRect, buffsContent);
        RefreshModifierScrollArea(debuffsScrollRect, debuffsContent);
    }

    void RefreshModifierScrollArea(ScrollRect scrollRect, Transform contentRoot)
    {
        if (!contentRoot)
            return;

        if (!scrollRect)
            scrollRect = contentRoot.GetComponentInParent<ScrollRect>(true);

        var contentRect = contentRoot as RectTransform;
        if (!contentRect)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float preferredHeight = LayoutUtility.GetPreferredHeight(contentRect);
        if (preferredHeight <= 0.01f)
            preferredHeight = CalculateChildrenHeight(contentRect);

        RectTransform viewport = null;
        if (scrollRect)
            viewport = scrollRect.viewport ? scrollRect.viewport : scrollRect.transform as RectTransform;

        float viewportHeight = viewport ? viewport.rect.height : 0f;
        float targetHeight = viewportHeight > 0f ? Mathf.Max(viewportHeight, preferredHeight) : preferredHeight;

        if (targetHeight > 0.01f)
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        if (!scrollRect)
            return;

        bool shouldScroll = preferredHeight > viewportHeight + 0.5f;
        scrollRect.vertical = shouldScroll;
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.velocity = Vector2.zero;

        var scrollbar = ResolveVerticalScrollbar(scrollRect);
        if (!scrollbar)
            return;

        scrollbar.gameObject.SetActive(shouldScroll);
        scrollbar.interactable = shouldScroll;
        scrollbar.size = shouldScroll && targetHeight > 0.01f
            ? Mathf.Clamp01(viewportHeight / targetHeight)
            : 1f;
        scrollbar.value = 1f;
    }

    static float CalculateChildrenHeight(RectTransform contentRect)
    {
        if (!contentRect)
            return 0f;

        var layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        float height = layoutGroup ? layoutGroup.padding.top + layoutGroup.padding.bottom : 0f;
        float spacing = layoutGroup ? layoutGroup.spacing : 0f;
        int visibleChildren = 0;

        for (int i = 0; i < contentRect.childCount; i++)
        {
            var child = contentRect.GetChild(i) as RectTransform;
            if (!child || !child.gameObject.activeSelf)
                continue;

            float childHeight = LayoutUtility.GetPreferredHeight(child);
            if (childHeight <= 0.01f)
                childHeight = child.rect.height;

            height += Mathf.Max(0f, childHeight);
            visibleChildren++;
        }

        if (visibleChildren > 1)
            height += spacing * (visibleChildren - 1);

        return height;
    }

    static Scrollbar ResolveVerticalScrollbar(ScrollRect scrollRect)
    {
        if (!scrollRect)
            return null;

        if (scrollRect.verticalScrollbar)
            return scrollRect.verticalScrollbar;

        var scrollbars = scrollRect.GetComponentsInChildren<Scrollbar>(true);
        for (int i = 0; i < scrollbars.Length; i++)
        {
            var scrollbar = scrollbars[i];
            if (!scrollbar)
                continue;

            if (scrollbar.direction == Scrollbar.Direction.BottomToTop ||
                scrollbar.direction == Scrollbar.Direction.TopToBottom)
            {
                scrollRect.verticalScrollbar = scrollbar;
                return scrollbar;
            }
        }

        return null;
    }

    void BindRow(Transform rowRoot, RunModifierSO mod, RunModRarity rarity, int copies)
    {
        if (!rowRoot || !mod)
            return;

        string titleValue = string.IsNullOrWhiteSpace(mod.displayName)
            ? mod.name
            : TetrabeastsLocalization.LocalizeText(mod.displayName);
        string counterValue = TetrabeastsLocalization.LocalizeFormat("x{0}", Mathf.Max(1, copies));
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

        if (!buffsScrollRect)
            buffsScrollRect = ResolveScrollRect(rootTransform, "Buff_ScrollView");

        if (!debuffsScrollRect)
            debuffsScrollRect = ResolveScrollRect(rootTransform, "Debuff_ScrollView");

        if (!buffsContent)
            buffsContent = ResolveScrollContent(rootTransform, "Buff_ScrollView", buffsScrollRect);

        if (!debuffsContent)
            debuffsContent = ResolveScrollContent(rootTransform, "Debuff_ScrollView", debuffsScrollRect);
    }

    ScrollRect ResolveScrollRect(Transform rootTransform, string scrollViewName)
    {
        var scrollView = FindDescendant(rootTransform, scrollViewName, exactMatch: true);
        if (!scrollView)
            return null;

        return scrollView.GetComponent<ScrollRect>();
    }

    Transform ResolveScrollContent(Transform rootTransform, string scrollViewName, ScrollRect knownScrollRect)
    {
        var scrollRect = knownScrollRect ? knownScrollRect : ResolveScrollRect(rootTransform, scrollViewName);
        if (scrollRect && scrollRect.content)
            return scrollRect.content;

        var scrollView = FindDescendant(rootTransform, scrollViewName, exactMatch: true);
        if (!scrollView)
            return null;

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

    static Transform FindDescendant(Transform current, string targetName, bool exactMatch)
    {
        string normalizedName = NormalizeName(targetName);
        return FindDescendantNormalized(current, normalizedName, exactMatch);
    }

    static Transform FindDescendantNormalized(Transform current, string normalizedName, bool exactMatch)
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
            var found = FindDescendantNormalized(current.GetChild(i), normalizedName, exactMatch);
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

    static string GetModifierGroupKey(RunModifierSO mod)
    {
        if (!mod)
            return string.Empty;

        string displayName = string.IsNullOrWhiteSpace(mod.displayName) ? mod.name : mod.displayName;
        return string.IsNullOrWhiteSpace(displayName)
            ? mod.GetInstanceID().ToString()
            : displayName.Trim();
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

    static string BuildStatsCategoryText()
    {
        return string.Join("\n",
            TetrabeastsLocalization.LocalizeText("Lines Cleared:"),
            TetrabeastsLocalization.LocalizeText("Special Used:"),
            TetrabeastsLocalization.LocalizeText("Obstacles Destroyed:"),
            TetrabeastsLocalization.LocalizeText("Highest Combo:"),
            TetrabeastsLocalization.LocalizeText("Highest Single Attack:"),
            TetrabeastsLocalization.LocalizeText("Units Died:"),
            TetrabeastsLocalization.LocalizeText("Units Healed:"),
            TetrabeastsLocalization.LocalizeText("Total Damage Dealt:"),
            TetrabeastsLocalization.LocalizeText("Clear Time:"))
            + "\n\n" + TetrabeastsLocalization.LocalizeText("Final Score:");
    }

    static string FormatLocalizedCount(int amount, string englishUnit)
    {
        return $"{amount} {TetrabeastsLocalization.LocalizeText(englishUnit)}";
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
        unlockedDifficultyText.text = hasMessage ? TetrabeastsLocalization.LocalizeText(message) : string.Empty;
    }
}
