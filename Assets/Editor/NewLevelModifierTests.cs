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
    CastleData castleData;

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
        yield return RuntimeChecks();
    }

    IEnumerator RuntimeChecks()
    {
        // Create the captured fixture after EnterPlayMode's domain reload.
        // Keep the full game controller inactive: exercise real board/modifier
        // methods without starting a run or writing player progression/checkpoints.
        controllerRoot = new GameObject("Modifier test controller");
        controllerRoot.SetActive(false);
        var gc = controllerRoot.AddComponent<GameController>();
        var controller = controllerRoot.AddComponent<LevelModifierController>();
        gc.levelModifierController = controller;
        root = new GameObject("Modifier test canvas", typeof(RectTransform), typeof(Canvas));
        ((RectTransform)root.transform).sizeDelta = new Vector2(1200, 1000);
        var damageText = root.AddComponent<FloatingDamageText>();
        Set(damageText, "enabledText", false);
        Set(gc, "floatingDamageText", damageText);
        var boardObject = new GameObject("Board", typeof(RectTransform));
        boardObject.transform.SetParent(root.transform, false);
        ((RectTransform)boardObject.transform).sizeDelta = new Vector2(400, 640);
        var board = boardObject.AddComponent<Board>();
        board.width = 10;
        board.height = 16;
        board.animateTetrominoBackgrounds = false;
        board.RecomputeCellMetrics();
        gc.gameBoard = board;
        gc.projectileRoot = board.gridRoot;
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
        Assert.AreEqual(4, healed.feelGoodStacks);
        healed.feelGoodTickTimer = 1f;
        ((Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters"))[cell] = healed;
        Call(board, "TickFeelGood");
        board.TryGetMonster(cell, out var damaged);
        Assert.AreEqual(healed.hp - 1f, damaged.hp, 0.001f, "Each heal adds two damage stacks.");
        Call(board, "LateUpdate");
        var portrait = tile.Find("MonsterPortrait").GetComponent<Image>();
        Assert.Less(portrait.canvasRenderer.GetColor().r, 1f);
        var curse = tile.transform.Find("Feel Good curse");
        Assert.IsTrue(curse.gameObject.activeSelf);
        Assert.AreEqual("4", curse.GetComponentInChildren<TMPro.TMP_Text>().text);
        board.SetCraterCell(new Vector2Int(0, 2), true);
        board.SettleAllColumns(true);
        Assert.IsTrue(board.TryGetMonster(new Vector2Int(0, 3), out var settled));
        Assert.AreEqual(4, settled.feelGoodStacks, "Debuff must follow the instance when settling.");
        Assert.IsFalse(board.Valid(new List<Vector2Int> { new Vector2Int(0, 2) }));
        Assert.IsFalse(board.IsClearableOccupiedRow(2));
        Assert.IsFalse(board.TryHandleEnemyProjectileObstacleImpact(new Vector2Int(0, 2)));
        Assert.IsFalse(board.TryHandlePlayerAttackObstacleImpact(new Vector2Int(0, 2)));
        board.RemovePlacedCellImmediate(new Vector2Int(0, 3));
        Assert.AreEqual(Color.white, portrait.canvasRenderer.GetColor(), "Pool release restores tint.");
        Assert.IsFalse(curse.gameObject.activeSelf, "Pool release removes the curse badge and border too.");
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
        Assert.AreEqual(beforePulse.hp, afterPulse.hp, "Dr. Feel Good must not shorten the normal heal interval.");
        ((Dictionary<Vector2Int, float>)Get(board, "healTimers"))[Vector2Int.zero] = 10.1f;
        Call(board, "Update");
        board.TryGetMonster(new Vector2Int(2, 0), out afterPulse);
        Assert.Greater(afterPulse.hp, beforePulse.hp, "Normal cadence still reaches the extra cell.");
        board.ReviveAllTilesToFull(new List<Vector2Int>());
        board.TryGetMonster(new Vector2Int(2, 0), out var revived);
        Assert.AreEqual(afterPulse.feelGoodStacks + 2, revived.feelGoodStacks, "Full restoration also adds stacks.");
        revived.hp = 10f;
        revived.feelGoodStacks = 20;
        ((Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters"))[new Vector2Int(2, 0)] = revived;
        board.HealTile(new Vector2Int(2, 0), 5f, null);
        board.TryGetMonster(new Vector2Int(2, 0), out revived);
        Assert.AreEqual(14f, revived.hp, .001f, "Twenty stacks reduce a five-point heal to four.");
        Assert.AreEqual(22, revived.feelGoodStacks, "Apply the incoming heal before its new stacks.");
        board.ReviveAllTilesToFull(new List<Vector2Int>());
        board.TryGetMonster(new Vector2Int(2, 0), out revived);
        Assert.AreEqual(18.68f, revived.hp, .001f, "Global healing respects the same reduction.");
        revived.feelGoodStacks = 100;
        ((Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters"))[new Vector2Int(2, 0)] = revived;
        board.DamageTile(new Vector2Int(2, 0), 1f);
        board.HealTile(new Vector2Int(2, 0), 1f, null);
        board.TryGetMonster(new Vector2Int(2, 0), out var manyStacks);
        Assert.AreEqual(102, manyStacks.feelGoodStacks, "Stacking has no gameplay cap.");
        Assert.AreEqual(revived.hp - 1f, manyStacks.hp, .001f, "A fully suppressed heal never damages or heals.");
        board.HealTile(new Vector2Int(2, 0), 5f, null);
        board.TryGetMonster(new Vector2Int(2, 0), out var overHundred);
        Assert.AreEqual(manyStacks.hp, overHundred.hp);
        Assert.AreEqual(104, overHundred.feelGoodStacks);
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
        Set(controller, "_floorWaveCountdown", 2.1f);
        Call(controller, "UpdateNewModifierEffects", .2f);
        var warnedCells = (List<Vector2Int>)Get(controller, "_floorWaveCells");
        Assert.That(warnedCells.Count, Is.InRange(1, 3));
        foreach (var warned in warnedCells)
        {
            Assert.IsTrue(board.IsFree(warned));
            Assert.IsFalse(board.HasFloorEffect(warned));
        }
        var occupiedWarning = warnedCells[0];
        PlaceMonster(occupiedWarning);
        var pending = warnedCells.ToArray();
        Call(controller, "UpdateNewModifierEffects", 1.9f);
        foreach (var warned in pending) Assert.IsFalse(board.HasFloorEffect(warned));
        Call(controller, "UpdateNewModifierEffects", .11f);
        Assert.IsFalse(board.HasFloorEffect(occupiedWarning), "Do not spawn under a unit placed during warning.");
        for (int i = 1; i < pending.Length; i++) Assert.IsTrue(board.HasFloorEffect(pending[i]));
        Assert.That((float)Get(controller, "_floorWaveCountdown"), Is.InRange(7f, 10f));
        board.ClearAll();

        Activate(LevelModifierKind.FlipTheBoard);
        var originalPosition = board.transform.localPosition;
        var originalRotation = board.transform.localRotation;
        Call(controller, "UpdateNewModifierEffects", 0f);
        Assert.IsFalse(controller.BoardFlipped, "Warn before the first rotation.");
        Assert.That((float)Get(controller, "_flipCountdown"), Is.InRange(5f, 20f));
        Set(controller, "_flipCountdown", 20f);
        Call(controller, "UpdateNewModifierEffects", 4.9f);
        controller.OnCastleDamaged();
        Assert.AreEqual(0, ((IList)Get(controller, "_newModifierWarnings")).Count);
        Call(controller, "UpdateNewModifierEffects", .11f);
        // Exercise the real damage funnel: invulnerability must not trigger a flip.
        var castle = root.AddComponent<EnemyCastleUI>();
        castleData = ScriptableObject.CreateInstance<CastleData>();
        Set(castle, "sourceData", castleData);
        Set(castle, "<currentHP>k__BackingField", 100);
        Set(castle, "<maxHP>k__BackingField", 100);
        Set(castle, "shakeOnDamage", false);
        gc.enemyCastleUI = castle;
        Set(castle, "_invulnerable", true);
        Call(gc, "ApplyCastleAttackDamage", 1, null, null, null, Vector2.zero);
        Assert.AreEqual(0, ((IList)Get(controller, "_newModifierWarnings")).Count);
        Set(castle, "_invulnerable", false);
        Call(gc, "ApplyCastleAttackDamage", 1, null, null, null, Vector2.zero);
        Assert.AreEqual(2, ((IList)Get(controller, "_newModifierWarnings")).Count);
        Assert.AreEqual(2f, Get(controller, "_flipCountdown"));
        Call(controller, "UpdateNewModifierEffects", .5f);
        controller.OnCastleDamaged();
        Assert.AreEqual(1.5f, Get(controller, "_flipCountdown"), "Repeated hits cannot postpone a pending flip.");
        Call(controller, "UpdateNewModifierEffects", 1.5f);
        Assert.IsTrue(controller.BoardFlipped);
        Assert.AreEqual(0f, Get(controller, "_secondsSinceFlip"));
        Assert.That((float)Get(controller, "_flipCountdown"), Is.InRange(5f, 20f));
        controller.OnCastleDamaged();
        Assert.AreEqual(0, ((IList)Get(controller, "_newModifierWarnings")).Count);
        float scheduled = (float)Get(controller, "_flipCountdown");
        Call(controller, "UpdateNewModifierEffects", scheduled - 2f);
        Assert.AreEqual(2, ((IList)Get(controller, "_newModifierWarnings")).Count);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.IsFalse(controller.BoardFlipped, "The scheduled timer flips back too.");
        Set(controller, "_secondsSinceFlip", 5f);
        Set(controller, "_flipCountdown", 15f);
        var chaosHost = controllerRoot.AddComponent<LevelModifierController>();
        var childEffects = (List<LevelModifierController>)Get(chaosHost, "chaosEffects");
        childEffects.Add(controller);
        chaosHost.OnCastleDamaged();
        Assert.AreEqual(2f, Get(controller, "_flipCountdown"), "Castle hits reach retained Chaos modifiers.");
        childEffects.Clear();
        Object.Destroy(chaosHost);
        Call(controller, "ToggleBoardOrientation");
        Assert.Greater(board.gridRoot.TransformVector(Vector3.down).y, 0f, "Gravity rises in world space.");
        controller.EndNewModifierEffects();
        Assert.AreEqual(originalPosition, board.transform.localPosition);
        Assert.AreEqual(originalRotation, board.transform.localRotation);
        Activate(LevelModifierKind.Inversion);
        modifier.inversionBoardSprite = AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/Inversion.asset").inversionBoardSprite;
        Assert.IsTrue(controller.InvertsPieceControls);
        Call(controller, "UpdateNewModifierEffects", 0f);
        var inversionImage = (Image)Get(controller, "_inversionImage");
        Assert.IsNotNull(inversionImage);
        Assert.AreEqual(Image.FillMethod.Radial360, inversionImage.fillMethod);
        var backdrop = (Image)Get(controller, "_inversionBackdrop");
        Assert.AreEqual(Image.Type.Simple, backdrop.type);
        Assert.Greater(inversionImage.color.a, backdrop.color.a);
        Assert.Less(backdrop.transform.GetSiblingIndex(), inversionImage.transform.GetSiblingIndex());
        Assert.AreSame(board.CraterVisualRoot, inversionImage.transform.parent);
        Call(controller, "UpdateNewModifierEffects", 4f);
        Assert.AreEqual(.5f, inversionImage.fillAmount, .0001f);
        Assert.AreEqual(backdrop.rectTransform.localRotation, inversionImage.rectTransform.localRotation);
        Assert.AreNotEqual(Quaternion.identity, backdrop.rectTransform.localRotation);
        Call(controller, "UpdateNewModifierEffects", 4f);
        Assert.IsFalse(controller.InvertsPieceControls);
        Assert.IsFalse(inversionImage.gameObject.activeSelf);
        Call(controller, "UpdateNewModifierEffects", 11f);
        Assert.IsFalse(controller.InvertsPieceControls);
        Call(controller, "UpdateNewModifierEffects", 1f);
        Assert.IsTrue(controller.InvertsPieceControls);
        Assert.AreEqual(1f, inversionImage.fillAmount);
        controller.EndNewModifierEffects();
        Assert.IsFalse(controller.InvertsPieceControls);
        Assert.IsNull(Get(controller, "_inversionImage"));

        Activate(LevelModifierKind.Bombardment);
        Call(controller, "UpdateNewModifierEffects", 0f);
        Call(controller, "UpdateNewModifierEffects", 13f);
        Assert.AreEqual(10, ((IList)Get(controller, "_newModifierWarnings")).Count);
        var target = (Vector2Int)Get(controller, "_bombardmentTarget");
        int bombardmentDeaths = 0;
        board.TileDied += (_, _) => bombardmentDeaths++;
        PlaceMonster(target);
        board.SetFloorEffect(target + Vector2Int.right, Board.FloorEffectType.Burn, 1f, 1f, -1);
        board.TrySpawnStoneObstacle(target + Vector2Int.up);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.IsFalse(board.TryGetMonster(target, out _));
        Assert.AreEqual(0, bombardmentDeaths, "Bombardment removal must not emit reserve-loss death events.");
        Assert.IsFalse(board.HasFloorEffect(target + Vector2Int.right));
        Assert.IsFalse(board.HasObstacle(target + Vector2Int.up));
        Assert.IsTrue(board.IsCraterCell(target));
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            var craterCell = target + new Vector2Int(x, y);
            Assert.IsFalse(board.TryHandleEnemyProjectileObstacleImpact(craterCell));
            Assert.IsFalse(board.TryHandlePlayerAttackObstacleImpact(craterCell));
        }
        Assert.AreEqual(1, ((IList)Get(controller, "_craters")).Count);
        Assert.IsNotNull(((RectTransform)Get(controller, "_modifierFeedbackRoot")).GetComponentInChildren<ImpactEchoVFX>());
        Call(controller, "UpdateNewModifierEffects", 13f);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.AreEqual(2, ((IList)Get(controller, "_craters")).Count);
        Call(controller, "UpdateNewModifierEffects", 13f);
        Call(controller, "UpdateNewModifierEffects", 2f);
        Assert.AreEqual(2, ((IList)Get(controller, "_craters")).Count);
        Assert.IsFalse(board.IsCraterCell(target), "First crater expires at 30 seconds.");
        controller.EndNewModifierEffects();
        board.ClearAll();
        // Craters fill a row but never move or create infinite all-crater clears.
        for (int x = 0; x < board.width; x++) board.SetCraterCell(new Vector2Int(x, 0), true);
        Assert.IsFalse(board.IsClearableOccupiedRow(0));
        board.SetCraterCell(Vector2Int.zero, false);
        PlaceMonster(Vector2Int.zero);
        Assert.IsTrue(board.IsClearableOccupiedRow(0));
        board.ClearFullLines(out var cleared, out _, out _, out _, out _, out _);
        Assert.AreEqual(1, cleared);
        board.SettleAllColumns(true);
        Assert.IsTrue(board.IsCraterCell(Vector2Int.right));
        Assert.IsFalse(board.IsClearableOccupiedRow(0));
        board.TrySpawnStoneObstacle(Vector2Int.zero);
        Assert.IsTrue(board.IsClearableOccupiedRow(0));
        board.ClearFullLines(out cleared, out _, out _, out _, out _, out _);
        Assert.AreEqual(1, cleared, "Craters plus surviving stones cannot loop a cascade forever.");
        board.ClearAll();
        PlaceMonster(new Vector2Int(0, 1));
        for (int y = 2; y <= 4; y++) board.SetCraterCell(new Vector2Int(0, y), true);
        object[] enemySweep = { 0, board.CellToAnchoredPos(new Vector2Int(0, 5)),
            board.CellToAnchoredPos(Vector2Int.zero), Vector2Int.zero, false, null, false };
        Assert.IsTrue((bool)Call(gc, "TryGetCastleDownshotImpactCell", enemySweep));
        Assert.AreEqual(new Vector2Int(0, 1), enemySweep[3], "Enemy attacks reach the unit behind craters.");
        Assert.IsTrue((bool)enemySweep[4]);
        object[] playerSweep = { board.CellToAnchoredPos(Vector2Int.zero),
            board.CellToAnchoredPos(new Vector2Int(0, 5)), Vector2Int.zero };
        Assert.IsFalse((bool)Call(gc, "TryGetPlayerAttackHardenedLavaImpactCell", playerSweep));
        board.ClearAll();

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
        var confirmation = new GameObject("Confirmation above pause", typeof(RectTransform));
        confirmation.transform.SetParent(root.transform, false);
        Call(controller, "LateUpdate");
        Assert.Greater(confirmation.transform.GetSiblingIndex(), pause.transform.GetSiblingIndex(), "Hallucination must not promote pause over a newer dialog.");
        Set(gc, "isPaused", false);
        pause.SetActive(false);
        var positionField = first.GetType().GetField("position");
        var velocityField = first.GetType().GetField("velocity");
        positionField.SetValue(first, new Vector2(1f, 8f));
        velocityField.SetValue(first, new Vector2(-4f, 0f));
        Call(controller, "UpdateNewModifierEffects", 1f);
        Assert.Greater(((Vector2)velocityField.GetValue(first)).x, 0f);
        Assert.That(((Vector2)positionField.GetValue(first)).x, Is.InRange(0f, 10f));
        Vector3 direction = board.gridRoot.TransformVector(Vector3.right).normalized;
        Assert.Greater(Vector3.Dot(hallucinationImage.transform.up, direction), .999f, "Sprite top follows reflected travel.");
        Rect visible = (Rect)Call(controller, "HallucinationVisibleBounds", first);
        float diameter = (float)first.GetType().GetField("diameter").GetValue(first);
        Assert.Less(visible.width, diameter * .9f, "Alpha padding must not determine movement boundaries.");
        positionField.SetValue(first, new Vector2(-10, 8));
        Call(controller, "ConstrainHallucination", first, false);
        Assert.AreEqual(-visible.xMin, ((Vector2)positionField.GetValue(first)).x, .001f);
        Call(controller, "SpawnHallucination");
        var second = hallucinations[1];
        positionField.SetValue(second, positionField.GetValue(first));
        Call(controller, "ResolveHallucinationMerges");
        Assert.AreEqual(1, hallucinations.Count);
        var survivor = hallucinations[0];
        Assert.Greater((float)survivor.GetType().GetField("diameter").GetValue(survivor), modifier.hallucinationBaseSizeCells);
        Assert.AreEqual(modifier.hallucinationBaseSizeCells * 1.65f, (float)survivor.GetType().GetField("diameter").GetValue(survivor), .001f);
        Assert.IsNotNull(((RectTransform)Get(controller, "_hallucinationRoot")).GetComponentInChildren<PixelSprayVFX>());
        float grownRate = (float)Call(controller, "HallucinationSpawnRate");
        Assert.Greater((float)Call(controller, "HallucinationGrowthSpeed", modifier.hallucinationBaseSizeCells * 2f), 1f);
        Set(controller, "_hallucinationElapsed", 180f);
        Assert.Greater((float)Call(controller, "HallucinationSpawnRate"), grownRate);
        survivor.GetType().GetField("consumeFlash").SetValue(survivor, .375f);
        Call(controller, "SyncHallucinationVisual", survivor);
        var grownImage = (Image)survivor.GetType().GetField("image").GetValue(survivor);
        Assert.Greater(grownImage.transform.localScale.x, grownImage.transform.localScale.y);
        var hitPosition = (Vector2)positionField.GetValue(survivor);
        Vector2 hitLocal = board.CellToAnchoredPos(Vector2Int.zero) + Vector2.Scale(hitPosition - Vector2.one * 0.5f, board.GetCellSize());
        var countField = survivor.GetType().GetField("combinedGhosts");
        countField.SetValue(survivor, 6);
        Call(controller, "ResizeHallucination", survivor);
        var projectileHits = new HashSet<int>();
        Assert.IsTrue(controller.TryHitHallucination(hitLocal - Vector2.right * 200f, hitLocal + Vector2.right * 200f, projectileHits));
        Assert.AreEqual(3, countField.GetValue(survivor));
        Assert.AreEqual(2, hallucinations.Count, "The shrunken ghost and new arrival both remain alive.");
        Assert.IsFalse(controller.TryHitHallucination(hitLocal - Vector2.right * 200f, hitLocal + Vector2.right * 200f, projectileHits),
            "The same projectile must not repeatedly shrink a ghost or hit its new arrival.");
        var arrival = hallucinations[1];
        positionField.SetValue(arrival, positionField.GetValue(survivor));
        countField.SetValue(survivor, 9);
        survivor.GetType().GetField("mergeCooldown").SetValue(survivor, 0f);
        arrival.GetType().GetField("mergeCooldown").SetValue(arrival, 0f);
        Call(controller, "ResizeHallucination", survivor);
        Call(controller, "ResolveHallucinationMerges");
        Assert.AreEqual(1, hallucinations.Count);
        survivor = hallucinations[0];
        Assert.AreEqual(10, countField.GetValue(survivor));
        Assert.IsTrue((bool)survivor.GetType().GetField("screenRoaming").GetValue(survivor));
        Rect screenArea = (Rect)Call(controller, "HallucinationMovementArea", survivor);
        Assert.Greater(screenArea.width, board.width);
        Assert.Greater(screenArea.height, board.height);
        grownImage = (Image)survivor.GetType().GetField("image").GetValue(survivor);
        var red = grownImage.color;
        Assert.Greater(red.r, red.g * 2);
        Set(controller, "_hallucinationElapsed", 180.2f);
        Call(controller, "SyncHallucinationVisual", survivor);
        Assert.AreNotEqual(red, grownImage.color, "Escaped ghosts pulse red.");
        Call(controller, "SpawnHallucination");
        positionField.SetValue(hallucinations[1], positionField.GetValue(survivor));
        Call(controller, "ResolveHallucinationMerges");
        Assert.AreEqual(2, hallucinations.Count, "A fully combined ghost stops merging.");
        // A real projectile crosses both ghosts and still damages the castle.
        positionField.SetValue(survivor, new Vector2(5, 8));
        positionField.SetValue(hallucinations[1], new Vector2(5, 8));
        Call(controller, "SyncHallucinationVisual", survivor);
        var projectile = new GameObject("Projectile test", typeof(RectTransform)).GetComponent<RectTransform>();
        projectile.SetParent(board.gridRoot, false);
        hitLocal = board.CellToAnchoredPos(Vector2Int.zero) + Vector2.Scale(new Vector2(4.5f, 7.5f), board.GetCellSize());
        projectile.anchoredPosition = hitLocal - Vector2.right * 200f;
        gc.projectileSpeed = 100000f;
        int beforeCastleHp = castle.currentHP;
        yield return (IEnumerator)Call(gc, "MoveProjectileAndHit", projectile, null, null,
            default(AttackAnimType), hitLocal + Vector2.right * 200f, 5, null, null);
        Assert.AreEqual(beforeCastleHp - 5, castle.currentHP, "Ghosts cannot consume or block a player attack.");
        Assert.AreEqual(5, countField.GetValue(survivor));
        Assert.IsTrue((bool)survivor.GetType().GetField("screenRoaming").GetValue(survivor), "Escaping remains permanent after shrinking.");
        Assert.AreEqual(4, hallucinations.Count);
        Assert.IsTrue(controller.TryHitHallucination(hitLocal, hitLocal, new HashSet<int>()));
        Assert.AreEqual(3, countField.GetValue(survivor), "Odd combinations round up.");
        Assert.Greater((float)Get(controller, "_hallucinationBaseDiameter"), modifier.hallucinationBaseSizeCells);
        Assert.LessOrEqual((float)Get(controller, "_hallucinationCountdown"), 10f);
        controller.EndNewModifierEffects();
        Assert.AreEqual(0, hallucinations.Count);
        Call(controller, "PrepareNewModifierEffects");
        Call(controller, "UpdateNewModifierEffects", 0f);
        Assert.AreEqual(0f, Get(controller, "_hallucinationElapsed"));
        Assert.AreEqual(modifier.hallucinationBaseSizeCells, Get(controller, "_hallucinationBaseDiameter"));
        Assert.AreEqual(1f, Call(controller, "HallucinationSpawnRate"));
        var randomState = Random.state;
        Random.InitState(521);
        float minY = board.height, maxY = 0f;
        for (int i = 0; i < 12; i++)
        {
            var spawned = Call(controller, "SpawnHallucination");
            var position = (Vector2)positionField.GetValue(spawned);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }
        Random.state = randomState;
        Assert.Greater(maxY - minY, board.height * .5f, "New ghosts appear in different board regions.");
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
            if (castleData) Object.Destroy(castleData);
            yield return null;
            yield return new ExitPlayMode();
        }
    }
}
#endif
