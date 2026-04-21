using System.Collections.Generic;
using UnityEngine;

public static class RunMonsterProgress
{
    private const int XpPerLevel = 100;
    private const int MaxLevel = 100;

    public struct RunState
    {
        public int level; // Current run level
        public float xpInto;
    }

    private static readonly Dictionary<string, RunState> _run = new();
    private static bool _runActive;

    public static bool RunActive => _runActive;

    public static void BeginRun(IEnumerable<MonsterData> roster)
    {
        _run.Clear();
        _runActive = true;

        if (roster == null) return;

        foreach (var md in roster)
        {
            if (!md) continue;
            if (string.IsNullOrEmpty(md.monsterName)) continue;

            int lvl = MonsterProgressStore.GetPermanentLevel(md.monsterName);
            float into = MonsterProgressStore.GetPermanentXpIntoLevel(md.monsterName);

            _run[md.monsterName] = new RunState
            {
                level = Mathf.Clamp(lvl, 1, MaxLevel),
                xpInto = Mathf.Clamp(into, 0f, XpPerLevel - 0.0001f)
            };
        }
    }

    public static void EndRunCommit(float keepFraction) // Use for debugging or direct commits
    {
        if (!_runActive)
            return;

        keepFraction = Mathf.Clamp01(keepFraction);

        foreach (var kv in _run)
        {
            string monsterName = kv.Key;
            var st = kv.Value;

            float permanentTotalXp = MonsterProgressStore.GetPermanentTotalXp(monsterName);
            float runTotalXp = ((st.level - 1) * XpPerLevel) + st.xpInto;
            float drainedXp = Mathf.Max(0f, runTotalXp - permanentTotalXp);
            float keepXp = drainedXp * keepFraction;

            if (keepXp > 0f)
                MonsterProgressStore.AddPermanentXp(monsterName, keepXp);
        }

        _run.Clear();
        _runActive = false;
    }

    public static int GetCurrentLevel(string monsterName)
    {
        if (_runActive && _run.TryGetValue(monsterName, out var st))
            return st.level;

        return MonsterProgressStore.GetPermanentLevel(monsterName);
    }

    public static float GetCurrentXpIntoLevel(string monsterName)
    {
        if (_runActive && _run.TryGetValue(monsterName, out var st))
            return st.xpInto;

        return MonsterProgressStore.GetPermanentXpIntoLevel(monsterName);
    }

    public static void AddRunXp(string monsterName, float addXp)
    {
        if (!_runActive) return;
        if (string.IsNullOrEmpty(monsterName)) return;
        if (addXp <= 0f) return;
        if (!_run.TryGetValue(monsterName, out var st)) return;

        if (st.level >= MaxLevel)
        {
            st.level = MaxLevel;
            st.xpInto = Mathf.Min(st.xpInto, XpPerLevel - 0.0001f);
            _run[monsterName] = st;
            return;
        }

        st.xpInto += addXp;

        while (st.xpInto >= XpPerLevel && st.level < MaxLevel)
        {
            st.xpInto -= XpPerLevel;
            st.level += 1;

            if (st.level >= MaxLevel)
            {
                st.level = MaxLevel;
                st.xpInto = Mathf.Min(st.xpInto, XpPerLevel - 0.0001f);
                break;
            }
        }

        st.xpInto = Mathf.Clamp(st.xpInto, 0f, XpPerLevel - 0.0001f);
        _run[monsterName] = st;
    }

    // ============================= Utility =============================

    public static Dictionary<string, RunState> GetSnapshot()
    {
        return new Dictionary<string, RunState>(_run);
    }

    public static void RestoreSnapshot(Dictionary<string, RunState> snapshot)
    {
        _run.Clear();

        if (snapshot != null)
        {
            foreach (var kv in snapshot)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;

                _run[kv.Key] = kv.Value;
            }
        }

        _runActive = snapshot != null;
    }

    public static float GetRunTotalXp(string monsterName)
    {
        if (!_run.TryGetValue(monsterName, out var st)) return 0f;
        return ((st.level - 1) * XpPerLevel) + st.xpInto;
    }

    public static Dictionary<string, float> EndRunAndComputeKeptXp(float keepFraction)
    {
        var kept = new Dictionary<string, float>();
        if (!_runActive) return kept;

        keepFraction = Mathf.Clamp01(keepFraction);

        foreach (var kv in _run)
        {
            string monsterName = kv.Key;
            var st = kv.Value;

            float permanentTotalXp = MonsterProgressStore.GetPermanentTotalXp(monsterName);
            float runTotalXp = ((st.level - 1) * XpPerLevel) + st.xpInto;
            float drainedXp = Mathf.Max(0f, runTotalXp - permanentTotalXp);
            float keepXp = drainedXp * keepFraction;
            kept[monsterName] = Mathf.Max(0f, keepXp);
        }

        _run.Clear();
        _runActive = false;
        return kept;
    }
}
