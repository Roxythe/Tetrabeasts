using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodexPanelUI : MonoBehaviour
{
    enum SectionIndex
    {
        Buffs = 0,
        Debuffs = 1,
        LevelModifiers = 2
    }

    [Serializable]
    class SectionView
    {
        public GameObject root;
        public Button tabButton;
        public ScrollRect scrollRect;
        public Transform contentParent;
    }

    [Header("Data Sources")]
    [Tooltip("Assign the buff modifier assets you want shown in the Codex.")]
    [SerializeField] RunModifierSO[] buffRunModifiers;
    [Tooltip("Assign the debuff modifier assets you want shown in the Codex.")]
    [SerializeField] RunModifierSO[] debuffRunModifiers;
    [SerializeField] LevelModifierDatabaseSO levelModifierDatabase;

    [Header("Entry Prefab")]
    [SerializeField] CodexEntryUI entryPrefab;

    [Header("Sections")]
    [SerializeField] SectionView buffSection = new SectionView();
    [SerializeField] SectionView debuffSection = new SectionView();
    [SerializeField] SectionView levelModifierSection = new SectionView();

    [Header("Optional Paging Buttons")]
    [SerializeField] Button previousSectionButton;
    [SerializeField] Button nextSectionButton;

    [SerializeField] TMP_Text discoverySummaryText;

    [Header("Keyboard Navigation")]
    [SerializeField] KeyCode previousSectionKey = KeyCode.A;
    [SerializeField] KeyCode alternatePreviousSectionKey = KeyCode.LeftArrow;
    [SerializeField] KeyCode nextSectionKey = KeyCode.D;
    [SerializeField] KeyCode alternateNextSectionKey = KeyCode.RightArrow;
    [SerializeField] bool wrapSectionNavigation = true;
    [SerializeField] bool resetScrollPositionOnSectionChange = true;

    int _currentSectionIndex;
    bool _buttonsRegistered;

    void Awake()
    {
        RegisterButtonsIfNeeded();
        ShowSection(_currentSectionIndex, forceRefresh: true);
    }

    void OnEnable()
    {
        CodexProgressStore.ProgressChanged += OnCodexProgressChanged;
        RebuildAll();
        ShowSection(_currentSectionIndex, forceRefresh: true);
    }

    void OnDisable()
    {
        CodexProgressStore.ProgressChanged -= OnCodexProgressChanged;
    }

    void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (Input.GetKeyDown(previousSectionKey) || Input.GetKeyDown(alternatePreviousSectionKey))
        {
            ShowPreviousSection();
            return;
        }

        if (Input.GetKeyDown(nextSectionKey) || Input.GetKeyDown(alternateNextSectionKey))
            ShowNextSection();
    }

    public void RebuildAll()
    {
        if (!entryPrefab)
        {
            Debug.LogWarning("CodexPanelUI: Entry prefab is not assigned.", this);
            return;
        }

        RebuildRunModifierSection(buffSection, buffRunModifiers);
        RebuildRunModifierSection(debuffSection, debuffRunModifiers);
        RebuildLevelModifierSection(levelModifierSection);
        UpdateDiscoverySummary();
    }

    public void ShowBuffSection()
    {
        ShowSection((int)SectionIndex.Buffs);
    }

    public void ShowDebuffSection()
    {
        ShowSection((int)SectionIndex.Debuffs);
    }

    public void ShowLevelModifierSection()
    {
        ShowSection((int)SectionIndex.LevelModifiers);
    }

    public void ShowPreviousSection()
    {
        ShowSection(_currentSectionIndex - 1);
    }

    public void ShowNextSection()
    {
        ShowSection(_currentSectionIndex + 1);
    }

    void OnCodexProgressChanged()
    {
        if (!isActiveAndEnabled)
            return;

        RebuildAll();
    }

    void RegisterButtonsIfNeeded()
    {
        if (_buttonsRegistered)
            return;

        _buttonsRegistered = true;

        if (buffSection != null && buffSection.tabButton)
            buffSection.tabButton.onClick.AddListener(ShowBuffSection);

        if (debuffSection != null && debuffSection.tabButton)
            debuffSection.tabButton.onClick.AddListener(ShowDebuffSection);

        if (levelModifierSection != null && levelModifierSection.tabButton)
            levelModifierSection.tabButton.onClick.AddListener(ShowLevelModifierSection);

        if (previousSectionButton)
            previousSectionButton.onClick.AddListener(ShowPreviousSection);

        if (nextSectionButton)
            nextSectionButton.onClick.AddListener(ShowNextSection);
    }

    void ShowSection(int targetIndex, bool forceRefresh = false)
    {
        int clampedIndex = WrapOrClampIndex(targetIndex);
        bool changed = clampedIndex != _currentSectionIndex;
        _currentSectionIndex = clampedIndex;

        SetSectionState(buffSection, _currentSectionIndex == (int)SectionIndex.Buffs);
        SetSectionState(debuffSection, _currentSectionIndex == (int)SectionIndex.Debuffs);
        SetSectionState(levelModifierSection, _currentSectionIndex == (int)SectionIndex.LevelModifiers);

        UpdateDiscoverySummary();

        if (forceRefresh || (changed && resetScrollPositionOnSectionChange))
            ResetCurrentSectionScroll();
    }

    int WrapOrClampIndex(int targetIndex)
    {
        const int SectionCount = 3;

        if (wrapSectionNavigation)
        {
            if (targetIndex < 0)
                return SectionCount - 1;

            if (targetIndex >= SectionCount)
                return 0;
        }

        return Mathf.Clamp(targetIndex, 0, SectionCount - 1);
    }

    void SetSectionState(SectionView section, bool isActive)
    {
        if (section == null)
            return;

        if (section.root)
            section.root.SetActive(isActive);

        if (section.tabButton)
            section.tabButton.interactable = !isActive;
    }

    void ResetCurrentSectionScroll()
    {
        SectionView current = GetSectionByIndex(_currentSectionIndex);
        if (current == null || current.scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        current.scrollRect.verticalNormalizedPosition = 1f;
        current.scrollRect.horizontalNormalizedPosition = 0f;
    }

    SectionView GetSectionByIndex(int index)
    {
        return index switch
        {
            (int)SectionIndex.Buffs => buffSection,
            (int)SectionIndex.Debuffs => debuffSection,
            _ => levelModifierSection
        };
    }

    void RebuildRunModifierSection(SectionView section, RunModifierSO[] source)
    {
        if (section == null || !section.contentParent)
            return;

        ClearChildren(section.contentParent);

        if (source == null || source.Length == 0)
            return;

        List<RunModifierSO> sortedModifiers = BuildRunModifierDisplayList(source);
        for (int i = 0; i < sortedModifiers.Count; i++)
        {
            RunModifierSO modifier = sortedModifiers[i];

            var entry = Instantiate(entryPrefab, section.contentParent);
            entry.Bind(modifier, CodexProgressStore.IsUnlocked(modifier), section == buffSection 
                       ? CodexEntryUI.EntryCategory.RunBuff
                       : CodexEntryUI.EntryCategory.RunDebuff);
        }
    }

    void RebuildLevelModifierSection(SectionView section)
    {
        if (section == null || !section.contentParent)
            return;

        ClearChildren(section.contentParent);

        if (!levelModifierDatabase)
            return;

        List<LevelModifierSO> modifiers = BuildLevelModifierDisplayList(levelModifierDatabase.BuildPool());

        for (int i = 0; i < modifiers.Count; i++)
        {
            LevelModifierSO modifier = modifiers[i];

            var entry = Instantiate(entryPrefab, section.contentParent);
            entry.Bind(modifier, CodexProgressStore.IsUnlocked(modifier));
        }
    }

    List<RunModifierSO> BuildRunModifierDisplayList(RunModifierSO[] source)
    {
        var discovered = new List<RunModifierSO>();
        var undiscovered = new List<RunModifierSO>();

        if (source == null || source.Length == 0)
            return discovered;

        var seen = new HashSet<RunModifierSO>();
        for (int i = 0; i < source.Length; i++)
        {
            RunModifierSO modifier = source[i];
            if (!modifier || !seen.Add(modifier))
                continue;

            if (CodexProgressStore.IsUnlocked(modifier))
                discovered.Add(modifier);
            else
                undiscovered.Add(modifier);
        }

        discovered.AddRange(undiscovered);
        return discovered;
    }

    List<LevelModifierSO> BuildLevelModifierDisplayList(List<LevelModifierSO> source)
    {
        var discovered = new List<LevelModifierSO>();
        var undiscovered = new List<LevelModifierSO>();

        if (source == null || source.Count == 0)
            return discovered;

        var seen = new HashSet<LevelModifierSO>();
        for (int i = 0; i < source.Count; i++)
        {
            LevelModifierSO modifier = source[i];
            if (!modifier || !seen.Add(modifier))
                continue;

            if (CodexProgressStore.IsUnlocked(modifier))
                discovered.Add(modifier);
            else
                undiscovered.Add(modifier);
        }

        discovered.AddRange(undiscovered);
        return discovered;
    }

    void UpdateDiscoverySummary()
    {
        if (!discoverySummaryText)
            return;

        int sectionUnlocked;
        int sectionTotal;
        string sectionLabel = GetCurrentSectionLabelAndCounts(out sectionUnlocked, out sectionTotal);

        int totalUnlocked = GetTotalUnlockedCount();
        int totalEntries = GetTotalEntryCount();
        int totalPercent = totalEntries > 0
            ? Mathf.RoundToInt((float)totalUnlocked / totalEntries * 100f)
            : 0;

        discoverySummaryText.text =
            $"{sectionUnlocked} of {sectionTotal} {sectionLabel} discovered. Total codex unlocked {totalPercent}%";
    }

    string GetCurrentSectionLabelAndCounts(out int unlocked, out int total)
    {
        switch ((SectionIndex)_currentSectionIndex)
        {
            case SectionIndex.Buffs:
                unlocked = CountUnlocked(buffRunModifiers);
                total = CountUnique(buffRunModifiers);
                return "Buffs";

            case SectionIndex.Debuffs:
                unlocked = CountUnlocked(debuffRunModifiers);
                total = CountUnique(debuffRunModifiers);
                return "Debuffs";

            default:
                unlocked = CountUnlockedLevelModifiers();
                total = CountTotalLevelModifiers();
                return "Level Modifiers";
        }
    }

    int GetTotalUnlockedCount()
    {
        return CountUnlocked(buffRunModifiers)
             + CountUnlocked(debuffRunModifiers)
             + CountUnlockedLevelModifiers();
    }

    int GetTotalEntryCount()
    {
        return CountUnique(buffRunModifiers)
             + CountUnique(debuffRunModifiers)
             + CountTotalLevelModifiers();
    }

    int CountUnlocked(RunModifierSO[] source)
    {
        if (source == null || source.Length == 0)
            return 0;

        var seen = new HashSet<RunModifierSO>();
        int count = 0;

        for (int i = 0; i < source.Length; i++)
        {
            RunModifierSO modifier = source[i];
            if (!modifier || !seen.Add(modifier))
                continue;

            if (CodexProgressStore.IsUnlocked(modifier))
                count++;
        }

        return count;
    }

    int CountUnique(RunModifierSO[] source)
    {
        if (source == null || source.Length == 0)
            return 0;

        var seen = new HashSet<RunModifierSO>();

        for (int i = 0; i < source.Length; i++)
        {
            RunModifierSO modifier = source[i];
            if (modifier)
                seen.Add(modifier);
        }

        return seen.Count;
    }

    int CountUnlockedLevelModifiers()
    {
        if (!levelModifierDatabase)
            return 0;

        List<LevelModifierSO> modifiers = levelModifierDatabase.BuildPool();
        var seen = new HashSet<LevelModifierSO>();
        int count = 0;

        for (int i = 0; i < modifiers.Count; i++)
        {
            LevelModifierSO modifier = modifiers[i];
            if (!modifier || !seen.Add(modifier))
                continue;

            if (CodexProgressStore.IsUnlocked(modifier))
                count++;
        }

        return count;
    }

    int CountTotalLevelModifiers()
    {
        if (!levelModifierDatabase)
            return 0;

        List<LevelModifierSO> modifiers = levelModifierDatabase.BuildPool();
        var seen = new HashSet<LevelModifierSO>();

        for (int i = 0; i < modifiers.Count; i++)
        {
            LevelModifierSO modifier = modifiers[i];
            if (modifier)
                seen.Add(modifier);
        }

        return seen.Count;
    }

    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
