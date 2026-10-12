#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BoulderAndModeTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private | BindingFlags.Public).SetValue(target, value);
    static object Get(object target, string name) => target.GetType().GetField(name, Private | BindingFlags.Public).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    GameObject controllerRoot, canvasRoot;
    readonly List<Object> temporary = new();

    [Test]
    public void AssetsBossPoolsAndSavedModeReferencesAreValid()
    {
        var config = GameModeAssets.Load();
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.boulderSprite);
        Assert.IsNotNull(config.modeMenuPrefab.closeButton);
        Assert.AreEqual(3, config.marathonRewardPrefab.choices.Length);
        Assert.IsNotNull(config.marathonGaugePrefab.gauge.fillRect.GetComponent<Image>().material);
        Assert.AreEqual(14, config.standardLevelCount);
        var db = AssetDatabase.LoadAssetAtPath<LevelModifierDatabaseSO>("Assets/ScriptableObjects/LevelModifier/Database/LevelModifier_Database.asset");
        var modifier = db.BuildPool().Find(m => m.kind == LevelModifierKind.BouldersAway);
        Assert.IsNotNull(modifier.icon);
        foreach (var guid in AssetDatabase.FindAssets("t:CastleData", new[] { "Assets/ScriptableObjects/Castle" }))
        {
            var castle = AssetDatabase.LoadAssetAtPath<CastleData>(AssetDatabase.GUIDToAssetPath(guid));
            if (castle.isBossLevel && castle.useBossAbilityOptionPool)
                Assert.IsTrue(castle.bossAbilityOptions.Any(o => o.kind == CastleData.BossAbilityKind.RocksForEveryone), castle.name);
        }
        var save = new TempRunSaveStore.SaveData { gameMode = RunGameMode.Chaos, marathonDamage = 235f, chaosModifierNames = new List<string> { "Inversion", "DrFeelGood" } };
        var restored = JsonUtility.FromJson<TempRunSaveStore.SaveData>(JsonUtility.ToJson(save));
        Assert.AreEqual(save.gameMode, restored.gameMode);
        CollectionAssert.AreEqual(save.chaosModifierNames, restored.chaosModifierNames);
        Assert.AreEqual(235f, restored.marathonDamage);
        Assert.AreEqual(RunGameMode.Standard, JsonUtility.FromJson<TempRunSaveStore.SaveData>("{\"version\":1}").gameMode);
    }

    [Test]
    public void RewardPairsAlwaysMatchRarityAndDamageThresholdGrows()
    {
        var buffs = new List<RunModifierSO>(); var debuffs = new List<RunModifierSO>();
        for (int i = 0; i < 5; i++)
        {
            var buff = ScriptableObject.CreateInstance<RunModifier>(); buff.rarity = (RunModRarity)i; buffs.Add(buff); temporary.Add(buff);
            var debuff = ScriptableObject.CreateInstance<RunModifier>(); debuff.rarity = (RunModRarity)i; debuffs.Add(debuff); temporary.Add(debuff);
        }
        for (int i = 0; i < 100; i++)
        {
            var choices = MarathonRewardUI.RollPairs(buffs.ToArray(), debuffs.ToArray());
            Assert.AreEqual(3, choices.Length);
            Assert.AreEqual(3, choices.Select(p => p.buff).Distinct().Count());
            foreach (var pair in choices) Assert.AreEqual(MarathonRewardUI.Rarity(pair.buff), MarathonRewardUI.Rarity(pair.debuff));
        }
        for (int i = 1; i < 150; i++) Assert.Greater(GameController.RequiredMarathonDamage(i, 500, 1.18f), GameController.RequiredMarathonDamage(i - 1, 500, 1.18f));
        Assert.IsFalse(float.IsInfinity(GameController.RequiredMarathonDamage(10000, 500, 1.18f)));
    }

    [UnityTest]
    public IEnumerator BouldersRespectTargetsContactsCapacityPauseAndCleanup()
    {
        yield return new EnterPlayMode();
        GameModeStore.Current = RunGameMode.Standard;
        controllerRoot = new GameObject("Boulder tests"); controllerRoot.SetActive(false);
        var gc = controllerRoot.AddComponent<GameController>();
        var modifiers = controllerRoot.AddComponent<LevelModifierController>(); gc.levelModifierController = modifiers;
        var boulders = controllerRoot.AddComponent<BoulderController>();
        canvasRoot = new GameObject("Test canvas", typeof(RectTransform), typeof(Canvas));
        var boardObject = new GameObject("Board", typeof(RectTransform)); boardObject.transform.SetParent(canvasRoot.transform, false);
        ((RectTransform)boardObject.transform).sizeDelta = new Vector2(400, 640);
        var board = boardObject.AddComponent<Board>(); board.width = 10; board.height = 16; board.animateTetrominoBackgrounds = false;
        board.RecomputeCellMetrics(); gc.gameBoard = board;
        Set(board, "_gc", gc); Set(boulders, "gc", gc); Set(boulders, "board", board);
        Set(modifiers, "_gc", gc); Set(modifiers, "board", board);
        var monster = ScriptableObject.CreateInstance<MonsterData>(); temporary.Add(monster); monster.maxHealth = 100;
        var cell = new Vector2Int(4, 5);
        board.Place(cell, board.InstantiateTileUI(Color.white)); board.SetMonsterAt(cell, new Board.MonsterInstance(monster));
        var instances = (Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters");
        var instance = instances[cell]; instance.hp = instance.maxHp = 100; instances[cell] = instance;
        Call(boulders, "Spawn", 4.5f);
        var rocks = (IList)Get(boulders, "rocks"); var rock = rocks[0];
        Set(rock, "position", new Vector2(4.5f, 6.43f)); Set(rock, "velocity", new Vector2(0, -6)); Set(rock, "bounces", 5);
        Call(boulders, "Simulate", 0.01f);
        board.TryGetMonster(cell, out instance); Assert.AreEqual(80, instance.hp, 0.001f);
        Assert.Greater(((Vector2)Get(rock, "velocity")).y, 0);
        Call(boulders, "Simulate", 0.001f);
        board.TryGetMonster(cell, out instance); Assert.AreEqual(80, instance.hp, 0.001f, "Sustained contact must not repeatedly damage.");
        instance.hp = 0; instances[cell] = instance; Assert.IsFalse(board.IsBoulderTarget(cell));
        var stone = new Vector2Int(0, 0); Assert.IsTrue(board.TrySpawnStoneObstacle(stone, 3));
        board.StrikeWithBoulder(stone); Assert.IsTrue(board.HasObstacle(stone));
        board.StrikeWithBoulder(stone); Assert.IsTrue(board.HasObstacle(stone));
        board.StrikeWithBoulder(stone); Assert.IsFalse(board.HasObstacle(stone));
        board.SetCraterCell(new Vector2Int(1, 1), true); Assert.IsFalse(board.IsBoulderTarget(new Vector2Int(1, 1)));
        boulders.Clear();
        for (int i = 0; i < 5; i++) Call(boulders, "Spawn", 1f + i * 1.5f);
        Assert.AreEqual(3, boulders.ActiveCount);
        var modifier = ScriptableObject.CreateInstance<LevelModifierSO>(); temporary.Add(modifier); modifier.kind = LevelModifierKind.BouldersAway;
        Set(modifiers, "<ActiveModifier>k__BackingField", modifier);
        Set(boulders, "countdown", 2f); Call(boulders, "Update"); Assert.AreEqual(2f, Get(boulders, "countdown"));
        Set(gc, "isPaused", true); var position = (Vector2)Get(rocks[0], "position"); Call(boulders, "Update"); Assert.AreEqual(position, Get(rocks[0], "position")); Set(gc, "isPaused", false);
        // Head-on collisions exchange horizontal momentum and consume one bounce each.
        boulders.Clear(); Call(boulders, "Spawn", 3f); Call(boulders, "Spawn", 3.8f);
        var a = rocks[0]; var b = rocks[1];
        Set(a, "position", new Vector2(3f, 10f)); Set(b, "position", new Vector2(3.8f, 10f));
        Set(a, "velocity", Vector2.right * 2); Set(b, "velocity", Vector2.left * 2); Set(a, "bounces", 5); Set(b, "bounces", 5);
        Call(boulders, "Simulate", 0.001f);
        Assert.Less(((Vector2)Get(a, "velocity")).x, 0); Assert.Greater(((Vector2)Get(b, "velocity")).x, 0);
        Assert.AreEqual(4, Get(a, "bounces"));
        Set(a, "bounces", 1); Set(a, "position", new Vector2(0.42f, 10f)); Set(a, "velocity", Vector2.left * 2);
        Call(boulders, "Simulate", 0.01f); Assert.AreEqual(1, boulders.ActiveCount);
        boulders.Clear(); Assert.AreEqual(0, boulders.ActiveCount);
        Set(modifiers, "<ActiveModifier>k__BackingField", null); Set(boulders, "countdown", -1f); Call(boulders, "Update"); Assert.AreEqual(0, boulders.ActiveCount, "Ordinary rounds never spawn boulders.");
        boulders.RequestBossVolley(); Call(boulders, "Update"); Assert.AreEqual(3, boulders.ActiveCount);
        Assert.Greater(Vector2.Distance((Vector2)Get(rocks[0], "position"), (Vector2)Get(rocks[1], "position")), 0.84f);
        boulders.Clear();
        Object.Destroy(controllerRoot); Object.Destroy(canvasRoot); controllerRoot = canvasRoot = null;
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ChaosRetainsIndependentEffectsAndMarathonCarriesDamageIntoPausedChoice()
    {
        yield return new EnterPlayMode();
        controllerRoot = new GameObject("Mode tests"); controllerRoot.SetActive(false);
        var gc = controllerRoot.AddComponent<GameController>();
        var controller = controllerRoot.AddComponent<LevelModifierController>(); gc.levelModifierController = controller;
        Set(controller, "_gc", gc);
        // Presentation is tested via prefab references; isolate gameplay from scene UI.
        Set(controller, "isChaosEffect", true);
        var history = (List<LevelModifierSO>)Get(controller, "chaosModifiers");
        foreach (var kind in new[] { LevelModifierKind.DoubleDamage, LevelModifierKind.Inversion, LevelModifierKind.DrFeelGood, LevelModifierKind.NoSpecialPieces })
        {
            var modifier = ScriptableObject.CreateInstance<LevelModifierSO>(); temporary.Add(modifier);
            modifier.kind = kind; modifier.name = kind.ToString(); modifier.receivedDamageMultiplier = 2f;
            history.Add(modifier);
        }
        GameModeStore.Current = RunGameMode.Standard;
        Set(gc, "_starDifficulty", 2);
        float standardConversion = (float)Call(gc, "GetRunEndXpConversionFraction", false);
        GameModeStore.Current = RunGameMode.Chaos;
        Assert.AreEqual(standardConversion + 0.20f, (float)Call(gc, "GetRunEndXpConversionFraction", false), 0.001f);
        Call(controller, "RebuildChaosEffects");
        Assert.IsTrue(controller.InvertsPieceControls);
        Assert.IsTrue(controller.BlocksSpecialPieceSpawns);
        Assert.IsNotNull(controller.GetModifier(LevelModifierKind.DrFeelGood));
        Assert.AreEqual(20f, controller.ModifyIncomingDamage(Vector2Int.zero, null, 10f, Board.DamageSource.BossAbility));
        var saved = controller.CaptureChaosModifiers();
        controller.EndNewModifierEffects(); Assert.IsFalse(controller.InvertsPieceControls);
        Call(controller, "RebuildChaosEffects");
        CollectionAssert.AreEqual(saved, controller.CaptureChaosModifiers());
        Assert.IsTrue(controller.InvertsPieceControls, "Earlier modifiers resume on the next level.");
        controller.ResetRunState(); Assert.IsFalse(controller.HasModifier(LevelModifierKind.DoubleDamage));
        Assert.AreEqual(0, controller.ChaosModifiers.Count);

        GameModeStore.Current = RunGameMode.Marathon;
        var castles = new List<CastleData>();
        for (int i = 0; i < 7; i++)
        {
            castles.Add(AssetDatabase.LoadAssetAtPath<CastleData>("Assets/ScriptableObjects/Castle/TestCastle " + i + ".asset"));
            castles.Add(AssetDatabase.LoadAssetAtPath<CastleData>("Assets/ScriptableObjects/Castle/TestBossLevel " + i + ".asset"));
        }
        gc.castlesByLevel = castles.ToArray();
        var finalBoss = AssetDatabase.LoadAssetAtPath<CastleData>("Assets/ScriptableObjects/Castle/FinalSurvivalBoss.asset");
        Set(gc, "postFinalSurvivalCastle", finalBoss);
        Assert.AreEqual(castles[1], Call(gc, "ResolveMarathonCastle", 2));
        Assert.AreEqual(castles[3], Call(gc, "ResolveMarathonCastle", 5));
        Assert.AreEqual(finalBoss, Call(gc, "ResolveMarathonCastle", 23));
        Assert.AreEqual(finalBoss, Call(gc, "ResolveMarathonCastle", 24));
        Assert.AreEqual(finalBoss, Call(gc, "ResolveMarathonCastle", 98));
        var config = GameModeAssets.Load(); Set(gc, "modeAssets", config);
        canvasRoot = new GameObject("Mode test canvas", typeof(RectTransform), typeof(Canvas));
        var reward = Object.Instantiate(config.marathonRewardPrefab, canvasRoot.transform); reward.gameObject.SetActive(false);
        Set(gc, "marathonRewards", reward);
        var buffs = new List<RunModifierSO>(); var debuffs = new List<RunModifierSO>();
        for (int i = 0; i < 3; i++)
        {
            var buff = ScriptableObject.CreateInstance<RunModifier>(); temporary.Add(buff); buff.displayName = "Buff " + i; buffs.Add(buff);
            var debuff = ScriptableObject.CreateInstance<RunModifier>(); temporary.Add(debuff); debuff.displayName = "Debuff " + i; debuffs.Add(debuff);
        }
        Set(gc, "buffPool", buffs.ToArray()); Set(gc, "debuffPool", debuffs.ToArray());
        gc.fallbackMonsters = new MonsterData[0];
        Set(gc, "marathonDamage", config.marathonStartingDamage + 125f);
        var levelUp = (IEnumerator)Call(gc, "MarathonLevelUp");
        Assert.IsTrue(levelUp.MoveNext());
        Assert.AreEqual(1, gc.CurrentLevel);
        Assert.AreEqual(125f, gc.MarathonDamage, 0.001f);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(gc.IsGameplaySuspended);
        Assert.IsTrue(reward.gameObject.activeSelf);
        Assert.IsFalse(reward.confirmButton.interactable);
        reward.choices[1].onClick.Invoke(); Assert.IsTrue(reward.confirmButton.interactable);
        // Do not confirm: that path intentionally writes the user's resumable run.
        Call(gc, "ResetModeProgress");
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(0f, gc.MarathonDamage);
        Assert.IsFalse(reward.gameObject.activeSelf);
        Object.Destroy(controllerRoot); Object.Destroy(canvasRoot); controllerRoot = canvasRoot = null;
        yield return null;
        yield return new ExitPlayMode();
    }

    [TearDown]
    public void Cleanup()
    {
        GameModeStore.Current = RunGameMode.Standard; Time.timeScale = 1f;
        if (controllerRoot) Object.DestroyImmediate(controllerRoot);
        if (canvasRoot) Object.DestroyImmediate(canvasRoot);
        foreach (var obj in temporary) if (obj) Object.DestroyImmediate(obj);
        temporary.Clear();
    }
}
#endif
