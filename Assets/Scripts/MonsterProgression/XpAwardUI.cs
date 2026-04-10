using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XpAwardUI : MonoBehaviour
{
    [Serializable]
    public struct RoundXpBreakdown
    {
        public int gameLevelNumber;
        public int baseXp;

        public float levelClearTime;
        public int clearTimeBonus;

        public int startReserve;
        public int endReserve;
        public int reserveBonus;

        public int comboBonus;
        public int obstacleBonus;

        public int difficultyStars;
        public int difficultyBonus;
        public int totalBeforeDifficulty;
        public int totalBeforeReduction;
    }

    [Header("Root")]
    public GameObject root;

    [Header("Round Win - XP Breakdown")]
    public GameObject roundBreakdownPanel;
    public TMP_Text breakdownTitleText;
    public TMP_Text breakdownTitleShadowText;
    public TMP_Text breakdownLinesText;
    public TMP_Text breakdownLinesShadowText;
    public TMP_Text breakdownTotalText;
    public TMP_Text breakdownTotalShadowText;
    public Button breakdownContinueButton;

    [Header("Round Win - XP Distribution")]
    public GameObject roundDistributePanel;
    public Transform roundRosterContainer;
    public XpMonsterRowUI monsterRowPrefab;
    public Button roundDistributeContinueButton;

    [Header("Run End - Row Prefabs")]
    public XpMonsterRowUI runDrainMonsterRowPrefab;
    public XpMonsterRowUI runCommitMonsterRowPrefab;

    [Header("Run End - Drain Run Instance")]
    public GameObject runDrainPanel;
    public Transform runDrainContainer;
    public Button runDrainContinueButton;
    public float runDrainToCommitPauseSeconds = 0.5f;

    [Header("Run End - Permanent Distribution")]
    public GameObject runCommitPanel;
    public Transform runCommitContainer;
    public Button runCommitContinueButton;

    [Header("XP VFX / SFX")]
    public RectTransform vfxRoot;
    public Image xpOrbPrefab;
    public float orbArcHeight = 60f;

    public float orbTravelStartSeconds = 0.7f;
    public float orbTravelEndSeconds = 0.18f;

    public float orbSpawnIntervalStartSeconds = 0.06f;
    public float orbSpawnIntervalEndSeconds = 0.008f;

    [Min(0.1f)] public float orbAccelPower = 1.7f;

    public int maxOrbsPerFrame = 3;

    [Header("Duration Caps")]
    public float breakdownMaxSeconds = 2.5f;
    public float orbTransferMaxSeconds = 4.0f;

    [Header("Orb Animation Start Delays")]
    public float orbGainStartDelaySeconds = 0.5f;
    public float orbDrainStartDelaySeconds = 0.5f;

    [Header("Orb SFX Throttle (Ramps With Accel)")]
    public int maxOrbSfxPerSecondStart = 10;
    public int maxOrbSfxPerSecondEnd = 40;

    public float minOrbSfxIntervalStartSeconds = 0.05f;
    public float minOrbSfxIntervalEndSeconds = 0.008f;

    [Header("Orb SFX Pitch Jitter")]
    [Range(0f, 0.25f)] public float orbSfxPitchJitter = 0.08f;

    [Header("Breakdown Count-Up")]
    public float breakdownStartDelaySeconds = 1.0f;
    public float breakdownTickStartSeconds = 0.12f;
    public float breakdownTickEndSeconds = 0.01f;
    [Min(0.1f)] public float breakdownAccelPower = 1.6f;
    public float breakdownTickJitterSeconds = 0.05f;

    [Header("Breakdown Skip Cushion")]
    public float breakdownSkipInputDelaySeconds = 0.5f;

    const float XpPerLevel = 100f;
    float _permanentXpConversion = 0.10f;

    readonly List<XpMonsterRowUI> _rows = new();
    readonly List<GameObject> _activeOrbGos = new();
    Coroutine _breakdownCountCR;

    bool _breakdownAnimating;
    bool _orbAnimating;
    bool _skipRequested;
    bool _hideOnRunEndFinalContinue = true;
    float _breakdownSkipAllowedAt = 0f;

    int _levelUpSfxFrame = -1;

    float _lastGainOrbSfxTime = -999f;
    float _lastDrainOrbSfxTime = -999f;
    int _gainOrbSfxCountThisSecond = 0;
    int _drainOrbSfxCountThisSecond = 0;
    float _gainOrbSfxSecondStart = 0f;
    float _drainOrbSfxSecondStart = 0f;

    void Awake()
    {
        HideAll();
    }

    void OnDisable()
    {
        HardStopAndClearAllVfx();
    }

    void Update()
    {
        if (!root || !root.activeSelf) return;
        if (!_breakdownAnimating && !_orbAnimating) return;

        if (!(Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            return;

        if (_breakdownAnimating && Time.unscaledTime < _breakdownSkipAllowedAt)
            return;

        _skipRequested = true;
    }

    public void HideAll()
    {
        HardStopAndClearAllVfx();

        if (root) root.SetActive(false);

        if (roundBreakdownPanel) roundBreakdownPanel.SetActive(false);
        if (roundDistributePanel) roundDistributePanel.SetActive(false);

        if (runDrainPanel) runDrainPanel.SetActive(false);
        if (runCommitPanel) runCommitPanel.SetActive(false);
    }

    void HardStopAndClearAllVfx()
    {
        StopAllCoroutines();
        _breakdownCountCR = null;

        _skipRequested = false;
        _breakdownAnimating = false;
        _orbAnimating = false;

        ClearActiveOrbs();
    }

    public void ShowRoundWin(RoundXpBreakdown breakdown, List<MonsterData> roster, Dictionary<string, float> perMonsterAwardXp,
                             Action onContinueToRewards)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!root) return;

        var sfxHook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!sfxHook) sfxHook = FindFirstObjectByType<GameplayUI_SFXHook>();
        if (sfxHook)
        {
            sfxHook.HookButton(breakdownContinueButton);
            sfxHook.HookButton(roundDistributeContinueButton);
            sfxHook.HookButton(runDrainContinueButton);
            sfxHook.HookButton(runCommitContinueButton);
        }

        HardStopAndClearAllVfx();

        root.SetActive(true);
        ShowBreakdown(breakdown);

        if (breakdownContinueButton)
        {
            breakdownContinueButton.onClick.RemoveAllListeners();
            breakdownContinueButton.onClick.AddListener(() =>
            {
                HardStopAndClearAllVfx();
                StartCoroutine(CoRoundDistribute(roster, perMonsterAwardXp, onContinueToRewards));
            });
        }
    }

    public void ShowRunEndCommit(List<MonsterData> roster, float keepFraction, Action onContinueToHighScore, bool hideOnFinalContinue = true)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!root) return;

        var sfxHook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!sfxHook) sfxHook = FindFirstObjectByType<GameplayUI_SFXHook>();
        if (sfxHook)
        {
            sfxHook.HookButton(breakdownContinueButton);
            sfxHook.HookButton(roundDistributeContinueButton);
            sfxHook.HookButton(runDrainContinueButton);
            sfxHook.HookButton(runCommitContinueButton);
        }

        HardStopAndClearAllVfx();

        root.SetActive(true);
        _hideOnRunEndFinalContinue = hideOnFinalContinue;

        var runSnap = RunMonsterProgress.GetSnapshot();
        _permanentXpConversion = Mathf.Clamp01(keepFraction);
        var keptXp = RunMonsterProgress.EndRunAndComputeKeptXp(keepFraction);

        StartCoroutine(CoRunDrainThenCommit(roster, runSnap, keptXp, onContinueToHighScore));
    }

    void ShowBreakdown(RoundXpBreakdown b)
    {
        if (roundBreakdownPanel) roundBreakdownPanel.SetActive(true);
        if (roundDistributePanel) roundDistributePanel.SetActive(false);
        if (runDrainPanel) runDrainPanel.SetActive(false);
        if (runCommitPanel) runCommitPanel.SetActive(false);

        string titleStr = $"Level {b.gameLevelNumber} Complete";

        if (breakdownTitleText)
            breakdownTitleText.text = titleStr;

        if (breakdownTitleShadowText)
            breakdownTitleShadowText.text = titleStr;

        string linesStr =
            $"Base XP: {b.baseXp}\n\n" +
            $"Clear Time: {b.levelClearTime:0.#}s  ->  {b.clearTimeBonus}\n\n" +
            $"Reserve Change = {b.endReserve - b.startReserve}  =>  {b.reserveBonus}\n\n" +
            $"Largest Combo: {b.comboBonus}\n\n" +
            $"Obstacles Cleared: {b.obstacleBonus}";

        if (b.difficultyStars > 0 && b.difficultyBonus > 0)
        {
            linesStr += $"\n\nStar Difficulty ({b.difficultyStars}): +{b.difficultyBonus}";
        }

        if (breakdownLinesText)
            breakdownLinesText.text = linesStr;

        if (breakdownLinesShadowText)
            breakdownLinesShadowText.text = linesStr;

        if (breakdownContinueButton)
            breakdownContinueButton.interactable = false;

        _skipRequested = false;
        _breakdownAnimating = true;
        _breakdownSkipAllowedAt = Time.unscaledTime + Mathf.Max(0f, breakdownSkipInputDelaySeconds);

        if (_breakdownCountCR != null) { StopCoroutine(_breakdownCountCR); _breakdownCountCR = null; }
        _breakdownCountCR = StartCoroutine(CoBreakdownCountUp(b.totalBeforeReduction));
    }

    IEnumerator CoRoundDistribute(List<MonsterData> roster, Dictionary<string, float> perMonsterAwardXp, Action onContinue)
    {
        if (roundBreakdownPanel) roundBreakdownPanel.SetActive(false);
        if (roundDistributePanel) roundDistributePanel.SetActive(true);

        BuildRosterRows(roundRosterContainer, roster, useRunState: true);

        if (roundDistributeContinueButton)
        {
            roundDistributeContinueButton.interactable = false;
            roundDistributeContinueButton.onClick.RemoveAllListeners();
        }

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].HideDeltaTexts();

            if (perMonsterAwardXp != null && perMonsterAwardXp.TryGetValue(md.monsterName, out var award))
                _rows[i].ShowXpDist(award);
        }

        var startLevels = new Dictionary<string, int>();
        var startInto = new Dictionary<string, float>();

        foreach (var md in roster)
        {
            if (!md) continue;
            startLevels[md.monsterName] = RunMonsterProgress.GetCurrentLevel(md.monsterName);
            startInto[md.monsterName] = RunMonsterProgress.GetCurrentXpIntoLevel(md.monsterName);
        }

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            int sLvl = startLevels.TryGetValue(md.monsterName, out var lv0) ? lv0 : 1;
            float sXp = startInto.TryGetValue(md.monsterName, out var xp0) ? xp0 : 0f;

            _rows[i].InitXpState(sLvl, sXp, XpPerLevel);
        }

        if (orbGainStartDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(orbGainStartDelaySeconds);

        _skipRequested = false;
        _orbAnimating = true;

        yield return StartCoroutine(CoOrbDistributeGainAndFillUi(roster, perMonsterAwardXp, startLevels));

        ClearActiveOrbs();

        _orbAnimating = false;
        _skipRequested = false;

        if (roundDistributeContinueButton)
        {
            roundDistributeContinueButton.interactable = false;
            roundDistributeContinueButton.onClick.RemoveAllListeners();
            roundDistributeContinueButton.onClick.AddListener(() =>
            {
                HardStopAndClearAllVfx();
                HideAll();
                onContinue?.Invoke();
            });
        }

        foreach (var md in roster)
        {
            if (!md) continue;
            if (!perMonsterAwardXp.TryGetValue(md.monsterName, out var award)) continue;
            RunMonsterProgress.AddRunXp(md.monsterName, award);
        }

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].SetLevel(RunMonsterProgress.GetCurrentLevel(md.monsterName));
            _rows[i].SetXp(RunMonsterProgress.GetCurrentXpIntoLevel(md.monsterName), XpPerLevel);
        }

        if (roundDistributeContinueButton)
            roundDistributeContinueButton.interactable = true;
    }

    IEnumerator CoRunDrainThenCommit(List<MonsterData> roster, Dictionary<string, RunMonsterProgress.RunState> runSnap,
                                     Dictionary<string, float> keptXp, Action onContinueToHighScore)
    {
        if (roundBreakdownPanel) roundBreakdownPanel.SetActive(false);
        if (roundDistributePanel) roundDistributePanel.SetActive(false);

        if (runDrainPanel) runDrainPanel.SetActive(true);

        if (runDrainContinueButton)
        {
            runDrainContinueButton.interactable = false;
            runDrainContinueButton.onClick.RemoveAllListeners();
        }

        if (runCommitPanel) runCommitPanel.SetActive(false);

        BuildRosterRows(runDrainContainer, roster, useRunState: false, usePermanentState: false,
                        prefabOverride: runDrainMonsterRowPrefab ? runDrainMonsterRowPrefab : monsterRowPrefab,
                        runSnapshot: runSnap);

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].HideDeltaTexts();
        }

        if (orbDrainStartDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(orbDrainStartDelaySeconds);

        _skipRequested = false;
        _orbAnimating = true;

        yield return StartCoroutine(CoOrbDrainRun(roster, runSnap));

        ClearActiveOrbs();

        _orbAnimating = false;
        _skipRequested = false;

        bool proceedToCommit = false;

        if (runDrainContinueButton)
        {
            runDrainContinueButton.onClick.RemoveAllListeners();
            runDrainContinueButton.onClick.AddListener(() => proceedToCommit = true);
            runDrainContinueButton.interactable = true;
        }

        yield return new WaitUntil(() => proceedToCommit);

        ClearActiveOrbs();

        if (runDrainPanel) runDrainPanel.SetActive(false);
        if (runCommitPanel) runCommitPanel.SetActive(true);

        if (runCommitContinueButton)
        {
            runCommitContinueButton.interactable = false;
            runCommitContinueButton.onClick.RemoveAllListeners();
        }

        BuildRosterRows(runCommitContainer, roster, useRunState: false, usePermanentState: true,
                        prefabOverride: runCommitMonsterRowPrefab ? runCommitMonsterRowPrefab : monsterRowPrefab,
                        runSnapshot: null);

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].HideDeltaTexts();

            if (keptXp != null && keptXp.TryGetValue(md.monsterName, out var kept))
                _rows[i].ShowXpDist(kept);
        }

        if (orbGainStartDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(orbGainStartDelaySeconds);

        yield return StartCoroutine(CoOrbDistributeGainAndFillUi(roster, keptXp, startLevels: null));

        ClearActiveOrbs(); // Ensure  no orbs can remain after commit gain ends

        foreach (var md in roster)
        {
            if (!md) continue;

            float add = keptXp != null && keptXp.TryGetValue(md.monsterName, out var k) ? k : 0f;
            if (add > 0f)
                MonsterProgressStore.AddPermanentXp(md.monsterName, add);
        }

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].SetLevel(MonsterProgressStore.GetPermanentLevel(md.monsterName));
            _rows[i].SetXp(MonsterProgressStore.GetPermanentXpIntoLevel(md.monsterName), XpPerLevel);
        }

        if (runCommitContinueButton)
        {
            runCommitContinueButton.onClick.RemoveAllListeners();

            runCommitContinueButton.onClick.AddListener(() =>
            {
                HardStopAndClearAllVfx();

                if (_hideOnRunEndFinalContinue)
                    HideAll();

                onContinueToHighScore?.Invoke();
            });

            runCommitContinueButton.interactable = true;
        }
    }

    void BuildRosterRows(Transform container, List<MonsterData> roster, bool useRunState, bool usePermanentState = false,
                         XpMonsterRowUI prefabOverride = null, Dictionary<string, RunMonsterProgress.RunState> runSnapshot = null)
    {
        _rows.Clear();

        var prefab = prefabOverride ? prefabOverride : monsterRowPrefab;
        if (!container || !prefab) return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        if (roster == null) return;

        foreach (var md in roster)
        {
            if (!md) continue;

            var row = Instantiate(prefab, container);
            _rows.Add(row);

            bool unlocked = UnlockStore.IsUnlocked(md);
            int skin = unlocked ? MonsterSkinStore.GetValidSelected(md) : 0;
            Sprite portrait = MonsterSkinStore.GetPortrait(md, skin);

            row.BindStatic(portrait, md.monsterName);

            int level;
            float into;

            if (useRunState)
            {
                level = RunMonsterProgress.GetCurrentLevel(md.monsterName);
                into = RunMonsterProgress.GetCurrentXpIntoLevel(md.monsterName);
            }
            else if (runSnapshot != null && runSnapshot.TryGetValue(md.monsterName, out var snap))
            {
                level = snap.level;
                into = snap.xpInto;
            }
            else if (usePermanentState)
            {
                level = MonsterProgressStore.GetPermanentLevel(md.monsterName);
                into = MonsterProgressStore.GetPermanentXpIntoLevel(md.monsterName);
            }
            else
            {
                level = 1;
                into = 0f;
            }

            row.SetLevel(level);
            row.SetXp(into, XpPerLevel);
            row.InitXpState(level, into, XpPerLevel);
        }
    }

    IEnumerator CoBreakdownCountUp(int finalTotal)
    {
        finalTotal = Mathf.Max(0, finalTotal);

        int current = 0;
        SetBreakdownTotal(current);

        if (breakdownStartDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(breakdownStartDelaySeconds);

        float elapsed = 0f;

        while (current < finalTotal)
        {
            if (_skipRequested)
            {
                current = finalTotal;
                SetBreakdownTotal(current);
                break;
            }

            current += 1;
            SetBreakdownTotal(current);

            if (AudioManager.I && AudioManager.I.sfxXpTick)
                AudioManager.I.PlayUISFX(AudioManager.I.sfxXpTick);

            float jitter = breakdownTickJitterSeconds > 0f
                ? UnityEngine.Random.Range(0f, breakdownTickJitterSeconds)
                : 0f;

            float progress = finalTotal > 0 ? (float)current / finalTotal : 1f;
            float a = Mathf.Pow(progress, Mathf.Max(0.1f, breakdownAccelPower));
            float shapedTick = Mathf.Lerp(breakdownTickStartSeconds, breakdownTickEndSeconds, a);

            float remainingTime = Mathf.Max(0.01f, breakdownMaxSeconds - elapsed);
            int remainingTicks = Mathf.Max(1, finalTotal - current);
            float budgetTick = remainingTime / remainingTicks;

            float cappedTick = Mathf.Min(shapedTick, budgetTick);

            float wait = Mathf.Max(0.001f, cappedTick + jitter);
            elapsed += wait;

            yield return new WaitForSecondsRealtime(wait);
        }

        _breakdownAnimating = false;
        _skipRequested = false;

        if (breakdownContinueButton)
            breakdownContinueButton.interactable = true;

        _breakdownCountCR = null;
    }

    void SetBreakdownTotal(int total)
    {
        string totalStr = $"Total XP Earned = {total}";

        if (breakdownTotalText)
            breakdownTotalText.text = totalStr;

        if (breakdownTotalShadowText)
            breakdownTotalShadowText.text = totalStr;
    }

    IEnumerator CoOrbDrainRun(List<MonsterData> roster, Dictionary<string, RunMonsterProgress.RunState> runSnap)
    {
        if (!vfxRoot || !xpOrbPrefab || roster == null || runSnap == null)
            yield break;

        var remaining = new int[_rows.Count];
        var preservedShown = new float[_rows.Count];
        var drainXpPerRow = new float[_rows.Count];

        int total = 0;

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            if (runSnap.TryGetValue(md.monsterName, out var st))
            {
                float runTotalXp = ((st.level - 1) * XpPerLevel) + st.xpInto;
                float permanentTotalXp = MonsterProgressStore.GetPermanentTotalXp(md.monsterName);
                float drainXp = Mathf.Max(0f, runTotalXp - permanentTotalXp);

                drainXpPerRow[i] = drainXp;
                int count = Mathf.Max(0, Mathf.CeilToInt(drainXp));
                remaining[i] = count;
                total += count;
            }

            preservedShown[i] = 0f;
            _rows[i].ShowXpDrainPreserved(0f);
        }

        int processed = 0;
        float elapsedSpawn = 0f;

        while (true)
        {
            if (_skipRequested)
            {
                ClearActiveOrbs();

                for (int r = 0; r < roster.Count && r < _rows.Count; r++)
                {
                    var md = roster[r];
                    if (!md) continue;

                    int permLevel = MonsterProgressStore.GetPermanentLevel(md.monsterName);
                    float permXpInto = MonsterProgressStore.GetPermanentXpIntoLevel(md.monsterName);
                    float preserved = drainXpPerRow[r] * _permanentXpConversion;

                    _rows[r].InitXpState(permLevel, permXpInto, XpPerLevel);
                    _rows[r].ShowXpDrainPreserved(preserved);
                }

                break;
            }

            int any = 0;
            for (int i = 0; i < remaining.Length; i++) any += remaining[i];
            if (any <= 0) break;

            float progress = total > 0 ? (float)processed / total : 1f;
            float accel = Mathf.Pow(progress, Mathf.Max(0.1f, orbAccelPower));

            float shapedInterval = Mathf.Lerp(orbSpawnIntervalStartSeconds, orbSpawnIntervalEndSeconds, accel);
            float travelSeconds = Mathf.Lerp(orbTravelStartSeconds, orbTravelEndSeconds, accel);

            int spawnsNeeded = Mathf.CeilToInt((float)any / Mathf.Max(1, maxOrbsPerFrame));
            float remainingTime = Mathf.Max(0.01f, orbTransferMaxSeconds - elapsedSpawn);
            float budgetInterval = remainingTime / Mathf.Max(1, spawnsNeeded);

            float spawnInterval = Mathf.Min(shapedInterval, budgetInterval);

            int spawnedThisFrame = 0;

            for (int i = 0; i < roster.Count && i < _rows.Count; i++)
            {
                if (spawnedThisFrame >= maxOrbsPerFrame) break;
                if (remaining[i] <= 0) continue;

                var row = _rows[i];
                if (!row) continue;

                var start = row.GetXpBarCenter();
                var end = row.GetDrainOrbAnchor();
                if (!start || !end) continue;

                remaining[i] -= 1;
                processed += 1;
                spawnedThisFrame += 1;

                int rowIndex = i;
                var rowRef = row;

                StartCoroutine(CoSpawnOrbArc(start, end, gain: false, travelSeconds: travelSeconds, accel01: accel,
                                             onArrive: () =>
                                             {
                                                 rowRef.SubtractXpFromOrb(1f, XpPerLevel);

                                                 preservedShown[rowIndex] += _permanentXpConversion;
                                                 rowRef.ShowXpDrainPreserved(preservedShown[rowIndex]);
                                             }));
            }

            float wait = Mathf.Max(0.001f, spawnInterval);
            yield return new WaitForSecondsRealtime(wait);
            elapsedSpawn += wait;
        }
    }

    IEnumerator CoSpawnOrbArc(RectTransform start, RectTransform end, bool gain, float travelSeconds, float accel01, Action onArrive)
    {
        var orb = Instantiate(xpOrbPrefab, vfxRoot);
        _activeOrbGos.Add(orb.gameObject);
        orb.gameObject.SetActive(true);

        var orbRect = orb.rectTransform;

        Vector2 p0 = WorldToLocal(vfxRoot, start.position);
        Vector2 p2 = WorldToLocal(vfxRoot, end.position);

        Vector2 mid = (p0 + p2) * 0.5f;
        Vector2 p1 = mid + Vector2.up * orbArcHeight;

        float t = 0f;

        while (t < travelSeconds)
        {
            if (!orb) yield break;

            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, travelSeconds));

            Vector2 pos = Bezier2(p0, p1, p2, a);
            orbRect.anchoredPosition = pos;

            yield return null;
        }

        if (!orb) yield break;

        orbRect.anchoredPosition = p2;
        TryPlayOrbSfx(gain, accel01);
        onArrive?.Invoke();

        _activeOrbGos.Remove(orb.gameObject);
        Destroy(orb.gameObject);
    }

    static Vector2 Bezier2(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + (2f * u * t) * p1 + (t * t) * p2;
    }

    static Vector2 WorldToLocal(RectTransform root, Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out var local);

        return local;
    }

    IEnumerator CoOrbDistributeGainAndFillUi(List<MonsterData> roster, Dictionary<string, float> perMonsterXp,
                                             Dictionary<string, int> startLevels)
    {
        if (!vfxRoot || !xpOrbPrefab || roster == null || perMonsterXp == null)
            yield break;

        var remaining = new int[_rows.Count];
        var total = 0;

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            if (perMonsterXp.TryGetValue(md.monsterName, out var xp))
            {
                int count = Mathf.Max(0, Mathf.FloorToInt(xp));
                remaining[i] = count;
                total += count;
            }
        }

        var stepLines = new List<string>[_rows.Count];
        var stepIdx = new int[_rows.Count];

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            int sLvl;
            if (startLevels != null && startLevels.TryGetValue(md.monsterName, out var lv0))
                sLvl = lv0;
            else
                sLvl = _rows[i].GetUiLevel();

            int eLvl = sLvl + EstimateLevelsGainedFromXp(_rows[i].GetUiXpInto(), remaining[i]);

            stepLines[i] = MonsterLeveling.GetOrderedStatStepLines(md, sLvl, eLvl);
            stepIdx[i] = 0;
        }

        int processed = 0;
        float elapsedSpawn = 0f;

        while (true)
        {
            if (_skipRequested)
            {
                ClearActiveOrbs();

                for (int r = 0; r < roster.Count && r < _rows.Count; r++)
                {
                    var md = roster[r];
                    if (!md) continue;

                    int toApply = remaining[r];
                    if (toApply <= 0) continue;

                    remaining[r] = 0;
                    _rows[r].ShowXpDist(0f);

                    for (int k = 0; k < toApply; k++)
                    {
                        int gained = _rows[r].AddXpFromOrb(1f, XpPerLevel);
                        for (int g = 0; g < gained; g++)
                        {
                            TryPlayLevelUpSfxOncePerFrame();

                            if (stepLines[r] == null)
                                stepLines[r] = new List<string>();

                            if (stepIdx[r] >= stepLines[r].Count)
                            {
                                int currentLevelAfter = _rows[r].GetUiLevel();
                                int from = currentLevelAfter - 1;
                                int to = currentLevelAfter;
                                stepLines[r].AddRange(MonsterLeveling.GetOrderedStatStepLines(roster[r], from, to));
                            }

                            if (stepIdx[r] < stepLines[r].Count)
                            {
                                _rows[r].AppendLevelUpStatLine(stepLines[r][stepIdx[r]]);
                                stepIdx[r] += 1;
                            }
                        }
                    }
                }

                break;
            }

            int any = 0;
            for (int i = 0; i < remaining.Length; i++) any += remaining[i];
            if (any <= 0) break;

            float p = total > 0 ? (float)processed / total : 1f;
            float a = Mathf.Pow(p, Mathf.Max(0.1f, orbAccelPower));

            float shapedInterval = Mathf.Lerp(orbSpawnIntervalStartSeconds, orbSpawnIntervalEndSeconds, a);
            float travelSeconds = Mathf.Lerp(orbTravelStartSeconds, orbTravelEndSeconds, a);

            int spawnsNeeded = Mathf.CeilToInt((float)any / Mathf.Max(1, maxOrbsPerFrame));
            float remainingTime = Mathf.Max(0.01f, orbTransferMaxSeconds - elapsedSpawn);
            float budgetInterval = remainingTime / Mathf.Max(1, spawnsNeeded);

            float spawnInterval = Mathf.Min(shapedInterval, budgetInterval);

            int spawnedThisFrame = 0;

            for (int i = 0; i < roster.Count && i < _rows.Count; i++)
            {
                if (spawnedThisFrame >= maxOrbsPerFrame) break;
                if (remaining[i] <= 0) continue;

                var row = _rows[i];
                if (!row) continue;

                var start = row.GetDistOrbAnchor();
                var end = row.GetXpBarCenter();
                if (!start || !end) continue;

                remaining[i] -= 1;
                processed += 1;
                spawnedThisFrame += 1;

                row.ShowXpDist(remaining[i]);

                int rowIndex = i;
                var rowRef = row;

                StartCoroutine(CoSpawnOrbArc(start, end, gain: true, travelSeconds: travelSeconds, accel01: a, onArrive: () =>
                {
                    int gained = rowRef.AddXpFromOrb(1f, XpPerLevel);
                    if (gained > 0)
                    {
                        for (int k = 0; k < gained; k++)
                        {
                            TryPlayLevelUpSfxOncePerFrame();

                            if (stepLines[rowIndex] == null)
                                stepLines[rowIndex] = new List<string>();

                            if (stepIdx[rowIndex] >= stepLines[rowIndex].Count)
                            {
                                int currentLevelAfter = rowRef.GetUiLevel();
                                int from = currentLevelAfter - 1;
                                int to = currentLevelAfter;
                                stepLines[rowIndex].AddRange(MonsterLeveling.GetOrderedStatStepLines(roster[rowIndex], from, to));
                            }

                            if (stepIdx[rowIndex] < stepLines[rowIndex].Count)
                            {
                                rowRef.AppendLevelUpStatLine(stepLines[rowIndex][stepIdx[rowIndex]]);
                                stepIdx[rowIndex] += 1;
                            }
                        }
                    }
                }));
            }

            float wait = Mathf.Max(0.001f, spawnInterval);
            yield return new WaitForSecondsRealtime(wait);
            elapsedSpawn += wait;
        }

        ClearActiveOrbs();
    }

    static int EstimateLevelsGainedFromXp(float startInto, int xpOrbs)
    {
        int total = Mathf.FloorToInt(startInto) + Mathf.Max(0, xpOrbs);
        return total / MonsterLeveling.XpPerLevel;
    }

    void TryPlayOrbSfx(bool gain, float accel01)
    {
        if (!AudioManager.I) return;

        accel01 = Mathf.Clamp01(accel01);

        int maxPerSec = Mathf.RoundToInt(Mathf.Lerp(maxOrbSfxPerSecondStart, maxOrbSfxPerSecondEnd, accel01));
        float minInterval = Mathf.Lerp(minOrbSfxIntervalStartSeconds, minOrbSfxIntervalEndSeconds, accel01);

        float now = Time.unscaledTime;

        ref float lastTime = ref (gain ? ref _lastGainOrbSfxTime : ref _lastDrainOrbSfxTime);
        ref int count = ref (gain ? ref _gainOrbSfxCountThisSecond : ref _drainOrbSfxCountThisSecond);
        ref float windowStart = ref (gain ? ref _gainOrbSfxSecondStart : ref _drainOrbSfxSecondStart);

        if (now - windowStart >= 1f)
        {
            windowStart = now;
            count = 0;
        }

        if (count >= Mathf.Max(1, maxPerSec))
            return;

        if (now - lastTime < Mathf.Max(0f, minInterval))
            return;

        lastTime = now;
        count += 1;

        float pitch = 1f;

        if (orbSfxPitchJitter > 0f)
        {
            float j = orbSfxPitchJitter;
            pitch = 1f + UnityEngine.Random.Range(-j, j);
        }

        if (gain && AudioManager.I.sfxXpGainOrb)
            AudioManager.I.PlayUISFX(AudioManager.I.sfxXpGainOrb, vol: 1f, pitch: pitch, jitter: false);
        else if (!gain && AudioManager.I.sfxXpDrainOrb)
            AudioManager.I.PlayUISFX(AudioManager.I.sfxXpDrainOrb, vol: 1f, pitch: pitch, jitter: false);
    }

    void TryPlayLevelUpSfxOncePerFrame()
    {
        if (!AudioManager.I || !AudioManager.I.sfxLevelUp) return;
        if (_levelUpSfxFrame == Time.frameCount) return;

        _levelUpSfxFrame = Time.frameCount;
        AudioManager.I.PlayUISFX(AudioManager.I.sfxLevelUp);
    }

    void ClearActiveOrbs()
    {
        for (int i = _activeOrbGos.Count - 1; i >= 0; i--)
        {
            var go = _activeOrbGos[i];
            if (go) Destroy(go);
        }
        _activeOrbGos.Clear();
    }
}
