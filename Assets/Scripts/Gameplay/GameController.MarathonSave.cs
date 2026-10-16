using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    float _savedBossGravityEndsAt, _savedDoubleStatsEndsAt, _savedImmunityEndsAt;
    int _snapshotTransitions;
    bool _restoringMarathon;
    string _snapshotSaveError;
    readonly Dictionary<RectTransform, MarathonMatchSnapshot.ProjectileState> _snapshotProjectiles = new();

    bool CanCaptureMarathon()
    {
        // A coroutine part-way through a row collapse or a telegraphed board
        // mutation cannot be reconstructed from just the resulting board state.
        // Keep the match open instead of reporting success with a lossy snapshot.
        bool safe = isPaused && gameBoard && piece && piece.HasActiveCells && !gameOver && !levelWon && !winQueued &&
            !marathonRewardActive && !_levelStartBlocked && !_roundTransitionActive && !_specialAbilityCinematicActive &&
            !_environmentRowClearResolving && !_rowClearComboResolutionActive && _snapshotTransitions == 0;
        _snapshotSaveError = safe ? null : "An attack or board change is still resolving. Resume briefly, then pause and use Save and Quit again. Your match is still open.";
        return safe;
    }

    IEnumerator TrackSnapshotTransition(IEnumerator action)
    {
        int token = _roundLifecycleToken;
        _snapshotTransitions++;
        try { yield return action; }
        finally { if (token == _roundLifecycleToken) _snapshotTransitions = Mathf.Max(0, _snapshotTransitions - 1); }
    }

    MarathonMatchSnapshot CaptureMarathonMatch()
    {
        var state = new MarathonMatchSnapshot {
            board = gameBoard.CaptureMarathonBoard(), piece = piece.CaptureMarathonPiece(),
            boulders = boulders.CaptureMarathonBoulders(), randomState = Random.state,
            levelTimer = _levelTimer, baseFallInterval = _thisLevelBaseFallInterval,
            castleAttackTimer = _castleAttackTimer, bossAbilityTimer = _bossAbilityTimer, bossNextAbilityAt = _bossNextAbilityAt,
            comboCount = _comboCount, comboTimer = _comboTimer, maxCombo = _maxComboThisLevel,
            obstaclesDestroyed = _obstaclesDestroyedThisLevel, levelStartMaxLives = _levelStartMaxLives,
            levelStartReserveUnits = _levelStartReserveUnits, rouletteTimer = _roulettePreviewTimer,
            normalPiecesSinceSpecial = _normalPiecesPulledSinceLastSpecial,
            pylonShield = _bossPylonShieldActive, specialSiphon = _specialSiphonActive,
            forcedPiece = MatchSnapshotAssets.Key(_bossForcedDuplicationPiece), forcedPieceCount = _bossForcedDuplicationRemaining,
            bossGravityBonus = _bossGravityBonusActive,
            bossGravityRemaining = _bossGravityCR != null ? Mathf.Max(0.0001f, _savedBossGravityEndsAt - Time.time) : 0f,
            doubleStatsRemaining = _playerDoubleStatsCR != null ? Mathf.Max(0.0001f, _savedDoubleStatsEndsAt - Time.time) : 0f,
            immunityRemaining = immunityActive ? Mathf.Max(0.0001f, _savedImmunityEndsAt - Time.time) : 0f,
            castleInvulnerabilityRemaining = enemyCastleUI ? enemyCastleUI.InvulnerabilityRemaining : 0f,
            slowGravitySource = (int)_activeTimedSlowGravitySource, slowGravityRemaining = _activeTimedSlowGravityRemainingSeconds,
            slowGravityMultiplier = _slowGravitySpecialMultActive, slowGravityRampMultiplier = _slowGravitySpecialRampRateMultActive,
            lastBossAbility = (int)_bossLastAbility, firstFullRowTutorialShown = _firstFullRowTutorialShownThisRun
        };
        foreach (var cooldown in _bossNextReadyTime)
            state.bossCooldowns.Add(new MarathonMatchSnapshot.BossCooldown { ability = (int)cooldown.Key, remaining = cooldown.Value - Time.time });
        var monsterSets = monstersBag.ToArray();
        int i = 0;
        foreach (var next in bag)
        {
            var queued = new MarathonMatchSnapshot.QueuedPiece { asset = MatchSnapshotAssets.Key(next) };
            if (i < monsterSets.Length && monsterSets[i] != null)
                foreach (var monster in monsterSets[i]) queued.monsters.Add(MatchSnapshotAssets.Key(monster));
            state.queue.Add(queued); i++;
        }
        foreach (var entry in _snapshotProjectiles)
        {
            if (!entry.Key || !entry.Key.gameObject.activeInHierarchy) continue;
            var projectile = entry.Value;
            projectile.position = ProjectilePositionToCells(entry.Key.anchoredPosition);
            state.projectiles.Add(projectile);
        }
        return state;
    }

    bool SnapshotAssetsAvailable(MarathonMatchSnapshot state)
    {
        var assets = new MatchSnapshotAssets();
        if (!assets.Get<TetrominoData>(state.piece.asset)) return false;
        foreach (string monster in state.piece.monsters)
            if (!string.IsNullOrEmpty(monster) && !assets.Get<MonsterData>(monster)) return false;
        foreach (var next in state.queue)
        {
            if (!assets.Get<TetrominoData>(next.asset)) return false;
            foreach (string monster in next.monsters)
                if (!string.IsNullOrEmpty(monster) && !assets.Get<MonsterData>(monster)) return false;
        }
        foreach (var tile in state.board.tiles)
            if (tile.hasMonster && !assets.Get<MonsterData>(tile.monster)) return false;
        foreach (var projectile in state.projectiles)
            if (!string.IsNullOrEmpty(projectile.monster) && !assets.Get<MonsterData>(projectile.monster)) return false;
        return string.IsNullOrEmpty(state.forcedPiece) || assets.Get<TetrominoData>(state.forcedPiece);
    }

    void RestoreMarathonMatch(MarathonMatchSnapshot state)
    {
        _restoringMarathon = true;
        // Freeze before creating gameplay objects or starting effect routines.
        isPaused = true; Time.timeScale = 0f; AudioListener.pause = true;
        _levelStartBlocked = false;
        var assets = new MatchSnapshotAssets();
        gameBoard.RestoreMarathonBoard(state.board, assets);
        _levelTimer = state.levelTimer; _thisLevelBaseFallInterval = state.baseFallInterval;
        _castleAttackTimer = state.castleAttackTimer; _bossAbilityTimer = state.bossAbilityTimer;
        _bossNextAbilityAt = state.bossNextAbilityAt;
        _comboCount = state.comboCount; _comboTimer = state.comboTimer; _maxComboThisLevel = state.maxCombo;
        _obstaclesDestroyedThisLevel = state.obstaclesDestroyed;
        _levelStartMaxLives = state.levelStartMaxLives; _levelStartReserveUnits = state.levelStartReserveUnits;
        _roulettePreviewTimer = state.rouletteTimer; _normalPiecesPulledSinceLastSpecial = state.normalPiecesSinceSpecial;
        _bossPylonShieldActive = state.pylonShield;
        _bossLastAbility = (CastleData.BossAbilityKind)state.lastBossAbility;
        _bossNextReadyTime.Clear();
        foreach (var cooldown in state.bossCooldowns)
            _bossNextReadyTime[(CastleData.BossAbilityKind)cooldown.ability] = Time.time + cooldown.remaining;
        _firstFullRowTutorialShownThisRun = state.firstFullRowTutorialShown;
        _bossForcedDuplicationPiece = assets.Get<TetrominoData>(state.forcedPiece);
        _bossForcedDuplicationRemaining = state.forcedPieceCount;
        bag.Clear(); monstersBag.Clear();
        foreach (var next in state.queue)
        {
            bag.Enqueue(assets.Get<TetrominoData>(next.asset));
            var monsters = new MonsterData[next.monsters.Count];
            for (int i = 0; i < monsters.Length; i++) monsters[i] = assets.Get<MonsterData>(next.monsters[i]);
            monstersBag.Enqueue(monsters);
        }
        boulders.RestoreMarathonBoulders(state.boulders);
        RestoreMarathonEffects(state);
        piece.RestoreMarathonPiece(state.piece, assets);
        ShowNextPreview();
        if (scoreUI) scoreUI.SetCombo(_comboCount);
        if (_bossForcedDuplicationRemaining > 0) ShowForcedDuplicationCountPulse();
        RefreshPylonShieldState();
        foreach (var projectile in state.projectiles)
        {
            Vector2 start = ProjectileCellsToPosition(projectile.position), target = ProjectileCellsToPosition(projectile.target);
            if (projectile.enemy) SpawnCastleDownProjectile(assets.Get<Sprite>(projectile.sprite), start, target, projectile.column);
            else SpawnAttackProjectile(assets.Get<Sprite>(projectile.sprite), assets.Get<Sprite>(projectile.altSprite),
                (AttackAnimType)projectile.animation, start, target, projectile.damage, assets.Get<MonsterData>(projectile.monster));
        }
        UpdateLevelTimerUI();
        UpdateGravityText(piece.fallInterval);
        EnsureMarathonUI(); RefreshMarathonGauge();
        Random.state = state.randomState;
        _restoringMarathon = false;
    }

    void RestoreMarathonEffects(MarathonMatchSnapshot state)
    {
        if (enemyCastleUI && state.castleInvulnerabilityRemaining > 0f)
            enemyCastleUI.StartInvulnerability(state.castleInvulnerabilityRemaining);
        if (state.bossGravityRemaining > 0f)
        {
            _bossGravityBonusActive = 0f;
            _bossGravityCR = StartCoroutine(BossGravityRoutine(state.bossGravityBonus, state.bossGravityRemaining));
        }
        if (state.doubleStatsRemaining > 0f)
            _playerDoubleStatsCR = StartCoroutine(PlayerDoubleStatsCo(state.doubleStatsRemaining, restoring: true));
        if (state.immunityRemaining > 0f)
            StartCoroutine(GlobalImmunityCo(state.immunityRemaining, restoring: true));
        if (state.slowGravityRemaining > 0f)
        {
            var source = (TimedSlowGravitySource)state.slowGravitySource;
            if (source == TimedSlowGravitySource.PlayerAbility)
                _playerGravityCR = StartCoroutine(PlayerReducedGravityCo(state.slowGravityRemaining));
            else if (source == TimedSlowGravitySource.SpecialBlock)
            {
                _slowGravitySpecialMultActive = state.slowGravityMultiplier;
                _slowGravitySpecialRampRateMultActive = state.slowGravityRampMultiplier;
                _slowGravitySpecialVisualActive = true;
                SetTimedSlowGravityEffect(source, state.slowGravityRemaining);
                EnsureSlowGravityImage();
                if (slowGravityImage) slowGravityImage.gameObject.SetActive(true);
                gameBoard.PlaySlowGravityBoardVFX(state.slowGravityRemaining, () => _activeTimedSlowGravityRemainingSeconds);
                _slowGravitySpecialCR = StartCoroutine(SlowGravitySpecialCo(state.slowGravityRemaining));
            }
        }
        if (state.specialSiphon)
        {
            _specialSiphonActive = true;
            _specialSiphonCR = StartCoroutine(ResumeSavedSimulation(SpecialSiphonRoutine(_roundLifecycleToken)));
        }
    }

    IEnumerator ResumeSavedSimulation(IEnumerator simulation)
    {
        // Creation restores visuals now; simulation starts only after Resume.
        while (IsGameplaySuspended) yield return null;
        yield return simulation;
    }

    void OpenRestoredPauseMenu()
    {
        isPaused = true; Time.timeScale = 0f; AudioListener.pause = true;
        if (AudioManager.I) AudioManager.I.PlayPauseMusic();
        if (pausePanel) UIPanelTransition.Show(pausePanel, true);
        RefreshPauseMenuNavigation();
        EnterUICursorMode();
        StartCoroutine(ReapplyUICursorNextFrame());
    }

    Vector2 ProjectilePositionToCells(Vector2 position)
    {
        Vector2 offset = ProjectileRootToBoardLocal(position) - gameBoard.CellToAnchoredPos(Vector2Int.zero);
        Vector2 size = gameBoard.GetCellSize();
        return new Vector2(offset.x / size.x, offset.y / size.y);
    }
    Vector2 ProjectileCellsToPosition(Vector2 cells) => BoardLocalToProjectileRoot(
        gameBoard.CellToAnchoredPos(Vector2Int.zero) + Vector2.Scale(cells, gameBoard.GetCellSize()));

    void TrackSavedProjectile(RectTransform rt, Sprite sprite, Sprite alt, AttackAnimType animation,
        Vector2 target, int damage, MonsterData monster, bool enemy = false, int column = 0)
    {
        if (!IsMarathon) return;
        _snapshotProjectiles[rt] = new MarathonMatchSnapshot.ProjectileState {
            enemy = enemy, sprite = MatchSnapshotAssets.Key(sprite), altSprite = MatchSnapshotAssets.Key(alt),
            animation = (int)animation, target = ProjectilePositionToCells(target), damage = damage,
            monster = MatchSnapshotAssets.Key(monster), column = column };
    }
}
