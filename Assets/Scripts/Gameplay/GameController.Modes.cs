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
    int marathonRewardLevel;
    bool marathonTransitionActive;
    MarathonCurtainTransition marathonCurtain;
    MarathonCastleSummaryUI marathonCastleSummary;
    CommanderLevelUpUI commanderLevelUp;
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
    // Marathon has its own castle curve; do not multiply it by the campaign star curve.
    public float EnemyCastleHealthMultiplier => enemyCastleHpMult * (IsMarathon ? 1f : _starDifficultyModifiers.enemyHealthMultiplier);
    public float MarathonDamage => marathonDamage;
    public float MarathonRequiredDamage => RequiredMarathonDamage(marathonRewardLevel,
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

    public static int MarathonCastleHealth(int levelIndex, float initial = 1000f, float growth = 1.35f)
        => (int)System.Math.Min(1000000000d, System.Math.Ceiling(RequiredMarathonDamage(levelIndex, initial, growth)));

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
        else if (IsRoundActive && enemyCastleUI && enemyCastleUI.currentHP <= 0)
            StartCoroutine(MarathonCastleTransition());
    }
    void RefreshMarathonGauge()
    {
        if (marathonGauge) marathonGauge.Refresh(marathonRewardLevel + 1, marathonDamage, MarathonRequiredDamage);
    }
    void HideMarathonUI()
    {
        if (marathonGauge) marathonGauge.gameObject.SetActive(false);
        if (marathonRewards) marathonRewards.gameObject.SetActive(false);
        if (marathonCurtain) marathonCurtain.gameObject.SetActive(false);
        if (marathonCastleSummary) marathonCastleSummary.gameObject.SetActive(false);
        if (commanderLevelUp) commanderLevelUp.gameObject.SetActive(false);
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
            Debug.LogError("Marathon requires its reward prefab and at least three compatible reward pairs.");
            yield break;
        }
        marathonRewardActive = true;
        marathonResumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        EnterUICursorMode();
        // Carryover can earn several levels; offer a separate confirmed choice for each.
        while (marathonDamage >= MarathonRequiredDamage && !gameOver)
        {
            // Keep the earned bar full until its flash and announcement have played.
            if (!commanderLevelUp)
            {
                var canvas = marathonRewards.GetComponentInParent<Canvas>()?.rootCanvas;
                if (canvas) commanderLevelUp = CommanderLevelUpUI.Create(canvas, marathonRewards.title.font);
            }
            if (commanderLevelUp) yield return commanderLevelUp.Play(marathonGauge);
            else if (marathonGauge) yield return marathonGauge.FlashLevelUp();
            if (gameOver) break;
            marathonCarriedGravity = Mathf.Max(GetLevelOneBaseGravityCellsPerSecond(),
                MarathonCoreGravity * (1f - marathonLevelUpGravityReduction));
            marathonDamage -= MarathonRequiredDamage;
            marathonRewardLevel++;
            bool confirmed = false;
            options = MarathonRewardUI.RollPairs(buffPool, debuffPool);
            marathonRewards.Show(marathonRewardLevel + 1, options, (buff, debuff) =>
            {
                ApplyChosenRunMods(buff, debuff);
                RefreshMarathonCombatStats();
                confirmed = true;
            }, this, keepOpenAfterConfirm: true);
            RefreshMarathonGauge();
            while (!confirmed && !gameOver) yield return null;
            if (gameOver) break;
            // Keep the small reward speed relief without resetting the castle timer or visuals.
            float ramp = GetCurrentGravityIncreasePerSecond() * GetCurrentFallRampRateMultiplier(1f) * _levelTimer;
            _thisLevelBaseFallInterval = 1f / Mathf.Max(.01f, marathonCarriedGravity - ramp);
            marathonCarriedGravity = 0f;
            if (piece) piece.SetFallInterval(GetCurrentFallInterval(), resetAccumulator: true);
            RefreshMarathonGauge();
            // Reward progression is independent of the castle/visual level.
            yield return marathonRewards.WaitUntilShown();
            marathonRewards.Dismiss();
            yield return marathonRewards.WaitUntilHidden();
            CacheAndWriteTempRunCheckpoint();
        }
        marathonRewardActive = false;
        Time.timeScale = marathonResumeTimeScale;
        if (!gameOver) EnterGameplayCursorMode();
    }

    IEnumerator MarathonCastleTransition()
    {
        if (marathonTransitionActive) yield break;
        var canvas = gameBoard ? gameBoard.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (!canvas || !modeAssets || !modeAssets.marathonCurtainPrefab || !modeAssets.marathonCurtainOpening)
        {
            Debug.LogError("Marathon requires its level curtain prefab and opening clip in GameModeAssets.");
            yield break;
        }
        marathonTransitionActive = true;
        marathonResumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        EnterUICursorMode();
        if (!marathonCurtain)
            marathonCurtain = MarathonCurtainTransition.Create(canvas, modeAssets.marathonCurtainPrefab, modeAssets.marathonCurtainOpening);
        try
        {
            yield return marathonCurtain.Close();
            if (gameOver) yield break;
            TrackCurrentRoundWinStats();
            int completedLevel = currentLevel + 1;
            // Capture and grant rewards before InitLevel resets the clear-time/combo counters.
            var summaryLines = GrantMarathonCastleRewards();
            levelModifierController?.EndNewModifierEffects();
            boulders?.Clear();
            marathonCarriedGravity = Mathf.Max(GetLevelOneBaseGravityCellsPerSecond(), MarathonCoreGravity);
            currentLevel++;
            InitLevel(currentLevel, spawnEnvironment: true);
            if (levelModifierController) yield return levelModifierController.WaitForMarathonPresentation();
            // Commit layout and deferred object destruction while the curtain is fully closed.
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (gameOver) yield break;
            if (!marathonCastleSummary)
                marathonCastleSummary = MarathonCastleSummaryUI.Create(canvas,
                    marathonRewards && marathonRewards.title ? marathonRewards.title.font : TMPro.TMP_Settings.defaultFontAsset);
            yield return marathonCastleSummary.Show(completedLevel, summaryLines);
            if (gameOver) yield break;
            marathonCurtain.SignalVisualsReady();
            StartCoroutine(marathonCastleSummary.FadeOut(Mathf.Min(1f, modeAssets.marathonCurtainOpening.length)));
            yield return marathonCurtain.Open();
            CacheAndWriteTempRunCheckpoint();
        }
        finally
        {
            if (marathonCurtain) marathonCurtain.gameObject.SetActive(false);
            if (marathonCastleSummary) marathonCastleSummary.gameObject.SetActive(false);
            marathonTransitionActive = false;
            Time.timeScale = marathonResumeTimeScale;
            if (!gameOver) EnterGameplayCursorMode();
        }
    }

    List<MarathonCastleSummaryUI.RewardLine> GrantMarathonCastleRewards()
    {
        var xp = ComputeRoundWinXp(currentLevel + 1);
        int goldBefore = CurrencyStore.Total;
        CurrencyStore.Add(GetRoundWinCurrency());
        int goldGained = Mathf.Max(0, CurrencyStore.Total - goldBefore);
        if (PlayerProgress.I && goldGained > 0)
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.GoldEarned, goldGained);
        if (currencyUI) currencyUI.Refresh();
        var lines = new List<MarathonCastleSummaryUI.RewardLine>
        {
            new MarathonCastleSummaryUI.RewardLine { text = TetrabeastsLocalization.LocalizeFormat("<color=#FFD34D>Gold</color>: +{0:N0}    Total: {1:N0}", goldGained, CurrencyStore.Total) }
        };
        foreach (var award in xp.perMonsterAwardXp)
        {
            float before = RunMonsterProgress.GetRunTotalXp(award.Key);
            RunMonsterProgress.AddRunXp(award.Key, award.Value);
            float total = RunMonsterProgress.GetRunTotalXp(award.Key);
            var unit = GetActiveMonsterRoster().Find(m => m && m.monsterName == award.Key);
            var role = unit ? unit.role : MonsterRole.Attack;
            lines.Add(new MarathonCastleSummaryUI.RewardLine { unitName = TetrabeastsLocalization.LocalizeText(award.Key), beforeXp = before, afterXp = total,
                roleColor = role == MonsterRole.Healer ? new Color(.3f, 1f, .35f) : role == MonsterRole.Defense ? new Color(.3f, .6f, 1f) : new Color(1f, .25f, .25f) });
        }
        RefreshActiveMonsterPassives(applyStartingReserveDelta: true);
        if (gameBoard) gameBoard.RefreshRunLevelStats();
        int reinforcements = GrantMarathonReinforcements();
        lines.Add(new MarathonCastleSummaryUI.RewardLine { text = TetrabeastsLocalization.LocalizeFormat("Reinforcements: +{0}    Unit reserves: {1} / {2}",
            reinforcements, unitLives, EffectiveMaxUnitLives) });
        return lines;
    }

    void RefreshMarathonCombatStats()
    {
        if (enemyCastleUI)
            enemyCastleUI.RescaleHealth(MarathonCastleHealth(currentLevel,
                modeAssets ? modeAssets.marathonCastleStartingHealth : 1000f,
                modeAssets ? modeAssets.marathonCastleHealthGrowth : 1.35f),
                EnemyCastleHealthMultiplier);
        if (!currentCastleData) return;
        castleAttackInterval = currentCastleData.projectileInterval * enemyAttackIntervalMult;
        projectileSpeed = Mathf.Max(1f, currentCastleData.projectileSpeed) * Mathf.Max(.01f, enemyProjectileSpeedMult);
        castleProjectileDamage = Mathf.Max(1, Mathf.RoundToInt(GetScaledEnemyDamage(currentCastleData.projectileDamage * enemyProjectileDamageMult)));
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
        var group = view.GetComponent<CanvasGroup>();
        if (!group) group = view.gameObject.AddComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        for (float elapsed = 0; elapsed < duration && view && group; elapsed += Time.deltaTime)
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
        if (marathonRewardActive || marathonTransitionActive) Time.timeScale = marathonResumeTimeScale;
        marathonTransitionActive = false;
        marathonRewardActive = false;
        marathonRewardLevel = 0;
        marathonDamage = 0f;
        marathonElapsedSeconds = marathonCarriedGravity = 0f;
        HideMarathonUI();
        boulders?.Clear();
    }
}
