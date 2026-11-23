#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Batch entry point: validates real paused animations, not only their prefab wiring.
[InitializeOnLoad]
public static class MarathonTransitionValidation
{
    const string Pending = "MarathonTransitionValidation.Pending";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly Stack<IEnumerator> routines = new();
    static double deadline;
    static MarathonTransitionValidation()
    {
        if (SessionState.GetBool(Pending, false)) EditorApplication.delayCall += Start;
    }
    public static void RunBatch()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending + ".Batch", true);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    [MenuItem("Tetrabeasts/Validate Marathon Transitions (Play Mode)")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run outside Play mode.");
        SessionState.SetBool(Pending + ".Batch", false);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    static void Start()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.delayCall += Start; return; }
        SessionState.SetBool(Pending, false);
        deadline = EditorApplication.timeSinceStartup + 90;
        routines.Push(Verify());
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out waiting for paused animation.");
            while (routines.Count > 0)
            {
                var current = routines.Peek();
                if (!current.MoveNext()) { routines.Pop(); continue; }
                if (current.Current is IEnumerator nested) { routines.Push(nested); continue; }
                return;
            }
            Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }
    static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        Time.timeScale = 1;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/MarathonTransition-validation.txt", error == null
            ? "PASS: finite increasing castle HP, separate reward progress, saved HP/reward progress, reversed curtain while paused, closed-frame hold until readiness signal, forward one-shot opening, authored curtain size, overlay sorting, animated reward opening and closing while paused.\n"
            : "FAIL: " + error);
        if (error != null) Debug.LogException(error);
        if (SessionState.GetBool(Pending + ".Batch", false)) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    static IEnumerator Verify()
    {
        var config = GameModeAssets.Load();
        Check(config && config.marathonCurtainPrefab && config.marathonCurtainOpening, "Curtain assets must be wired.");
        Check(GameController.MarathonCastleHealth(0) == 1000, "Marathon must start at 1000 base HP.");
        Check(GameController.MarathonCastleHealth(1) == 1350 && GameController.MarathonCastleHealth(2) >= 1822, "Castle HP must increase per level.");
        Check(GameController.MarathonCastleHealth(1000) > 0, "Long-run HP must not overflow.");
        var data = ScriptableObject.CreateInstance<CastleData>(); data.maxHP = 10; data.infiniteHealth = true;
        var enemy = new GameObject("Test castle").AddComponent<EnemyCastleUI>();
        enemy.InitCastle(data, 1, 1, false, false, 3000);
        Check(!enemy.InfiniteHealth && enemy.currentHP == 3000, "Marathon must override infinite-health castle assets.");
        enemy.ApplyDamage(500); enemy.RescaleHealth(3000, 2);
        Check(enemy.currentHP == 5000 && enemy.maxHP == 6000, "HP rewards must retain the damage fraction.");
        enemy.ApplyDamage(6000); enemy.RescaleHealth(3000, 1);
        Check(enemy.currentHP == 0, "Rewards must never resurrect a defeated castle.");
        var save = new TempRunSaveStore.SaveData { marathonProgressVersion = 1, marathonRewardLevel = 7, currentLevel = 2, marathonCastleHP = 1234 };
        var restored = JsonUtility.FromJson<TempRunSaveStore.SaveData>(JsonUtility.ToJson(save));
        Check(restored.marathonRewardLevel == 7 && restored.currentLevel == 2 && restored.marathonCastleHP == 1234, "Progress must round-trip independently.");
        enemy.RestoreHealth(restored.marathonCastleHP);
        Check(enemy.currentHP == 1234, "Saved castle damage must be restored.");
        var inactive = new GameObject("Inactive test controller"); inactive.SetActive(false);
        var game = inactive.AddComponent<GameController>();
        Set(game, "modeAssets", config); Set(game, "currentLevel", 9); Set(game, "marathonRewardLevel", 0);
        Check(game.MarathonRequiredDamage == 500, "Castle progression must not advance reward requirements.");
        Set(game, "marathonRewardLevel", 1);
        Check(Mathf.Approximately(game.MarathonRequiredDamage, 590), "Rewards need their own progression.");

        var canvas = new GameObject("Transition test canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var curtain = MarathonCurtainTransition.Create(canvas, config.marathonCurtainPrefab, config.marathonCurtainOpening);
        var art = curtain.GetComponentInChildren<Image>(true);
        Check(art.rectTransform.sizeDelta == config.marathonCurtainPrefab.GetComponent<RectTransform>().sizeDelta, "Keep the artist's curtain dimensions.");
        Check(curtain.GetComponent<Canvas>().overrideSorting && curtain.GetComponent<Canvas>().sortingOrder == 32760, "Curtain must cover level visuals.");
        var curve = AnimationUtility.GetObjectReferenceCurve(config.marathonCurtainOpening,
            EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite"));
        Check(curve != null && curve.Length > 1, "Opening clip must animate the image.");
        Time.timeScale = 0;
        yield return curtain.Close();
        Check(curtain.IsClosed && art.sprite == curve[0].value, "Reverse animation must finish on the closed first frame.");
        var opening = curtain.Open();
        for (int i = 0; i < 8; i++)
        {
            Check(opening.MoveNext() && curtain.IsClosed && art.sprite == curve[0].value, "Curtain must hold until signaled.");
            yield return null;
        }
        curtain.SignalVisualsReady();
        yield return opening;
        Check(!curtain.gameObject.activeSelf && art.sprite == curve[curve.Length - 1].value, "Opening must end once on the final frame.");
        Check(Time.timeScale == 0, "Curtain must not resume gameplay itself before its owner is ready.");

        var rewards = Object.Instantiate(config.marathonRewardPrefab, canvas.transform);
        rewards.gameObject.SetActive(false);
        var buff = ScriptableObject.CreateInstance<RunModifier>(); var debuff = ScriptableObject.CreateInstance<RunModifier>();
        buff.displayName = "Buff"; debuff.displayName = "Debuff"; buff.description = debuff.description = "Test";
        var pair = new MarathonRewardUI.Pair { buff = buff, debuff = debuff };
        int confirmations = 0;
        rewards.Show(2, new[] { pair, pair, pair }, (b, d) => confirmations++, game, true);
        Check(UIPanelTransition.IsPanelTransitioning(rewards.gameObject), "Rewards must animate open like pause.");
        yield return rewards.WaitUntilShown();
        rewards.choices[0].onClick.Invoke(); rewards.confirmButton.onClick.Invoke(); rewards.confirmButton.onClick.Invoke();
        Check(confirmations == 1 && rewards.gameObject.activeSelf, "Confirm must be one-shot with deferred animated closing.");
        rewards.Dismiss();
        Check(UIPanelTransition.IsPanelTransitioning(rewards.gameObject), "Rewards must animate closed.");
        yield return rewards.WaitUntilHidden();
        Check(Time.timeScale == 0, "Rewards must finish animating while gameplay stays paused.");
        Object.Destroy(canvas.gameObject); Object.Destroy(inactive); Object.Destroy(enemy.gameObject);
        Object.Destroy(data); Object.Destroy(buff); Object.Destroy(debuff);
    }
}
#endif
