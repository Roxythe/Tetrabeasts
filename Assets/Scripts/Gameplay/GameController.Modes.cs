using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    BoulderController boulders;
    GameModeAssets modeAssets;
    MarathonGaugeUI marathonGauge;
    MarathonRewardUI marathonRewards;
    float marathonDamage;
    bool marathonRewardActive;
    float marathonResumeTimeScale = 1f;
    [Header("Marathon Gravity (cells per second)")]
    [SerializeField, Min(0.1f)] float marathonStartingGravityCap = 3f;
    [SerializeField, Min(0f)] float marathonGravityCapGrowth = 2f;
    [SerializeField, Range(0f, 0.3f)] float marathonLevelUpGravityReduction = 0.08f;
    [SerializeField, Min(30f)] float marathonRampAccelerationSeconds = 180f;
    [SerializeField, Min(0f)] float marathonEnemyDamageGrowthPerLevel = 0.03f;
    float marathonElapsedSeconds;
    float marathonCarriedGravity;
    public static float MarathonGravityCap(int levelIndex, float initial = 3f, float growth = 2f) =>
        Mathf.Max(0.1f, initial) + Mathf.Max(0f, growth) * Mathf.Log(1f + Mathf.Max(0, levelIndex));
    float CurrentMarathonGravityCap => MarathonGravityCap(currentLevel, marathonStartingGravityCap, marathonGravityCapGrowth);
    float MarathonPersistentGravityMultiplier => Mathf.Max(0.01f, pieceGravityMult * ShopBuffEffects.GravityMultiplier * ActiveLevelModifierGravityMult);
    float MarathonCoreGravity => Mathf.Min(CurrentMarathonGravityCap / MarathonPersistentGravityMultiplier,
        1f / Mathf.Max(0.0001f, _thisLevelBaseFallInterval) +
        GetCurrentGravityIncreasePerSecond() * GetCurrentFallRampRateMultiplier(1f) * _levelTimer);
    public bool IsMarathon => GameModeStore.Current == RunGameMode.Marathon;
    public float MarathonDamage => marathonDamage;
    public float MarathonRequiredDamage => RequiredMarathonDamage(currentLevel,
        modeAssets ? modeAssets.marathonStartingDamage : 500f,
        modeAssets ? modeAssets.marathonDamageGrowth : 1.18f);
    public static float RequiredMarathonDamage(int completedLevels, float initial, float growth)
    {
        int level = Mathf.Max(0, completedLevels);
        float rate = Mathf.Clamp(growth, 1.01f, 2f);
        // Continue growing after very long runs without overflowing to infinite HP.
        return Mathf.Max(1f, initial) * Mathf.Pow(rate, Mathf.Min(level, 60)) *
            (1f + Mathf.Max(0, level - 60) * (rate - 1f));
    }

    void InitializeModeRuntime()
    {
        modeAssets = GameModeAssets.Load();
        boulders = GetComponent<BoulderController>();
        if (!boulders) boulders = gameObject.AddComponent<BoulderController>();
        if (modeAssets)
        {
            boulders.boulderSprite = modeAssets.boulderSprite;
            boulders.impactSfx = modeAssets.boulderImpactSfx;
            boulders.alternateImpactSfx = modeAssets.boulderImpactAlternates;
        }
    }
    void EnsureMarathonUI()
    {
        if (!IsMarathon || gameOver) return;
        if (marathonGauge) { marathonGauge.gameObject.SetActive(true); return; }
        if (!modeAssets || !gameBoard) return;
        var canvas = gameBoard.GetComponentInParent<Canvas>()?.rootCanvas;
        if (!canvas) return;
        marathonGauge = Instantiate(modeAssets.marathonGaugePrefab, canvas.transform);
        marathonRewards = Instantiate(modeAssets.marathonRewardPrefab, canvas.transform);
        marathonRewards.gameObject.SetActive(false);
        // Reflect the ultimate gauge's center across the board center in canvas space.
        var target = (RectTransform)marathonGauge.transform;
        var canvasRect = (RectTransform)canvas.transform;
        Vector3 boardCenter = canvasRect.InverseTransformPoint(gameBoard.gridRoot.TransformPoint(gameBoard.gridRoot.rect.center));
        var reference = specialGaugeSlider ? specialGaugeSlider.transform as RectTransform : null;
        if (reference)
        {
            Vector3 ultimateCenter = canvasRect.InverseTransformPoint(reference.TransformPoint(reference.rect.center));
            target.localPosition = new Vector3(2f * boardCenter.x - ultimateCenter.x, ultimateCenter.y, 0f);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, reference.rect.height * reference.lossyScale.y / canvasRect.lossyScale.y);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, reference.rect.width * reference.lossyScale.x / canvasRect.lossyScale.x);
        }
        else target.localPosition = boardCenter + Vector3.left * (gameBoard.gridRoot.rect.width * 0.5f + 70f);
        target.localPosition += Vector3.right * marathonGauge.horizontalOffset;
        if (reference)
        {
            // Share the special gauge's HUD parent, visibility and canvas sorting.
            target.SetParent(reference.parent, true);
            target.SetSiblingIndex(reference.GetSiblingIndex());
        }
    }
    void TickGameMode()
    {
        if (!IsMarathon) return;
        if (gameOver) { HideMarathonUI(); return; }
        EnsureMarathonUI();
        RefreshMarathonGauge();
        if (IsRoundActive && !marathonRewardActive && marathonDamage >= MarathonRequiredDamage)
            StartCoroutine(MarathonLevelUp());
    }
    void RefreshMarathonGauge()
    {
        if (marathonGauge) marathonGauge.Refresh(currentLevel + 1, marathonDamage, MarathonRequiredDamage);
        if (IsMarathon && enemyCastleUI)
            enemyCastleUI.SetMarathonProgress(Mathf.Clamp01(marathonDamage / MarathonRequiredDamage), false);
    }
    void HideMarathonUI()
    {
        if (marathonGauge) marathonGauge.gameObject.SetActive(false);
        if (marathonRewards) marathonRewards.gameObject.SetActive(false);
    }
    List<CastleData> MarathonBosses()
    {
        var bosses = new List<CastleData>();
        if (castlesByLevel != null)
            foreach (var castle in castlesByLevel)
                if (castle && castle.isBossLevel && !bosses.Contains(castle)) bosses.Add(castle);
        if (postFinalSurvivalCastle && !bosses.Contains(postFinalSurvivalCastle)) bosses.Add(postFinalSurvivalCastle);
        return bosses;
    }
    CastleData ResolveMarathonCastle(int levelIndex)
    {
        var bosses = MarathonBosses();
        int cycleIndex = bosses.Count > 0 ? Mathf.Max(0, levelIndex) % (bosses.Count * 3) : Mathf.Max(0, levelIndex);
        int level = cycleIndex + 1;
        if (bosses.Count > 0 && level % 3 == 0)
            return bosses[level / 3 - 1];
        var normal = new List<CastleData>();
        if (castlesByLevel != null)
            foreach (var castle in castlesByLevel) if (castle && !castle.isBossLevel) normal.Add(castle);
        return normal.Count > 0 ? normal[cycleIndex % normal.Count] : (bosses.Count > 0 ? bosses[0] : null);
    }
    IEnumerator MarathonLevelUp()
    {
        var options = MarathonRewardUI.RollPairs(buffPool, debuffPool);
        if (!marathonRewards || options.Length != 3)
        {
            Debug.LogError("Marathon requires its reward prefab and at least three matching-rarity reward pairs.");
            yield break;
        }
        marathonRewardActive = true;
        marathonResumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        EnterUICursorMode();
        // Carryover can earn several levels; offer a separate confirmed choice for each.
        while (marathonDamage >= MarathonRequiredDamage && !gameOver)
        {
            marathonCarriedGravity = Mathf.Max(GetLevelOneBaseGravityCellsPerSecond(),
                MarathonCoreGravity * (1f - marathonLevelUpGravityReduction));
            marathonDamage -= MarathonRequiredDamage;
            var xp = ComputeRoundWinXp(currentLevel + 1);
            foreach (var award in xp.perMonsterAwardXp) RunMonsterProgress.AddRunXp(award.Key, award.Value);
            int reinforcements = GrantMarathonReinforcements();
            currentLevel++;
            bool confirmed = false;
            options = MarathonRewardUI.RollPairs(buffPool, debuffPool);
            marathonRewards.Show(currentLevel + 1, options, (buff, debuff) =>
            {
                ApplyChosenRunMods(buff, debuff);
                confirmed = true;
            });
            RefreshMarathonGauge();
            while (!confirmed && !gameOver) yield return null;
            if (gameOver) break;
            InitLevel(currentLevel, spawnEnvironment: false);
            ShowMarathonReinforcements(reinforcements);
            RefreshMarathonGauge();
            CacheAndWriteTempRunCheckpoint();
        }
        marathonRewardActive = false;
        Time.timeScale = marathonResumeTimeScale;
        if (!gameOver) EnterGameplayCursorMode();
    }
    int GrantMarathonReinforcements()
    {
        int before = unitLives;
        unitLives = Mathf.Clamp(unitLives + CurrentReinforcementsPerWin, 0, EffectiveMaxUnitLives);
        UpdateUnitLivesUI();
        return Mathf.Max(0, unitLives - before);
    }
    void ShowMarathonReinforcements(int amount)
    {
        if (amount <= 0 || !unitLivesSlider || !stoneBuffPopupStyle || !stoneBuffPopupStyle.popupPrefab) return;
        var instance = Instantiate(stoneBuffPopupStyle.popupPrefab, unitLivesSlider.transform);
        var view = instance.GetComponent<BuffPopupView>();
        if (!view || !view.IsValid) { Destroy(instance); return; }
        view.RectTransform.anchorMin = view.RectTransform.anchorMax = new Vector2(0.5f, 1f);
        view.RectTransform.pivot = new Vector2(0.5f, 0f);
        view.RectTransform.anchoredPosition = new Vector2(0, 12);
        view.RectTransform.sizeDelta = new Vector2(360, 50);
        view.mainText.text = view.shadowText.text = "+" + amount + " Reinforcements";
        view.mainText.color = new Color(0.5f, 1f, 0.45f);
        view.shadowText.color = Color.black;
        foreach (var text in new[] { view.mainText, view.shadowText })
        {
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 30;
            text.alignment = TMPro.TextAlignmentOptions.Center;
        }
        view.shadowText.rectTransform.anchoredPosition = stoneBuffPopupStyle.shadowOffset;
        StartCoroutine(FloatReinforcements(view, Mathf.Max(1.5f, stoneBuffPopupStyle.duration), stoneBuffPopupStyle.floatPixels));
    }
    IEnumerator FloatReinforcements(BuffPopupView view, float duration, float rise)
    {
        var start = view.RectTransform.anchoredPosition;
        var group = view.GetComponent<CanvasGroup>() ?? view.gameObject.AddComponent<CanvasGroup>();
        for (float elapsed = 0; elapsed < duration && view; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            view.RectTransform.anchoredPosition = start + Vector2.up * rise * progress;
            group.alpha = 1f - progress;
            yield return null;
        }
        if (view) Destroy(view.gameObject);
    }
    void ResetModeProgress()
    {
        if (marathonRewardActive) Time.timeScale = marathonResumeTimeScale;
        marathonRewardActive = false;
        marathonDamage = 0f;
        marathonElapsedSeconds = marathonCarriedGravity = 0f;
        HideMarathonUI();
        boulders?.Clear();
    }
}
