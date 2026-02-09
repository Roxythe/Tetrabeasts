using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Buff UI")]
    public Transform buffContainer;
    public Button confirmBuffButton;

    [Header("Debuff UI")]
    public Transform debuffContainer;
    public Button confirmDebuffButton;

    [Header("Prefabs")]
    public RunModOptionButton optionButtonPrefab;
    public TMP_Text currnecyGained;

    [Header("Hooked SFX")]
    public GameplayUI_SFXHook _sfxHook;

    RunModifierSO _selectedBuff;
    RunModifierSO _selectedDebuff;

    Action<RunModifierSO, RunModifierSO> _onComplete;


    void OnEnable()
    {
        _sfxHook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!_sfxHook) _sfxHook = FindFirstObjectByType<GameplayUI_SFXHook>();
    }

    public void Show(RunModifierSO[] buffPool, RunModifierSO[] debuffPool, Action<RunModifierSO, RunModifierSO> onComplete,
                     int currencyGained)
    {
        var gc = FindFirstObjectByType<GameController>();
        float luck = gc ? gc.luck : RunModsStore.Luck;
        float misfortune = gc ? gc.misfortune : RunModsStore.Misfortune;

        _onComplete = onComplete;

        rootPanel.SetActive(true);
        buffPanel.SetActive(true);
        debuffPanel.SetActive(false);

        confirmBuffButton.interactable = false;
        confirmDebuffButton.interactable = false;

        _selectedBuff = null;
        _selectedDebuff = null;

        if (currnecyGained)
            currnecyGained.text = $"+{currencyGained}";

        Populate(buffContainer, Pick3UniqueWeighted(buffPool, luck), isBuff: true);
        confirmBuffButton.onClick.RemoveAllListeners();
        confirmBuffButton.onClick.AddListener(() =>
        {
            buffPanel.SetActive(false);
            debuffPanel.SetActive(true);

            Populate(debuffContainer, Pick3UniqueWeighted(debuffPool, misfortune), isBuff: false);
        });

        confirmDebuffButton.onClick.RemoveAllListeners();
        confirmDebuffButton.onClick.AddListener(() =>
        {
            // Prevent extra clicks
            confirmDebuffButton.interactable = false;
            confirmBuffButton.interactable = false;

            // Lock the whole panel while next level loads
            var cg = rootPanel.GetComponent<CanvasGroup>();
            if (!cg) cg = rootPanel.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = true;

            _onComplete?.Invoke(_selectedBuff, _selectedDebuff);
        });
    }

    void Populate(Transform container, List<RunModifierSO> picks, bool isBuff)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        foreach (var mod in picks)
        {
            var btn = Instantiate(optionButtonPrefab, container);
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

    float[] GetRarityProbsFromLuck(float luck)
    {
        float L = Mathf.Clamp(luck, 0f, 200f); // Clamp luck to reasonable range

        // Anchor probability vectors: [Com, Uncom, Rare, Epic, Legndary]
        float[] p0 = { 0.60f, 0.25f, 0.10f, 0.04f, 0.01f }; // 0 luck (baseline)
        float[] p25 = { 0.60f, 0.25f, 0.10f, 0.04f, 0.01f }; // keep new players basically baseline through 25
        float[] p50 = { 0.20f, 0.40f, 0.25f, 0.10f, 0.05f }; // 26–50 guideline
        float[] p75 = { 0.10f, 0.20f, 0.40f, 0.20f, 0.10f }; // 51–75 guideline
        float[] p100 = { 0.05f, 0.10f, 0.30f, 0.40f, 0.15f }; // 75–100 guideline

        // Over 100 luck: start favoring Epic/Legendary more
        float[] p150 = { 0.02f, 0.05f, 0.18f, 0.45f, 0.30f };
        float[] p200 = { 0.01f, 0.03f, 0.10f, 0.40f, 0.46f };

        float[] a, b;
        float t;

        if (L <= 25f) { a = p0; b = p25; t = Mathf.InverseLerp(0f, 25f, L); }
        else if (L <= 50f) { a = p25; b = p50; t = Mathf.InverseLerp(25f, 50f, L); }
        else if (L <= 75f) { a = p50; b = p75; t = Mathf.InverseLerp(50f, 75f, L); }
        else if (L <= 100f) { a = p75; b = p100; t = Mathf.InverseLerp(75f, 100f, L); }
        else if (L <= 150f) { a = p100; b = p150; t = Mathf.InverseLerp(100f, 150f, L); }
        else { a = p150; b = p200; t = Mathf.InverseLerp(150f, 200f, L); }

        t = t * t * (3f - 2f * t); // Smoothstep makes the transition feel gradual even within each segment

        float[] p = new float[5];
        for (int i = 0; i < 5; i++)
            p[i] = Mathf.Lerp(a[i], b[i], t);

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

    RunModifierSO PickByRarityCurve(RunModifierSO[] pool, float luck, HashSet<RunModifierSO> excludeAssets,
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

        float[] probs = GetRarityProbsFromLuck(luck); // Get target rarity probabilities for this luck value

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

        // Pick random mod within that rarity bucket
        var list = buckets[chosenR];
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    List<RunModifierSO> Pick3UniqueWeighted(RunModifierSO[] pool, float skew)
    {
        var results = new List<RunModifierSO>(3);
        var usedAssets = new HashSet<RunModifierSO>();
        var usedGroups = new HashSet<string>();

        int safety = 100;
        while (results.Count < 3 && safety-- > 0)
        {
            var pick = PickByRarityCurve(pool, skew, usedAssets, usedGroups);
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

        // Re-enable for next time
        var cg = rootPanel.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        rootPanel.SetActive(false);
    }

}
