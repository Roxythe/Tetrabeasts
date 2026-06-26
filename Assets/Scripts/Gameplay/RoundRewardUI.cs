using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_ANDROID && UNITY_EDITOR
using Unity.Android.Gradle.Manifest;
#endif


public class RoundRewardUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject rootPanel;
    public GameObject buffPanel;
    public GameObject debuffPanel;

    [Header("Rarity Balancing")]
    [Tooltip("Lower values = more common rewards during normal levels")]
    [Range(0.25f, 1.5f)]
    public float normalLevelRarityMultiplier = 0.65f;

    [Tooltip("Higher values = better rewards after boss fights")]
    [Range(0.5f, 2.5f)]
    public float bossLevelRarityMultiplier = 1.35f;

    [Header("Buff UI")]
    public Transform buffContainer;
    public Button confirmBuffButton;
    public Button buffRerollButton;
    public TMP_Text buffRerollButtonText;

    [Header("Debuff UI")]
    public Transform debuffContainer;
    public Button confirmDebuffButton;
    public Button debuffRerollButton;
    public TMP_Text debuffRerollButtonText;

    [Header("Prefabs")]
    public RunModOptionButton buffOptionButtonPrefab;
    public RunModOptionButton debuffOptionButtonPrefab;
    public TMP_Text currnecyGained;
    public TMP_Text reinforcementsGained;

    [Header("Hooked SFX")]
    public GameplayUI_SFXHook _sfxHook;

    [Header("Round Win Blink")]
    public GameObject blinkImage1;          // Starts on
    public GameObject blinkImage2;          // Starts off
    public float blinkIntervalSeconds = 0.25f;

    [Header("Tutorial")]
    [SerializeField] TutorialSequenceController _buffPanelTutorial;
    [SerializeField] TutorialSequenceController _debuffPanelTutorial;

    Coroutine _blinkRoutine;

    RunModifierSO _selectedBuff;
    RunModifierSO _selectedDebuff;
    ScopedMenuNavigator _navigator;
    RunModifierSO[] _buffPool;
    RunModifierSO[] _debuffPool;
    float _currentLuck;
    float _currentMisfortune;
    bool _currentWasBossLevel;
    bool _currentUseLegendaryBossDebuffs;
    Func<int> _getAvailableRerolls;
    Func<bool> _trySpendReroll;
    CanvasGroup _rootCanvasGroup;
    bool _tutorialInteractionLocked;
    bool _completionLocked;
    Action<RunModifierSO, RunModifierSO> _onComplete;


    void OnEnable()
    {
        _sfxHook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!_sfxHook) _sfxHook = FindFirstObjectByType<GameplayUI_SFXHook>();
    }

    public void Show(RunModifierSO[] buffPool, RunModifierSO[] debuffPool, Action<RunModifierSO, RunModifierSO> onComplete,
                 int currencyGained, int reinforcementsReceived, Func<int> getAvailableRerolls = null,
                 Func<bool> trySpendReroll = null)
    {
        var gc = FindFirstObjectByType<GameController>();
        float luck = gc ? gc.luck : RunModsStore.Luck;
        float misfortune = gc ? gc.CurrentMisfortune : RunModsStore.Misfortune;

        bool wasBossLevel = gc && gc.LastLevelWasBoss;
        bool useLegendaryBossDebuffs = wasBossLevel && (!gc || gc.BossLegendaryDebuffRewardsUnlocked);

        _onComplete = onComplete;
        _buffPool = buffPool;
        _debuffPool = debuffPool;
        _currentLuck = luck;
        _currentMisfortune = misfortune;
        _currentWasBossLevel = wasBossLevel;
        _currentUseLegendaryBossDebuffs = useLegendaryBossDebuffs;
        _getAvailableRerolls = getAvailableRerolls;
        _trySpendReroll = trySpendReroll;
        _completionLocked = false;
        _tutorialInteractionLocked = false;
        EnsureRerollReferences();
        ResolvePanelTutorials();
        StopPanelTutorials(markComplete: false);
        SetRootPanelInteractable(true);

        ClearRewardOptions(buffContainer);
        ClearRewardOptions(debuffContainer);

        if (rootPanel)
            rootPanel.transform.SetAsLastSibling();

        UIPanelTransition.Show(rootPanel);
        UIPanelTransition.Show(buffPanel, true);
        UIPanelTransition.Hide(debuffPanel, true);
        StartBlink();

        confirmBuffButton.interactable = false;
        confirmDebuffButton.interactable = false;

        _selectedBuff = null;
        _selectedDebuff = null;

        // Ensure panel buttons also get UIButtonSFX
        _sfxHook?.HookButton(confirmBuffButton);
        _sfxHook?.HookButton(confirmDebuffButton);
        _sfxHook?.HookButton(buffRerollButton);
        _sfxHook?.HookButton(debuffRerollButton);

        if (currnecyGained)
            currnecyGained.text = $"+{currencyGained}";

        if (reinforcementsGained)
        {
            reinforcementsGained.text = reinforcementsReceived > 0
                ? $"+{reinforcementsReceived} {TetrabeastsLocalization.LocalizeText("Reinforcements")}"
                : $"+0 {TetrabeastsLocalization.LocalizeText("Reinforcements")}";
        }

        WireRerollButtons();
        RefreshRerollButtons();

        Populate(buffContainer, PickBuffRewards(), isBuff: true);
        SetNavigationRoot(buffPanel, selectFirst: true);
        StartPanelTutorial(_buffPanelTutorial);

        confirmBuffButton.onClick.RemoveAllListeners();
        confirmBuffButton.onClick.AddListener(() =>
        {
            StopPanelTutorial(_buffPanelTutorial, markComplete: false);
            Populate(debuffContainer, PickDebuffRewards(), isBuff: false);

            UIPanelTransition.Hide(buffPanel, true);
            UIPanelTransition.Show(debuffPanel, true);
            SetNavigationRoot(debuffPanel, selectFirst: true);
            RefreshRerollButtons();
            StartPanelTutorial(_debuffPanelTutorial);
        });

        confirmDebuffButton.onClick.RemoveAllListeners();
        confirmDebuffButton.onClick.AddListener(() =>
        {
            // Prevent extra clicks
            confirmDebuffButton.interactable = false;
            confirmBuffButton.interactable = false;

            // Lock the whole panel while next level loads
            _completionLocked = true;
            SetRootPanelInteractable(false);
            StopPanelTutorials(markComplete: false);

            _onComplete?.Invoke(_selectedBuff, _selectedDebuff);
        });
    }

    void Update()
    {
        RefreshTutorialInteractionLock();
    }

    List<RunModifierSO> PickBuffRewards()
    {
        return Pick3UniqueWeighted(_buffPool, _currentLuck, _currentWasBossLevel);
    }

    List<RunModifierSO> PickDebuffRewards()
    {
        var excludedLegendaryDebuffs = BuildChosenLegendaryDebuffExclusions();
        return _currentUseLegendaryBossDebuffs
            ? Pick3UniqueLegendary(_debuffPool, excludedLegendaryDebuffs)
            : Pick3UniqueWeighted(_debuffPool, _currentMisfortune, _currentWasBossLevel, excludedLegendaryDebuffs);
    }

    void WireRerollButtons()
    {
        if (buffRerollButton)
        {
            buffRerollButton.onClick.RemoveListener(OnBuffRerollClicked);
            buffRerollButton.onClick.AddListener(OnBuffRerollClicked);
        }

        if (debuffRerollButton)
        {
            debuffRerollButton.onClick.RemoveListener(OnDebuffRerollClicked);
            debuffRerollButton.onClick.AddListener(OnDebuffRerollClicked);
        }
    }

    void OnBuffRerollClicked()
    {
        if (!TrySpendReroll())
            return;

        _selectedBuff = null;
        if (confirmBuffButton)
            confirmBuffButton.interactable = false;

        Populate(buffContainer, PickBuffRewards(), isBuff: true);
        RefreshRerollButtons();
        SetNavigationRoot(buffPanel, selectFirst: true);
    }

    void OnDebuffRerollClicked()
    {
        if (!TrySpendReroll())
            return;

        _selectedDebuff = null;
        if (confirmDebuffButton)
            confirmDebuffButton.interactable = false;

        Populate(debuffContainer, PickDebuffRewards(), isBuff: false);
        RefreshRerollButtons();
        SetNavigationRoot(debuffPanel, selectFirst: true);
    }

    bool TrySpendReroll()
    {
        if (_trySpendReroll == null || GetAvailableRerolls() <= 0)
            return false;

        return _trySpendReroll();
    }

    int GetAvailableRerolls()
    {
        return Mathf.Max(0, _getAvailableRerolls?.Invoke() ?? 0);
    }

    void RefreshRerollButtons()
    {
        int available = GetAvailableRerolls();
        string label = TetrabeastsLocalization.LocalizeFormat("Rerolls ({0})", available);

        if (buffRerollButtonText)
            buffRerollButtonText.text = label;

        if (debuffRerollButtonText)
            debuffRerollButtonText.text = label;

        bool canReroll = available > 0;
        if (buffRerollButton)
            buffRerollButton.interactable = canReroll;

        if (debuffRerollButton)
            debuffRerollButton.interactable = canReroll;
    }

    void EnsureRerollReferences()
    {
        if (!buffRerollButton && buffPanel)
            buffRerollButton = FindButton(buffPanel.transform, "Reroll_Button");

        if (!debuffRerollButton && debuffPanel)
            debuffRerollButton = FindButton(debuffPanel.transform, "Reroll_Button");

        if (!buffRerollButtonText && buffRerollButton)
            buffRerollButtonText = buffRerollButton.GetComponentInChildren<TMP_Text>(true);

        if (!debuffRerollButtonText && debuffRerollButton)
            debuffRerollButtonText = debuffRerollButton.GetComponentInChildren<TMP_Text>(true);
    }

    Button FindButton(Transform root, string exactName)
    {
        if (!root)
            return null;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button && string.Equals(button.name, exactName, StringComparison.Ordinal))
                return button;
        }

        return null;
    }

    void ResolvePanelTutorials()
    {
        if (!_buffPanelTutorial && buffPanel)
            _buffPanelTutorial = buffPanel.GetComponent<TutorialSequenceController>();

        if (!_debuffPanelTutorial && debuffPanel)
            _debuffPanelTutorial = debuffPanel.GetComponent<TutorialSequenceController>();
    }

    static void StartPanelTutorial(TutorialSequenceController sequence)
    {
        if (sequence && sequence.gameObject.activeInHierarchy)
            sequence.StartSequenceIfNeeded();
    }

    void StopPanelTutorials(bool markComplete)
    {
        ResolvePanelTutorials();

        StopPanelTutorial(_buffPanelTutorial, markComplete);
        StopPanelTutorial(_debuffPanelTutorial, markComplete);
    }

    static void StopPanelTutorial(TutorialSequenceController sequence, bool markComplete)
    {
        if (sequence && sequence.IsSequenceRunning)
            sequence.StopSequence(markComplete);
    }

    void Populate(Transform container, List<RunModifierSO> picks, bool isBuff)
    {
        ClearRewardOptions(container);

        if (!container || picks == null)
            return;

        foreach (var mod in picks)
        {
            var prefab = isBuff ? buffOptionButtonPrefab : debuffOptionButtonPrefab;
            if (!prefab)
            {
                Debug.LogError($"RoundRewardUI: Missing {(isBuff ? "buff" : "debuff")} option button prefab.");
                continue;
            }

            var btn = Instantiate(prefab, container);

            _sfxHook?.HookButton(btn.GetComponent<Button>()); // Hook SFX onto the instantiated button
            btn.Bind(mod, selected =>
            {
                // Clear all highlights
                for (int i = 0; i < container.childCount; i++)
                    container.GetChild(i).GetComponent<RunModOptionButton>()?.SetSelected(false);

                btn.SetSelected(true);

                if (isBuff)
                {
                    _selectedBuff = mod;
                    confirmBuffButton.interactable = true;
                }
                else
                {
                    _selectedDebuff = mod;
                    confirmDebuffButton.interactable = true;
                }
            });
        }
    }

    static void ClearRewardOptions(Transform container)
    {
        if (!container)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (!child)
                continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    void SetNavigationRoot(GameObject panel, bool selectFirst = false)
    {
        if (!rootPanel || !panel)
            return;

        if (!_navigator)
            _navigator = ScopedMenuNavigator.Attach(rootPanel, panel);

        if (_navigator)
        {
            _navigator.enabled = true;
            _navigator.SetNavigationRoot(panel, selectFirst);
        }
    }

    void DisableNavigation()
    {
        if (_navigator)
            _navigator.enabled = false;
    }

    void RefreshTutorialInteractionLock()
    {
        if (!rootPanel || !rootPanel.activeInHierarchy)
            return;

        bool shouldLock = TriggeredTutorialPopupController.IsAnyPopupBlockingUi ||
            TutorialSequenceController.IsAnySequenceBlockingUi;
        if (shouldLock == _tutorialInteractionLocked)
            return;

        _tutorialInteractionLocked = shouldLock;
        SetRootPanelInteractable(!_tutorialInteractionLocked && !_completionLocked);

        if (_tutorialInteractionLocked)
        {
            ClearRewardOptionTransientHighlights();
            ClearRewardPanelSelection();
            DisableNavigation();
            return;
        }

        var activePanel = GetActivePanel();
        if (!_completionLocked && activePanel)
            SetNavigationRoot(activePanel, selectFirst: true);
    }

    void SetRootPanelInteractable(bool interactable)
    {
        if (!rootPanel)
            return;

        if (!_rootCanvasGroup)
            _rootCanvasGroup = rootPanel.GetComponent<CanvasGroup>();

        if (!_rootCanvasGroup)
            _rootCanvasGroup = rootPanel.AddComponent<CanvasGroup>();

        _rootCanvasGroup.interactable = interactable;
        _rootCanvasGroup.blocksRaycasts = true;
    }

    void ClearRewardOptionTransientHighlights()
    {
        ClearRewardOptionTransientHighlights(buffContainer);
        ClearRewardOptionTransientHighlights(debuffContainer);
    }

    static void ClearRewardOptionTransientHighlights(Transform container)
    {
        if (!container)
            return;

        var buttons = container.GetComponentsInChildren<RunModOptionButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i])
                buttons[i].ClearTransientHighlight();
        }

        var targetVisuals = container.GetComponentsInChildren<UIButtonTargetVisual>(true);
        for (int i = 0; i < targetVisuals.Length; i++)
        {
            if (targetVisuals[i])
                targetVisuals[i].ClearTransientTargeting();
        }
    }

    void ClearRewardPanelSelection()
    {
        if (!rootPanel || !EventSystem.current)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected && selected.transform.IsChildOf(rootPanel.transform))
            EventSystem.current.SetSelectedGameObject(null);
    }

    GameObject GetActivePanel()
    {
        if (debuffPanel && UIPanelTransition.IsVisible(debuffPanel))
            return debuffPanel;

        if (buffPanel && UIPanelTransition.IsVisible(buffPanel))
            return buffPanel;

        return null;
    }

    float[] GetRarityProbsFromLuck(float luck, bool wasBossLevel)
    {
        float L = Mathf.Clamp(luck, 0f, 200f);

        // Index order: [Common, Uncommon, Rare, Epic, Legendary]
        // Normal Levels are heavily Common-weighted 
        float[] n0 = { 0.80f, 0.16f, 0.03f, 0.01f, 0.00f };
        float[] n25 = { 0.80f, 0.16f, 0.03f, 0.01f, 0.00f };
        float[] n50 = { 0.60f, 0.25f, 0.11f, 0.035f, 0.005f };
        float[] n75 = { 0.45f, 0.28f, 0.18f, 0.075f, 0.015f };
        float[] n100 = { 0.30f, 0.28f, 0.24f, 0.14f, 0.04f };
        float[] n150 = { 0.18f, 0.22f, 0.25f, 0.25f, 0.10f };
        float[] n200 = { 0.12f, 0.18f, 0.24f, 0.28f, 0.18f };

        // Boss Levels have noticeably higher rarity odds
        float[] b0 = { 0.60f, 0.26f, 0.10f, 0.03f, 0.01f };
        float[] b25 = { 0.60f, 0.26f, 0.10f, 0.03f, 0.01f };
        float[] b50 = { 0.40f, 0.30f, 0.20f, 0.08f, 0.02f };
        float[] b75 = { 0.25f, 0.27f, 0.26f, 0.16f, 0.06f };
        float[] b100 = { 0.16f, 0.20f, 0.25f, 0.24f, 0.15f };
        float[] b150 = { 0.10f, 0.15f, 0.22f, 0.28f, 0.25f };
        float[] b200 = { 0.07f, 0.12f, 0.18f, 0.28f, 0.35f };

        float[] a, b;
        float t;

        if (wasBossLevel)
        {
            if (L <= 25f) { a = b0; b = b25; t = Mathf.InverseLerp(0f, 25f, L); }
            else if (L <= 50f) { a = b25; b = b50; t = Mathf.InverseLerp(25f, 50f, L); }
            else if (L <= 75f) { a = b50; b = b75; t = Mathf.InverseLerp(50f, 75f, L); }
            else if (L <= 100f) { a = b75; b = b100; t = Mathf.InverseLerp(75f, 100f, L); }
            else if (L <= 150f) { a = b100; b = b150; t = Mathf.InverseLerp(100f, 150f, L); }
            else { a = b150; b = b200; t = Mathf.InverseLerp(150f, 200f, L); }
        }
        else
        {
            if (L <= 25f) { a = n0; b = n25; t = Mathf.InverseLerp(0f, 25f, L); }
            else if (L <= 50f) { a = n25; b = n50; t = Mathf.InverseLerp(25f, 50f, L); }
            else if (L <= 75f) { a = n50; b = n75; t = Mathf.InverseLerp(50f, 75f, L); }
            else if (L <= 100f) { a = n75; b = n100; t = Mathf.InverseLerp(75f, 100f, L); }
            else if (L <= 150f) { a = n100; b = n150; t = Mathf.InverseLerp(100f, 150f, L); }
            else { a = n150; b = n200; t = Mathf.InverseLerp(150f, 200f, L); }
        }

        t = t * t * (3f - 2f * t); // Smoothstep for smoother transitions between curves

        float[] p = new float[5];
        for (int i = 0; i < 5; i++)
            p[i] = Mathf.Lerp(a[i], b[i], t);

        // Extra global reduction for higher rarities on normal levels
        if (!wasBossLevel)
        {
            p[1] *= 0.85f; // Uncommon
            p[2] *= 0.60f; // Rare
            p[3] *= 0.40f; // Epic
            p[4] *= 0.30f; // Legendary
        }

        NormalizeInPlace(p);
        return p;
    }

    void NormalizeInPlace(float[] p)
    {
        float sum = 0f;
        for (int i = 0; i < p.Length; i++) sum += Mathf.Max(0f, p[i]);
        if (sum <= 0f) { p[0] = 1f; for (int i = 1; i < p.Length; i++) p[i] = 0f; return; }
        for (int i = 0; i < p.Length; i++) p[i] = Mathf.Max(0f, p[i]) / sum;
    }

    RunModRarity GetRarity(RunModifierSO so)
    {
        return (so is RunModifier rm) ? rm.rarity : RunModRarity.Common;
    }

    int RarityIndex(RunModRarity r)
    {
        return (int)r; // Assumes enum order matches index
    }

    string GetGroupKey(RunModifierSO so)
    {
        if (so is RunModifier rm)
            return $"{rm.stat}:{rm.op}";

        return string.IsNullOrEmpty(so.displayName) ? so.name : so.displayName;
    }

    RunModifierSO PickByRarityCurve(RunModifierSO[] pool, float luck, bool wasBossLevel, HashSet<RunModifierSO> excludeAssets,
                                HashSet<string> excludeGroups)
    {
        if (pool == null || pool.Length == 0) return null;

        // Build buckets by rarity, excluding already-used
        var buckets = new List<RunModifierSO>[5];
        for (int i = 0; i < 5; i++) buckets[i] = new List<RunModifierSO>();

        for (int i = 0; i < pool.Length; i++)
        {
            var so = pool[i];
            if (!so) continue;
            if (excludeAssets != null && excludeAssets.Contains(so)) continue;

            // Prevent same effect-group showing multiple rarities in the same round
            if (excludeGroups != null)
            {
                string key = GetGroupKey(so);
                if (excludeGroups.Contains(key)) continue;
            }

            int idx = RarityIndex(GetRarity(so));
            idx = Mathf.Clamp(idx, 0, 4);
            buckets[idx].Add(so);
        }

        // If everything is excluded/empty, bail
        int totalCount = 0;
        for (int i = 0; i < 5; i++) totalCount += buckets[i].Count;
        if (totalCount == 0) return null;

        // Get base rarity probabilities from luck and level type
        float[] probs = GetRarityProbsFromLuck(luck, wasBossLevel);

        float rarityMultiplier = wasBossLevel ? bossLevelRarityMultiplier : normalLevelRarityMultiplier;
        rarityMultiplier = Mathf.Max(0.01f, rarityMultiplier);

        // Reduce or increase higher rarity appearance
        probs[1] *= rarityMultiplier; // Uncommon
        probs[2] *= rarityMultiplier; // Rare
        probs[3] *= rarityMultiplier; // Epic
        probs[4] *= rarityMultiplier; // Legendary

        NormalizeInPlace(probs);

        // Zero out rarities that have no available mods, then renormalize
        for (int i = 0; i < 5; i++)
            if (buckets[i].Count == 0) probs[i] = 0f;

        NormalizeInPlace(probs);

        // Roll rarity
        float roll = UnityEngine.Random.value;
        int chosenR = 0;
        for (int i = 0; i < 5; i++)
        {
            roll -= probs[i];
            if (roll <= 0f) { chosenR = i; break; }
        }

        // If rolled rarity has no available mods fallback to closest available rarity
        if (buckets[chosenR].Count == 0)
        {
            for (int i = 4; i >= 0; i--)
            {
                if (buckets[i].Count > 0)
                {
                    chosenR = i;
                    break;
                }
            }
        }

        // Pick random mod within that rarity bucket
        var list = buckets[chosenR];
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    HashSet<RunModifierSO> BuildChosenLegendaryDebuffExclusions()
    {
        var exclusions = new HashSet<RunModifierSO>();

        for (int i = 0; i < RunModsStore.Debuffs.Count; i++)
        {
            var debuff = RunModsStore.Debuffs[i];
            if (debuff && GetRarity(debuff) == RunModRarity.Legendary)
                exclusions.Add(debuff);
        }

        return exclusions;
    }

    RunModifierSO PickByExactRarity(RunModifierSO[] pool, RunModRarity rarity, HashSet<RunModifierSO> excludeAssets,
                                HashSet<string> excludeGroups)
    {
        if (pool == null || pool.Length == 0) return null;

        var candidates = new List<RunModifierSO>();
        for (int i = 0; i < pool.Length; i++)
        {
            var so = pool[i];
            if (!so) continue;
            if (GetRarity(so) != rarity) continue;
            if (excludeAssets != null && excludeAssets.Contains(so)) continue;

            if (excludeGroups != null)
            {
                string key = GetGroupKey(so);
                if (excludeGroups.Contains(key)) continue;
            }

            candidates.Add(so);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    List<RunModifierSO> Pick3UniqueLegendary(RunModifierSO[] pool, HashSet<RunModifierSO> excludeAssets = null)
    {
        var results = new List<RunModifierSO>(3);
        var usedAssets = new HashSet<RunModifierSO>();
        var usedGroups = new HashSet<string>();

        if (excludeAssets != null)
        {
            foreach (var excluded in excludeAssets)
                if (excluded) usedAssets.Add(excluded);
        }

        int safety = 100;
        while (results.Count < 3 && safety-- > 0)
        {
            var pick = PickByExactRarity(pool, RunModRarity.Legendary, usedAssets, usedGroups);
            if (!pick) break;

            results.Add(pick);
            usedAssets.Add(pick);
            usedGroups.Add(GetGroupKey(pick));
        }

        if (results.Count < 3)
            Debug.LogWarning($"RoundRewardUI: Only found {results.Count} available Legendary debuff options.");

        return results;
    }

    List<RunModifierSO> Pick3UniqueWeighted(RunModifierSO[] pool, float skew, bool wasBossLevel, HashSet<RunModifierSO> excludeAssets = null)
    {
        var results = new List<RunModifierSO>(3);
        var usedAssets = new HashSet<RunModifierSO>();
        var usedGroups = new HashSet<string>();

        if (excludeAssets != null)
        {
            foreach (var excluded in excludeAssets)
                if (excluded) usedAssets.Add(excluded);
        }

        int safety = 100;
        while (results.Count < 3 && safety-- > 0)
        {
            var pick = PickByRarityCurve(pool, skew, wasBossLevel, usedAssets, usedGroups);
            if (!pick) break;

            results.Add(pick);
            usedAssets.Add(pick);
            usedGroups.Add(GetGroupKey(pick));
        }

        return results;
    }

    public void Hide()
    {
        if (!rootPanel) return;

        StopPanelTutorials(markComplete: false);

        // Re-enable for next time
        var cg = rootPanel.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        _completionLocked = false;
        _tutorialInteractionLocked = false;
        StopBlink();
        DisableNavigation();
        UIPanelTransition.Hide(rootPanel);
    }

    // ========= Round Win Blink Logic =========

    void StartBlink()
    {
        StopBlink();

        if (!blinkImage1 || !blinkImage2)
            return;

        // Initial state: image1 ON, image2 OFF
        blinkImage1.SetActive(true);
        blinkImage2.SetActive(false);

        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    void StopBlink()
    {
        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }
    }

    System.Collections.IEnumerator BlinkRoutine()
    {
        // Keep blinking while the panel is active
        while (rootPanel && rootPanel.activeInHierarchy)
        {
            // swap
            if (blinkImage1) blinkImage1.SetActive(!blinkImage1.activeSelf);
            if (blinkImage2) blinkImage2.SetActive(!blinkImage2.activeSelf);

            yield return new WaitForSecondsRealtime(blinkIntervalSeconds);
        }

        _blinkRoutine = null;
    }

}
