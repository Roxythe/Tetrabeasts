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

    [Header("Debuff UI")]
    public Transform debuffContainer;
    public Button confirmDebuffButton;

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

    Coroutine _blinkRoutine;

    RunModifierSO _selectedBuff;
    RunModifierSO _selectedDebuff;

    Action<RunModifierSO, RunModifierSO> _onComplete;


    void OnEnable()
    {
        _sfxHook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!_sfxHook) _sfxHook = FindFirstObjectByType<GameplayUI_SFXHook>();
    }

    public void Show(RunModifierSO[] buffPool, RunModifierSO[] debuffPool, Action<RunModifierSO, RunModifierSO> onComplete,
                 int currencyGained, int reinforcementsReceived)
    {
        var gc = FindFirstObjectByType<GameController>();
        float luck = gc ? gc.luck : RunModsStore.Luck;
        float misfortune = gc ? gc.misfortune : RunModsStore.Misfortune;

        bool wasBossLevel = gc && gc.LastLevelWasBoss;

        _onComplete = onComplete;

        rootPanel.SetActive(true);
        buffPanel.SetActive(true);
        debuffPanel.SetActive(false);
        StartBlink();

        confirmBuffButton.interactable = false;
        confirmDebuffButton.interactable = false;

        _selectedBuff = null;
        _selectedDebuff = null;

        // Ensure panel buttons also get UIButtonSFX
        _sfxHook?.HookButton(confirmBuffButton);
        _sfxHook?.HookButton(confirmDebuffButton);

        if (currnecyGained)
            currnecyGained.text = $"+{currencyGained}";

        if (reinforcementsGained)
        {
            // Only show reinforcements gained if it's >0, otherwise show maxed out text
            reinforcementsGained.text = reinforcementsReceived > 0
                ? $"+{reinforcementsReceived} Reinforcements"
                : "+0 Unit Lives at Max Capacity";
        }

        Populate(buffContainer, Pick3UniqueWeighted(buffPool, luck, wasBossLevel), isBuff: true);
        confirmBuffButton.onClick.RemoveAllListeners();
        confirmBuffButton.onClick.AddListener(() =>
        {
            buffPanel.SetActive(false);
            debuffPanel.SetActive(true);

            Populate(debuffContainer, Pick3UniqueWeighted(debuffPool, misfortune, wasBossLevel), isBuff: false);
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

    List<RunModifierSO> Pick3UniqueWeighted(RunModifierSO[] pool, float skew, bool wasBossLevel)
    {
        var results = new List<RunModifierSO>(3);
        var usedAssets = new HashSet<RunModifierSO>();
        var usedGroups = new HashSet<string>();

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

        // Re-enable for next time
        var cg = rootPanel.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        StopBlink();
        rootPanel.SetActive(false);
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

            yield return new WaitForSeconds(blinkIntervalSeconds);
        }

        _blinkRoutine = null;
    }

}
