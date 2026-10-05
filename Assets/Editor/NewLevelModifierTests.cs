#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class NewLevelModifierTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject root;
    GameObject controllerRoot;
    MonsterData monster;
    LevelModifierSO modifier;

    static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);
    static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);

    [Test]
    public void AllSixModifiersHaveWorkingIconsAndPoolEntries()
    {
        var db = AssetDatabase.LoadAssetAtPath<LevelModifierDatabaseSO>("Assets/ScriptableObjects/LevelModifier/Database/LevelModifier_Database.asset");
        foreach (string name in new[] { "DrFeelGood", "WatchYourStep", "FlipTheBoard", "Inversion", "Bombardment", "Hallucination" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelModifierSO>($"Assets/ScriptableObjects/LevelModifier/{name}.asset");
            Assert.IsNotNull(asset, name);
            Assert.IsNotNull(asset.icon, name + " icon reference");
            Assert.Contains(asset, db.BuildPool());
            if (name == "Hallucination") Assert.IsNotNull(asset.hallucinationAnimation[0]);
        }
    }

    [UnityTest]
    public IEnumerator RuntimeCollisionPoolingTimersAndCleanup()
    {
        yield return new EnterPlayMode();
        // Keep the full game controller inactive: exercise real board/modifier
        // methods without starting a run or writing player progression/checkpoints.
        controllerRoot = new GameObject("Modifier test controller");
        controllerRoot.SetActive(false);
        var gc = controllerRoot.AddComponent<GameController>();
        var controller = controllerRoot.AddComponent<LevelModifierController>();
        gc.levelModifierController = controller;
        root = new GameObject("Modifier test canvas", typeof(RectTransform), typeof(Canvas));
        var boardObject = new GameObject("Board", typeof(RectTransform));
        boardObject.transform.SetParent(root.transform, false);
        ((RectTransform)boardObject.transform).sizeDelta = new Vector2(400, 640);
        var board = boardObject.AddComponent<Board>();
        board.width = 10;
        board.height = 16;
        board.animateTetrominoBackgrounds = false;
        board.RecomputeCellMetrics();
        Set(board, "_gc", gc);
        Set(controller, "board", board);
        Set(controller, "_gc", gc);
        modifier = ScriptableObject.CreateInstance<LevelModifierSO>();
        monster = ScriptableObject.CreateInstance<MonsterData>();
        monster.maxHealth = 20f;
        monster.portrait = AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/Hallucination.asset").hallucinationAnimation[0];
        Set(controller, "<ActiveModifier>k__BackingField", modifier);

        void Activate(LevelModifierKind kind)
        {
            controller.EndNewModifierEffects();
            modifier.kind = kind;
            Call(controller, "PrepareNewModifierEffects");
        }
        RectTransform PlaceMonster(Vector2Int cell)
        {
            var tile = board.InstantiateTileUI(Color.cyan, monster.portrait);
            board.Place(cell, tile);
            board.SetMonsterAt(cell, new Board.MonsterInstance(monster));
            // Isolate the fixture from the developer's saved shop/progression buffs.
            var instances = (Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters");
            var instance = instances[cell];
            instance.hp = instance.maxHp = 20f;
            instance.healAmount = monster.healAmount;
            instance.healRange = monster.healRange;
            instance.healSpeed = monster.healSpeed;
            instances[cell] = instance;
            return tile;
        }

        Activate(LevelModifierKind.DrFeelGood);
        var cell = new Vector2Int(0, 5);
        var tile = PlaceMonster(cell);
        board.DamageTile(cell, 5f);
        Assert.IsTrue(board.HealTile(cell, 1f, null));
        Assert.IsTrue(board.HealTile(cell, 1f, null));
        Assert.IsTrue(board.TryGetMonster(cell, out var healed));
        Assert.AreEqual(2, healed.feelGoodStacks);
        healed.feelGoodTickTimer = 1f;
        ((Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters"))[cell] = healed;
        Call(board, "TickFeelGood");
        board.TryGetMonster(cell, out var damaged);
        Assert.AreEqual(healed.hp - 0.5f, damaged.hp, 0.001f, "Two stacks deal twice the base damage.");
        Call(board, "LateUpdate");
        var portrait = tile.Find("MonsterPortrait").GetComponent<Image>();
        Assert.Less(portrait.canvasRenderer.GetColor().r, 1f);
        board.SetCraterCell(new Vector2Int(0, 2), true);
        board.SettleAllColumns(true);
        Assert.IsTrue(board.TryGetMonster(new Vector2Int(0, 3), out var settled));
        Assert.AreEqual(2, settled.feelGoodStacks, "Debuff must follow the instance when settling.");
        Assert.IsFalse(board.Valid(new List<Vector2Int> { new Vector2Int(0, 2) }));
        Assert.IsFalse(board.IsClearableOccupiedRow(2));
        Assert.IsFalse(board.TryHandleEnemyProjectileObstacleImpact(new Vector2Int(0, 2)));
        Assert.IsFalse(board.TryHandlePlayerAttackObstacleImpact(new Vector2Int(0, 2)));
        board.RemovePlacedCellImmediate(new Vector2Int(0, 3));
        Assert.AreEqual(Color.white, portrait.canvasRenderer.GetColor(), "Pool release restores tint.");
        PlaceMonster(cell);
        Assert.IsTrue(board.TryGetMonster(cell, out var reused));
        Assert.AreEqual(0, reused.feelGoodStacks);
        board.DamageTile(cell, 2f);
        board.HealTile(cell, 1f, null);
        controller.EndNewModifierEffects();
        board.TryGetMonster(cell, out reused);
        Assert.AreEqual(0, reused.feelGoodStacks, "Round-end clears surviving units too.");
        Assert.IsFalse(board.IsCraterCell(new Vector2Int(0, 2)));
        board.ClearAll();

        Activate(LevelModifierKind.DrFeelGood);
        monster.healAmount = 1f;
        monster.healSpeed = 10f;
        monster.healRange = 1f;
        PlaceMonster(Vector2Int.zero);
        PlaceMonster(new Vector2Int(2, 0));
        board.DamageTile(new Vector2Int(2, 0), 5f);
        board.TryGetMonster(new Vector2Int(2, 0), out var beforePulse);
        ((Dictionary<Vector2Int, float>)Get(board, "healTimers"))[Vector2Int.zero] = 5.1f;
        Call(board, "Update");
        board.TryGetMonster(new Vector2Int(2, 0), out var afterPulse);
        Assert.Greater(afterPulse.hp, beforePulse.hp, "Heal triggers at half interval and reaches the extra cell.");
        board.ReviveAllTilesToFull(new List<Vector2Int>());
        board.TryGetMonster(new Vector2Int(2, 0), out var revived);
        Assert.AreEqual(afterPulse.feelGoodStacks + 1, revived.feelGoodStacks, "Full restoration also adds a stack.");
        board.DamageTile(new Vector2Int(2, 0), 100000f);
        board.TryGetMonster(new Vector2Int(2, 0), out var dead);
        Assert.AreEqual(0, dead.feelGoodStacks, "Death clears stacks immediately.");
        monster.healAmount = 0f;
        board.ClearAll();

        Activate(LevelModifierKind.WatchYourStep);
        Call(controller, "UpdateNewModifierEffects", 0f);
        int occupied = 0;
        for (int y = 0; y < board.height; y++)
        for (int x = 0; x < board.width; x++)
        {
            var c = new Vector2Int(x, y);
            bool obstacle = board.HasObstacle(c), floor = board.HasFloorEffect(c);
            Assert.IsFalse(obstacle && floor);
            if (y >= board.height - 5) Assert.IsFalse(obstacle || floor);
            if (obstacle || floor) occupied++;
        }
        Assert.AreEqual(Mathf.RoundToInt(10 * 11 * 0.33f), occupied);
        board.ClearAll();

        Activate(LevelModifierKind.FlipTheBoard);
        var originalPosition = board.transform.localPosition;
        var originalRotation = board.transform.localRotation;
        Call(controller, "UpdateNewModifierEffects", 0f);
        Assert.IsFalse(controller.BoardFlipped, "Warn before the first rotation.");
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.IsTrue(controller.BoardFlipped);
        Assert.Greater(board.gridRoot.TransformVector(Vector3.down).y, 0f, "Gravity rises in world space.");
        Assert.That((float)Get(controller, "_flipCountdown"), Is.InRange(20f, 30f));
        controller.EndNewModifierEffects();
        Assert.AreEqual(originalPosition, board.transform.localPosition);
        Assert.AreEqual(originalRotation, board.transform.localRotation);
        Activate(LevelModifierKind.Inversion);
        Assert.IsTrue(controller.InvertsPieceControls);
        controller.EndNewModifierEffects();
        Assert.IsFalse(controller.InvertsPieceControls);

        Activate(LevelModifierKind.Bombardment);
        Call(controller, "UpdateNewModifierEffects", 0f);
        Call(controller, "UpdateNewModifierEffects", 13f);
        Assert.AreEqual(9, ((IList)Get(controller, "_newModifierWarnings")).Count);
        var target = (Vector2Int)Get(controller, "_bombardmentTarget");
        PlaceMonster(target);
        board.SetFloorEffect(target + Vector2Int.right, Board.FloorEffectType.Burn, 1f, 1f, -1);
        board.TrySpawnStoneObstacle(target + Vector2Int.up);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.IsFalse(board.TryGetMonster(target, out _));
        Assert.IsFalse(board.HasFloorEffect(target + Vector2Int.right));
        Assert.IsFalse(board.HasObstacle(target + Vector2Int.up));
        Assert.IsTrue(board.IsCraterCell(target));
        Assert.AreEqual(1, ((IList)Get(controller, "_craters")).Count);
        Call(controller, "UpdateNewModifierEffects", 13f);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.AreEqual(2, ((IList)Get(controller, "_craters")).Count);
        Call(controller, "UpdateNewModifierEffects", 13f);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.AreEqual(2, ((IList)Get(controller, "_craters")).Count);
        Assert.IsFalse(board.IsCraterCell(target), "First crater expires at 30 seconds.");
        controller.EndNewModifierEffects();

        Activate(LevelModifierKind.Hallucination);
        modifier.hallucinationAnimation = new[] { monster.portrait, monster.portrait };
        modifier.hallucinationIdleFrames = AssetDatabase.LoadAssetAtPath<LevelModifierSO>(
            "Assets/ScriptableObjects/LevelModifier/Hallucination.asset").hallucinationIdleFrames;
        Call(controller, "UpdateNewModifierEffects", 0f);
        var hallucinations = (IList)Get(controller, "_hallucinations");
        Assert.AreEqual(1, hallucinations.Count);
        var first = hallucinations[0];
        var idleAnimator = (UIImagePingPongAnimator)first.GetType().GetField("idleAnimator").GetValue(first);
        var hallucinationImage = (Image)first.GetType().GetField("image").GetValue(first);
        Assert.IsFalse(idleAnimator.enabled, "Gameplay owns the animation clock.");
        Call(controller, "UpdateNewModifierEffects", 1f / modifier.hallucinationIdleFramesPerSecond);
        Assert.AreEqual(modifier.hallucinationIdleFrames[1], hallucinationImage.sprite);
        var pause = new GameObject("Pause", typeof(RectTransform));
        pause.transform.SetParent(root.transform, false);
        gc.pausePanel = pause;
        Set(gc, "isPaused", true);
        float countdownBeforePause = (float)Get(controller, "_hallucinationCountdown");
        Call(controller, "Update");
        Call(controller, "LateUpdate");
        Assert.AreEqual(countdownBeforePause, Get(controller, "_hallucinationCountdown"));
        Assert.AreEqual(modifier.hallucinationIdleFrames[1], hallucinationImage.sprite, "Pause freezes idle animation too.");
        Assert.Greater(pause.transform.GetSiblingIndex(), ((RectTransform)Get(controller, "_hallucinationRoot")).GetSiblingIndex());
        Set(gc, "isPaused", false);
        pause.SetActive(false);
        var positionField = first.GetType().GetField("position");
        var velocityField = first.GetType().GetField("velocity");
        positionField.SetValue(first, new Vector2(1f, 8f));
        velocityField.SetValue(first, new Vector2(-4f, 0f));
        Call(controller, "UpdateNewModifierEffects", 1f);
        Assert.Greater(((Vector2)velocityField.GetValue(first)).x, 0f);
        Assert.That(((Vector2)positionField.GetValue(first)).x, Is.InRange(0.75f, 9.25f));
        Call(controller, "SpawnHallucination");
        var second = hallucinations[1];
        positionField.SetValue(second, positionField.GetValue(first));
        Call(controller, "ResolveHallucinationMerges");
        Assert.AreEqual(1, hallucinations.Count);
        var survivor = hallucinations[0];
        Assert.Greater((float)survivor.GetType().GetField("diameter").GetValue(survivor), modifier.hallucinationBaseSizeCells);
        var hitPosition = (Vector2)positionField.GetValue(survivor);
        Vector2 hitLocal = board.CellToAnchoredPos(Vector2Int.zero) + Vector2.Scale(hitPosition - Vector2.one * 0.5f, board.GetCellSize());
        Assert.IsTrue(controller.TryHitHallucination(hitLocal - Vector2.right * 200f, hitLocal + Vector2.right * 200f));
        Assert.AreEqual(2, hallucinations.Count, "Death animation and immediate replacement coexist.");
        Assert.Greater((float)Get(controller, "_hallucinationBaseDiameter"), modifier.hallucinationBaseSizeCells);
        Assert.AreEqual(10f, Get(controller, "_hallucinationCountdown"));
        controller.EndNewModifierEffects();
        Assert.AreEqual(0, hallucinations.Count);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (Application.isPlaying)
        {
            if (controllerRoot) Object.Destroy(controllerRoot);
            if (root) Object.Destroy(root);
            if (monster) Object.Destroy(monster);
            if (modifier) Object.Destroy(modifier);
            yield return null;
            yield return new ExitPlayMode();
        }
    }
}
#endif
