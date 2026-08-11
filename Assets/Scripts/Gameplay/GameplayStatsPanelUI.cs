using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameplayStatsPanelUI : MonoBehaviour
{
    const float Epsilon = 0.0001f;

    enum StatSource
    {
        ShopBuff,
        Passive,
        RunModBuff,
        RunModDebuff,
        LevelMod,
        BossAbility,
        StarDifficulty
    }

    [SerializeField] GameController gameController;
    [SerializeField] Color unchangedColor = Color.white;
    [SerializeField] Color positiveColor = new Color(0.35f, 1f, 0.35f, 1f);
    [SerializeField] Color negativeColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] Color headerColor = new Color(1f, 0.84f, 0.25f, 1f);
    [SerializeField] Color shopBuffSourceColor = new Color(0.38f, 0.95f, 1f, 1f);
    [SerializeField] Color passiveSourceColor = new Color(0.54f, 1f, 0.46f, 1f);
    [SerializeField] Color runModBuffSourceColor = new Color(0.35f, 0.72f, 1f, 1f);
    [SerializeField] Color runModDebuffSourceColor = new Color(1f, 0.38f, 0.34f, 1f);
    [SerializeField] Color levelModSourceColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] Color bossAbilitySourceColor = new Color(0.86f, 0.45f, 1f, 1f);
    [SerializeField] Color starDifficultySourceColor = new Color(1f, 0.58f, 0.24f, 1f);
    [SerializeField] TMP_FontAsset gameplayStatsFont;
    [SerializeField, Min(12f)] float gameplayStatsFontSize = 24f;
    [SerializeField, Min(12f)] float gameplayStatsHeaderFontSize = 28f;
    [SerializeField, Min(140f)] float gameplayStatsColumnWidth = 380f;
    [SerializeField, Min(0f)] float gameplayStatsColumnSpacing = 16f;
    [SerializeField, Min(0f)] float gameplayStatsValueColumnInset = 36f;
    [SerializeField] Vector2 gameplayStatsShadowOffset = new Vector2(-2f, -2f);
    [SerializeField] Color gameplayStatsShadowColor = Color.black;
    [SerializeField] GameObject monsterUnitStatsPrefab;
    [SerializeField] bool refreshOnEnable = true;

    [Header("Prefab Bindings")]
    [SerializeField] RectTransform _statsContent;
    [SerializeField] RectTransform _monsterContent;
    [SerializeField] GameObject _statsView;
    [SerializeField] GameObject _monsterView;
    [SerializeField] Button _statsTabButton;
    [SerializeField] Button _monsterTabButton;
    [SerializeField] Button _closeButton;

    bool _built;
    bool _showingStatsTab = true;
    bool _statsUsePairGrid;
    bool _statsHasSection;

    public bool IsVisible => UIPanelTransition.IsVisible(gameObject);

    public static GameplayStatsPanelUI Create(GameController controller, Transform parent, GameObject monsterPrefab = null)
    {
        if (!parent)
            return null;

        RectTransform rt = AddRect("GameplayStats_Panel", parent);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1120f, 780f);

        var image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.025f, 0.94f);

        var panel = rt.gameObject.AddComponent<GameplayStatsPanelUI>();
        panel.Initialize(controller, monsterPrefab);
        UIPanelTransition.Hide(panel.gameObject, true);
        return panel;
    }

    public void Initialize(GameController controller, GameObject monsterPrefab = null)
    {
        if (controller)
            gameController = controller;
        else if (!gameController)
            gameController = FindFirstObjectByType<GameController>();

        if (monsterPrefab)
            monsterUnitStatsPrefab = monsterPrefab;

        if (!_built)
            BuildGeneratedUI();

        Refresh();
        ShowStatsTab();
    }

    void OnEnable()
    {
        TetrabeastsLocalization.LanguageChanged += OnLanguageChanged;

        if (_built && refreshOnEnable)
            Refresh();
    }

    void OnDisable()
    {
        TetrabeastsLocalization.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        if (_built && isActiveAndEnabled)
            Refresh();
    }

    public void Open(GameController controller = null)
    {
        Initialize(controller ? controller : gameController);
        transform.SetAsLastSibling();
        ShowStatsTab();
        UIPanelTransition.Show(gameObject);
    }

    public void Close(bool instant = false)
    {
        UIPanelTransition.Hide(gameObject, instant);
    }

    void CloseFromButton()
    {
        Close();
    }

    public void ShowStatsTab()
    {
        _showingStatsTab = true;
        if (_statsView) _statsView.SetActive(true);
        if (_monsterView) _monsterView.SetActive(false);
        SetTabButtonInteractable(_statsTabButton, false);
        SetTabButtonInteractable(_monsterTabButton, true);
    }

    public void ShowMonsterTab()
    {
        _showingStatsTab = false;
        if (_statsView) _statsView.SetActive(false);
        if (_monsterView) _monsterView.SetActive(true);
        SetTabButtonInteractable(_statsTabButton, true);
        SetTabButtonInteractable(_monsterTabButton, false);
    }

    public void Refresh()
    {
        if (!_built)
            BuildGeneratedUI();

        RebuildGameplayStats();
        RebuildMonsterStats();

        if (_showingStatsTab)
            ShowStatsTab();
        else
            ShowMonsterTab();
    }

    void BuildGeneratedUI()
    {
        if (TryBindExistingPrefabUI())
        {
            _built = true;
            ShowStatsTab();
            return;
        }

        _built = true;

        RectTransform root = (RectTransform)transform;
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        if (root.sizeDelta == Vector2.zero)
            root.sizeDelta = new Vector2(1120f, 780f);

        var bg = GetComponent<Image>();
        if (!bg)
            bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.025f, 0.94f);
        bg.raycastTarget = true;

        var header = AddRect("Header", root);
        Stretch(header, 24f, 24f, -76f, 24f);

        TMP_Text title = AddText("Title_Text", header, "Stats", 46f, FontStyles.Bold, TextAlignmentOptions.Left);
        Stretch(title.rectTransform, 0f, 360f, 0f, 0f);

        _closeButton = AddButton("Close_Button", header, "X", new Color(0.55f, 0.10f, 0.16f, 1f), () => Close());
        _closeButton.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0.5f);
        _closeButton.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.5f);
        _closeButton.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
        _closeButton.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        _closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 48f);

        RectTransform tabs = AddRect("Tabs", root);
        tabs.anchorMin = new Vector2(0f, 1f);
        tabs.anchorMax = new Vector2(0f, 1f);
        tabs.pivot = new Vector2(0f, 1f);
        tabs.anchoredPosition = new Vector2(24f, -82f);
        tabs.sizeDelta = new Vector2(440f, 52f);
        var tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 12f;
        tabsLayout.childControlWidth = false;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = false;
        tabsLayout.childForceExpandHeight = true;

        _statsTabButton = AddButton("StatsTab_Button", tabs, "Stats", new Color(0.29f, 0.44f, 0.68f, 1f), ShowStatsTab);
        _statsTabButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;

        _monsterTabButton = AddButton("MonsterTab_Button", tabs, "Monsters", new Color(0.58f, 0.36f, 0.16f, 1f), ShowMonsterTab);
        _monsterTabButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;

        _statsView = AddScrollView("Stats_ScrollView", root, out _statsContent).gameObject;
        _monsterView = AddScrollView("Monsters_ScrollView", root, out _monsterContent).gameObject;
        EnsureStatsContentLayout(_statsContent);

        ShowStatsTab();
    }

    bool TryBindExistingPrefabUI()
    {
        _statsView = _statsView ? _statsView : FindChildGameObject("GPStats_Panel");
        _monsterView = _monsterView ? _monsterView : FindChildGameObject("UnitStats_Panel");
        _statsContent = _statsContent ? _statsContent : FindChildRect("GPStats_Content");
        _monsterContent = _monsterContent ? _monsterContent : FindChildRect("UnitStats_Content");
        _statsTabButton = _statsTabButton ? _statsTabButton : FindChildButton("GPStatsTab_Button", "StatsTab_Button");
        _monsterTabButton = _monsterTabButton ? _monsterTabButton : FindChildButton("UnitsTab_Button", "MonsterTab_Button");
        _closeButton = _closeButton ? _closeButton : FindChildButton("Close_Button");

        if (!_statsView || !_monsterView || !_statsContent || !_monsterContent)
            return false;

        HookButton(_statsTabButton, ShowStatsTab);
        HookButton(_monsterTabButton, ShowMonsterTab);
        HookButton(_closeButton, CloseFromButton);

        EnsureStatsContentLayout(_statsContent);
        EnsureMonsterContentLayout(_monsterContent);
        ConfigureScrollRect(FindChildScrollRect("GPStats_ScrollView"), _statsContent);
        ConfigureScrollRect(FindChildScrollRect("UnitStats_ScrollView"), _monsterContent);

        return true;
    }

    void RebuildGameplayStats()
    {
        EnsureStatsContentLayout(_statsContent);
        ClearChildren(_statsContent);
        _statsHasSection = false;

        if (!gameController)
            gameController = FindFirstObjectByType<GameController>();

        if (!gameController)
        {
            AddInfoText(_statsContent, "No GameController found.");
            return;
        }

        GameController gc = gameController;
        LevelModifierSO levelModifier = gc.levelModifierController ? gc.levelModifierController.ActiveModifier : null;
        StarDifficultyModifiers stars = StarDifficultySystem.GetModifiers(gc.CurrentStarDifficulty);
        CastleData castle = gc.CurrentCastleDataForStats;
        MonsterPassiveBonuses passives = gc.PartyPassiveBonusesForStats;

        AddSection(_statsContent, "Run");
        AddTextStat(_statsContent, "Level", (gc.CurrentLevel + 1).ToString(), unchangedColor);
        AddComparedFloat(_statsContent, "Star Difficulty", gc.CurrentStarDifficulty, 0f, FormatWhole, FormatSignedWhole,
            higherIsBetter: false, sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0));
        AddComparedFloat(_statsContent, "Score Gain", stars.scoreGainMultiplier, 1f, FormatMultiplier, FormatSignedPercentDelta,
            higherIsBetter: true, sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0));
        AddComparedFloat(_statsContent, "EXP Gain", stars.expGainMultiplier * gc.CurrentPartyExperienceGainMultiplierForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.partyExperienceGainMultiplierAdd > Epsilon,
                starDifficulty: gc.CurrentStarDifficulty > 0));
        AddTextStat(_statsContent, "Level Modifier", levelModifier ? levelModifier.displayName : "None",
            levelModifier ? negativeColor : unchangedColor,
            sources: SourcesFor(levelMod: levelModifier != null));

        AddSection(_statsContent, "Reserves And Rewards");
        AddTextStat(_statsContent, "Reserve Units", $"{gc.CurrentReserveUnits} / {gc.EffectiveMaxUnitLivesForStats}",
            ColorForDelta(gc.EffectiveMaxUnitLivesForStats, gc.BaseMaxUnitLivesForStats, higherIsBetter: true),
            sources: SourcesForMaxReserve(passives));
        AddComparedFloat(_statsContent, "Max Reserve Units", gc.EffectiveMaxUnitLivesForStats, gc.BaseMaxUnitLivesForStats,
            FormatWhole, FormatSignedWhole, higherIsBetter: true, sources: SourcesForMaxReserve(passives));
        AddComparedFloat(_statsContent, "Reserve Restored On Win", gc.CurrentReinforcementsPerWinForStats,
            gc.BaseReinforcementsPerWinForStats, FormatWhole, FormatSignedWhole, higherIsBetter: true,
            sources: SourcesFor(passive: passives.reserveUnitsRestoredOnWin != 0,
                runModStats: new[] { RunModStat.ReserveUnitsRestoredOnWinAdd, RunModStat.DisableRoundWinReserveRestore }));
        AddComparedFloat(_statsContent, "Round Win Currency", gc.CurrentRoundWinCurrencyForStats, gc.BaseCurrencyPerRoundWinForStats,
            FormatWhole, FormatSignedWhole, higherIsBetter: true,
            sources: SourcesFor(passive: passives.currencyGainMultiplierAdd > Epsilon,
                runModStats: new[] { RunModStat.CurrencyPerRoundWin }));
        AddComparedFloat(_statsContent, "Line Clear Currency Chance", gc.EffectiveCurrencyChancePerClearedRowForStats,
            gc.BaseCurrencyChancePerClearedRowForStats, FormatPercent, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(shopBuff: ShopBuffEffects.GoldChanceBonus > Epsilon,
                starDifficulty: gc.PostFinalSurvivalStarCurrencyBonusActiveForStats,
                runModStats: new[] { RunModStat.LineClearCurrencyChanceAdd }));
        AddComparedFloat(_statsContent, "Line Clear Currency Amount", gc.LineClearCurrencyAmountMultiplierForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.currencyGainMultiplierAdd > Epsilon,
                runModStats: new[] { RunModStat.LineClearCurrencyAmountMult }));

        AddSection(_statsContent, "Monster Combat");
        AddComparedFloat(_statsContent, "Monster Damage", gc.MonsterDamageOutputMultiplierForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.allyMonsterDamageDoneReduction > Epsilon,
                levelMod: levelModifier && levelModifier.kind == LevelModifierKind.DoubleDamage,
                runModStats: new[] { RunModStat.MonsterDamageMult }));
        AddComparedFloat(_statsContent, "Monster Special Gain", gc.MonsterSpecialGaugeGainMultiplierForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0,
                levelMod: gc.SpecialUsageLockedForStats,
                runModStats: new[] { RunModStat.MonsterSpecialGainMult }));
        AddComparedFloat(_statsContent, "Monster Max HP", gc.monsterMaxHpMult, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(runModStats: new[] { RunModStat.MonsterMaxHpMult }));
        AddComparedFloat(_statsContent, "Healing Power", gc.healPowerMult, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(runModStats: new[] { RunModStat.HealPowerMult }));
        AddComparedFloat(_statsContent, "Healing Range Bonus", gc.healRangeAdd, 0f,
            FormatSignedWhole, FormatSignedWhole, higherIsBetter: true,
            sources: SourcesFor(runModStats: new[] { RunModStat.HealRangeAdd }));
        AddComparedFloat(_statsContent, "Ally Damage Dealt", gc.AllyMonsterOutgoingDamageMultiplierForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.allyMonsterDamageDoneReduction > Epsilon));
        AddComparedFloat(_statsContent, "Ally Damage Taken", gc.AllyMonsterDamageTakenMultiplier, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: false,
            sources: SourcesFor(passive: passives.allyMonsterDamageTakenReduction > Epsilon));

        AddSection(_statsContent, "Combo And Passives");
        AddComparedFloat(_statsContent, "Combo Window", gc.CurrentComboWindowSecondsForStats,
            gc.BaseComboWindowSecondsForStats, FormatSeconds, FormatSignedSeconds, higherIsBetter: true,
            sources: SourcesFor(passive: passives.comboDurationSeconds > Epsilon));
        AddComparedFloat(_statsContent, "Bonus Combo Chance", passives.bonusComboChance, 0f,
            FormatPercent, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.bonusComboChance > Epsilon));
        AddComparedFloat(_statsContent, "Stone Buff Drop Chance", gc.CurrentStoneBuffDropChanceForStats,
            gc.BaseStoneBuffDropChanceForStats, FormatPercent, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.stoneBuffDropChance > Epsilon,
                runModStats: new[] { RunModStat.StoneBuffDropChanceAdd }));
        AddComparedFloat(_statsContent, "Starting Reserve Passive", passives.startingReserveUnits, 0f,
            FormatSignedWhole, FormatSignedWhole, higherIsBetter: true,
            sources: SourcesFor(passive: passives.startingReserveUnits != 0));
        AddComparedFloat(_statsContent, "Round Win Reserve Passive", passives.reserveUnitsRestoredOnWin, 0f,
            FormatSignedWhole, FormatSignedWhole, higherIsBetter: true,
            sources: SourcesFor(passive: passives.reserveUnitsRestoredOnWin != 0));
        AddComparedFloat(_statsContent, "Currency Gain Passive", passives.currencyGainMultiplierAdd, 0f,
            FormatPercent, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.currencyGainMultiplierAdd > Epsilon));
        AddComparedFloat(_statsContent, "Party EXP Passive", passives.partyExperienceGainMultiplierAdd, 0f,
            FormatPercent, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(passive: passives.partyExperienceGainMultiplierAdd > Epsilon));

        AddSection(_statsContent, "Enemy");
        AddComparedFloat(_statsContent, "Enemy Castle HP", gc.enemyCastleHpMult * stars.enemyHealthMultiplier, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: false,
            sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0,
                runModStats: new[] { RunModStat.EnemyCastleHpMult }));
        AddComparedFloat(_statsContent, "Enemy Damage", gc.enemyProjectileDamageMult * stars.enemyDamageMultiplier, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: false,
            sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0,
                runModStats: new[] { RunModStat.EnemyProjectileDamageMult }));
        AddComparedFloat(_statsContent, "Enemy Attack Interval", gc.enemyAttackIntervalMult, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(runModStats: new[] { RunModStat.EnemyAttackIntervalMult }));
        AddComparedFloat(_statsContent, "Enemy Projectile Speed", gc.enemyProjectileSpeedMult, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: false,
            sources: SourcesFor(runModStats: new[] { RunModStat.EnemyProjectileSpeedMult }));
        if (castle)
        {
            AddComparedFloat(_statsContent, "Castle Projectile Damage", gc.CastleProjectileDamageForStats,
                Mathf.Max(1f, castle.projectileDamage), FormatWhole, FormatSignedWhole, higherIsBetter: false,
                sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0,
                    runModStats: new[] { RunModStat.EnemyProjectileDamageMult }));
            AddComparedFloat(_statsContent, "Castle Attack Interval", gc.castleAttackInterval,
                Mathf.Max(0.1f, castle.projectileInterval), FormatSeconds, FormatSignedSeconds, higherIsBetter: true,
                sources: SourcesFor(runModStats: new[] { RunModStat.EnemyAttackIntervalMult }));
        }

        AddSection(_statsContent, "Piece And Special");
        AddComparedFloat(_statsContent, "Piece Gravity", gc.EffectivePieceGravityMultForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: false,
            sources: SourcesFor(shopBuff: !Approximately(ShopBuffEffects.GravityMultiplier, 1f),
                levelMod: gc.LevelModifierGravitySlowActiveForStats,
                bossAbility: gc.BossGravityActiveForStats,
                runModStats: new[] { RunModStat.PieceGravityMult }));
        AddComparedFloat(_statsContent, "Gravity Ramp Rate", gc.EffectiveFallRampRateMultForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: false,
            sources: SourcesFor(shopBuff: !Approximately(ShopBuffEffects.VelocityMultiplier, 1f),
                starDifficulty: gc.CurrentStarDifficulty > 0,
                levelMod: gc.LevelModifierGravitySlowActiveForStats,
                runModStats: new[] { RunModStat.FallRampRateMult }));
        AddComparedFloat(_statsContent, "Special Block Chance", gc.CurrentSpecialBlockChanceForStats,
            gc.BaseSpecialChancePerEnqueueForStats, FormatPercent, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(levelMod: gc.SpecialBlocksLockedForStats,
                runModStats: new[] { RunModStat.SpecialBlockChanceAdd }));
        AddComparedFloat(_statsContent, "Commander Special Gain", gc.EffectiveSpecialGaugeGainMultiplierForStats, 1f,
            FormatMultiplier, FormatSignedPercentDelta, higherIsBetter: true,
            sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0,
                levelMod: gc.SpecialUsageLockedForStats));
        AddComparedFloat(_statsContent, "Special Gauge Gain Per Second", gc.SpecialGaugeGainPerSecondForStats, 0f,
            FormatPerSecond, FormatSignedPerSecond, higherIsBetter: true,
            sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0 && gc.specialGaugeGainPerSecond > Epsilon,
                levelMod: gc.SpecialUsageLockedForStats,
                runModStats: new[] { RunModStat.SpecialGaugeGainPerSecond }));
        AddComparedFloat(_statsContent, "Special Gauge Drain Per Second", gc.SpecialGaugeDrainPerSecondForStats, 0f,
            FormatPerSecond, FormatSignedPerSecond, higherIsBetter: false,
            sources: SourcesFor(runModStats: new[] { RunModStat.SpecialGaugeDrainPerSecond }));
        AddComparedBool(_statsContent, "Next Preview Disabled", gc.disableNextPreview, false, trueIsPositive: false,
            sources: SourcesFor(runModStats: new[] { RunModStat.DisableNextPreview }));
        AddComparedBool(_statsContent, "Landing Hint Disabled", gc.disableLandingHint, false, trueIsPositive: false,
            sources: SourcesFor(runModStats: new[] { RunModStat.DisableLandingHint }));
        AddComparedBool(_statsContent, "Special Usage Locked", gc.SpecialUsageLockedForStats, false, trueIsPositive: false,
            sources: SourcesFor(levelMod: gc.SpecialUsageLockedForStats));
        AddComparedBool(_statsContent, "Special Blocks Blocked", gc.SpecialBlocksLockedForStats, false, trueIsPositive: false,
            sources: SourcesFor(levelMod: gc.SpecialBlocksLockedForStats));

        AddSection(_statsContent, "Run Modifier Drops");
        AddComparedBool(_statsContent, "Stone Drops Debuffs Only", gc.stoneObstacleDropsDebuffsOnly, false, trueIsPositive: false,
            sources: SourcesFor(runModStats: new[] { RunModStat.StoneObstacleDropsDebuffsOnly }));
        AddComparedFloat(_statsContent, "Luck", gc.EffectiveLuckForStats, 0f, FormatSignedWhole, FormatSignedWhole,
            higherIsBetter: true,
            sources: SourcesFor(shopBuff: ShopBuffEffects.LuckBonus != 0, runModStats: new[] { RunModStat.LuckAdd }));
        AddComparedFloat(_statsContent, "Misfortune", gc.CurrentMisfortune, 0f, FormatSignedWhole, FormatSignedWhole,
            higherIsBetter: false,
            sources: SourcesFor(starDifficulty: gc.CurrentStarDifficulty > 0,
                runModStats: new[] { RunModStat.MisfortuneAdd }));

        AddLevelModifierSection(levelModifier);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_statsContent);
    }

    void AddLevelModifierSection(LevelModifierSO modifier)
    {
        AddSection(_statsContent, "Active Level Modifier");

        if (!modifier)
        {
            AddTextStat(_statsContent, "Effect", "None", unchangedColor);
            return;
        }

        StatSource[] levelModSource = SourcesFor(levelMod: true);

        switch (modifier.kind)
        {
            case LevelModifierKind.DoubleDamage:
                AddComparedFloat(_statsContent, "Outgoing Damage", modifier.dealtDamageMultiplier, 1f, FormatMultiplier,
                    FormatSignedPercentDelta, true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Incoming Damage", modifier.receivedDamageMultiplier, 1f, FormatMultiplier,
                    FormatSignedPercentDelta, false, sources: levelModSource);
                break;

            case LevelModifierKind.Overgrowth:
                AddComparedFloat(_statsContent, "Overgrowth Target Interval", modifier.overgrowthTargetInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Initial Target Rows", modifier.overgrowthInitialTargetRows, 0f, FormatWhole,
                    FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Partial Growth Time", modifier.overgrowthPartialSeconds, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Full Growth Time", modifier.overgrowthFullSeconds, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.StormyWeather:
                AddComparedFloat(_statsContent, "Storm Strike Damage", modifier.stormInitialStrikeDamage, 0f, FormatNumber,
                    FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Storm Floor Tick Damage", modifier.stormFloorTickDamage, 0f, FormatNumber,
                    FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Storm Floor Duration", modifier.stormFloorDuration, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.RearAmbush:
                AddComparedFloat(_statsContent, "Rear Ambush Interval", modifier.rearAmbushInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.LowRations:
                AddComparedFloat(_statsContent, "Rations Tick Interval", modifier.lowRationsTickInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Low Reserve Damage", modifier.lowRationsNegligibleDamage, 0f, FormatNumber,
                    FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "High Reserve Damage", modifier.lowRationsDamageAtMaxReserve, 0f, FormatNumber,
                    FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.Contagion:
                AddComparedFloat(_statsContent, "Infection Chance", modifier.contagionInfectionChanceOnPieceSet, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Damage Per Tick", modifier.contagionPercentDamagePerTick, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Damage Increase Per Tick", modifier.contagionPercentIncreasePerTick, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Spread Chance", modifier.contagionSpreadChance, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.SpecialLock:
                AddComparedBool(_statsContent, "Special Gauge Gain", true, false, trueIsPositive: false, displayOverride: "Locked",
                    sources: levelModSource);
                break;

            case LevelModifierKind.DeathExplosion:
                AddComparedFloat(_statsContent, "Death Explosion Damage", modifier.deathExplosionMaxHpPercent, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.Swamp:
                AddComparedFloat(_statsContent, "Swamp Poison Damage", modifier.swampPoisonDamage, 0f, FormatNumber,
                    FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Swamp Poison Interval", modifier.swampPoisonInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.AutoRotate:
                AddComparedBool(_statsContent, "Manual Rotation", false, true, trueIsPositive: true, displayOverride: "Blocked",
                    sources: levelModSource);
                AddComparedFloat(_statsContent, "Auto Rotate Interval", modifier.autoRotateInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Base Gravity", LevelModifierController.AutoMovementGravityMultiplier, 1f,
                    FormatMultiplier, FormatSignedPercentDelta, false, sources: levelModSource);
                AddComparedFloat(_statsContent, "Gravity Ramp Rate", LevelModifierController.AutoMovementGravityMultiplier, 1f,
                    FormatMultiplier, FormatSignedPercentDelta, false, sources: levelModSource);
                break;

            case LevelModifierKind.AutoShift:
                AddComparedBool(_statsContent, "Manual Horizontal Shift", false, true, trueIsPositive: true, displayOverride: "Blocked",
                    sources: levelModSource);
                AddComparedFloat(_statsContent, "Auto Shift Interval", modifier.autoShiftInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Base Gravity", LevelModifierController.AutoMovementGravityMultiplier, 1f,
                    FormatMultiplier, FormatSignedPercentDelta, false, sources: levelModSource);
                AddComparedFloat(_statsContent, "Gravity Ramp Rate", LevelModifierController.AutoMovementGravityMultiplier, 1f,
                    FormatMultiplier, FormatSignedPercentDelta, false, sources: levelModSource);
                break;

            case LevelModifierKind.ComboGated:
                AddComparedFloat(_statsContent, "Combo Threshold", modifier.comboThreshold, 0f, FormatWhole,
                    FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Below Threshold Damage", modifier.comboBelowThresholdDamageMultiplier, 1f, FormatMultiplier,
                    FormatSignedPercentDelta, true, sources: levelModSource);
                AddComparedFloat(_statsContent, "At Threshold Damage", modifier.comboAtOrAboveThresholdDamageMultiplier, 1f, FormatMultiplier,
                    FormatSignedPercentDelta, true, sources: levelModSource);
                break;

            case LevelModifierKind.ComboShield:
                AddComparedFloat(_statsContent, "Shield Combo Threshold", modifier.comboShieldThreshold, 0f, FormatWhole,
                    FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Blocked Damage", modifier.comboShieldBlockedDamageMultiplier, 1f, FormatMultiplier,
                    FormatSignedPercentDelta, true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Shield Count", modifier.comboShieldCount, 0f, FormatWhole,
                    FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.NoSpecialPieces:
                AddComparedBool(_statsContent, "Special Pieces", false, true, trueIsPositive: true, displayOverride: "Blocked",
                    sources: levelModSource);
                break;

            case LevelModifierKind.HalfHealthStart:
                AddComparedFloat(_statsContent, "Starting Monster Health", modifier.startingHealthFraction, 1f, FormatMultiplier,
                    FormatSignedPercentDelta, true, sources: levelModSource);
                break;

            case LevelModifierKind.SoulLink:
                AddTextStat(_statsContent, "Monster Damage Sharing", "Active", negativeColor, sources: levelModSource);
                break;

            case LevelModifierKind.PiercingShot:
                AddComparedBool(_statsContent, "Enemy Projectiles", true, false, trueIsPositive: false,
                    displayOverride: "Pierce Monsters", sources: levelModSource);
                AddTextStat(_statsContent, "Projectile Stops On", "Defense Units", negativeColor, sources: levelModSource);
                break;

            case LevelModifierKind.Disarray:
                AddComparedFloat(_statsContent, "Trimino Spawn Chance", modifier.disarrayTriminoChance, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Pentamino Spawn Chance", modifier.disarrayPentaminoChance, 0f, FormatPercent,
                    FormatSignedPercentDelta, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.OutFlanked:
                AddTextStat(_statsContent, "Arrow Interval",
                    $"{FormatSeconds(Mathf.Min(modifier.outFlankedIntervalMin, modifier.outFlankedIntervalMax))} to {FormatSeconds(Mathf.Max(modifier.outFlankedIntervalMin, modifier.outFlankedIntervalMax))}",
                    negativeColor, sources: levelModSource);
                AddComparedFloat(_statsContent, "Rows Per Wave", modifier.outFlankedRowsPerWave, 0f, FormatWhole,
                    FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Arrow Damage", gameController ? gameController.CastleProjectileDamageForStats : 0f, 0f,
                    FormatNumber, FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.VolcanicEruption:
                AddTextStat(_statsContent, "Eruption Interval",
                    $"{FormatSeconds(Mathf.Min(modifier.volcanicEruptionIntervalMin, modifier.volcanicEruptionIntervalMax))} to {FormatSeconds(Mathf.Max(modifier.volcanicEruptionIntervalMin, modifier.volcanicEruptionIntervalMax))}",
                    negativeColor, sources: levelModSource);
                AddComparedFloat(_statsContent, "Warning Time", modifier.volcanicEruptionWarningSeconds, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Cells Targeted", modifier.volcanicEruptionTargetCount, 0f, FormatWhole,
                    FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Top Rows Excluded", modifier.volcanicEruptionExcludedTopRows, 0f, FormatWhole,
                    FormatSignedWhole, true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Eruption Damage", gameController ? gameController.CastleProjectileDamageForStats : 0f, 0f,
                    FormatNumber, FormatSignedNumber, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.Roulette:
                AddComparedFloat(_statsContent, "Preview Change Interval", modifier.roulettePreviewInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                break;

            case LevelModifierKind.SoftSpace:
                AddComparedFloat(_statsContent, "Teleport Spawn Interval", modifier.softSpaceTeleportInterval, 0f, FormatSeconds,
                    FormatSignedSeconds, false, forceNegative: true, sources: levelModSource);
                AddComparedFloat(_statsContent, "Destination Top Rows Excluded", modifier.softSpaceTeleportDestinationExcludedTopRows, 0f,
                    FormatWhole, FormatSignedWhole, true, sources: levelModSource);
                break;

            case LevelModifierKind.TurboBoosted:
                AddComparedFloat(_statsContent, "Zip Pad Row From Top", modifier.turboBoostedRowFromTop, 0f,
                    FormatWhole, FormatSignedWhole, false, forceNegative: true, sources: levelModSource);
                break;

            default:
                AddTextStat(_statsContent, "Effect", modifier.description, negativeColor, sources: levelModSource);
                break;
        }
    }

    void RebuildMonsterStats()
    {
        ClearChildren(_monsterContent);

        if (!gameController)
            gameController = FindFirstObjectByType<GameController>();

        if (!gameController)
        {
            AddInfoText(_monsterContent, "No GameController found.");
            return;
        }

        List<MonsterData> roster = gameController.GetActiveMonsterRosterSnapshot();
        if (roster == null || roster.Count == 0)
        {
            AddInfoText(_monsterContent, "No monsters in roster.");
            return;
        }

        bool usingMonsterPrefab = monsterUnitStatsPrefab != null;
        if (!usingMonsterPrefab)
            AddSection(_monsterContent, "Roster");

        for (int i = 0; i < roster.Count; i++)
        {
            MonsterData monster = roster[i];
            if (!monster)
                continue;

            AddMonsterCard(monster);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_monsterContent);
    }

    void AddMonsterCard(MonsterData monster)
    {
        int level = RunMonsterProgress.GetCurrentLevel(monster.monsterName);
        MonsterLeveling.LeveledStats leveled = MonsterLeveling.GetLeveledStats(monster, level);

        float baseHp = leveled.maxHealth;
        float currentHp = (leveled.maxHealth + ShopBuffEffects.MonsterHpBonus) * Mathf.Max(0f, gameController.monsterMaxHpMult);
        float startingHp = currentHp;
        LevelModifierSO modifier = gameController.levelModifierController ? gameController.levelModifierController.ActiveModifier : null;
        if (modifier && modifier.kind == LevelModifierKind.HalfHealthStart)
            startingHp *= Mathf.Clamp01(modifier.startingHealthFraction);

        float baseAttack = leveled.attackPower;
        float currentAttack = (leveled.attackPower + ShopBuffEffects.MonsterAttackBonus) * gameController.MonsterDamageOutputMultiplierForStats;

        float baseSpecial = leveled.specialGaugeGain;
        float currentSpecial = leveled.specialGaugeGain * gameController.MonsterSpecialGaugeGainMultiplierForStats;

        float baseHeal = leveled.healAmount;
        float currentHeal = leveled.healAmount > 0f
            ? (leveled.healAmount + ShopBuffEffects.MonsterHealBonus) * Mathf.Max(0f, gameController.healPowerMult)
            : 0f;

        float baseRange = leveled.healRange;
        float currentRange = leveled.healRange + gameController.healRangeAdd;

        Sprite portrait = MonsterSkinStore.GetPortrait(monster, MonsterSkinStore.GetValidSelected(monster));
        if (monsterUnitStatsPrefab)
        {
            GameObject entry = Instantiate(monsterUnitStatsPrefab, _monsterContent);
            entry.name = $"{monster.monsterName}_Stats";
            var monsterStatsUI = entry.GetComponent<MonsterUnitStatsUI>();
            if (!monsterStatsUI)
                monsterStatsUI = entry.AddComponent<MonsterUnitStatsUI>();

            monsterStatsUI.Bind(monster, portrait, level,
                BuildMonsterStatsColumn1(monster, currentHp, startingHp, currentAttack, currentSpecial, level),
                BuildMonsterStatsColumn2(monster, currentHeal, currentRange, leveled.healSpeed));
            return;
        }

        RectTransform card = AddRect($"{monster.monsterName}_Card", _monsterContent);
        var image = card.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.10f, 0.92f);

        var layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 18f;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        var cardElement = card.gameObject.AddComponent<LayoutElement>();
        cardElement.preferredHeight = 196f;
        cardElement.flexibleWidth = 1f;

        AddMonsterPortrait(card, monster);

        RectTransform details = AddRect("Details", card);
        var detailsLayoutElement = details.gameObject.AddComponent<LayoutElement>();
        detailsLayoutElement.flexibleWidth = 1f;

        var detailsLayout = details.gameObject.AddComponent<VerticalLayoutGroup>();
        detailsLayout.spacing = 5f;
        detailsLayout.childControlWidth = true;
        detailsLayout.childControlHeight = true;
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = false;

        TMP_Text name = AddText("Name_Text", details, $"{monster.monsterName}  Lv. {level}", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        name.color = unchangedColor;
        name.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        string passiveName = MonsterPassiveSystem.GetPassiveName(monster);
        string passive = string.IsNullOrWhiteSpace(passiveName)
            ? "Passive: None"
            : $"Passive: {passiveName} Lv. {GetPassiveTierLevel(level)}";
        TMP_Text passiveText = AddText("Passive_Text", details, passive, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        passiveText.color = string.IsNullOrWhiteSpace(passiveName) ? unchangedColor : positiveColor;
        passiveText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        RectTransform stats = AddRect("Stats", details);
        var statsLayout = stats.gameObject.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 2f;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = false;
        stats.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        AddCompactMonsterStat(stats, "Max HP", currentHp, baseHp, FormatNumber, true);
        if (!Approximately(startingHp, currentHp))
            AddCompactMonsterStat(stats, "Starting HP", startingHp, currentHp, FormatNumber, true);
        AddCompactMonsterStat(stats, "Attack", currentAttack, baseAttack, FormatNumber, true);
        AddCompactMonsterStat(stats, "Special Gain", currentSpecial, baseSpecial, FormatNumber, true);
        if (monster.role == MonsterRole.Healer)
        {
            AddCompactMonsterStat(stats, "Heal Power", currentHeal, baseHeal, FormatNumber, true);
            AddCompactMonsterStat(stats, "Heal Range", currentRange, baseRange, FormatNumber, true);
            AddTextStat(stats, "Heal Speed", FormatSeconds(leveled.healSpeed), unchangedColor, compact: true);
        }
        else
        {
            AddTextStat(stats, "Heal Power", "-", unchangedColor, compact: true);
            AddTextStat(stats, "Heal Range", "-", unchangedColor, compact: true);
            AddTextStat(stats, "Heal Speed", "-", unchangedColor, compact: true);
        }
        AddTextStat(stats, "Spawn Weight", FormatNumber(monster.weightedSpawnRate), unchangedColor, compact: true);
    }

    void AddMonsterPortrait(RectTransform parent, MonsterData monster)
    {
        RectTransform portraitRoot = AddRect("Portrait", parent);
        var portraitLayout = portraitRoot.gameObject.AddComponent<LayoutElement>();
        portraitLayout.preferredWidth = 136f;
        portraitLayout.preferredHeight = 136f;

        var portraitBg = portraitRoot.gameObject.AddComponent<Image>();
        portraitBg.color = new Color(0.015f, 0.015f, 0.02f, 1f);
        portraitBg.raycastTarget = false;

        Sprite portraitSprite = MonsterSkinStore.GetPortrait(monster, MonsterSkinStore.GetValidSelected(monster));
        Image portrait = AddRect("Portrait_Image", portraitRoot).gameObject.AddComponent<Image>();
        Stretch(portrait.rectTransform, 8f);
        portrait.sprite = portraitSprite;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
    }

    string BuildMonsterStatsColumn1(MonsterData monster, float currentHp, float startingHp, float currentAttack,
        float currentSpecial, int level)
    {
        string passiveName = MonsterPassiveSystem.GetPassiveName(monster);
        string passive = string.IsNullOrWhiteSpace(passiveName) ? "None" : passiveName;
        string hp = Approximately(startingHp, currentHp)
            ? FormatNumber(currentHp)
            : $"{FormatNumber(startingHp)}/{FormatNumber(currentHp)}";

        return
            $"Role: {FormatRole(monster.role)}\n" +
            $"HP: {hp}\n" +
            $"Attack: {FormatNumber(currentAttack)}\n" +
            $"Special Rate: {FormatNumber(currentSpecial)}\n" +
            $"Passive: {passive}\n" +
            $"Passive Lv: {GetPassiveTierLevel(level)}";
    }

    string BuildMonsterStatsColumn2(MonsterData monster, float currentHeal, float currentRange, float healSpeed)
    {
        if (!monster || monster.role != MonsterRole.Healer)
        {
            return
                "Heal Power: -\n" +
                "Heal Range: -\n" +
                "Heal Speed: -";
        }

        return
            $"Heal Power: {FormatNumber(currentHeal)}\n" +
            $"Heal Range: {FormatNumber(currentRange)}\n" +
            $"Heal Speed: {FormatSeconds(healSpeed)}";
    }

    ScrollRect AddScrollView(string name, RectTransform parent, out RectTransform content)
    {
        RectTransform scrollRt = AddRect(name, parent);
        Stretch(scrollRt, 24f, 24f, 24f, 146f);

        var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 32f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = AddRect("Viewport", scrollRt);
        Stretch(viewport, 0f, 22f, 0f, 0f);
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.20f);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = AddRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform scrollbarRt = AddRect("Scrollbar", scrollRt);
        scrollbarRt.anchorMin = new Vector2(1f, 0f);
        scrollbarRt.anchorMax = new Vector2(1f, 1f);
        scrollbarRt.pivot = new Vector2(1f, 0.5f);
        scrollbarRt.anchoredPosition = Vector2.zero;
        scrollbarRt.sizeDelta = new Vector2(18f, 0f);
        var scrollbarBg = scrollbarRt.gameObject.AddComponent<Image>();
        scrollbarBg.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
        var scrollbar = scrollbarRt.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform handleArea = AddRect("Sliding Area", scrollbarRt);
        Stretch(handleArea, 3f);

        RectTransform handle = AddRect("Handle", handleArea);
        Stretch(handle, 0f);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(0.82f, 0.82f, 0.90f, 0.95f);
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = -3f;

        return scroll;
    }

    void AddSection(RectTransform parent, string title)
    {
        string localizedTitle = TetrabeastsLocalization.LocalizeText(title);

        if (UsesStatsPairGrid(parent))
        {
            if (_statsHasSection)
                AddStatsSpacerRow(parent);
            _statsHasSection = true;

            AddStatsGridCell(parent, $"{title}_HeaderLabel", localizedTitle, headerColor, gameplayStatsHeaderFontSize, FontStyles.Bold,
                allowOverflow: true);
            AddStatsGridCell(parent, $"{title}_HeaderValue", string.Empty, headerColor, gameplayStatsHeaderFontSize, FontStyles.Bold);
            AddStatsGridCell(parent, $"{title}_HeaderSource", string.Empty, headerColor, gameplayStatsHeaderFontSize, FontStyles.Bold);
            return;
        }

        TMP_Text text = AddText($"{title}_Header", parent, localizedTitle, 26f, FontStyles.Bold, TextAlignmentOptions.Left);
        text.color = headerColor;
        ApplyGameplayStatsFont(text, gameplayStatsHeaderFontSize);
        var layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 34f;
    }

    void AddInfoText(RectTransform parent, string body)
    {
        string localizedBody = TetrabeastsLocalization.LocalizeText(body);

        if (UsesStatsPairGrid(parent))
        {
            AddStatsGridCell(parent, "Info_Label", localizedBody, unchangedColor, gameplayStatsFontSize, FontStyles.Normal,
                allowOverflow: true);
            AddStatsGridCell(parent, "Info_Value", string.Empty, unchangedColor, gameplayStatsFontSize, FontStyles.Normal);
            AddStatsGridCell(parent, "Info_Source", string.Empty, unchangedColor, gameplayStatsFontSize, FontStyles.Normal);
            return;
        }

        TMP_Text text = AddText("Info_Text", parent, localizedBody, 24f, FontStyles.Normal, TextAlignmentOptions.Left);
        text.color = unchangedColor;
        ApplyGameplayStatsFont(text, gameplayStatsFontSize);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
    }

    void AddTextStat(RectTransform parent, string label, string value, Color color, bool compact = false,
        StatSource[] sources = null)
    {
        AddStatRow(parent, label, value, color, compact, sources);
    }

    void AddComparedFloat(RectTransform parent, string label, float current, float baseline, Func<float, string> formatter,
        bool higherIsBetter, bool forceNegative = false, StatSource[] sources = null)
    {
        AddComparedFloat(parent, label, current, baseline, formatter, FormatSignedNumber, higherIsBetter, forceNegative, sources);
    }

    void AddComparedFloat(RectTransform parent, string label, float current, float baseline, Func<float, string> formatter,
        Func<float, string> deltaFormatter, bool higherIsBetter, bool forceNegative = false, StatSource[] sources = null)
    {
        Color color = forceNegative ? negativeColor : ColorForDelta(current, baseline, higherIsBetter);
        string value = formatter(current);
        if (!Approximately(current, baseline))
            value += $" ({deltaFormatter(current - baseline)})";
        AddStatRow(parent, label, value, color, sources: sources);
    }

    void AddComparedBool(RectTransform parent, string label, bool current, bool baseline, bool trueIsPositive,
        string displayOverride = null, StatSource[] sources = null)
    {
        Color color = ColorForBoolDelta(current, baseline, trueIsPositive);
        string value = string.IsNullOrEmpty(displayOverride) ? (current ? "Yes" : "No") : displayOverride;
        AddStatRow(parent, label, value, color, sources: sources);
    }

    void AddCompactMonsterStat(RectTransform parent, string label, float current, float baseline, Func<float, string> formatter,
        bool higherIsBetter)
    {
        Color color = ColorForDelta(current, baseline, higherIsBetter);
        string value = formatter(current);
        if (!Approximately(current, baseline))
            value += $" ({FormatSignedNumber(current - baseline)})";
        AddStatRow(parent, label, value, color, compact: true);
    }

    void AddStatRow(RectTransform parent, string label, string value, Color color, bool compact = false,
        StatSource[] sources = null)
    {
        string localizedLabel = TetrabeastsLocalization.LocalizeText(label);
        string localizedValue = TetrabeastsLocalization.LocalizeText(value);

        if (UsesStatsPairGrid(parent))
        {
            AddStatsGridCell(parent, $"{label}_Label", localizedLabel, color, gameplayStatsFontSize, FontStyles.Normal);
            AddStatsGridCell(parent, $"{label}_Value", localizedValue, color, gameplayStatsFontSize, FontStyles.Normal);
            AddStatsGridCell(parent, $"{label}_Source", BuildSourceRichText(sources), unchangedColor,
                gameplayStatsFontSize, FontStyles.Normal, shadowBody: BuildSourcePlainText(sources), allowOverflow: true);
            return;
        }

        RectTransform row = AddRect($"{label}_Row", parent);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(compact ? 0 : 8, compact ? 0 : 8, 0, 0);
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        var rowElement = row.gameObject.AddComponent<LayoutElement>();
        rowElement.preferredHeight = compact ? 24f : 32f;

        TMP_Text labelText = AddText("Label", row, localizedLabel, compact ? 18f : 21f, FontStyles.Normal, TextAlignmentOptions.Left);
        labelText.color = color;
        ApplyGameplayStatsFont(labelText, compact ? 18f : gameplayStatsFontSize);
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = compact ? 0.9f : 1.1f;

        TMP_Text valueText = AddText("Value", row, localizedValue, compact ? 18f : 21f, FontStyles.Normal, TextAlignmentOptions.Left);
        valueText.color = color;
        ApplyGameplayStatsFont(valueText, compact ? 18f : gameplayStatsFontSize);
        valueText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    bool UsesStatsPairGrid(RectTransform parent)
    {
        return _statsUsePairGrid && parent == _statsContent;
    }

    void AddStatsSpacerRow(RectTransform parent)
    {
        AddStatsGridCell(parent, "SectionSpacer_Label", string.Empty, unchangedColor, gameplayStatsFontSize, FontStyles.Normal);
        AddStatsGridCell(parent, "SectionSpacer_Value", string.Empty, unchangedColor, gameplayStatsFontSize, FontStyles.Normal);
        AddStatsGridCell(parent, "SectionSpacer_Source", string.Empty, unchangedColor, gameplayStatsFontSize, FontStyles.Normal);
    }

    void AddStatsGridCell(RectTransform parent, string name, string body, Color color, float size, FontStyles style,
        bool allowOverflow = false, string shadowBody = null)
    {
        RectTransform cell = AddRect(name, parent);
        cell.anchorMin = new Vector2(0f, 1f);
        cell.anchorMax = new Vector2(0f, 1f);
        cell.pivot = new Vector2(0f, 1f);

        if (parent && parent.TryGetComponent(out GridLayoutGroup grid))
            cell.sizeDelta = grid.cellSize;

        TextOverflowModes overflow = allowOverflow ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        float leftInset = IsStatsValueCell(name) ? gameplayStatsValueColumnInset : 0f;

        TMP_Text shadow = AddStatsCellText("Shadow_Text", cell, shadowBody ?? body, gameplayStatsShadowColor, size, style, overflow);
        StretchWithInsetAndOffset(shadow.rectTransform, leftInset, gameplayStatsShadowOffset);

        TMP_Text text = AddStatsCellText("Text", cell, body, color, size, style, overflow);
        Stretch(text.rectTransform, leftInset, 0f, 0f, 0f);
    }

    TMP_Text AddStatsCellText(string name, RectTransform parent, string body, Color color, float size, FontStyles style,
        TextOverflowModes overflow)
    {
        TMP_Text text = AddText(name, parent, body, size, style, TextAlignmentOptions.Left);
        text.color = color;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = overflow;
        ApplyGameplayStatsFont(text, size);
        return text;
    }

    void ApplyGameplayStatsFont(TMP_Text text, float size)
    {
        if (!text)
            return;

        if (gameplayStatsFont)
            text.font = gameplayStatsFont;

        text.fontSize = size;
        text.enableAutoSizing = false;
        text.characterSpacing = 0f;
        text.wordSpacing = 0f;
    }

    Color ColorForDelta(float current, float baseline, bool higherIsBetter)
    {
        if (Approximately(current, baseline))
            return unchangedColor;

        bool improved = higherIsBetter ? current > baseline : current < baseline;
        return improved ? positiveColor : negativeColor;
    }

    Color ColorForBoolDelta(bool current, bool baseline, bool trueIsPositive)
    {
        if (current == baseline)
            return unchangedColor;

        bool improved = current ? trueIsPositive : !trueIsPositive;
        return improved ? positiveColor : negativeColor;
    }

    StatSource[] SourcesFor(bool shopBuff = false, bool passive = false, bool levelMod = false,
        bool bossAbility = false, bool starDifficulty = false, RunModStat[] runModStats = null)
    {
        var sources = new List<StatSource>(6);

        if (shopBuff) AddSource(sources, StatSource.ShopBuff);
        if (passive) AddSource(sources, StatSource.Passive);
        if (levelMod) AddSource(sources, StatSource.LevelMod);
        if (bossAbility) AddSource(sources, StatSource.BossAbility);
        if (starDifficulty) AddSource(sources, StatSource.StarDifficulty);

        if (runModStats != null && runModStats.Length > 0)
        {
            if (RunModsContain(RunModsStore.Buffs, runModStats))
                AddSource(sources, StatSource.RunModBuff);
            if (RunModsContain(RunModsStore.Debuffs, runModStats))
                AddSource(sources, StatSource.RunModDebuff);
        }

        return sources.Count == 0 ? null : sources.ToArray();
    }

    StatSource[] SourcesForMaxReserve(MonsterPassiveBonuses passives)
    {
        return SourcesFor(
            shopBuff: ShopBuffEffects.UnitLivesBonus != 0,
            passive: passives.startingReserveUnits != 0,
            runModStats: new[] { RunModStat.MaxReserveUnitsAdd });
    }

    static bool RunModsContain(List<RunModifierSO> mods, RunModStat[] stats)
    {
        if (mods == null || stats == null)
            return false;

        for (int i = 0; i < mods.Count; i++)
        {
            if (mods[i] is not RunModifier modifier)
                continue;

            for (int s = 0; s < stats.Length; s++)
            {
                if (modifier.stat == stats[s])
                    return true;
            }
        }

        return false;
    }

    static void AddSource(List<StatSource> sources, StatSource source)
    {
        if (sources == null || sources.Contains(source))
            return;

        sources.Add(source);
    }

    string BuildSourceRichText(StatSource[] sources)
    {
        if (sources == null || sources.Length == 0)
            return string.Empty;

        var parts = new List<string>(sources.Length);
        for (int i = 0; i < sources.Length; i++)
        {
            string label = SourceLabel(sources[i]);
            string color = ColorUtility.ToHtmlStringRGBA(SourceColor(sources[i]));
            parts.Add($"<color=#{color}>{label}</color>");
        }

        return string.Join(", ", parts);
    }

    string BuildSourcePlainText(StatSource[] sources)
    {
        if (sources == null || sources.Length == 0)
            return string.Empty;

        var parts = new List<string>(sources.Length);
        for (int i = 0; i < sources.Length; i++)
            parts.Add(SourceLabel(sources[i]));

        return string.Join(", ", parts);
    }

    string SourceLabel(StatSource source)
    {
        string label;
        switch (source)
        {
            case StatSource.ShopBuff:
                label = "Shop Buff";
                break;
            case StatSource.Passive:
                label = "Passive";
                break;
            case StatSource.RunModBuff:
                label = "Run Mod Buff";
                break;
            case StatSource.RunModDebuff:
                label = "Run Mod Debuff";
                break;
            case StatSource.LevelMod:
                label = "Level Mod";
                break;
            case StatSource.BossAbility:
                label = "Boss Ability";
                break;
            case StatSource.StarDifficulty:
                label = "Star Difficulty";
                break;
            default:
                label = string.Empty;
                break;
        }

        return TetrabeastsLocalization.LocalizeText(label);
    }

    Color SourceColor(StatSource source)
    {
        switch (source)
        {
            case StatSource.ShopBuff:
                return shopBuffSourceColor;
            case StatSource.Passive:
                return passiveSourceColor;
            case StatSource.RunModBuff:
                return runModBuffSourceColor;
            case StatSource.RunModDebuff:
                return runModDebuffSourceColor;
            case StatSource.LevelMod:
                return levelModSourceColor;
            case StatSource.BossAbility:
                return bossAbilitySourceColor;
            case StatSource.StarDifficulty:
                return starDifficultySourceColor;
            default:
                return unchangedColor;
        }
    }

    void SetTabButtonInteractable(Button button, bool interactable)
    {
        if (!button)
            return;

        button.interactable = interactable;
    }

    void HookButton(Button button, UnityAction action)
    {
        if (!button || action == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    static bool IsStatsValueCell(string name)
    {
        return !string.IsNullOrEmpty(name) && name.EndsWith("_Value", StringComparison.Ordinal);
    }

    void EnsureStatsContentLayout(RectTransform content)
    {
        if (!content)
            return;

        var grid = content.GetComponent<GridLayoutGroup>();
        if (!grid)
            grid = content.gameObject.AddComponent<GridLayoutGroup>();

        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout)
            layout.enabled = false;

        grid.enabled = true;
        grid.padding = new RectOffset(28, 28, 16, 16);
        grid.spacing = new Vector2(gameplayStatsColumnSpacing, 6f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(GetStatsGridCellWidth(content, grid), GetStatsGridCellHeight());

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _statsUsePairGrid = true;
    }

    float GetStatsGridCellWidth(RectTransform content, GridLayoutGroup grid)
    {
        float width = 0f;
        if (content.parent is RectTransform viewport)
            width = viewport.rect.width;

        if (width <= 1f)
            width = content.rect.width;

        if (width <= 1f)
            width = 1560f;

        float available = width - grid.padding.left - grid.padding.right - (grid.spacing.x * 2f);
        float maxPairedWidth = Mathf.Max(120f, available / 3f);
        return Mathf.Clamp(gameplayStatsColumnWidth, 120f, maxPairedWidth);
    }

    float GetStatsGridCellHeight()
    {
        return Mathf.Ceil(Mathf.Max(gameplayStatsFontSize, gameplayStatsHeaderFontSize) + 14f);
    }

    void EnsureMonsterContentLayout(RectTransform content)
    {
        if (!content)
            return;

        var grid = content.GetComponent<GridLayoutGroup>();
        if (grid)
        {
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, grid.constraintCount);
        }
        else
        {
            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (!layout)
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (!fitter)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void ConfigureScrollRect(ScrollRect scroll, RectTransform content)
    {
        if (!scroll || !content)
            return;

        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        if (scroll.scrollSensitivity <= 10f)
            scroll.scrollSensitivity = 32f;
    }

    GameObject FindChildGameObject(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child ? child.gameObject : null;
    }

    RectTransform FindChildRect(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child as RectTransform;
    }

    Button FindChildButton(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child ? child.GetComponent<Button>() : null;
    }

    ScrollRect FindChildScrollRect(params string[] names)
    {
        Transform child = FindChildTransform(names);
        return child ? child.GetComponent<ScrollRect>() : null;
    }

    Transform FindChildTransform(params string[] names)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform)
                continue;

            for (int n = 0; n < names.Length; n++)
            {
                if (child.name == names[n])
                    return child;
            }
        }

        return null;
    }

    static Button AddButton(string name, RectTransform parent, string label, Color background, Action onClick)
    {
        RectTransform rt = AddRect(name, parent);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = background;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
            button.onClick.AddListener(() => onClick());

        TMP_Text text = AddText("Label", rt, label, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 4f);
        text.color = Color.white;

        return button;
    }

    static TMP_Text AddText(string name, RectTransform parent, string body, float fontSize, FontStyles style,
        TextAlignmentOptions alignment)
    {
        RectTransform rt = AddRect(name, parent);
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = TetrabeastsLocalization.LocalizeText(body ?? string.Empty);
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
        return text;
    }

    static RectTransform AddRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent ? parent.gameObject.layer : 5;
        var rt = go.GetComponent<RectTransform>();
        if (parent)
            rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        Stretch(rt, inset, inset, inset, inset);
    }

    static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void StretchWithOffset(RectTransform rt, Vector2 offset)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offset;
        rt.offsetMax = offset;
    }

    static void StretchWithInsetAndOffset(RectTransform rt, float leftInset, Vector2 offset)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(leftInset + offset.x, offset.y);
        rt.offsetMax = offset;
    }

    static void ClearChildren(RectTransform parent)
    {
        if (!parent)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    static int GetPassiveTierLevel(int level)
    {
        if (level >= 50) return 3;
        if (level >= 25) return 2;
        if (level >= 10) return 1;
        return 0;
    }

    static string FormatRole(MonsterRole role)
    {
        switch (role)
        {
            case MonsterRole.Defense:
                return "Defender";
            case MonsterRole.Healer:
                return "Healer";
            default:
                return "Attack";
        }
    }

    static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) <= Epsilon;
    }

    static string FormatNumber(float value)
    {
        if (Mathf.Abs(value - Mathf.Round(value)) < 0.001f)
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }

    static string FormatSignedNumber(float value)
    {
        string formatted = FormatNumber(Mathf.Abs(value));
        if (Approximately(value, 0f))
            return "0";

        return value > 0f ? $"+{formatted}" : $"-{formatted}";
    }

    static string FormatWhole(float value)
    {
        return Mathf.RoundToInt(value).ToString();
    }

    static string FormatSignedWhole(float value)
    {
        int rounded = Mathf.RoundToInt(value);
        return rounded > 0 ? $"+{rounded}" : rounded.ToString();
    }

    static string FormatMultiplier(float value)
    {
        return FormatPercent(value);
    }

    static string FormatPercent(float value)
    {
        return $"{value * 100f:0.#}%";
    }

    static string FormatPerSecond(float value)
    {
        return $"{FormatNumber(value)}/s";
    }

    static string FormatSignedPerSecond(float value)
    {
        if (Approximately(value, 0f))
            return "0/s";

        string formatted = FormatNumber(Mathf.Abs(value));
        return value > 0f ? $"+{formatted}/s" : $"-{formatted}/s";
    }

    static string FormatSignedPercentDelta(float value)
    {
        if (Approximately(value, 0f))
            return "0%";

        string formatted = $"{Mathf.Abs(value) * 100f:0.#}%";
        return value > 0f ? $"+{formatted}" : $"-{formatted}";
    }

    static string FormatSeconds(float value)
    {
        return $"{value:0.##}s";
    }

    static string FormatSignedSeconds(float value)
    {
        if (Approximately(value, 0f))
            return "0s";

        string formatted = $"{Mathf.Abs(value):0.##}s";
        return value > 0f ? $"+{formatted}" : $"-{formatted}";
    }
}
