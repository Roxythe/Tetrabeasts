#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

public class PanelAuthoringFollowupTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static GameObject Gauge(out Slider slider, out GaugeLiquidMotion motion)
    {
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab"));
        slider = go.GetComponentInChildren<Slider>(true);
        motion = slider.GetComponent<GaugeLiquidMotion>();
        Call(motion, "OnEnable");
        return go;
    }

    [Test]
    public void RepeatedCastleHitsRetargetSmoothlyAndNewCastlesStartFull()
    {
        var gauge = Gauge(out var slider, out var motion);
        var go = new GameObject("Castle test"); go.SetActive(false);
        var castle = go.AddComponent<EnemyCastleUI>(); castle.healthBarSlider = slider;
        var data = ScriptableObject.CreateInstance<CastleData>(); data.maxHP = 875;
        Set(castle, "shakeOnDamage", false);
        try
        {
            castle.InitCastle(data, 1);
            Assert.AreEqual(1f, slider.normalizedValue);
            castle.ApplyDamage(400);
            Assert.AreEqual(1f, slider.normalizedValue, "Damage should change gameplay HP before the visual catches up.");
            Call(motion, "AdvanceVisual", .05f);
            float beforeSecondHit = slider.value;
            Assert.That(beforeSecondHit, Is.InRange(476f, 874f));
            castle.ApplyDamage(100);
            Assert.AreEqual(beforeSecondHit, slider.value);
            Call(motion, "AdvanceVisual", .05f);
            Assert.Less(slider.value, beforeSecondHit);
            for (int i = 0; i < 180; i++) Call(motion, "AdvanceVisual", 1f / 60f);
            Assert.AreEqual(375f, slider.value, .1f);
            castle.InitCastle(data, 2);
            Assert.AreEqual(875f, slider.value);
        }
        finally { Call(motion, "OnDisable"); Object.DestroyImmediate(go); Object.DestroyImmediate(gauge); Object.DestroyImmediate(data); }
    }

    [Test]
    public void ReserveGainAndLossEaseWithoutIntegerRounding()
    {
        var gauge = Gauge(out var slider, out var motion);
        var go = new GameObject("Reserve test"); go.SetActive(false);
        var gc = go.AddComponent<GameController>(); gc.unitLivesSlider = slider;
        try
        {
            slider.wholeNumbers = true; // The original reserve scene used an integer slider.
            int full = gc.MaxReserveUnits;
            Set(gc, "unitLives", full); Call(gc, "UpdateUnitLivesUI", true);
            Assert.AreEqual(1f, slider.normalizedValue);
            Set(gc, "unitLives", full / 2); Call(gc, "UpdateUnitLivesUI", false);
            Call(motion, "AdvanceVisual", .05f);
            Assert.That(slider.normalizedValue, Is.InRange(.51f, .99f));
            Assert.IsFalse(slider.wholeNumbers);
            for (int i = 0; i < 180; i++) Call(motion, "AdvanceVisual", 1f / 60f);
            float low = slider.normalizedValue;
            Set(gc, "unitLives", full); Call(gc, "UpdateUnitLivesUI", false);
            Assert.AreEqual(low, slider.normalizedValue);
            Call(motion, "AdvanceVisual", .05f);
            Assert.Greater(slider.normalizedValue, low); Assert.Less(slider.normalizedValue, 1f);
        }
        finally { Call(motion, "OnDisable"); Object.DestroyImmediate(go); Object.DestroyImmediate(gauge); }
    }

    [Test]
    public void LoadingStallsCannotSkipDotsOrConsumeTheVisibleAnimationTime()
    {
        var go = new GameObject("Loading test"); go.SetActive(false);
        var loading = go.AddComponent<LoadingScreen>();
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(go.transform);
        try
        {
            Set(loading, "_loadingTexts", new[] { label });
            Set(loading, "_loadingTextBaseStrings", new[] { "Loading" });
            Set(loading, "_loadingTextBaseColors", new[] { Color.white });
            Call(loading, "StartLoadingTextBlink");
            Assert.AreEqual("Loading", label.text);
            Call(loading, "AdvanceLoadingAnimation", 5f);
            Assert.AreEqual("Loading", label.text);
            Assert.Greater((float)Call(loading, "GetMinimumVisibleRemainingSeconds"), .9f);
            for (int i = 0; i < 3; i++) Call(loading, "AdvanceLoadingAnimation", .0625f);
            Assert.AreEqual("Loading.", label.text);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void MonsterCardsHideAndRestoreTheHealingColumnWhenRebound()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MonsterUnitStats_Prefab.prefab"));
        var view = root.GetComponent<MonsterUnitStatsUI>();
        var monster = ScriptableObject.CreateInstance<MonsterData>();
        try
        {
            foreach (var role in new[] { MonsterRole.Attack, MonsterRole.Healer, MonsterRole.Defense, MonsterRole.Healer })
            {
                monster.role = role;
                view.Bind(monster, null, 1, "HP: 20", "Heal Power: 4");
                var field = (TMP_Text)typeof(MonsterUnitStatsUI).GetField("stats2Text", Private).GetValue(view);
                Assert.AreEqual(role == MonsterRole.Healer, field.gameObject.activeSelf);
            }
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(monster); }
    }

    [Test]
    public void UnlockedAchievementBorderAndHaloAreClearedForLockedRows()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AchievementRow_Prefab.prefab"));
        var row = root.GetComponent<AchievementRowUI>();
        try
        {
            row.SetCustom(null, "Completed", "Example", true, false);
            Assert.IsTrue(row.completedBorder.activeSelf); Assert.IsTrue(row.completedGlow.activeSelf);
            row.SetCustom(null, "Locked", "Example", false, true);
            Assert.IsFalse(row.completedBorder.activeSelf); Assert.IsFalse(row.completedGlow.activeSelf);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void EditableModePopupKeepsItsAuthoredTypography()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameModes/GameModeTooltip.prefab");
        Assert.IsNotNull(prefab);
        Capture("Mode-Popup-Editable", canvas =>
        {
            var instance = Object.Instantiate(prefab, canvas.transform);
            instance.SetActive(true);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            var view = instance.GetComponent<GameModeTooltip>();
            float authoredSize = view.description.fontSize;
            view.ShowMode(RunGameMode.Marathon, "Marathon", false);
            Assert.AreEqual(view.descriptions.marathon, view.description.text);
            Assert.AreEqual(authoredSize, view.description.fontSize);
            Assert.IsFalse(view.lockNote.gameObject.activeSelf);
        });
        Capture("Achievement-Completed-And-Locked", canvas =>
        {
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AchievementRow_Prefab.prefab");
            var defs = AssetDatabase.FindAssets("t:AchievementDefSO");
            var def = AssetDatabase.LoadAssetAtPath<AchievementDefSO>(AssetDatabase.GUIDToAssetPath(defs[0]));
            for (int i = 0; i < 2; i++)
            {
                var go = Object.Instantiate(rowPrefab, canvas.transform); go.SetActive(true);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0f, i == 0 ? 190f : -190f);
                var row = go.GetComponent<AchievementRowUI>();
                row.SetCustom(def.icon, i == 0 ? "Achievement Complete" : "Achievement Locked", def.description, i == 0, i != 0);
                row.progressText.text = i == 0 ? "18 Seconds" : "0 / 1";
                row.progressSlider.normalizedValue = i == 0 ? 1f : 0f;
            }
        });
    }

    static void Capture(string name, System.Action<Canvas> build)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1920, 1080, 24);
        try
        {
            var camera = new GameObject("Preview camera", typeof(Camera)).GetComponent<Camera>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.overrideSceneCullingMask = UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
            camera.targetTexture = texture; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.06f, .07f, .08f);
            var canvas = new GameObject("Preview canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            build(canvas);
            GameplayPlaylistTests.Capture(camera, name);
            camera.targetTexture = null;
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(texture); }
    }
}

public class LoadingTransitionFollowupTests
{
    [SetUp] public void SetUp()
    {
        LocalSaveTestScope.Begin();
        LocalSaveTestScope.BackupScenePreferences();
    }

    [UnityTearDown] public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        LocalSaveTestScope.RestoreScenePreferences();
        LocalSaveTestScope.End();
    }

    [UnityTest] public IEnumerator ActualScenesPresentAllFourLoadingStatesBeforeClosing()
    {
        yield return new EnterPlayMode();
        LocalSaveTestScope.Apply();
        foreach (string scene in new[] { "TitleScene", "GameplayScene" })
        {
            Assert.IsTrue(LoadingScreen.LoadSceneAsync(scene));
            var loading = LoadingScreen.Ensure();
            var field = typeof(LoadingScreen).GetField("_loadingDotStep", BindingFlags.Instance | BindingFlags.NonPublic);
            var seen = new HashSet<int>();
            float started = Time.realtimeSinceStartup, previous = started, longestFrame = 0f;
            while (LoadingScreen.IsVisible && Time.realtimeSinceStartup - started < 60f)
            {
                seen.Add((int)field.GetValue(loading));
                yield return null;
                float now = Time.realtimeSinceStartup;
                longestFrame = Mathf.Max(longestFrame, now - previous); previous = now;
            }
            Assert.IsFalse(LoadingScreen.IsVisible, scene + " must finish loading.");
            Assert.AreEqual(4, seen.Count, scene + " should visibly cycle every dot state.");
            Debug.Log($"Loading verification {scene}: all four states, longest frame {longestFrame:0.000}s.");
        }
        yield return new ExitPlayMode();
    }
}
#endif
