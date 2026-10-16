#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class MarathonSaveTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    readonly List<Object> temporary = new();
    static void Set(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, Private);
        if (field != null) field.SetValue(target, value);
        else target.GetType().GetProperty(name, Private).SetValue(target, value);
    }
    static T Get<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, Private);
        return (T)(field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Private).GetValue(target));
    }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);

    [SetUp] public void SetUp()
    {
        LocalSaveTestScope.Begin();
        SessionState.SetInt("MarathonSaveTests.Mode", (int)GameModeStore.Current);
        SessionState.SetString("MarathonSaveTests.Playlist", PlayerPrefs.GetString(GameplayPlaylist.PreferenceKey, ""));
        SessionState.SetBool("MarathonSaveTests.HadPlaylist", PlayerPrefs.HasKey(GameplayPlaylist.PreferenceKey));
        SessionState.SetInt("MarathonSaveTests.PendingWrite", PlayerPrefs.GetInt("gameplay_playlist_pending_file_write", 0));
        PlayerPrefs.DeleteKey("gameplay_playlist_pending_file_write");
    }
    [TearDown] public void TearDown()
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        foreach (var item in temporary) if (item) Object.DestroyImmediate(item);
        temporary.Clear();
        LocalSaveTestScope.RestoreScenePreferences();
        TempRunSaveStore.CancelPendingResume();
        GameModeStore.Current = (RunGameMode)SessionState.GetInt("MarathonSaveTests.Mode", 0);
        if (SessionState.GetBool("MarathonSaveTests.HadPlaylist", false))
            PlayerPrefs.SetString(GameplayPlaylist.PreferenceKey, SessionState.GetString("MarathonSaveTests.Playlist", ""));
        else PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        PlayerPrefs.SetInt("gameplay_playlist_pending_file_write", SessionState.GetInt("MarathonSaveTests.PendingWrite", 0));
        PlayerPrefs.Save();
        LocalSaveTestScope.End();
    }

    [Test] public void PlaylistFileSurvivesPreferenceLossAndRestoresBackup()
    {
        var audio = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab").GetComponent<AudioManager>();
        PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        GameplayPlaylist.SetEnabled(audio.EDMGameplayMusic[0], false);
        GameplayPlaylist.SetEnabled(audio.MetalBossMusic[0], false);
        string path = Path.Combine(LocalSaveFiles.DirectoryPath, GameplayPlaylist.FileName);
        Assert.IsTrue(File.Exists(path));
        PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        LocalSaveTestScope.Apply();
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.EDMGameplayMusic[0]));
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.MetalBossMusic[0]));
        Assert.IsTrue(GameplayPlaylist.IsEnabled(audio.EDMGameplayMusic[1]));
        File.WriteAllText(path, "broken json");
        PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        LocalSaveTestScope.Apply();
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.EDMGameplayMusic[0]), "The prior valid file is recovered.");
    }

    [Test] public void AtomicRunSaveRecoversBackupAndFailedWriteDoesNotReportSuccess()
    {
        var save = new TempRunSaveStore.SaveData { gameMode = RunGameMode.Marathon, currentLevel = 8, score = 80 };
        Assert.IsTrue(TempRunSaveStore.Save(save));
        save.score = 95;
        Assert.IsTrue(TempRunSaveStore.Save(save));
        string path = Path.Combine(LocalSaveFiles.DirectoryPath, "tetrabeasts_temp_run.json");
        Assert.IsTrue(File.Exists(path + ".bak"));
        Directory.CreateDirectory(path + ".tmp");
        save.score = 120;
        Assert.IsFalse(TempRunSaveStore.Save(save));
        Assert.IsTrue(TempRunSaveStore.TryLoad(out var unchanged)); Assert.AreEqual(95, unchanged.score);
        File.WriteAllText(path, "incomplete write");
        Assert.IsTrue(TempRunSaveStore.TryLoad(out var backup)); Assert.AreEqual(80, backup.score);
        Directory.Delete(path + ".tmp");
        TempRunSaveStore.Delete();
        Assert.IsFalse(TempRunSaveStore.Exists(), "Discard must delete backups too, so Continue cannot resurrect a discarded run.");
    }

    [Test] public void PlaylistMigratesOldPreferencesAndRetriesInterruptedFileWrites()
    {
        var audio = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab").GetComponent<AudioManager>();
        PlayerPrefs.SetString(GameplayPlaylist.PreferenceKey, "{\"songs\":[\"" + audio.EDMGameplayMusic[0].name + "\"]}");
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.EDMGameplayMusic[0]));
        PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        LocalSaveTestScope.Apply();
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.EDMGameplayMusic[0]), "The migrated file works without the old preferences.");
        string path = Path.Combine(LocalSaveFiles.DirectoryPath, GameplayPlaylist.FileName);
        Directory.CreateDirectory(path + ".tmp");
        GameplayPlaylist.SetEnabled(audio.MetalGameplayMusic[0], false);
        Assert.AreEqual(1, PlayerPrefs.GetInt("gameplay_playlist_pending_file_write"));
        Directory.Delete(path + ".tmp");
        LocalSaveTestScope.Apply();
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.MetalGameplayMusic[0]), "A stale file must not override a pending newer preference.");
        Assert.AreEqual(0, PlayerPrefs.GetInt("gameplay_playlist_pending_file_write"));
        PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        LocalSaveTestScope.Apply();
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.MetalGameplayMusic[0]));
    }

    [Test] public void LegacyCheckpointsLoadAndBrokenLiveSnapshotsAreRejected()
    {
        string path = Path.Combine(LocalSaveFiles.DirectoryPath, "tetrabeasts_temp_run.json");
        Directory.CreateDirectory(LocalSaveFiles.DirectoryPath);
        File.WriteAllText(path, "{\"version\":1,\"currentLevel\":4,\"score\":30}");
        Assert.IsTrue(TempRunSaveStore.TryLoad(out var legacy));
        Assert.IsNull(legacy.marathonSnapshot); Assert.AreEqual(RunGameMode.Standard, legacy.gameMode);
        legacy.gameMode = RunGameMode.Marathon; legacy.marathonSnapshot = new MarathonMatchSnapshot();
        Assert.IsFalse(TempRunSaveStore.Save(legacy));
        legacy.marathonSnapshot = null; legacy.version = 999;
        Assert.IsFalse(TempRunSaveStore.Save(legacy));
    }

    GameController CreateFixture()
    {
        var root = new GameObject("Snapshot controller"); temporary.Add(root); root.SetActive(false);
        var gc = root.AddComponent<GameController>(); gc.enabled = false;
        var piece = root.AddComponent<Piece>(); piece.enabled = false;
        var tilePrefab = new GameObject("Active tile", typeof(RectTransform), typeof(Image)); temporary.Add(tilePrefab);
        piece.activeTilePrefab = tilePrefab.GetComponent<Image>();
        var modifiers = root.AddComponent<LevelModifierController>(); modifiers.enabled = false;
        gc.levelModifierController = modifiers;
        var rocks = root.AddComponent<BoulderController>();
        Set(gc, "piece", piece); Set(gc, "boulders", rocks);
        var canvas = new GameObject("Snapshot canvas", typeof(RectTransform), typeof(Canvas)); temporary.Add(canvas);
        var boardGo = new GameObject("Board", typeof(RectTransform)); boardGo.transform.SetParent(canvas.transform, false);
        ((RectTransform)boardGo.transform).sizeDelta = new Vector2(400, 640);
        var board = boardGo.AddComponent<Board>(); board.width = 10; board.height = 16; board.animateTetrominoBackgrounds = false;
        board.RecomputeCellMetrics(); gc.gameBoard = board; piece.board = board;
        Set(board, "_gc", gc); Set(modifiers, "_gc", gc); Set(modifiers, "board", board);
        Set(rocks, "gc", gc); Set(rocks, "board", board);
        gc.pausePanel = new GameObject("Pause", typeof(RectTransform)); gc.pausePanel.transform.SetParent(canvas.transform, false);
        gc.pausePanel.SetActive(false);
        gc.enemyCastleUI = canvas.AddComponent<EnemyCastleUI>();
        root.SetActive(true);
        Set(gc, "isPaused", true);
        Set(gc, "_thisLevelBaseFallInterval", 0.8f);
        return gc;
    }

    [UnityTest] public IEnumerator LiveSnapshotRestoresBoardQueueTimersProjectilesAndStaysPaused()
    {
        yield return new EnterPlayMode();
        LocalSaveTestScope.Apply();
        GameModeStore.Current = RunGameMode.Marathon;
        Time.timeScale = 0f;
        var gc = CreateFixture();
        var board = gc.gameBoard;
        var monster = ScriptableObject.CreateInstance<MonsterData>(); temporary.Add(monster);
        monster.name = "Snapshot Monster"; monster.monsterName = "Snapshot Monster"; monster.maxHealth = 100;
        var shape = ScriptableObject.CreateInstance<TetrominoData>(); temporary.Add(shape);
        shape.name = "Snapshot Shape"; shape.cells = new[] { Vector2Int.zero, Vector2Int.right };
        gc.allTetrominoes = new[] { shape }; gc.fallbackMonsters = new[] { monster };
        var assets = new MatchSnapshotAssets();
        var tile = new Vector2Int(2, 2); var deadTile = new Vector2Int(3, 2);
        board.Place(tile, board.InstantiateTileUI(Color.cyan)); board.SetMonsterAt(tile, new Board.MonsterInstance(monster));
        board.Place(deadTile, board.InstantiateTileUI(Color.red)); board.SetMonsterAt(deadTile, new Board.MonsterInstance(monster));
        var monsters = Get<Dictionary<Vector2Int, Board.MonsterInstance>>(board, "monsters");
        var stats = monsters[tile]; stats.hp = 37; stats.maxHp = 200; stats.attackPower = 19; stats.pieceGroupId = 15; monsters[tile] = stats;
        stats.hp = 0; monsters[deadTile] = stats;
        Get<Dictionary<Vector2Int, float>>(board, "healTimers")[tile] = 0.42f;
        board.TrySpawnStoneObstacle(new Vector2Int(0, 0), 3);
        board.SetFloorEffect(tile, Board.FloorEffectType.Poison, 7f, 2f, 4);
        var piece = Get<Piece>(gc, "piece");
        piece.RestoreMarathonPiece(new MarathonMatchSnapshot.PieceState { asset = shape.name, origin = new Vector2Int(4, 9),
            cells = new List<Vector2Int> { new(4, 9), new(5, 9) }, monsters = new List<string> { monster.name, monster.name },
            color = Color.magenta, fallTimer = 0.23f, lockTimer = 0.12f, fallInterval = 0.6f, lockDelay = 0.3f }, assets);
        Get<Queue<TetrominoData>>(gc, "bag").Enqueue(shape);
        Get<Queue<MonsterData[]>>(gc, "monstersBag").Enqueue(new[] { monster, monster });
        Set(gc, "_levelTimer", 83.75f); Set(gc, "_castleAttackTimer", 1.25f); Set(gc, "_bossAbilityTimer", 2.5f);
        Set(gc, "_bossNextAbilityAt", 9f); Set(gc, "_comboCount", 6); Set(gc, "_comboTimer", 3.5f);
        var rocks = Get<BoulderController>(gc, "boulders"); Call(rocks, "Spawn", 6f);
        var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f); temporary.Add(sprite); sprite.name = "Snapshot bolt";
        Set(gc, "_restoringMarathon", true);
        Call(gc, "SpawnCastleDownProjectile", sprite, board.CellToAnchoredPos(new Vector2Int(2, 8)), board.CellToAnchoredPos(new Vector2Int(2, 0)), 2);
        Set(gc, "_restoringMarathon", false);
        var snapshot = (MarathonMatchSnapshot)Call(gc, "CaptureMarathonMatch");
        snapshot.doubleStatsRemaining = 0.25f;
        snapshot.bossGravityRemaining = 0.25f; snapshot.bossGravityBonus = 1.5f;
        snapshot.immunityRemaining = 0.25f;
        snapshot.castleInvulnerabilityRemaining = 0.25f;
        snapshot.slowGravitySource = 2; snapshot.slowGravityRemaining = 0.25f;
        Assert.IsTrue(snapshot.IsValid());
        var save = new TempRunSaveStore.SaveData { gameMode = RunGameMode.Marathon, currentLevel = 7, score = 452,
            marathonDamage = 345, marathonElapsedSeconds = 1400, unitLives = 11, marathonSnapshot = snapshot };
        Assert.IsTrue(TempRunSaveStore.Save(save));
        TempRunSaveStore.PrepareResume(); Assert.IsTrue(TempRunSaveStore.TryConsumePendingResume(out save));
        Assert.IsFalse(TempRunSaveStore.TryConsumePendingResume(out _), "Continue is consumed exactly once.");
        Assert.AreEqual(452, save.score); Assert.AreEqual(345, save.marathonDamage); Assert.AreEqual(1400, save.marathonElapsedSeconds);
        // Create entirely new scene components; only persistent asset identities survive.
        Object.DestroyImmediate(gc.gameObject); Object.DestroyImmediate(board.transform.parent.gameObject);
        gc = CreateFixture(); gc.allTetrominoes = new[] { shape }; gc.fallbackMonsters = new[] { monster };
        Call(gc, "RestoreMarathonMatch", save.marathonSnapshot); Call(gc, "OpenRestoredPauseMenu");
        Assert.IsTrue(gc.IsPaused); Assert.IsTrue(gc.pausePanel.activeInHierarchy); Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(AudioListener.pause);
        board = gc.gameBoard; piece = Get<Piece>(gc, "piece"); rocks = Get<BoulderController>(gc, "boulders");
        Assert.IsTrue(board.TryGetMonster(tile, out stats)); Assert.AreEqual(37, stats.hp); Assert.AreEqual(200, stats.maxHp);
        Assert.AreEqual(19, stats.attackPower); Assert.AreEqual(15, stats.pieceGroupId);
        Assert.IsTrue(board.TryGetMonster(deadTile, out stats)); Assert.AreEqual(0, stats.hp, "Dead tiles must not be healed on restore.");
        Assert.IsTrue(board.HasObstacle(new Vector2Int(0, 0)));
        Assert.AreEqual(0.42f, Get<Dictionary<Vector2Int, float>>(board, "healTimers")[tile]);
        Assert.AreEqual(83.75f, Get<float>(gc, "_levelTimer")); Assert.AreEqual(1.25f, Get<float>(gc, "_castleAttackTimer"));
        Assert.AreEqual(2.5f, Get<float>(gc, "_bossAbilityTimer")); Assert.AreEqual(6, Get<int>(gc, "_comboCount"));
        Assert.AreEqual(0.23f, Get<float>(piece, "fallTimer")); Assert.AreEqual(0.12f, Get<float>(piece, "lockTimer"));
        Assert.AreEqual(1, Get<Queue<TetrominoData>>(gc, "bag").Count);
        Assert.AreEqual(1, rocks.ActiveCount);
        Assert.AreEqual(2f, gc.PlayerMonsterAttackMult);
        Assert.IsTrue(gc.immunityActive);
        Assert.Greater(gc.enemyCastleUI.InvulnerabilityRemaining, 0f);
        Assert.AreEqual(1.5f, Get<float>(gc, "_bossGravityBonusActive"));
        var restored = (MarathonMatchSnapshot)Call(gc, "CaptureMarathonMatch");
        Assert.AreEqual(1, restored.projectiles.Count);
        Assert.AreEqual(snapshot.projectiles[0].position, restored.projectiles[0].position);
        yield return new WaitForSecondsRealtime(0.3f);
        var frozen = (MarathonMatchSnapshot)Call(gc, "CaptureMarathonMatch");
        CollectionAssert.AreEqual(restored.piece.cells, frozen.piece.cells);
        Assert.AreEqual(restored.boulders.rocks[0].position, frozen.boulders.rocks[0].position);
        Assert.AreEqual(restored.projectiles[0].position, frozen.projectiles[0].position);
        Assert.AreEqual(restored.board.floors[0].state.nextTickTime, frozen.board.floors[0].state.nextTickTime, 0.01f);
        Assert.AreEqual(restored.doubleStatsRemaining, frozen.doubleStatsRemaining, 0.01f);
        Assert.AreEqual(restored.slowGravityRemaining, frozen.slowGravityRemaining, 0.01f);
        Call(gc, "ResumeGame");
        Assert.IsFalse(gc.IsPaused); Assert.AreEqual(1f, Time.timeScale);
        float runningUntil = Time.time + 0.1f;
        while (Time.time < runningUntil) yield return null;
        var running = (MarathonMatchSnapshot)Call(gc, "CaptureMarathonMatch");
        Assert.AreNotEqual(frozen.boulders.rocks[0].position, running.boulders.rocks[0].position);
        runningUntil = Time.time + 0.35f;
        while (Time.time < runningUntil) yield return null;
        Assert.AreEqual(1f, gc.PlayerMonsterAttackMult);
        Assert.IsFalse(gc.immunityActive);
        Assert.AreEqual(0f, gc.enemyCastleUI.InvulnerabilityRemaining);
        Assert.AreEqual(0f, Get<float>(gc, "_bossGravityBonusActive"));
        Assert.AreEqual(0f, Get<float>(gc, "_activeTimedSlowGravityRemainingSeconds"));
        Assert.IsTrue(board.TryGetMonster(tile, out stats)); Assert.AreEqual(100f, stats.maxHp, "Restoring must not apply the health doubling twice.");
        yield return new ExitPlayMode();
    }

    static T FirstAsset<T>() where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset is TetrominoData shape && shape.special != SpecialType.None) continue;
            if (asset) return asset;
        }
        return null;
    }

    [UnityTest] public IEnumerator MainMenuContinueLoadsActualGameplayScenePausedAndCanSaveAgain()
    {
        yield return new EnterPlayMode();
        LocalSaveTestScope.Apply();
        LocalSaveTestScope.BackupScenePreferences();
        GameModeStore.Current = RunGameMode.Marathon;
        var shape = FirstAsset<TetrominoData>(); var monster = FirstAsset<MonsterData>(); var commander = FirstAsset<PlayerCharacterData>();
        Assert.IsNotNull(shape); Assert.IsNotNull(monster); Assert.IsNotNull(commander);
        SelectedMonstersStore.Active = new List<MonsterData> { monster };
        SelectedCharacterStore.Current = commander;
        var state = new MarathonMatchSnapshot {
            board = new MarathonMatchSnapshot.BoardState { width = 10, height = 16, nextGroupId = 4 },
            piece = new MarathonMatchSnapshot.PieceState { asset = shape.name, color = shape.color,
                origin = new Vector2Int(4, 10), fallTimer = 0.21f, fallInterval = 0.5f, lockDelay = 0.3f },
            boulders = new MarathonMatchSnapshot.BoulderState { countdown = 3f }, levelTimer = 37.5f, baseFallInterval = 0.8f,
            castleAttackTimer = 1.2f, bossNextAbilityAt = 8, comboCount = 3, comboTimer = 4f, randomState = Random.state };
        foreach (var cell in shape.cells)
        {
            state.piece.cells.Add(state.piece.origin + cell);
            state.piece.monsters.Add(monster.name);
        }
        for (int i = 0; i < 4; i++) state.queue.Add(new MarathonMatchSnapshot.QueuedPiece {
            asset = shape.name, monsters = new List<string>(state.piece.monsters) });
        var tileStats = new Board.MonsterInstance(monster); tileStats.hp = 17; tileStats.maxHp = 100; tileStats.data = null;
        state.board.tiles.Add(new MarathonMatchSnapshot.TileState { cell = new Vector2Int(3, 1), color = Color.cyan,
            monster = monster.name, hasMonster = true, stats = tileStats });
        var save = new TempRunSaveStore.SaveData { gameMode = RunGameMode.Marathon, currentLevel = 2, score = 400,
            unitLives = 7, specialGauge = 32f, marathonDamage = 123f, marathonElapsedSeconds = 267.5f,
            selectedCharacterName = commander.displayName, selectedMonsterNames = new List<string> { monster.monsterName },
            marathonSnapshot = state };
        Assert.IsTrue(TempRunSaveStore.Save(save));
        yield return SceneManager.LoadSceneAsync("TitleScene");
        yield return null;
        var title = Object.FindFirstObjectByType<TitleMenuUI>(); Assert.IsNotNull(title);
        title.OnContinueRun();
        float deadline = Time.realtimeSinceStartup + 60f;
        while (SceneManager.GetActiveScene().name != "GameplayScene" && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
        var gc = Object.FindFirstObjectByType<GameController>(); Assert.IsNotNull(gc);
        while (!gc.IsPaused && Time.realtimeSinceStartup < deadline) yield return null;
        while (LoadingScreen.IsVisible && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(LoadingScreen.IsVisible);
        Assert.IsTrue(gc.IsPaused); Assert.IsTrue(gc.pausePanel.activeInHierarchy); Assert.AreEqual(0f, Time.timeScale);
        Assert.AreEqual(RunGameMode.Marathon, GameModeStore.Current);
        Assert.AreEqual(400, Get<int>(gc, "score")); Assert.AreEqual(7, Get<int>(gc, "unitLives"));
        Assert.AreEqual(123f, gc.MarathonDamage); Assert.AreEqual(267.5f, Get<float>(gc, "marathonElapsedSeconds"));
        Assert.AreEqual(37.5f, Get<float>(gc, "_levelTimer"));
        Assert.IsTrue(gc.gameBoard.TryGetMonster(new Vector2Int(3, 1), out tileStats)); Assert.AreEqual(17f, tileStats.hp);
        CollectionAssert.AreEqual(state.piece.cells, Get<Piece>(gc, "piece").CaptureMarathonPiece().cells);
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.AreEqual(37.5f, Get<float>(gc, "_levelTimer"));
        Assert.AreEqual(32f, gc.specialGauge);
        Set(gc, "score", 415);
        Assert.IsTrue((bool)Call(gc, "SaveTempRunForQuit"));
        Assert.IsTrue(TempRunSaveStore.TryLoad(out save)); Assert.AreEqual(415, save.score);
        Assert.IsNotNull(save.marathonSnapshot);
        Set(gc, "_snapshotTransitions", 1);
        Assert.IsFalse((bool)Call(gc, "SaveTempRunForQuit"), "Unfinished board mutations must not be silently lost.");
        Assert.IsTrue(TempRunSaveStore.TryLoad(out save)); Assert.AreEqual(415, save.score);
        Set(gc, "_snapshotTransitions", 0);
        // A failed new write must not be mistaken for success merely because an old save exists.
        Directory.CreateDirectory(Path.Combine(LocalSaveFiles.DirectoryPath, "tetrabeasts_temp_run.json.tmp"));
        Set(gc, "score", 430);
        Assert.IsFalse((bool)Call(gc, "SaveTempRunForQuit"));
        Assert.IsTrue(TempRunSaveStore.TryLoad(out save)); Assert.AreEqual(415, save.score);
        Directory.Delete(Path.Combine(LocalSaveFiles.DirectoryPath, "tetrabeasts_temp_run.json.tmp"));
        yield return new ExitPlayMode();
    }
}
#endif
