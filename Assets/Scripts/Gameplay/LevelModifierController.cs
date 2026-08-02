using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class LevelModifierController : MonoBehaviour
{
    public const float AutoMovementGravityMultiplier = 0.333334f;

    struct OvergrowthState
    {
        public float elapsed;
        public bool partialShown;
    }

    [SerializeField] LevelModifierSelectionUI selectionUI;
    [SerializeField] Board board;
    [SerializeField] Piece piece;
    [SerializeField] BattleLogUI battleLog;
    [SerializeField] Image backgroundOverrideImage;
    [SerializeField] RawImage animatedBackgroundDisplay;
    [SerializeField] VideoPlayer animatedBackgroundPlayer;
    [SerializeField] Sprite defaultBackgroundSprite;
    [SerializeField] GameObject volcanicEruptionAnimsRoot;
    [SerializeField] GameObject defaultBoardBordersRoot;

    [Header("Modifier UI")]
    [SerializeField] GameObject specialGaugeRoot;
    [SerializeField] TMP_Text activeModifierTitleText;

    [Header("Combo Shield UI")]
    [SerializeField] Image comboShieldImageA;
    [SerializeField] Image comboShieldImageB;
    [SerializeField] TMP_Text comboShieldCountTextA;
    [SerializeField] TMP_Text comboShieldCountTextB;
    [SerializeField] float comboShieldPulseScale = 1.18f;
    [SerializeField] float comboShieldPulseDuration = 0.18f;

    [Header("Auto Shift Tuning")]
    [SerializeField, Range(0.5f, 2f)] float autoShiftBaseSpeedMultiplier = 1.15f;
    [SerializeField, Range(0f, 1f)] float autoShiftGravityRampInfluence = 0.45f;

    [Header("Rear Ambush Tuning")]
    [SerializeField, Min(0)] int rearAmbushBaseRows = 3;
    [SerializeField, Min(1)] int rearAmbushLevelsPerExtraRow = 2;

    readonly Dictionary<Vector2Int, OvergrowthState> _growingOvergrowth = new();
    readonly Dictionary<Vector2Int, Image> _overgrowthVisuals = new();
    readonly Dictionary<Vector2Int, float> _contagionFloorSpreadTimers = new();
    readonly Dictionary<Vector2Int, Image> _contagionFloorVisuals = new();
    readonly Dictionary<Vector2Int, Image> _infectionVisuals = new();
    readonly HashSet<int> _soulLinkPropagationGroups = new();
    readonly List<Vector2Int> _soulLinkScratchCells = new();
    readonly List<int> _outFlankedRows = new();
    readonly List<RectTransform> _outFlankedProjectiles = new();
    readonly HashSet<Vector2Int> _volcanicTargetSet = new();
    readonly List<Vector2Int> _volcanicTargetList = new();

    GameController _gc;
    LevelModifierSelectionUI _selectionUI;
    RainOverlayUI _rainOverlay;
    Sprite _currentLevelBackgroundSprite;
    VideoClip _currentLevelBackgroundVideoClip;
    VideoClip _pendingAnimatedBackgroundClip;
    VideoPlayer _hookedAnimatedBackgroundPlayer;

    float _overgrowthSpawnTimer;
    float _stormCountdown;
    float _rearAmbushCountdown;
    float _lowRationsCountdown;
    float _autoRotateTimer;
    float _autoShiftTimer;
    float _autoShiftPauseRemaining;
    bool _autoShiftMovingRight = true;
    float _outFlankedCountdown;
    float _volcanicEruptionCountdown;
    int _availableRerolls;
    int _comboShieldRemaining;
    int _pendingComboShieldBreaks;
    int _rearAmbushRowsSpawnedThisLevel;
    Coroutine _comboShieldPulseCR;

    static Sprite _onePx;

    public LevelModifierSO ActiveModifier { get; private set; }
    public int AvailableRerolls => Mathf.Max(0, _availableRerolls);
    public bool IsSelectionRunning { get; private set; }

    void Awake()
    {
        _gc = GetComponent<GameController>();
        if (!board) board = FindFirstObjectByType<Board>();
        if (!piece) piece = GetComponent<Piece>();
        if (!battleLog) battleLog = FindFirstObjectByType<BattleLogUI>(FindObjectsInactive.Include);
    }

    void Start()
    {
        EnsureAnimatedBackgroundRefs();
        if (ActiveModifier)
            ApplyResolvedBackgroundSprite();
        else
            StopAnimatedBackground(clearTexture: false);
        SyncVolcanicBoardAnimationRoots();
    }

    void OnDestroy()
    {
        UnhookAnimatedBackgroundEvents();
    }

    void Update()
    {
        if (IsSelectionRunning)
            MaintainSelectionCursorState();

        if (!_gc || !_gc.IsRoundActive || !ActiveModifier || !board)
            return;

        float dt = Time.deltaTime;

        switch (ActiveModifier.kind)
        {
            case LevelModifierKind.Overgrowth:
                UpdateOvergrowth(dt);
                break;
            case LevelModifierKind.StormyWeather:
                UpdateStorm(dt);
                break;
            case LevelModifierKind.RearAmbush:
                UpdateRearAmbush(dt);
                break;
            case LevelModifierKind.LowRations:
                UpdateLowRations(dt);
                break;
            case LevelModifierKind.Contagion:
                UpdateContagion(dt);
                break;
            case LevelModifierKind.OutFlanked:
                UpdateOutFlanked(dt);
                break;
            case LevelModifierKind.VolcanicEruption:
                UpdateVolcanicEruption(dt);
                break;
        }

        SyncPersistentVisuals();
    }

    public IEnumerator BeginLevel(CastleData castleData, System.Action onSelectionPanelFullyShown = null)
    {
        CacheLevelBackground(castleData);
        ResetLevelState();

        if (castleData == null)
            yield break;

        if (castleData.isBossLevel || !castleData.enableLevelModifierSelection)
            yield break;

        var database = castleData.levelModifierDatabase;
        if (!database)
            yield break;

        var pool = database.BuildPool(castleData.allowedLevelModifierKinds);
        if (pool.Count == 0)
            yield break;

        var chosen = database.PickWeighted(castleData.allowedLevelModifierKinds);
        if (!chosen)
            yield break;

        EnsureSelectionUI();

        SetSelectionCursorState(true);

        IsSelectionRunning = true;
        if (_selectionUI)
        {
            bool TryConsumeReroll(LevelModifierSO currentModifier, out LevelModifierSO nextModifier)
            {
                nextModifier = null;

                if (_availableRerolls <= 0)
                    return false;

                nextModifier = database.PickWeighted(castleData.allowedLevelModifierKinds, currentModifier);
                if (!nextModifier)
                    return false;

                _availableRerolls = Mathf.Max(0, _availableRerolls - 1);
                return true;
            }

            yield return _selectionUI.PlaySelection(pool, chosen, () => AvailableRerolls, TryConsumeReroll, onSelectionPanelFullyShown);
            chosen = _selectionUI.CurrentChosenModifier ? _selectionUI.CurrentChosenModifier : chosen;
        }
        IsSelectionRunning = false;

        SetSelectionCursorState(false);

        ActiveModifier = chosen;
        CodexProgressStore.Unlock(ActiveModifier);
        ApplyModifierStartupEffects();
        LogModifierActivation();
    }

    public void ResetRunState()
    {
        _availableRerolls = 0;
        ResetLevelState();
    }

    public void GrantRerollForCompletedLevel(int completedLevelNumber)
    {
        if (completedLevelNumber <= 0)
            return;

        if ((completedLevelNumber % 2) == 0)
            _availableRerolls++;
    }

    public void RestoreCheckpointState(LevelModifierSO modifier, int availableRerolls)
    {
        CacheLevelBackgroundFromCurrentLevel();
        ResetLevelState();
        _availableRerolls = Mathf.Max(0, availableRerolls);
        ActiveModifier = modifier;

        if (ActiveModifier)
        {
            CodexProgressStore.Unlock(ActiveModifier);
            ApplyModifierStartupEffects();
        }
        else
            RefreshModifierUI();
    }

    public LevelModifierSO ResolveModifierFromCurrentCastle(string modifierName, string modifierDisplayName)
    {
        if (string.IsNullOrWhiteSpace(modifierName) && string.IsNullOrWhiteSpace(modifierDisplayName))
            return null;

        if (!_gc)
            _gc = GetComponent<GameController>();

        if (_gc == null || _gc.castlesByLevel == null)
            return null;

        int levelIndex = _gc.CurrentLevel;
        if (levelIndex < 0 || levelIndex >= _gc.castlesByLevel.Length)
            return null;

        var castleData = _gc.castlesByLevel[levelIndex];
        if (!castleData || !castleData.levelModifierDatabase)
            return null;

        var pool = castleData.levelModifierDatabase.BuildPool(castleData.allowedLevelModifierKinds);
        for (int i = 0; i < pool.Count; i++)
        {
            var modifier = pool[i];
            if (!modifier)
                continue;

            if (!string.IsNullOrWhiteSpace(modifierName) && modifier.name == modifierName)
                return modifier;

            if (!string.IsNullOrWhiteSpace(modifierDisplayName) && modifier.displayName == modifierDisplayName)
                return modifier;
        }

        return null;
    }

    public bool BlocksManualRotation => ActiveModifier && ActiveModifier.kind == LevelModifierKind.AutoRotate;
    public bool BlocksManualHorizontalShift => ActiveModifier && ActiveModifier.kind == LevelModifierKind.AutoShift;
    public bool BlocksSpecialUsage => ActiveModifier && ActiveModifier.kind == LevelModifierKind.SpecialLock;
    public bool BlocksSpecialPieceSpawns => ActiveModifier && ActiveModifier.kind == LevelModifierKind.NoSpecialPieces;
    public bool EnablesPiercingCastleProjectiles => ActiveModifier && ActiveModifier.kind == LevelModifierKind.PiercingShot;
    public Sprite ActivePiercingCastleProjectileSprite =>
        EnablesPiercingCastleProjectiles ? ActiveModifier.piercingProjectileSprite : null;
    public Sprite ActiveCastleProjectileSpriteOverride
    {
        get
        {
            if (!ActiveModifier)
                return null;

            if (ActiveModifier.kind == LevelModifierKind.PiercingShot)
                return ActiveModifier.piercingProjectileSprite;

            return null;
        }
    }
    public bool AppliesAutoMovementGravitySlow => ActiveModifier &&
        (ActiveModifier.kind == LevelModifierKind.AutoRotate || ActiveModifier.kind == LevelModifierKind.AutoShift);
    public float ActiveGravityMultiplier => AppliesAutoMovementGravitySlow ? AutoMovementGravityMultiplier : 1f;

    public float ModifyIncomingDamage(Vector2Int cell, MonsterData target, float amount, Board.DamageSource source)
    {
        if (!ActiveModifier)
            return amount;

        if (ActiveModifier.kind == LevelModifierKind.DoubleDamage)
            return amount * Mathf.Max(0f, ActiveModifier.receivedDamageMultiplier);

        return amount;
    }

    public float ModifySpecialGaugeGain(float amount)
    {
        if (!ActiveModifier)
            return amount;

        if (ActiveModifier.kind == LevelModifierKind.SpecialLock)
            return 0f;

        return amount;
    }

    public int ModifyOutgoingDamage(int damage, int comboCount)
    {
        if (!ActiveModifier)
            return damage;

        switch (ActiveModifier.kind)
        {
            case LevelModifierKind.DoubleDamage:
                return Mathf.RoundToInt(damage * Mathf.Max(0f, ActiveModifier.dealtDamageMultiplier));

            case LevelModifierKind.ComboGated:
                if (comboCount < Mathf.Max(1, ActiveModifier.comboThreshold))
                    return Mathf.RoundToInt(damage * Mathf.Clamp01(ActiveModifier.comboBelowThresholdDamageMultiplier));
                return Mathf.RoundToInt(damage * Mathf.Max(0f, ActiveModifier.comboAtOrAboveThresholdDamageMultiplier));

            case LevelModifierKind.ComboShield:
                if (_comboShieldRemaining <= 0)
                    return damage;

                if (comboCount >= Mathf.Max(1, ActiveModifier.comboShieldThreshold))
                {
                    _pendingComboShieldBreaks++;
                    return damage;
                }

                return Mathf.RoundToInt(damage * Mathf.Clamp01(ActiveModifier.comboShieldBlockedDamageMultiplier));
        }

        return damage;
    }

    public void OnCastleProjectileImpact()
    {
        if (!ActiveModifier || ActiveModifier.kind != LevelModifierKind.ComboShield)
            return;

        if (_comboShieldRemaining <= 0 || _pendingComboShieldBreaks <= 0)
            return;

        _pendingComboShieldBreaks = Mathf.Max(0, _pendingComboShieldBreaks - 1);
        _comboShieldRemaining = Mathf.Max(0, _comboShieldRemaining - 1);

        RefreshComboShieldUI(true);

        string modifierName = TetrabeastsLocalization.LocalizeText(ActiveModifier.displayName);
        if (_comboShieldRemaining <= 0)
            battleLog?.LogPlain(TetrabeastsLocalization.LocalizeFormat("{0} is broken.", modifierName));
        else
            battleLog?.LogPlain(TetrabeastsLocalization.LocalizeFormat("{0}: {1} shield(s) remain.", modifierName, _comboShieldRemaining));
    }

    public void HandlePieceAutomation(Piece activePiece, float dt)
    {
        if (!ActiveModifier || !activePiece || !activePiece.HasActiveCells)
            return;

        switch (ActiveModifier.kind)
        {
            case LevelModifierKind.AutoRotate:
                _autoRotateTimer += dt;
                if (_autoRotateTimer >= Mathf.Max(0.05f, ActiveModifier.autoRotateInterval))
                {
                    _autoRotateTimer = 0f;
                    activePiece.RotateClockwiseExternal();
                }
                break;

            case LevelModifierKind.AutoShift:
                if (_autoShiftPauseRemaining > 0f)
                {
                    _autoShiftPauseRemaining -= dt;
                    return;
                }

                float baseInterval = Mathf.Max(0.03f, ActiveModifier.autoShiftInterval / Mathf.Max(0.01f, autoShiftBaseSpeedMultiplier));
                float gravityRamp01 = _gc ? _gc.CurrentGravityRamp : 0f;
                float effectiveInterval = Mathf.Lerp(
                    baseInterval,
                    baseInterval * Mathf.Clamp01(1f - autoShiftGravityRampInfluence),
                    gravityRamp01);

                _autoShiftTimer += dt;
                if (_autoShiftTimer >= Mathf.Max(0.03f, effectiveInterval))
                {
                    _autoShiftTimer = 0f;
                    Vector2Int delta = _autoShiftMovingRight ? Vector2Int.right : Vector2Int.left;
                    if (!activePiece.TryTranslate(delta, resetLockTimer: true))
                    {
                        _autoShiftMovingRight = !_autoShiftMovingRight;
                        _autoShiftPauseRemaining = Mathf.Max(0f, ActiveModifier.autoShiftEdgePauseSeconds);
                    }
                }
                break;
        }
    }

    public void OnNormalPiecePlaced(IReadOnlyList<Vector2Int> cells)
    {
        if (!ActiveModifier || cells == null || cells.Count == 0 || !board)
            return;

        if (ActiveModifier.kind == LevelModifierKind.SoulLink)
            ApplySoulLink(cells);

        if (ActiveModifier.kind == LevelModifierKind.HalfHealthStart)
            ApplyHalfHealthStart(cells);

        if (ActiveModifier.kind == LevelModifierKind.Contagion)
            ApplyContagionOnPiecePlaced(cells);
    }

    public void OnRowsResolved(Dictionary<int, int> rowDamage)
    {
        if (!ActiveModifier || rowDamage == null || rowDamage.Count == 0)
            return;

        if (ActiveModifier.kind == LevelModifierKind.Overgrowth)
        {
            var clearedRows = new HashSet<int>();
            foreach (var kv in rowDamage)
                clearedRows.Add(kv.Key % 1000);

            ClearGrowingOvergrowthInRows(clearedRows);
        }
    }

    public void OnSpecialEnvironmentHit(IEnumerable<Vector2Int> affectedCells, SpecialType special)
    {
        if (!ActiveModifier || ActiveModifier.kind != LevelModifierKind.Overgrowth || affectedCells == null)
            return;

        if (special != SpecialType.Bomb && special != SpecialType.Bolt)
            return;

        foreach (var cell in affectedCells)
            RemoveGrowingOvergrowth(cell);
    }

    public void OnTileDamaged(Vector2Int cell, MonsterData data, float amount, Board.DamageSource source)
    {
        if (!ActiveModifier || !board || amount <= 0f || ActiveModifier.kind != LevelModifierKind.SoulLink)
            return;

        if (!board.TryGetMonster(cell, out var inst))
            return;

        int groupId = inst.soulLinkGroupId;
        if (groupId <= 0 || !_soulLinkPropagationGroups.Add(groupId))
            return;

        try
        {
            board.GetMonsterCellsNonAlloc(_soulLinkScratchCells, includeDead: false);
            for (int i = 0; i < _soulLinkScratchCells.Count; i++)
            {
                Vector2Int other = _soulLinkScratchCells[i];
                if (other == cell)
                    continue;

                if (!board.TryGetMonster(other, out var otherInst) || otherInst.soulLinkGroupId != groupId || otherInst.hp <= 0f)
                    continue;

                board.DamageTile(other, amount, source);
            }
        }
        finally
        {
            _soulLinkScratchCells.Clear();
            _soulLinkPropagationGroups.Remove(groupId);
        }
    }

    public void OnTileHealed(Vector2Int cell, MonsterData target, MonsterData source, float amount)
    {
        if (!ActiveModifier || !board || amount <= 0f || ActiveModifier.kind != LevelModifierKind.SoulLink)
            return;

        if (!board.TryGetMonster(cell, out var inst))
            return;

        int groupId = inst.soulLinkGroupId;
        if (groupId <= 0 || !_soulLinkPropagationGroups.Add(groupId))
            return;

        try
        {
            board.GetMonsterCellsNonAlloc(_soulLinkScratchCells, includeDead: false);
            for (int i = 0; i < _soulLinkScratchCells.Count; i++)
            {
                Vector2Int other = _soulLinkScratchCells[i];
                if (other == cell)
                    continue;

                if (!board.TryGetMonster(other, out var otherInst) || otherInst.soulLinkGroupId != groupId || otherInst.hp <= 0f)
                    continue;

                board.HealTile(other, amount, source);
            }
        }
        finally
        {
            _soulLinkScratchCells.Clear();
            _soulLinkPropagationGroups.Remove(groupId);
        }
    }

    public void OnTileDied(Vector2Int cell, MonsterData data)
    {
        if (!ActiveModifier || !board)
            return;

        if (ActiveModifier.kind == LevelModifierKind.DeathExplosion)
            TriggerDeathExplosion(cell);

        if (ActiveModifier.kind == LevelModifierKind.Contagion &&
            board.TryGetMonster(cell, out var inst) &&
            inst.contagionInfected)
        {
            CreateContagionFloor(cell);
        }
    }

    void ApplyModifierStartupEffects()
    {
        EnsureBoardPresentation();

        _comboShieldRemaining = (ActiveModifier && ActiveModifier.kind == LevelModifierKind.ComboShield)
        ? Mathf.Max(0, ActiveModifier.comboShieldCount)
        : 0;

        _pendingComboShieldBreaks = 0;
        RefreshComboShieldUI(false);

        _overgrowthSpawnTimer = 0f;
        _autoRotateTimer = 0f;
        _autoShiftTimer = 0f;
        _autoShiftPauseRemaining = 0f;
        _autoShiftMovingRight = true;
        _outFlankedCountdown = 0f;
        _volcanicEruptionCountdown = 0f;

        if (ActiveModifier.kind == LevelModifierKind.SpecialLock)
            _gc?.SetSpecialGaugeImmediate(0f);

        RefreshModifierUI();
        EnsureSelectionUI();
        _selectionUI?.SetActiveModifierInfo(ActiveModifier);

        if (ActiveModifier.kind == LevelModifierKind.StormyWeather)
        {
            _stormCountdown = Random.Range(
                Mathf.Min(ActiveModifier.stormStrikeIntervalMin, ActiveModifier.stormStrikeIntervalMax),
                Mathf.Max(ActiveModifier.stormStrikeIntervalMin, ActiveModifier.stormStrikeIntervalMax));
        }

        if (ActiveModifier.kind == LevelModifierKind.RearAmbush)
            _rearAmbushCountdown = Mathf.Max(0.1f, ActiveModifier.rearAmbushInterval);

        if (ActiveModifier.kind == LevelModifierKind.LowRations)
            _lowRationsCountdown = Mathf.Max(0.1f, ActiveModifier.lowRationsTickInterval);

        if (ActiveModifier.kind == LevelModifierKind.Swamp)
            ApplySwampRows();

        if (ActiveModifier.kind == LevelModifierKind.OutFlanked)
            ScheduleOutFlankedWave();

        if (ActiveModifier.kind == LevelModifierKind.VolcanicEruption)
        {
            ScheduleVolcanicEruption();
            SyncVolcanicBoardAnimationRoots();
        }
        else
        {
            SyncVolcanicBoardAnimationRoots();
        }
    }

    void RefreshModifierUI()
    {
        bool specialLocked = ActiveModifier && ActiveModifier.kind == LevelModifierKind.SpecialLock;

        if (specialGaugeRoot)
            specialGaugeRoot.SetActive(!specialLocked);

        if (activeModifierTitleText)
        {
            activeModifierTitleText.gameObject.SetActive(true);
            activeModifierTitleText.text = ActiveModifier
                ? TetrabeastsLocalization.LocalizeText(ActiveModifier.displayName)
                : TetrabeastsLocalization.LocalizeText("No level Modifier");
        }
    }

    void LogModifierActivation()
    {
        if (!ActiveModifier)
            return;

        battleLog?.LogPlain(TetrabeastsLocalization.LocalizeFormat(
            "Level modifier: {0}.",
            TetrabeastsLocalization.LocalizeText(ActiveModifier.displayName)));
    }

    void UpdateOvergrowth(float dt)
    {
        _overgrowthSpawnTimer += dt;
        if (_overgrowthSpawnTimer >= Mathf.Max(0.1f, ActiveModifier.overgrowthTargetInterval))
        {
            _overgrowthSpawnTimer = 0f;
            TryStartOvergrowthCell();
        }

        var keys = new List<Vector2Int>(_growingOvergrowth.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Vector2Int cell = keys[i];
            OvergrowthState state = _growingOvergrowth[cell];
            state.elapsed += dt;

            if (!state.partialShown && state.elapsed >= Mathf.Max(0.1f, ActiveModifier.overgrowthPartialSeconds))
            {
                state.partialShown = true;
                SetOvergrowthVisual(cell, ActiveModifier.overgrowthPartialSprite);
            }

            if (state.elapsed >= Mathf.Max(0.1f, ActiveModifier.overgrowthFullSeconds))
            {
                PromoteOvergrowthToFull(cell);
                continue;
            }

            _growingOvergrowth[cell] = state;
        }
    }

    void UpdateStorm(float dt)
    {
        _stormCountdown -= dt;
        if (_stormCountdown > 0f)
            return;

        _stormCountdown = Random.Range(
            Mathf.Min(ActiveModifier.stormStrikeIntervalMin, ActiveModifier.stormStrikeIntervalMax),
            Mathf.Max(ActiveModifier.stormStrikeIntervalMin, ActiveModifier.stormStrikeIntervalMax));

        int targetCount = Random.Range(
            Mathf.Max(1, ActiveModifier.stormTargetsMin),
            Mathf.Max(ActiveModifier.stormTargetsMin, ActiveModifier.stormTargetsMax) + 1);

        var used = new HashSet<Vector2Int>();
        for (int i = 0; i < targetCount; i++)
        {
            if (!TryPickStormCell(out var cell, used))
                break;

            used.Add(cell);
            bool cross = Random.value <= Mathf.Clamp01(ActiveModifier.stormCrossChance);
            StartCoroutine(StormStrikeRoutine(cell, cross));
        }
    }

    IEnumerator StormStrikeRoutine(Vector2Int center, bool cross)
    {
        var affected = BuildStormStrikeCells(center, cross);
        Sprite warning = board && board.lightningBorderSprite ? board.lightningBorderSprite : ActiveModifier.icon;
        const float warningSeconds = 0.75f;

        for (int i = 0; i < affected.Count; i++)
            board?.FlashWarningAtCell(affected[i], warning, warningSeconds, 0.08f, 0.75f);

        yield return new WaitForSeconds(warningSeconds);

        if (AudioManager.I)
            AudioManager.I.PlayBossLightningStrike();

        int ticks = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Max(0.1f, ActiveModifier.stormFloorDuration) /
            Mathf.Max(0.05f, ActiveModifier.stormFloorTickInterval)));

        for (int i = 0; i < affected.Count; i++)
        {
            Vector2Int cell = affected[i];
            if (!board || !board.InBounds(cell))
                continue;

            board.DamageTile(cell, Mathf.Max(0f, ActiveModifier.stormInitialStrikeDamage), Board.DamageSource.FloorLightning);
            board.ClearFloorEffect(cell);
            board.SetFloorEffect(
                cell,
                Board.FloorEffectType.Lightning,
                GetScaledFloorEffectDamage(ActiveModifier.stormFloorTickDamage),
                Mathf.Max(0.05f, ActiveModifier.stormFloorTickInterval),
                ticks);
        }
    }

    void UpdateRearAmbush(float dt)
    {
        if (_rearAmbushRowsSpawnedThisLevel >= GetRearAmbushRowLimit())
            return;

        _rearAmbushCountdown -= dt;
        if (_rearAmbushCountdown > 0f)
            return;

        _rearAmbushCountdown = Mathf.Max(0.1f, ActiveModifier.rearAmbushInterval);

        board?.ShiftBoardUpOneRowAndFillBottom(ActiveModifier.rearAmbushSoldierSprite);
        _rearAmbushRowsSpawnedThisLevel++;

        if (!piece || !piece.HasActiveCells || !piece.OverlapsPlacedCells())
            return;

        if (!piece.TryResolvePlacedOverlapByShiftingUp())
            _gc?.GameOver();
    }

    int GetRearAmbushRowLimit()
    {
        int completedLevelsThisRun = Mathf.Max(0, _gc ? _gc.CurrentLevel : 0);
        int extraRows = completedLevelsThisRun / Mathf.Max(1, rearAmbushLevelsPerExtraRow);

        return Mathf.Max(0, rearAmbushBaseRows + extraRows);
    }

    void UpdateLowRations(float dt)
    {
        _lowRationsCountdown -= dt;
        if (_lowRationsCountdown > 0f)
            return;

        _lowRationsCountdown = Mathf.Max(0.1f, ActiveModifier.lowRationsTickInterval);

        int reserve = _gc ? _gc.CurrentReserveUnits : 0;
        int maxReserve = Mathf.Max(1, _gc ? _gc.MaxReserveUnits : 1);

        float damage = (reserve <= ActiveModifier.lowRationsNegligibleReserveThreshold)
            ? Mathf.Max(0f, ActiveModifier.lowRationsNegligibleDamage)
            : Mathf.Lerp(
                Mathf.Max(0f, ActiveModifier.lowRationsNegligibleDamage),
                Mathf.Max(0f, ActiveModifier.lowRationsDamageAtMaxReserve),
                Mathf.Clamp01((reserve - ActiveModifier.lowRationsNegligibleReserveThreshold) /
                              Mathf.Max(1f, maxReserve - ActiveModifier.lowRationsNegligibleReserveThreshold)));

        var cells = board.GetMonsterCells(includeDead: false);
        for (int i = 0; i < cells.Count; i++)
            board.DamageTile(cells[i], damage, Board.DamageSource.Rations);
    }

    void UpdateContagion(float dt)
    {
        var cells = board.GetMonsterCells(includeDead: false);
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (!board.TryGetMonster(cell, out var inst) || !inst.contagionInfected || inst.hp <= 0f)
                continue;

            inst.contagionTickTimer += dt;
            inst.contagionSpreadTimer += dt;

            bool shouldSpread = false;

            if (inst.contagionTickTimer >= Mathf.Max(0.1f, ActiveModifier.contagionTickInterval))
            {
                inst.contagionTickTimer = 0f;

                float pct = Mathf.Max(0f, inst.contagionPercentPerTick);
                inst.contagionPercentPerTick = pct + Mathf.Max(0f, ActiveModifier.contagionPercentIncreasePerTick);

                board.SetMonsterAt(cell, inst);

                float damage = Mathf.Max(0f, inst.maxHp * pct);
                board.DamageTile(cell, damage, Board.DamageSource.Contagion);
                board.FlashMonsterTick(cell, board.contagionTickFlashTint);

                if (!board.TryGetMonster(cell, out inst) || inst.hp <= 0f)
                    continue;
            }

            if (inst.contagionSpreadTimer >= Mathf.Max(0.1f, ActiveModifier.contagionSpreadInterval))
            {
                inst.contagionSpreadTimer = 0f;
                shouldSpread = true;
            }

            if (inst.hp <= 0f)
                continue;

            board.SetMonsterAt(cell, inst);

            if (shouldSpread)
                SpreadContagionToAdjacent(cell, Mathf.Clamp01(ActiveModifier.contagionSpreadChance));
        }

        var floorKeys = new List<Vector2Int>(_contagionFloorSpreadTimers.Keys);
        for (int i = 0; i < floorKeys.Count; i++)
        {
            Vector2Int cell = floorKeys[i];
            float timer = _contagionFloorSpreadTimers[cell] + dt;
            if (timer >= Mathf.Max(0.1f, ActiveModifier.contagionSpreadInterval))
            {
                timer = 0f;
                SpreadContagionFromFloor(cell);
            }

            _contagionFloorSpreadTimers[cell] = timer;
        }
    }

    void UpdateOutFlanked(float dt)
    {
        _outFlankedCountdown -= dt;
        if (_outFlankedCountdown > 0f)
            return;

        ScheduleOutFlankedWave();
        SpawnOutFlankedWave();
    }

    void ScheduleOutFlankedWave()
    {
        if (!ActiveModifier)
        {
            _outFlankedCountdown = 0f;
            return;
        }

        float min = Mathf.Min(ActiveModifier.outFlankedIntervalMin, ActiveModifier.outFlankedIntervalMax);
        float max = Mathf.Max(ActiveModifier.outFlankedIntervalMin, ActiveModifier.outFlankedIntervalMax);
        _outFlankedCountdown = Random.Range(Mathf.Max(0.1f, min), Mathf.Max(0.1f, max));
    }

    void SpawnOutFlankedWave()
    {
        if (!board || !ActiveModifier)
            return;

        int rowsToSpawn = Mathf.Max(1, ActiveModifier.outFlankedRowsPerWave);
        PickOutFlankedRows(_outFlankedRows, rowsToSpawn);

        for (int i = 0; i < _outFlankedRows.Count; i++)
        {
            int row = _outFlankedRows[i];
            SpawnOutFlankedProjectile(row, fromLeft: true, ActiveModifier);
            SpawnOutFlankedProjectile(row, fromLeft: false, ActiveModifier);
        }
    }

    void PickOutFlankedRows(List<int> rows, int count)
    {
        rows.Clear();
        if (!board || board.height <= 0 || count <= 0)
            return;

        var aliveRows = new List<int>();
        var seen = new HashSet<int>();
        var monsterCells = board.GetMonsterCells(includeDead: false);
        for (int i = 0; i < monsterCells.Count; i++)
        {
            int row = monsterCells[i].y;
            if (row >= 0 && row < board.height && seen.Add(row))
                aliveRows.Add(row);
        }

        while (rows.Count < count && aliveRows.Count > 0)
        {
            int index = Random.Range(0, aliveRows.Count);
            rows.Add(aliveRows[index]);
            aliveRows.RemoveAt(index);
        }

        int maxRows = Mathf.Min(count, board.height);
        int safety = board.height * 3;
        while (rows.Count < maxRows && safety-- > 0)
        {
            int row = Random.Range(0, board.height);
            if (!rows.Contains(row))
                rows.Add(row);
        }
    }

    void SpawnOutFlankedProjectile(int row, bool fromLeft, LevelModifierSO modifier)
    {
        if (!board || !modifier || row < 0 || row >= board.height)
            return;

        RectTransform root = board.overlayRoot ? board.overlayRoot : board.gridRoot;
        if (!root)
            return;

        Sprite sprite = modifier.outFlankedProjectileSprite ? modifier.outFlankedProjectileSprite : modifier.icon;
        var go = new GameObject(fromLeft ? "OutFlankedArrow_Left" : "OutFlankedArrow_Right", typeof(Image));
        go.transform.SetParent(root, false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite ? sprite : OnePx();
        img.preserveAspect = true;
        img.raycastTarget = false;

        Vector2 cellSize = board.GetCellSize();
        float visualScale = _gc ? _gc.CurrentEnemyProjectileVisualScaleForModifiers : 1f;
        img.rectTransform.sizeDelta = cellSize *
            Mathf.Max(0.1f, visualScale) *
            Mathf.Max(0.1f, modifier.outFlankedProjectileSizeMultiplier);

        float sideOffset = cellSize.x * Mathf.Max(0.5f, modifier.outFlankedProjectileSideOffsetCells);
        Vector2 left = board.CellToAnchoredPos(new Vector2Int(0, row)) - new Vector2(sideOffset, 0f);
        Vector2 right = board.CellToAnchoredPos(new Vector2Int(board.width - 1, row)) + new Vector2(sideOffset, 0f);

        img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchoredPosition = BoardLocalToRoot(root, fromLeft ? left : right);
        img.rectTransform.localEulerAngles = new Vector3(0f, 0f, fromLeft ? 0f : 180f);

        _outFlankedProjectiles.Add(img.rectTransform);
        StartCoroutine(OutFlankedProjectileRoutine(img.rectTransform, row, fromLeft, modifier));
    }

    IEnumerator OutFlankedProjectileRoutine(RectTransform projectile, int row, bool movingRight, LevelModifierSO modifier)
    {
        RectTransform root = projectile ? projectile.parent as RectTransform : null;
        if (!projectile || !root || !board)
            yield break;

        Image image = projectile.GetComponent<Image>();
        float warningSeconds = Mathf.Max(0f, modifier ? modifier.outFlankedWarningSeconds : 2f);
        float warningTimer = 0f;
        const float warningFlashInterval = 0.15f;
        while (projectile && ActiveModifier == modifier && board && warningTimer < warningSeconds)
        {
            if (_gc && !_gc.IsRoundActive)
            {
                yield return null;
                continue;
            }

            if (image)
                image.enabled = Mathf.FloorToInt(warningTimer / warningFlashInterval) % 2 == 0;

            warningTimer += Time.deltaTime;
            yield return null;
        }

        if (!projectile)
            yield break;

        if (ActiveModifier != modifier || !board)
        {
            DestroyOutFlankedProjectile(projectile);
            yield break;
        }

        if (image)
            image.enabled = true;

        _gc?.PlayEnemyProjectileFiredSfx();

        Vector2 cellSize = board.GetCellSize();
        float sideOffset = cellSize.x * Mathf.Max(0.5f, modifier.outFlankedProjectileSideOffsetCells);
        Vector2 endBoard = movingRight
            ? board.CellToAnchoredPos(new Vector2Int(board.width - 1, row)) + new Vector2(sideOffset, 0f)
            : board.CellToAnchoredPos(new Vector2Int(0, row)) - new Vector2(sideOffset, 0f);
        Vector2 end = BoardLocalToRoot(root, endBoard);

        while (projectile && ActiveModifier == modifier && board)
        {
            if (_gc && !_gc.IsRoundActive)
            {
                yield return null;
                continue;
            }

            if ((projectile.anchoredPosition - end).sqrMagnitude <= 4f)
                break;

            Vector2 previous = projectile.anchoredPosition;
            float baseSpeed = _gc ? _gc.CurrentEnemyProjectileSpeedForModifiers : 280f;
            float speedMultiplier = Mathf.Max(0.05f, modifier.outFlankedProjectileSpeedMultiplier);
            float speed = Mathf.Max(10f, baseSpeed * speedMultiplier);
            projectile.anchoredPosition = Vector2.MoveTowards(projectile.anchoredPosition, end, speed * Time.deltaTime);

            if (TryGetOutFlankedImpactCell(root, row, previous, projectile.anchoredPosition, movingRight, out var hitCell))
            {
                projectile.anchoredPosition = BoardLocalToRoot(root, board.CellToAnchoredPos(hitCell));

                if (board.HasObstacle(hitCell))
                {
                    board.TryHandleEnemyProjectileObstacleImpact(hitCell);
                    _gc?.PlayEnemyProjectileHitSfx();
                }
                else
                {
                    AudioClip hitClip = _gc ? _gc.PickEnemyProjectileHitSfx() : null;
                    board.DamageTile(hitCell, _gc ? _gc.CastleProjectileDamageForStats : 1,
                        Board.DamageSource.CastleProjectile, hitClip);
                }

                DestroyOutFlankedProjectile(projectile);
                yield break;
            }

            yield return null;
        }

        DestroyOutFlankedProjectile(projectile);
    }

    bool TryGetOutFlankedImpactCell(RectTransform root, int row, Vector2 previousRootPos, Vector2 currentRootPos,
                                    bool movingRight, out Vector2Int hitCell)
    {
        hitCell = default;
        if (!board || row < 0 || row >= board.height)
            return false;

        Vector2 previousBoard = RootLocalToBoard(root, previousRootPos);
        Vector2 currentBoard = RootLocalToBoard(root, currentRootPos);
        float sweepLeft = Mathf.Min(previousBoard.x, currentBoard.x);
        float sweepRight = Mathf.Max(previousBoard.x, currentBoard.x);
        float halfCell = board.GetCellSize().x * 0.5f;

        int start = movingRight ? 0 : board.width - 1;
        int end = movingRight ? board.width : -1;
        int step = movingRight ? 1 : -1;

        for (int x = start; x != end; x += step)
        {
            var c = new Vector2Int(x, row);
            float centerX = board.CellToAnchoredPos(c).x;
            float cellLeft = centerX - halfCell;
            float cellRight = centerX + halfCell;

            if (sweepRight < cellLeft || sweepLeft > cellRight)
                continue;

            if (board.HasObstacle(c))
            {
                hitCell = c;
                return true;
            }

            if (board.TryGetMonster(c, out var inst) && inst.data && inst.hp > 0f)
            {
                hitCell = c;
                return true;
            }
        }

        return false;
    }

    void UpdateVolcanicEruption(float dt)
    {
        _volcanicEruptionCountdown -= dt;
        if (_volcanicEruptionCountdown > 0f)
            return;

        ScheduleVolcanicEruption();
        TryStartVolcanicEruption();
    }

    void ScheduleVolcanicEruption()
    {
        if (!ActiveModifier)
        {
            _volcanicEruptionCountdown = 0f;
            return;
        }

        float min = Mathf.Min(ActiveModifier.volcanicEruptionIntervalMin, ActiveModifier.volcanicEruptionIntervalMax);
        float max = Mathf.Max(ActiveModifier.volcanicEruptionIntervalMin, ActiveModifier.volcanicEruptionIntervalMax);
        _volcanicEruptionCountdown = Random.Range(Mathf.Max(0.1f, min), Mathf.Max(0.1f, max));
    }

    void TryStartVolcanicEruption()
    {
        if (!board || !ActiveModifier)
            return;

        if (!PickVolcanicTargets(_volcanicTargetList))
            return;

        StartCoroutine(VolcanicEruptionRoutine(new List<Vector2Int>(_volcanicTargetList), ActiveModifier));
    }

    bool PickVolcanicTargets(List<Vector2Int> targets)
    {
        targets.Clear();
        _volcanicTargetSet.Clear();

        if (!board || !ActiveModifier)
            return false;

        int availableRows = board.height - Mathf.Max(0, ActiveModifier.volcanicEruptionExcludedTopRows);
        if (availableRows <= 0)
            return false;

        var monsterCandidates = new List<Vector2Int>();
        var openCandidates = new List<Vector2Int>();
        for (int y = 0; y < availableRows; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (board.HasObstacle(cell))
                    continue;

                if (board.TryGetMonster(cell, out var inst) && inst.data && inst.hp > 0f)
                {
                    monsterCandidates.Add(cell);
                    continue;
                }

                if (board.IsFree(cell))
                    openCandidates.Add(cell);
            }
        }

        int candidateCount = monsterCandidates.Count + openCandidates.Count;
        int targetCount = Mathf.Min(Mathf.Max(1, ActiveModifier.volcanicEruptionTargetCount), candidateCount);
        float monsterTargetChance = Mathf.Clamp01(ActiveModifier.volcanicEruptionMonsterTargetChance);

        while (targets.Count < targetCount && (monsterCandidates.Count > 0 || openCandidates.Count > 0))
        {
            bool targetMonster = monsterCandidates.Count > 0 && Random.value < monsterTargetChance;
            List<Vector2Int> source = targetMonster ? monsterCandidates : openCandidates;
            if (source.Count == 0)
                source = monsterCandidates.Count > 0 ? monsterCandidates : openCandidates;

            int index = Random.Range(0, source.Count);
            Vector2Int cell = source[index];
            source.RemoveAt(index);

            if (_volcanicTargetSet.Add(cell))
                targets.Add(cell);
        }

        return targets.Count > 0;
    }

    IEnumerator VolcanicEruptionRoutine(List<Vector2Int> targets, LevelModifierSO modifier)
    {
        if (targets == null || targets.Count == 0 || !board || !modifier)
            yield break;

        float warningSeconds = Mathf.Max(0.1f, modifier.volcanicEruptionWarningSeconds);
        Sprite warningSprite = modifier.volcanicEruptionWarningSprite ? modifier.volcanicEruptionWarningSprite : modifier.icon;
        Color warningTint = modifier.volcanicEruptionWarningTint;

        for (int i = 0; i < targets.Count; i++)
        {
            Vector2Int cell = targets[i];
            board.FlashTintAtCell(cell, warningTint, warningSeconds, 0.08f);
            board.FlashWarningAtCell(cell, warningSprite, warningSeconds, 0.08f, 1f);
        }

        yield return new WaitForSeconds(warningSeconds);

        if (ActiveModifier != modifier || !board)
            yield break;

        float damage = _gc ? _gc.CastleProjectileDamageForStats : 1f;
        for (int i = 0; i < targets.Count; i++)
        {
            Vector2Int cell = targets[i];
            if (!board.InBounds(cell) || board.HasObstacle(cell))
                continue;

            if (board.TryGetMonster(cell, out var inst) && inst.data && inst.hp > 0f)
            {
                board.DamageTile(cell, damage, Board.DamageSource.VolcanicEruption);
                continue;
            }

            if (board.IsFree(cell))
                board.TrySpawnCustomObstacle(cell, Board.ObstacleType.HardenedLava,
                    modifier.hardenedLavaObstacleSprite, 1, false, warningSprite);
        }
    }

    Vector2 BoardLocalToRoot(RectTransform root, Vector2 anchored)
    {
        if (!board || !board.gridRoot || !root || root == board.gridRoot)
            return anchored;

        var world = board.gridRoot.TransformPoint(new Vector3(anchored.x, anchored.y, 0f));
        var local = root.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }

    Vector2 RootLocalToBoard(RectTransform root, Vector2 anchored)
    {
        if (!board || !board.gridRoot || !root || root == board.gridRoot)
            return anchored;

        var world = root.TransformPoint(new Vector3(anchored.x, anchored.y, 0f));
        var local = board.gridRoot.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }

    void DestroyOutFlankedProjectile(RectTransform projectile)
    {
        if (projectile)
        {
            _outFlankedProjectiles.Remove(projectile);
            Destroy(projectile.gameObject);
            return;
        }

        _outFlankedProjectiles.Remove(projectile);
    }

    void ClearOutFlankedProjectiles()
    {
        for (int i = _outFlankedProjectiles.Count - 1; i >= 0; i--)
        {
            if (_outFlankedProjectiles[i])
                Destroy(_outFlankedProjectiles[i].gameObject);
        }

        _outFlankedProjectiles.Clear();
    }

    void SpreadContagionFromFloor(Vector2Int cell)
    {
        var neighbors = GetAdjacentCells(cell, includeDiagonals: false);
        for (int i = 0; i < neighbors.Count; i++)
            TryInfectCell(neighbors[i], Mathf.Clamp01(ActiveModifier.contagionSpreadChance));
    }

    void TriggerDeathExplosion(Vector2Int cell)
    {
        if (!board || !board.TryGetMonster(cell, out var deadInst) || deadInst.data == null)
            return;

        float percent = Mathf.Clamp01(ActiveModifier.deathExplosionMaxHpPercent);
        float damage = Mathf.Max(0f, deadInst.maxHp * percent);
        if (damage <= 0f)
            return;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                Vector2Int other = new Vector2Int(cell.x + dx, cell.y + dy);
                if (!board.InBounds(other))
                    continue;

                if (!board.TryGetMonster(other, out var otherInst) || otherInst.data == null || otherInst.hp <= 0f)
                    continue;

                board.DamageTile(other, damage, Board.DamageSource.DeathExplosion);
            }
        }
    }

    void ApplySwampRows()
    {
        if (!board)
            return;

        int ticks = ActiveModifier.swampPoisonTicks;
        for (int y = 1; y < board.height; y += 2)
        {
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (board.HasFloorEffect(cell))
                    continue;

                board.SetFloorEffect(
                    cell,
                    Board.FloorEffectType.Poison,
                    GetScaledFloorEffectDamage(ActiveModifier.swampPoisonDamage),
                    Mathf.Max(0.05f, ActiveModifier.swampPoisonInterval),
                    ticks);
            }
        }
    }

    float GetScaledFloorEffectDamage(float amount)
    {
        return _gc ? _gc.GetScaledFloorEffectDamage(amount) : Mathf.Max(0f, amount);
    }

    void ApplySoulLink(IReadOnlyList<Vector2Int> cells)
    {
        int groupId = Random.Range(1, int.MaxValue);
        for (int i = 0; i < cells.Count; i++)
        {
            if (!board.TryGetMonster(cells[i], out var inst))
                continue;

            inst.soulLinkGroupId = groupId;
            board.SetMonsterAt(cells[i], inst);
        }
    }

    void ApplyHalfHealthStart(IReadOnlyList<Vector2Int> cells)
    {
        float fraction = Mathf.Clamp01(ActiveModifier.startingHealthFraction);
        for (int i = 0; i < cells.Count; i++)
        {
            if (!board.TryGetMonster(cells[i], out var inst) || inst.hp <= 0f)
                continue;

            inst.hp = Mathf.Clamp(inst.maxHp * fraction, 0f, inst.maxHp);
            board.SetMonsterAt(cells[i], inst);
        }
    }

    void ApplyContagionOnPiecePlaced(IReadOnlyList<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];

            if (_contagionFloorSpreadTimers.ContainsKey(cell))
            {
                TryInfectCell(cell, 1f);
                continue;
            }

            TryInfectCell(cell, Mathf.Clamp01(ActiveModifier.contagionInfectionChanceOnPieceSet));
        }
    }

    bool TryInfectCell(Vector2Int cell, float chance)
    {
        if (!board.TryGetMonster(cell, out var inst) || inst.data == null || inst.hp <= 0f)
            return false;

        if (inst.contagionInfected)
            return false;

        if (chance < 1f && Random.value > Mathf.Clamp01(chance))
            return false;

        inst.contagionInfected = true;
        inst.contagionPercentPerTick = Mathf.Max(inst.contagionPercentPerTick, Mathf.Max(0f, ActiveModifier.contagionPercentDamagePerTick));
        inst.contagionTickTimer = 0f;
        inst.contagionSpreadTimer = 0f;
        board.SetMonsterAt(cell, inst);
        return true;
    }

    void SpreadContagionToAdjacent(Vector2Int cell, float chance)
    {
        var neighbors = GetAdjacentCells(cell, includeDiagonals: false);
        for (int i = 0; i < neighbors.Count; i++)
            TryInfectCell(neighbors[i], chance);
    }

    List<Vector2Int> GetAdjacentCells(Vector2Int cell, bool includeDiagonals)
    {
        var result = new List<Vector2Int>();
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                if (!includeDiagonals && Mathf.Abs(dx) + Mathf.Abs(dy) != 1)
                    continue;

                Vector2Int next = new Vector2Int(cell.x + dx, cell.y + dy);
                if (board && board.InBounds(next))
                    result.Add(next);
            }
        }

        return result;
    }

    void CreateContagionFloor(Vector2Int cell)
    {
        if (_contagionFloorSpreadTimers.ContainsKey(cell))
            return;

        _contagionFloorSpreadTimers[cell] = 0f;
        EnsureContagionFloorVisual(cell);
    }

    void TryStartOvergrowthCell()
    {
        if (!board)
            return;

        int fullCount = board.CountObstaclesOfType(Board.ObstacleType.Overgrowth);
        int unlockedRows = Mathf.Max(1, ActiveModifier.overgrowthInitialTargetRows) +
                           (fullCount / Mathf.Max(1, ActiveModifier.overgrowthCellsPerExtraRow));
        int maxY = Mathf.Clamp(unlockedRows - 1, 0, board.height - 1);

        var candidates = new List<Vector2Int>();
        for (int y = 0; y <= maxY; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (_growingOvergrowth.ContainsKey(cell))
                    continue;

                if (board.HasObstacle(cell))
                    continue;

                candidates.Add(cell);
            }
        }

        if (candidates.Count == 0)
            return;

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        _growingOvergrowth[chosen] = new OvergrowthState { elapsed = 0f, partialShown = false };
        SetOvergrowthVisual(chosen, ActiveModifier.overgrowthInitialSprite);
    }

    void PromoteOvergrowthToFull(Vector2Int cell)
    {
        RemoveGrowingOvergrowth(cell);

        if (board.TryGetMonster(cell, out var inst) && inst.data != null)
        {
            if (board.TilesDamageImmune && inst.hp > 0f)
                return;

            board.ForceKillMonsterAndRemove(cell, Board.DamageSource.Overgrowth);
        }
        else if (!board.IsFree(cell))
        {
            board.RemovePlacedCellImmediate(cell);
        }

        board.TrySpawnCustomObstacle(cell, Board.ObstacleType.Overgrowth, ActiveModifier.overgrowthFullSprite, 1, false);
    }

    void ClearGrowingOvergrowthInRows(HashSet<int> rows)
    {
        var keys = new List<Vector2Int>(_growingOvergrowth.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (rows.Contains(keys[i].y))
                RemoveGrowingOvergrowth(keys[i]);
        }
    }

    void RemoveGrowingOvergrowth(Vector2Int cell)
    {
        _growingOvergrowth.Remove(cell);

        if (_overgrowthVisuals.TryGetValue(cell, out var visual) && visual)
            Destroy(visual.gameObject);

        _overgrowthVisuals.Remove(cell);
    }

    void SyncPersistentVisuals()
    {
        SyncOvergrowthVisuals();
        SyncContagionFloorVisuals();
        SyncInfectionVisuals();
    }

    void SyncOvergrowthVisuals()
    {
        var keys = new List<Vector2Int>(_overgrowthVisuals.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (!_growingOvergrowth.ContainsKey(keys[i]))
            {
                if (_overgrowthVisuals[keys[i]])
                    Destroy(_overgrowthVisuals[keys[i]].gameObject);
                _overgrowthVisuals.Remove(keys[i]);
                continue;
            }

            if (_overgrowthVisuals[keys[i]])
                _overgrowthVisuals[keys[i]].rectTransform.anchoredPosition = board.CellToAnchoredPos(keys[i]);
        }
    }

    void SyncContagionFloorVisuals()
    {
        var keys = new List<Vector2Int>(_contagionFloorVisuals.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Vector2Int cell = keys[i];
            if (!_contagionFloorSpreadTimers.ContainsKey(cell))
            {
                if (_contagionFloorVisuals[cell])
                    Destroy(_contagionFloorVisuals[cell].gameObject);

                _contagionFloorVisuals.Remove(cell);
                board.ClearContagionFloorUnderlays(cell);
                continue;
            }

            if (_contagionFloorVisuals[cell])
                _contagionFloorVisuals[cell].rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
        }
    }

    void SyncInfectionVisuals()
    {
        var infectedCells = new HashSet<Vector2Int>();
        var cells = board.GetMonsterCells(includeDead: false);
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (!board.TryGetMonster(cell, out var inst) || !inst.contagionInfected || inst.hp <= 0f)
                continue;

            infectedCells.Add(cell);
            EnsureInfectionVisual(cell);
            if (_infectionVisuals.TryGetValue(cell, out var visual) && visual)
                visual.rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
        }

        var existing = new List<Vector2Int>(_infectionVisuals.Keys);
        for (int i = 0; i < existing.Count; i++)
        {
            if (infectedCells.Contains(existing[i]))
                continue;

            if (_infectionVisuals[existing[i]])
                Destroy(_infectionVisuals[existing[i]].gameObject);
            _infectionVisuals.Remove(existing[i]);
        }
    }

    void SetOvergrowthVisual(Vector2Int cell, Sprite sprite)
    {
        if (!board)
            return;

        if (_overgrowthVisuals.TryGetValue(cell, out var existing) && existing)
        {
            existing.sprite = sprite;
            existing.color = ActiveModifier ? ActiveModifier.overgrowthTint : Color.white;
            existing.rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
            return;
        }

        var img = CreateOverlayImage(
            $"Overgrowth_{cell.x}_{cell.y}",
            board.overlayRoot ? board.overlayRoot : board.gridRoot,
            sprite,
            ActiveModifier ? ActiveModifier.overgrowthTint : Color.white,
            board.GetCellSize());

        img.rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
        _overgrowthVisuals[cell] = img;
    }

    void EnsureContagionFloorVisual(Vector2Int cell)
    {
        if (_contagionFloorVisuals.ContainsKey(cell))
            return;

        board.SetContagionFloorUnderlays(
            cell,
            ActiveModifier.contagionFloorTint,
            board.contagionSecondaryUnderlayTint);

        var img = CreateOverlayImage(
            $"ContagionFloor_{cell.x}_{cell.y}",
            board.underlayRoot ? board.underlayRoot : board.gridRoot,
            ActiveModifier.contagionFloorSprite,
            ActiveModifier.contagionFloorTint,
            board.GetCellSize());

        img.rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
        _contagionFloorVisuals[cell] = img;
    }

    void EnsureInfectionVisual(Vector2Int cell)
    {
        if (_infectionVisuals.ContainsKey(cell))
            return;

        var img = CreateOverlayImage(
            $"Infection_{cell.x}_{cell.y}",
            board.overlayRoot ? board.overlayRoot : board.gridRoot,
            ActiveModifier.contagionUnitOverlaySprite,
            ActiveModifier.contagionUnitTint,
            board.GetCellSize() * 0.85f);

        img.rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
        _infectionVisuals[cell] = img;
    }

    Image CreateOverlayImage(string name, RectTransform parent, Sprite sprite, Color tint, Vector2 size)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite ? sprite : OnePx();
        img.color = tint;
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.type = Image.Type.Simple;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return img;
    }

    void EnsureBoardPresentation()
    {
        if (!board)
            return;

        EnsureAnimatedBackgroundRefs();
        EnsureVolcanicBoardAnimationRefs();

        if (!backgroundOverrideImage)
        {
            var go = new GameObject("LevelModifierBackground", typeof(Image));
            go.transform.SetParent(board.transform, false);

            backgroundOverrideImage = go.GetComponent<Image>();
            backgroundOverrideImage.raycastTarget = false;
            backgroundOverrideImage.preserveAspect = false;

            var rt = backgroundOverrideImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetSiblingIndex(0);
        }

        ApplyResolvedBackgroundSprite();
        SyncVolcanicBoardAnimationRoots();

        if (ActiveModifier && ActiveModifier.enableRainOverlay)
        {
            if (!_rainOverlay)
            {
                var go = new GameObject("LevelModifierRain", typeof(RectTransform), typeof(RainOverlayUI));
                go.transform.SetParent(board.underlayRoot ? board.underlayRoot : board.transform, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;

                _rainOverlay = go.GetComponent<RainOverlayUI>();
            }

            _rainOverlay.Configure(
                ActiveModifier.rainDrops,
                ActiveModifier.rainSpeed,
                ActiveModifier.rainDropLength,
                ActiveModifier.rainDropWidth,
                ActiveModifier.rainAngle,
                ActiveModifier.rainTint);
            _rainOverlay.gameObject.SetActive(true);

            AudioManager.I?.PlayRainAmbience();
        }
        else
        {
            if (_rainOverlay)
                _rainOverlay.gameObject.SetActive(false);

            AudioManager.I?.StopRainAmbience();
        }
    }

    void ResetLevelState()
    {
        ActiveModifier = null;
        IsSelectionRunning = false;
        SetSelectionCursorState(false);

        _overgrowthSpawnTimer = 0f;
        _stormCountdown = 0f;
        _rearAmbushCountdown = 0f;
        _lowRationsCountdown = 0f;
        _autoRotateTimer = 0f;
        _autoShiftTimer = 0f;
        _autoShiftPauseRemaining = 0f;
        _autoShiftMovingRight = true;
        _outFlankedCountdown = 0f;
        _volcanicEruptionCountdown = 0f;
        _comboShieldRemaining = 0;
        _pendingComboShieldBreaks = 0;
        _rearAmbushRowsSpawnedThisLevel = 0;
        RefreshComboShieldUI(false);

        _growingOvergrowth.Clear();
        _contagionFloorSpreadTimers.Clear();
        _soulLinkPropagationGroups.Clear();
        _outFlankedRows.Clear();
        _volcanicTargetSet.Clear();
        _volcanicTargetList.Clear();

        ClearVisualDictionary(_overgrowthVisuals);
        ClearVisualDictionary(_contagionFloorVisuals);
        ClearVisualDictionary(_infectionVisuals);
        ClearOutFlankedProjectiles();

        ApplyResolvedBackgroundSprite();
        SyncVolcanicBoardAnimationRoots();

        if (_rainOverlay)
            _rainOverlay.gameObject.SetActive(false);

        AudioManager.I?.StopRainAmbience();

        RefreshModifierUI();
    }

    void CacheLevelBackground(CastleData castleData)
    {
        _currentLevelBackgroundSprite = castleData ? castleData.levelBackgroundSprite : null;
        _currentLevelBackgroundVideoClip = castleData ? castleData.levelBackgroundVideoClip : null;
    }

    void CacheLevelBackgroundFromCurrentLevel()
    {
        if (!_gc)
            _gc = GetComponent<GameController>();

        CastleData castleData = null;
        if (_gc && _gc.castlesByLevel != null)
        {
            int levelIndex = _gc.CurrentLevel;
            if (levelIndex >= 0 && levelIndex < _gc.castlesByLevel.Length)
                castleData = _gc.castlesByLevel[levelIndex];
        }

        CacheLevelBackground(castleData);
    }

    Sprite ResolveBackgroundSprite()
    {
        if (ActiveModifier && ActiveModifier.backgroundOverrideSprite)
            return ActiveModifier.backgroundOverrideSprite;

        return _currentLevelBackgroundSprite ? _currentLevelBackgroundSprite : defaultBackgroundSprite;
    }

    VideoClip ResolveBackgroundVideoClip()
    {
        if (ActiveModifier && ActiveModifier.backgroundOverrideVideoClip)
            return ActiveModifier.backgroundOverrideVideoClip;

        return _currentLevelBackgroundVideoClip;
    }

    void ApplyResolvedBackgroundSprite()
    {
        if (!backgroundOverrideImage)
            return;

        VideoClip videoClip = ResolveBackgroundVideoClip();
        ApplyAnimatedBackground(videoClip);

        Sprite bg = ResolveBackgroundSprite();
        if (bg)
            backgroundOverrideImage.sprite = bg;

        backgroundOverrideImage.color = Color.white;
        backgroundOverrideImage.gameObject.SetActive(true);
        backgroundOverrideImage.enabled = true;
    }

    bool ApplyAnimatedBackground(VideoClip clip)
    {
        EnsureAnimatedBackgroundRefs();
        if (!clip || !animatedBackgroundDisplay || !animatedBackgroundPlayer)
        {
            StopAnimatedBackground(clearTexture: clip == null);
            return false;
        }

        HookAnimatedBackgroundEvents();

        GameObject displayGO = animatedBackgroundDisplay.gameObject;
        if (!displayGO.activeSelf)
            displayGO.SetActive(true);

        GameObject playerGO = animatedBackgroundPlayer.gameObject;
        if (!playerGO.activeSelf)
            playerGO.SetActive(true);

        animatedBackgroundDisplay.enabled = true;
        animatedBackgroundDisplay.raycastTarget = false;

        if (!animatedBackgroundPlayer.targetTexture && animatedBackgroundDisplay.texture is RenderTexture displayTexture)
            animatedBackgroundPlayer.targetTexture = displayTexture;

        if (animatedBackgroundPlayer.targetTexture)
            animatedBackgroundDisplay.texture = animatedBackgroundPlayer.targetTexture;

        animatedBackgroundDisplay.rectTransform.SetAsFirstSibling();
        animatedBackgroundPlayer.playOnAwake = false;
        animatedBackgroundPlayer.isLooping = true;
        animatedBackgroundPlayer.renderMode = VideoRenderMode.RenderTexture;
        animatedBackgroundPlayer.audioOutputMode = VideoAudioOutputMode.None;

        if (animatedBackgroundPlayer.clip == clip && (animatedBackgroundPlayer.isPlaying || animatedBackgroundPlayer.isPrepared))
            return true;

        ClearAnimatedBackgroundRenderTexture();

        _pendingAnimatedBackgroundClip = clip;
        animatedBackgroundPlayer.Stop();
        animatedBackgroundPlayer.clip = clip;
        animatedBackgroundPlayer.Prepare();
        return true;
    }

    void StopAnimatedBackground(bool clearTexture)
    {
        EnsureAnimatedBackgroundRefs();
        _pendingAnimatedBackgroundClip = null;

        if (animatedBackgroundPlayer)
        {
            animatedBackgroundPlayer.Stop();
            animatedBackgroundPlayer.clip = null;

            if (clearTexture)
                ClearAnimatedBackgroundRenderTexture();

            animatedBackgroundPlayer.gameObject.SetActive(false);
        }

        if (animatedBackgroundDisplay)
        {
            animatedBackgroundDisplay.enabled = false;
            animatedBackgroundDisplay.gameObject.SetActive(false);
        }
    }

    void ClearAnimatedBackgroundRenderTexture()
    {
        if (!animatedBackgroundPlayer || !animatedBackgroundPlayer.targetTexture)
            return;

        RenderTexture rt = animatedBackgroundPlayer.targetTexture;
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
    }

    void EnsureAnimatedBackgroundRefs()
    {
        if (!board)
            return;

        if (!animatedBackgroundDisplay)
            animatedBackgroundDisplay = FindComponentByGameObjectName<RawImage>("AnimatedBG_VideoDisplay_RawImage");

        if (!animatedBackgroundPlayer)
        {
            if (animatedBackgroundDisplay)
                animatedBackgroundPlayer = animatedBackgroundDisplay.GetComponentInChildren<VideoPlayer>(true);

            if (!animatedBackgroundPlayer)
                animatedBackgroundPlayer = FindComponentByGameObjectName<VideoPlayer>("AnimatedBG_VideoPlayer");
        }

        HookAnimatedBackgroundEvents();
    }

    void HookAnimatedBackgroundEvents()
    {
        if (!animatedBackgroundPlayer || _hookedAnimatedBackgroundPlayer == animatedBackgroundPlayer)
            return;

        UnhookAnimatedBackgroundEvents();
        animatedBackgroundPlayer.prepareCompleted += OnAnimatedBackgroundPrepared;
        _hookedAnimatedBackgroundPlayer = animatedBackgroundPlayer;
    }

    void UnhookAnimatedBackgroundEvents()
    {
        if (!_hookedAnimatedBackgroundPlayer)
            return;

        _hookedAnimatedBackgroundPlayer.prepareCompleted -= OnAnimatedBackgroundPrepared;
        _hookedAnimatedBackgroundPlayer = null;
    }

    void OnAnimatedBackgroundPrepared(VideoPlayer player)
    {
        if (!player || (_pendingAnimatedBackgroundClip && player.clip != _pendingAnimatedBackgroundClip))
            return;

        player.Play();
    }

    void EnsureVolcanicBoardAnimationRefs()
    {
        if (!board)
            return;

        if (!volcanicEruptionAnimsRoot)
            volcanicEruptionAnimsRoot = FindChildGameObjectByName("VolcanicEruption_Anims");

        if (!defaultBoardBordersRoot)
            defaultBoardBordersRoot = FindChildGameObjectByName("DefaultBoardBorders");
    }

    void SyncVolcanicBoardAnimationRoots()
    {
        EnsureVolcanicBoardAnimationRefs();

        bool volcanicActive = ActiveModifier && ActiveModifier.kind == LevelModifierKind.VolcanicEruption;

        ConfigureVolcanicLavaBorderAnimators();

        if (volcanicEruptionAnimsRoot)
            volcanicEruptionAnimsRoot.SetActive(volcanicActive);

        if (defaultBoardBordersRoot)
            defaultBoardBordersRoot.SetActive(!volcanicActive);
    }

    void ConfigureVolcanicLavaBorderAnimators()
    {
        if (!volcanicEruptionAnimsRoot)
            return;

        var animators = volcanicEruptionAnimsRoot.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator && IsVolcanicLavaBorderAnimator(animator))
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    bool IsVolcanicLavaBorderAnimator(Animator animator)
    {
        Transform current = animator ? animator.transform : null;
        while (current)
        {
            if (current.name.IndexOf("LavaBorder", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (volcanicEruptionAnimsRoot && current == volcanicEruptionAnimsRoot.transform)
                break;

            current = current.parent;
        }

        return false;
    }

    T FindComponentByGameObjectName<T>(string objectName) where T : Component
    {
        if (!board || string.IsNullOrWhiteSpace(objectName))
            return null;

        var components = board.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] && components[i].gameObject.name == objectName)
                return components[i];
        }

        return null;
    }

    GameObject FindChildGameObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        if (board)
        {
            var childTransforms = board.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < childTransforms.Length; i++)
            {
                if (childTransforms[i] && childTransforms[i].name == objectName)
                    return childTransforms[i].gameObject;
            }
        }

        var sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            if (sceneTransforms[i] &&
                sceneTransforms[i].gameObject.scene.IsValid() &&
                sceneTransforms[i].name == objectName)
            {
                return sceneTransforms[i].gameObject;
            }
        }

        return null;
    }

    void ClearVisualDictionary(Dictionary<Vector2Int, Image> dict)
    {
        var keys = new List<Vector2Int>(dict.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (dict[keys[i]])
                Destroy(dict[keys[i]].gameObject);
        }

        dict.Clear();
    }

    void EnsureSelectionUI()
    {
        if (_selectionUI)
            return;

        if (selectionUI)
        {
            _selectionUI = selectionUI;
            return;
        }

        _selectionUI = FindFirstObjectByType<LevelModifierSelectionUI>(FindObjectsInactive.Include);

        if (!_selectionUI)
            Debug.LogWarning("LevelModifierController: No LevelModifierSelectionUI found in scene.");
    }

    bool TryPickStormCell(out Vector2Int cell, HashSet<Vector2Int> used)
    {
        cell = default;
        if (!board)
            return false;

        int maxRow = Mathf.Clamp(Mathf.Max(1, ActiveModifier.stormLowerRowsOnlyCount) - 1, 0, board.height - 1);
        var candidates = new List<Vector2Int>();
        for (int y = 0; y <= maxRow; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int next = new Vector2Int(x, y);
                if (used.Contains(next))
                    continue;

                candidates.Add(next);
            }
        }

        if (candidates.Count == 0)
            return false;

        cell = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    List<Vector2Int> BuildStormStrikeCells(Vector2Int center, bool cross)
    {
        var result = new List<Vector2Int> { center };
        if (!cross)
            return result;

        var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int next = center + directions[i];
            if (board && board.InBounds(next))
                result.Add(next);
        }

        return result;
    }

    static Sprite OnePx()
    {
        if (_onePx)
            return _onePx;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _onePx = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _onePx;
    }

    void SetSelectionCursorState(bool active)
    {
        if (active)
        {
            MaintainSelectionCursorState();
            return;
        }

        if (_gc && _gc.pauseCursor)
        {
            _gc.pauseCursor.SetKeepVisibleDuringButtonNavigation(false);
            _gc.pauseCursor.SetVisible(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void MaintainSelectionCursorState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        if (_gc && _gc.pauseCursor)
        {
            _gc.pauseCursor.SetKeepVisibleDuringButtonNavigation(false);
            _gc.pauseCursor.SetVisible(true);
            _gc.pauseCursor.SetScale(SettingsStore.LoadCursorScale());
        }
    }

    void RefreshComboShieldUI(bool pulse)
    {
        bool active = ActiveModifier && ActiveModifier.kind == LevelModifierKind.ComboShield && _comboShieldRemaining > 0;

        if (comboShieldImageA)
            comboShieldImageA.gameObject.SetActive(active);

        if (comboShieldImageB)
            comboShieldImageB.gameObject.SetActive(active);

        string countText = _comboShieldRemaining.ToString();

        if (comboShieldCountTextA)
            comboShieldCountTextA.text = active ? countText : string.Empty;

        if (comboShieldCountTextB)
            comboShieldCountTextB.text = active ? countText : string.Empty;

        if (!active)
        {
            StopComboShieldPulse();
            ResetComboShieldPulseScale();
            return;
        }

        if (pulse)
        {
            StopComboShieldPulse();
            _comboShieldPulseCR = StartCoroutine(PulseComboShieldUI());
        }
    }

    IEnumerator PulseComboShieldUI()
    {
        Vector3 aBase = comboShieldCountTextA ? comboShieldCountTextA.rectTransform.localScale : Vector3.one;
        Vector3 bBase = comboShieldCountTextB ? comboShieldCountTextB.rectTransform.localScale : Vector3.one;

        float half = Mathf.Max(0.05f, comboShieldPulseDuration * 0.5f);
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            float scale = Mathf.Lerp(1f, comboShieldPulseScale, k);
            ApplyComboShieldTextScale(aBase, bBase, scale);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            float scale = Mathf.Lerp(comboShieldPulseScale, 1f, k);
            ApplyComboShieldTextScale(aBase, bBase, scale);
            yield return null;
        }

        ApplyComboShieldTextScale(aBase, bBase, 1f);
        _comboShieldPulseCR = null;
    }

    void ApplyComboShieldTextScale(Vector3 aBase, Vector3 bBase, float mult)
    {
        if (comboShieldCountTextA)
            comboShieldCountTextA.rectTransform.localScale = aBase * mult;

        if (comboShieldCountTextB)
            comboShieldCountTextB.rectTransform.localScale = bBase * mult;
    }

    void StopComboShieldPulse()
    {
        if (_comboShieldPulseCR != null)
        {
            StopCoroutine(_comboShieldPulseCR);
            _comboShieldPulseCR = null;
        }
    }

    void ResetComboShieldPulseScale()
    {
        if (comboShieldCountTextA)
            comboShieldCountTextA.rectTransform.localScale = Vector3.one;

        if (comboShieldCountTextB)
            comboShieldCountTextB.rectTransform.localScale = Vector3.one;
    }
}
