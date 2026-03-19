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

        public int maxReserve;
        public int currentReserve;
        public int unitsLostBonus;

        public int comboBonus;
        public int obstacleBonus;

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
    public float roundDistributeSeconds = 0.9f;

    [Header("Run End - Row Prefabs")]
    public XpMonsterRowUI runDrainMonsterRowPrefab; 
    public XpMonsterRowUI runCommitMonsterRowPrefab; 

    [Header("Run End - Drain Run Instance")]
    public GameObject runDrainPanel;
    public Transform runDrainContainer;
    public Button runDrainContinueButton;
    public float runDrainSeconds = 0.9f;

    [Header("Run End - Permanent Distribution")]
    public GameObject runCommitPanel;
    public Transform runCommitContainer;
    public Button runCommitContinueButton;
    public float runCommitSeconds = 0.9f;

    const float XpPerLevel = 100f;

    readonly List<XpMonsterRowUI> _rows = new();

    void Awake()
    {
        HideAll();
    }

    public void HideAll()
    {
        if (root) root.SetActive(false);

        if (roundBreakdownPanel) roundBreakdownPanel.SetActive(false);
        if (roundDistributePanel) roundDistributePanel.SetActive(false);

        if (runDrainPanel) runDrainPanel.SetActive(false);
        if (runCommitPanel) runCommitPanel.SetActive(false);
    }

    public void ShowRoundWin(RoundXpBreakdown breakdown, List<MonsterData> roster, Dictionary<string, float> perMonsterAwardXp,
                             Action onContinueToRewards)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!root) return;

        root.SetActive(true);
        ShowBreakdown(breakdown);

        if (breakdownContinueButton)
        {
            breakdownContinueButton.onClick.RemoveAllListeners();
            breakdownContinueButton.onClick.AddListener(() =>
            {
                StartCoroutine(CoRoundDistribute(roster, perMonsterAwardXp, onContinueToRewards));
            });
        }
    }

    public void ShowRunEndCommit(List<MonsterData> roster, float keepFraction, Action onContinueToHighScore)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!root) return;

        root.SetActive(true);

        var runSnap = RunMonsterProgress.GetSnapshot();
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
            $"Clear Time: {b.levelClearTime:0.#}s  =>  {b.clearTimeBonus}\n\n" +
            $"Units Lost: {b.maxReserve - b.currentReserve}  =>  {b.unitsLostBonus}\n\n" +
            $"Largest Combo: {b.comboBonus}\n\n" +
            $"Obstacles Cleared: {b.obstacleBonus}";

        if (breakdownLinesText)
            breakdownLinesText.text = linesStr;

        string lineAltString =
            $"Base XP \n\n" +
            $"Clear Time \n\n" +
            $"Units Lost \n\n" +
            $"Largest Combo \n\n" +
            $"Obstacles Cleared";

        if (breakdownLinesShadowText)
            breakdownLinesShadowText.text = lineAltString;

        string totalStr = $"Total XP Earned = {b.totalBeforeReduction}";

        if (breakdownTotalText)
            breakdownTotalText.text = totalStr;

        if (breakdownTotalShadowText)
            breakdownTotalShadowText.text = totalStr;
        ;
    }

    IEnumerator CoRoundDistribute(List<MonsterData> roster, Dictionary<string, float> perMonsterAwardXp, Action onContinue)
    {
        if (roundBreakdownPanel) roundBreakdownPanel.SetActive(false);
        if (roundDistributePanel) roundDistributePanel.SetActive(true);

        BuildRosterRows(roundRosterContainer, roster, useRunState: true);

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].HideDeltaTexts();

            if (perMonsterAwardXp != null && perMonsterAwardXp.TryGetValue(md.monsterName, out var award))
                _rows[i].ShowXpDist(award);
        }

        if (roundDistributeContinueButton)
        {
            roundDistributeContinueButton.interactable = false;
            roundDistributeContinueButton.onClick.RemoveAllListeners();
            roundDistributeContinueButton.onClick.AddListener(() =>
            {
                HideAll();
                onContinue?.Invoke();
            });
        }

        float t = 0f;

        var startLevels = new Dictionary<string, int>();
        var startInto = new Dictionary<string, float>();

        foreach (var md in roster)
        {
            if (!md) continue;
            startLevels[md.monsterName] = RunMonsterProgress.GetCurrentLevel(md.monsterName);
            startInto[md.monsterName] = RunMonsterProgress.GetCurrentXpIntoLevel(md.monsterName);
        }

        foreach (var md in roster)
        {
            if (!md) continue;
            if (!perMonsterAwardXp.TryGetValue(md.monsterName, out var award)) continue;
            RunMonsterProgress.AddRunXp(md.monsterName, award);
        }

        var endLevels = new Dictionary<string, int>();
        var endInto = new Dictionary<string, float>();

        foreach (var md in roster)
        {
            if (!md) continue;
            endLevels[md.monsterName] = RunMonsterProgress.GetCurrentLevel(md.monsterName);
            endInto[md.monsterName] = RunMonsterProgress.GetCurrentXpIntoLevel(md.monsterName);
        }

        while (t < roundDistributeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, roundDistributeSeconds));

            for (int i = 0; i < roster.Count && i < _rows.Count; i++)
            {
                var md = roster[i];
                if (!md) continue;

                int sLvl = startLevels.TryGetValue(md.monsterName, out var lv0) ? lv0 : 1;
                float sXp = startInto.TryGetValue(md.monsterName, out var xp0) ? xp0 : 0f;

                int eLvl = endLevels.TryGetValue(md.monsterName, out var lv1) ? lv1 : sLvl;
                float eXp = endInto.TryGetValue(md.monsterName, out var xp1) ? xp1 : sXp;

                int shownLevel = (a < 0.85f) ? sLvl : eLvl;
                float shownXp = Mathf.Lerp(sXp, eXp, a);

                _rows[i].SetLevel(shownLevel);
                _rows[i].SetXp(shownXp, XpPerLevel);
            }

            yield return null;
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
        if (runCommitPanel) runCommitPanel.SetActive(false);

        BuildRosterRows(runDrainContainer, roster, useRunState: false, usePermanentState: false,
                        prefabOverride: runDrainMonsterRowPrefab ? runDrainMonsterRowPrefab : monsterRowPrefab,
                        runSnapshot: runSnap);

        for (int i = 0; i < roster.Count && i < _rows.Count; i++)
        {
            var md = roster[i];
            if (!md) continue;

            _rows[i].HideDeltaTexts();

            if (keptXp != null && keptXp.TryGetValue(md.monsterName, out var kept))
                _rows[i].ShowXpDrainPreserved(kept);
        }

        if (runDrainContinueButton)
        {
            runDrainContinueButton.interactable = false;
            runDrainContinueButton.onClick.RemoveAllListeners();
        }

        var startTotals = new Dictionary<string, float>();
        foreach (var md in roster)
        {
            if (!md) continue;
            if (runSnap.TryGetValue(md.monsterName, out var st))
                startTotals[md.monsterName] = Mathf.Max(0f, ((st.level - 1) * XpPerLevel) + st.xpInto);
            else
                startTotals[md.monsterName] = 0f;
        }

        float t = 0f;
        while (t < runDrainSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, runDrainSeconds));

            for (int i = 0; i < roster.Count && i < _rows.Count; i++)
            {
                var md = roster[i];
                if (!md) continue;

                float start = startTotals.TryGetValue(md.monsterName, out var v) ? v : 0f;
                float remaining = Mathf.Lerp(start, 0f, a);

                float into = remaining % XpPerLevel;
                int lvl = Mathf.Clamp(Mathf.FloorToInt(remaining / XpPerLevel) + 1, 1, 100);

                _rows[i].SetLevel(lvl);
                _rows[i].SetXp(into, XpPerLevel);
            }

            yield return null;
        }

        if (runDrainPanel) runDrainPanel.SetActive(false);

        if (runCommitPanel) runCommitPanel.SetActive(true);

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

        var permStartTotal = new Dictionary<string, float>();
        var permEndLevel = new Dictionary<string, int>();
        var permEndInto = new Dictionary<string, float>();

        foreach (var md in roster)
        {
            if (!md) continue;

            float sTotal = MonsterProgressStore.GetPermanentTotalXp(md.monsterName);
            permStartTotal[md.monsterName] = sTotal;

            float add = keptXp.TryGetValue(md.monsterName, out var k) ? k : 0f;
            if (add > 0f)
                MonsterProgressStore.AddPermanentXp(md.monsterName, add);

            permEndLevel[md.monsterName] = MonsterProgressStore.GetPermanentLevel(md.monsterName);
            permEndInto[md.monsterName] = MonsterProgressStore.GetPermanentXpIntoLevel(md.monsterName);
        }

        t = 0f;
        while (t < runCommitSeconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, runCommitSeconds));

            for (int i = 0; i < roster.Count && i < _rows.Count; i++)
            {
                var md = roster[i];
                if (!md) continue;

                float sTotal = permStartTotal.TryGetValue(md.monsterName, out var st) ? st : 0f;

                int sLvl = Mathf.Clamp(Mathf.FloorToInt(sTotal / XpPerLevel) + 1, 1, 100);
                float sInto = Mathf.Clamp(sTotal - ((sLvl - 1) * XpPerLevel), 0f, XpPerLevel);

                int eLvl = permEndLevel.TryGetValue(md.monsterName, out var lv1) ? lv1 : sLvl;
                float eInto = permEndInto.TryGetValue(md.monsterName, out var xp1) ? xp1 : sInto;

                int shownLevel = (a < 0.85f) ? sLvl : eLvl;
                float shownXp = Mathf.Lerp(sInto, eInto, a);

                _rows[i].SetLevel(shownLevel);
                _rows[i].SetXp(shownXp, XpPerLevel);
            }

            yield return null;
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
        }
    }
}