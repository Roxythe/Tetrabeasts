#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MarathonFollowupTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private | BindingFlags.Public).SetValue(obj, value);
    static object Get(object obj, string field) => obj.GetType().GetField(field, Private | BindingFlags.Public).GetValue(obj);

    [Test]
    public void MarathonGravityCarriesOverAndCapGrowthDiminishes()
    {
        float previousIncrease = float.MaxValue;
        for (int level = 1; level < 100; level++)
        {
            float increase = GameController.MarathonGravityCap(level) - GameController.MarathonGravityCap(level - 1);
            Assert.That(increase, Is.GreaterThan(0).And.LessThan(previousIncrease));
            previousIncrease = increase;
        }
        var previousMode = GameModeStore.Current;
        var go = new GameObject("Inactive gravity test"); go.SetActive(false);
        try
        {
            GameModeStore.Current = RunGameMode.Marathon;
            var gc = go.AddComponent<GameController>();
            Set(gc, "marathonCarriedGravity", 2.5f);
            Set(gc, "marathonElapsedSeconds", 360f);
            Set(gc, "_level1FallInterval", 1f);
            typeof(GameController).GetMethod("ResetLevelTimerAndDrop", Private).Invoke(gc, new object[] { 1 });
            Assert.AreEqual(0.4f, (float)Get(gc, "_thisLevelBaseFallInterval"), 0.0001f, "Level-up must retain carried speed instead of resetting to the starting speed.");
            Assert.AreEqual(360f, Get(gc, "marathonElapsedSeconds"));
            float late = (float)typeof(GameController).GetMethod("GetCurrentGravityIncreasePerSecond", Private).Invoke(gc, null);
            Set(gc, "marathonElapsedSeconds", 0f);
            float initial = (float)typeof(GameController).GetMethod("GetCurrentGravityIncreasePerSecond", Private).Invoke(gc, null);
            Assert.Greater(late, initial);
        }
        finally { Object.DestroyImmediate(go); GameModeStore.Current = previousMode; }
    }

    [Test]
    public void GameOverHidesBothMarathonPanelsAndCannotRecreateGauge()
    {
        var previousMode = GameModeStore.Current;
        var go = new GameObject("Inactive game over test"); go.SetActive(false);
        var gauge = Object.Instantiate(GameModeAssets.Load().marathonGaugePrefab);
        var rewards = Object.Instantiate(GameModeAssets.Load().marathonRewardPrefab);
        try
        {
            GameModeStore.Current = RunGameMode.Marathon;
            var gc = go.AddComponent<GameController>();
            Set(gc, "marathonGauge", gauge); Set(gc, "marathonRewards", rewards);
            Set(gc, "gameOver", true);
            typeof(GameController).GetMethod("TickGameMode", Private).Invoke(gc, null);
            Assert.IsFalse(gauge.gameObject.activeSelf); Assert.IsFalse(rewards.gameObject.activeSelf);
            Set(gc, "gameOver", false);
            typeof(GameController).GetMethod("EnsureMarathonUI", Private).Invoke(gc, null);
            Assert.IsTrue(gauge.gameObject.activeSelf, "Restarting Marathon must show the reused gauge.");
            Set(gc, "gameOver", true);
            Set(gc, "marathonGauge", null);
            typeof(GameController).GetMethod("EnsureMarathonUI", Private).Invoke(gc, null);
            Assert.IsNull(Get(gc, "marathonGauge"));
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(gauge.gameObject); Object.DestroyImmediate(rewards.gameObject); GameModeStore.Current = previousMode; }
    }

    [TestCase(Slider.Direction.LeftToRight)]
    [TestCase(Slider.Direction.BottomToTop)]
    public void SharedGaugeMotionRespondsToGainAndLoss(Slider.Direction direction)
    {
        var root = new GameObject("Gauge", typeof(RectTransform), typeof(Slider));
        try
        {
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image), typeof(MarathonLiquidSlosh));
            fill.transform.SetParent(root.transform, false);
            var slider = root.GetComponent<Slider>(); slider.fillRect = (RectTransform)fill.transform;
            slider.direction = direction;
            var image = fill.GetComponent<Image>(); image.type = Image.Type.Filled;
            image.fillMethod = direction == Slider.Direction.LeftToRight ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;
            slider.value = 0.5f;
            var slosh = fill.GetComponent<MarathonLiquidSlosh>();
            var motion = root.AddComponent<GaugeLiquidMotion>();
            typeof(GaugeLiquidMotion).GetMethod("OnEnable", Private).Invoke(motion, null);
            slider.value = 0.7f;
            typeof(GaugeLiquidMotion).GetMethod("LateUpdate", Private).Invoke(motion, null);
            Assert.Greater(slosh.WaveEnergy, 0);
            slosh.enabled = false; slosh.enabled = true;
            slider.value = 0.4f;
            typeof(GaugeLiquidMotion).GetMethod("LateUpdate", Private).Invoke(motion, null);
            Assert.Greater(slosh.WaveEnergy, 0, "Spending or losing gauge points must also trigger a wave.");
            var bounds = slider.fillRect.rect;
            using (var mesh = new VertexHelper())
            {
                var quad = new UIVertex[4];
                for (int i = 0; i < 4; i++) quad[i] = UIVertex.simpleVert;
                float right = direction == Slider.Direction.LeftToRight ? Mathf.Lerp(bounds.xMin, bounds.xMax, 0.4f) : bounds.xMax;
                float top = direction == Slider.Direction.BottomToTop ? Mathf.Lerp(bounds.yMin, bounds.yMax, 0.4f) : bounds.yMax;
                quad[0].position = new Vector3(bounds.xMin, bounds.yMin);
                quad[1].position = new Vector3(bounds.xMin, top);
                quad[2].position = new Vector3(right, top);
                quad[3].position = new Vector3(right, bounds.yMin);
                mesh.AddUIVertexQuad(quad); slosh.ModifyMesh(mesh);
                Assert.Greater(mesh.currentVertCount, 4);
                UIVertex vertex = default;
                for (int i = 0; i < mesh.currentVertCount; i++)
                {
                    mesh.PopulateUIVertex(ref vertex, i);
                    Assert.That(vertex.position.x, Is.InRange(bounds.xMin, bounds.xMax));
                    Assert.That(vertex.position.y, Is.InRange(bounds.yMin, bounds.yMax));
                }
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void ScoreStorageSeparatesAllModesAndPreservesLegacyArchive()
    {
        var modes = new[] { RunGameMode.Standard, RunGameMode.Chaos, RunGameMode.Marathon };
        var keys = modes.Select(HighScoreManager.KeyForMode).ToArray();
        var present = keys.Select(PlayerPrefs.HasKey).ToArray();
        var backup = keys.Select(k => PlayerPrefs.GetString(k, "")).ToArray();
        string legacy = PlayerPrefs.GetString("HighScores_v1", "");
        bool hadLegacy = PlayerPrefs.HasKey("HighScores_v1");
        try
        {
            PlayerPrefs.DeleteKey("HighScores_v1");
            for (int i = 0; i < modes.Length; i++)
            {
                PlayerPrefs.DeleteKey(keys[i]);
                HighScoreManager.Add("Mode " + i, (i + 1) * 100, mode: modes[i]);
            }
            for (int i = 0; i < modes.Length; i++)
            {
                var rows = HighScoreManager.Load(modes[i]);
                Assert.AreEqual(1, rows.Count); Assert.AreEqual((i + 1) * 100, rows[0].score);
            }
            Assert.AreEqual(3, modes.Select(SteamLeaderboardService.NameForMode).Distinct().Count());
            Assert.AreEqual(3, modes.Select(m => SteamLeaderboardResetSchedule.GetActiveLeaderboardName(SteamLeaderboardService.NameForMode(m))).Distinct().Count());
            Assert.AreEqual("", PlayerPrefs.GetString("HighScores_v1", ""));
        }
        finally
        {
            if (hadLegacy) PlayerPrefs.SetString("HighScores_v1", legacy);
            for (int i = 0; i < keys.Length; i++)
                if (present[i]) PlayerPrefs.SetString(keys[i], backup[i]); else PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void LegacyScoresMergeIntoStandardOnceAndKeepNewScores()
    {
        var keys = new[] { "HighScores_v1", HighScoreManager.LegacyMigrationKey, HighScoreManager.KeyForMode(RunGameMode.Standard) };
        var saved = keys.Select(k => PlayerPrefs.GetString(k, "")).ToArray();
        var present = keys.Select(PlayerPrefs.HasKey).ToArray();
        try
        {
            foreach (var key in keys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.SetString(keys[0], "{\"entries\":[{\"name\":\"Legacy\",\"score\":500,\"date\":\"2026-01-01\"}]}");
            HighScoreManager.Save(new System.Collections.Generic.List<HighScoreEntry> { new HighScoreEntry { name = "New", score = 800 } });
            var scores = HighScoreManager.Load();
            Assert.AreEqual(2, scores.Count); Assert.AreEqual(800, scores[0].score); Assert.AreEqual(500, scores[1].score);
            Assert.AreEqual(2, HighScoreManager.Load().Count, "Repeated loads must not duplicate migrated scores.");
            Assert.IsTrue(PlayerPrefs.HasKey("HighScores_v1"));
            Assert.AreEqual("Tetrabeasts_GlobalScore", SteamLeaderboardService.NameForMode(RunGameMode.Standard));
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++) if (present[i]) PlayerPrefs.SetString(keys[i], saved[i]); else PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void MarathonReinforcementsUseNormalBonusesAndReserveCap()
    {
        var go = new GameObject("Inactive reinforcement test"); go.SetActive(false);
        try
        {
            var gc = go.AddComponent<GameController>();
            Set(gc, "unitLives", 1); Set(gc, "reinforcementsPerWin", 5);
            int actual = (int)typeof(GameController).GetMethod("GrantMarathonReinforcements", Private).Invoke(gc, null);
            Assert.AreEqual(5, actual); Assert.AreEqual(6, Get(gc, "unitLives"));
            Set(gc, "unitLives", gc.MaxReserveUnits);
            Assert.AreEqual(0, typeof(GameController).GetMethod("GrantMarathonReinforcements", Private).Invoke(gc, null));
            Set(gc, "unitLives", 1); Set(gc, "disableRoundWinReserveRestore", true);
            Assert.AreEqual(0, typeof(GameController).GetMethod("GrantMarathonReinforcements", Private).Invoke(gc, null));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void SceneGaugesUseSharedLiquidEffectsAndCastleArtworkRefreshes()
    {
        var previousMode = GameModeStore.Current;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
        try
        {
            var gc = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameController>(true)).Single();
            GameModeStore.Current = RunGameMode.Marathon;
            Set(gc, "modeAssets", GameModeAssets.Load());
            typeof(GameController).GetMethod("EnsureMarathonUI", Private).Invoke(gc, null);
            var gauge = (MarathonGaugeUI)Get(gc, "marathonGauge");
            Assert.AreSame(gc.specialSlider.transform.parent, gauge.transform.parent);
            Assert.Less(gauge.transform.GetSiblingIndex(), gc.specialSlider.transform.GetSiblingIndex());
            gc.specialSlider.transform.parent.gameObject.SetActive(false);
            Assert.IsFalse(gauge.gameObject.activeInHierarchy, "HUD visibility must hide both gauges together.");
            gc.specialSlider.transform.parent.gameObject.SetActive(true);
            Assert.GreaterOrEqual(gc.specialSlider.fillRect.GetComponent<Image>().material.GetFloat("_BubbleScale"), 6);
            foreach (var slider in new[] { gc.specialSlider, gc.enemyCastleUI.healthBarSlider, gc.unitLivesSlider })
            {
                Assert.IsNotNull(slider.GetComponent<GaugeLiquidMotion>(), slider.name);
                var fill = slider.fillRect.GetComponent<Image>();
                Assert.IsNotNull(fill.GetComponent<MarathonLiquidSlosh>(), slider.name);
                Assert.AreEqual(Image.Type.Filled, fill.type);
                Assert.AreEqual("Tetrabeasts/UI/Marathon Liquid", fill.material.shader.name);
            }
            var modifiers = gc.GetComponent<LevelModifierController>();
            var castles = gc.castlesByLevel.Where(c => c && !c.isBossLevel).Take(2).ToArray();
            Assert.AreEqual(2, castles.Length);
            foreach (var castle in castles)
            {
                modifiers.RefreshCastlePresentation(castle);
                var background = (Image)Get(modifiers, "boardBackgroundImage");
                Assert.AreSame(castle.boardBackgroundSprite, background.sprite);
                var border = (GameObject)typeof(LevelModifierController).GetMethod("ResolveCastleBoardBorderRoot", Private)?.Invoke(modifiers, new object[] { castle });
                if (border) Assert.IsTrue(border.activeSelf);
                gc.enemyCastleUI.InitCastle(castle, 1);
                Assert.AreSame(castle.hpGaugeFrameSprite, gc.enemyCastleUI.hpGaugeFrameImage.sprite);
                Assert.AreSame(castle.hpGaugeFrameSprite, gc.enemyCastleUI.hpGaugeFrameImage.overrideSprite);
            }
        }
        finally { EditorSceneManager.CloseScene(scene, true); GameModeStore.Current = previousMode; }
    }
}
#endif
