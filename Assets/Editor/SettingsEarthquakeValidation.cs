#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SettingsEarthquakeValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    [MenuItem("Tetrabeasts/Validate Settings and Earthquake")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run outside Play mode.");
        var settings = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Panels/Volume_Panel.prefab");
        var fps = settings.GetComponentsInChildren<TMP_Dropdown>(true).Single(d => d.name == "Frame_Rate_Dropdown");
        Check(fps.options.Select(o => o.text).SequenceEqual(VideoSettingsStore.FrameRateLabels), "Authored FPS choices must match saved values.");
        Check(VideoSettingsStore.Defaults.frameRate == 60 && !VideoSettingsStore.Defaults.vSync, "Default limiter must be 60 FPS.");
        foreach (int rate in VideoSettingsStore.FrameRates)
        {
            var options = VideoSettingsStore.Defaults; options.frameRate = rate;
            Check(VideoSettingsStore.Validate(options).frameRate == rate, "Limiter value rejected: " + rate);
        }
        foreach (string name in new[] { "Video_Keep", "Video_Revert" })
        {
            var label = settings.GetComponentsInChildren<Button>(true).Single(b => b.name == name).GetComponentInChildren<TMP_Text>(true);
            Check(label.alignment == TextAlignmentOptions.Center && Mathf.Approximately(label.rectTransform.anchoredPosition.y, 5), "Confirmation label must match the button face alignment.");
        }
        var reward = AssetDatabase.LoadAssetAtPath<MarathonRewardUI>(MarathonRewardPresentationAuthoring.RewardPath);
        var panel = (RectTransform)reward.transform.Find("Panel");
        Check(panel.anchorMin == Vector2.zero && panel.anchorMax == Vector2.one && panel.sizeDelta == Vector2.zero, "Reward panel must fill the screen.");
        var confirm = (RectTransform)reward.confirmButton.transform;
        Check(confirm.parent == reward.transform && confirm.anchorMin == new Vector2(.5f, 0), "Confirm must be independently anchored at the bottom.");
        Check(Mathf.Abs(confirm.sizeDelta.x * confirm.localScale.x - 280f * 692f / 900f) < .01f, "Confirm's previous visual size must be preserved.");

        var scene = EditorSceneManager.NewPreviewScene();
        IEnumerator ripple = null;
        try
        {
            var root = new GameObject("Earthquake validation", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);
            var board = root.AddComponent<Board>();
            board.width = 9; board.height = 16;
            typeof(Board).GetMethod("Awake", Private).Invoke(board, null);
            typeof(Board).GetField("_gc", Private).SetValue(board, null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(360, 640);
            board.RecomputeCellMetrics();
            var placed = (Dictionary<Vector2Int, RectTransform>)typeof(Board).GetField("placed", Private).GetValue(board);
            for (int x = 0; x < 9; x++)
            {
                var cell = new Vector2Int(x, 4);
                var tile = new GameObject("Tile " + x, typeof(RectTransform)).GetComponent<RectTransform>();
                tile.SetParent(board.gridRoot, false); tile.anchoredPosition = board.CellToAnchoredPos(cell);
                placed.Add(cell, tile);
            }
            board.SetCraterCell(new Vector2Int(0, 4), true);
            var obstacles = (Dictionary<Vector2Int, Board.ObstacleState>)typeof(Board).GetField("obstacles", Private).GetValue(board);
            obstacles[new Vector2Int(3, 4)] = new Board.ObstacleState(Board.ObstacleType.Stone, 3);
            obstacles[new Vector2Int(4, 4)] = new Board.ObstacleState(Board.ObstacleType.MagicExplosive, 1);
            obstacles[new Vector2Int(5, 4)] = new Board.ObstacleState(Board.ObstacleType.MagicPylon, 1);
            var explosives = (IDictionary)typeof(Board).GetField("magicExplosives", Private).GetValue(board);
            var explosiveType = explosives.GetType().GetGenericArguments()[1];
            var explosiveState = Activator.CreateInstance(explosiveType);
            explosiveType.GetField("fuseSeconds").SetValue(explosiveState, 123f);
            explosives.Add(new Vector2Int(4, 4), explosiveState);
            var original = placed.ToDictionary(p => p.Key, p => p.Value.anchoredPosition);
            var visuals = placed.ToDictionary(p => p.Key, p => p.Value);
            var animate = typeof(Board).GetMethod("AnimateEarthquakeRipple", Private);
            ripple = (IEnumerator)animate.Invoke(board, new object[] { 4, new Func<float>(() => .01f) });
            Check(ripple.MoveNext() && board.IsEarthquakeAnimating, "Ripple must start and block gameplay during animation.");
            float Offset(int x) => visuals[new Vector2Int(x, 4)].anchoredPosition.y - original[new Vector2Int(x, 4)].y;
            Check(Offset(4) > 0 && Offset(3) == 0 && Offset(5) == 0, "Epicenter must move before adjacent columns.");
            for (int i = 0; i < 7; i++) ripple.MoveNext();
            Check(Offset(3) > 0 && Mathf.Approximately(Offset(3), Offset(5)) && Offset(2) == 0, "Ripple must move out symmetrically one column at a time.");
            for (int i = 0; i < 9; i++) ripple.MoveNext();
            Check(placed.ContainsKey(new Vector2Int(4, 0)) && !placed.ContainsKey(new Vector2Int(4, 4)) && Offset(8) == 0,
                "The epicenter must start settling before the wave reaches the outer columns.");
            float descending = Offset(4);
            Check(descending > 0 && descending < board.GetCellSize().y * board.earthquakeBounceHeightInCells,
                "After the crest, the tile must descend smoothly rather than snapping to its destination.");
            for (int i = 0; i < 12; i++)
            {
                ripple.MoveNext();
                Check(Offset(4) < descending, "Descent must continue without bouncing back or waiting at the old position.");
                descending = Offset(4);
            }
            Check(descending < 0, "The falling block must pass through its old row directly into the gap below.");
            for (int steps = 0; ripple.MoveNext(); steps++) Check(steps < 200, "Ripple must finish.");
            Check(!board.IsEarthquakeAnimating, "Gameplay must resume after the ripple.");
            foreach (var tile in placed) Check(Vector2.Distance(tile.Value.anchoredPosition, board.CellToAnchoredPos(tile.Key)) < .001f, "Every tile must land at its actual settled cell.");
            Check(board.IsCraterCell(new Vector2Int(0, 4)) && Offset(0) == 0, "Bombardment craters must stay fixed.");
            ripple = (IEnumerator)animate.Invoke(board, new object[] { 4, new Func<float>(() => .02f) });
            ripple.MoveNext(); ((IDisposable)ripple).Dispose();
            Check(!board.IsEarthquakeAnimating && Vector2.Distance(visuals[new Vector2Int(4, 4)].anchoredPosition, board.CellToAnchoredPos(new Vector2Int(4, 0))) < .001f,
                "Cancelling during lift must restore the current cell and release gameplay.");
            Check(obstacles[new Vector2Int(3, 0)].type == Board.ObstacleType.Stone &&
                obstacles[new Vector2Int(5, 0)].type == Board.ObstacleType.MagicPylon, "Movable obstacles must settle after the ripple.");
            Check(!explosives.Contains(new Vector2Int(4, 4)) && explosives.Contains(new Vector2Int(4, 0)) &&
                (float)explosiveType.GetField("fuseSeconds").GetValue(explosives[new Vector2Int(4, 0)]) == 123f,
                "Explosive fuse data must follow the settled explosive.");
            var extra = new GameObject("Cancellation during fall", typeof(RectTransform)).GetComponent<RectTransform>();
            extra.SetParent(board.gridRoot, false); placed[new Vector2Int(4, 8)] = extra;
            extra.anchoredPosition = board.CellToAnchoredPos(new Vector2Int(4, 8));
            ripple = (IEnumerator)animate.Invoke(board, new object[] { 4, new Func<float>(() => .01f) });
            for (int i = 0; i < 20; i++) ripple.MoveNext();
            ((IDisposable)ripple).Dispose();
            Check(!board.IsEarthquakeAnimating && placed[new Vector2Int(4, 1)] == extra &&
                Vector2.Distance(extra.anchoredPosition, board.CellToAnchoredPos(new Vector2Int(4, 1))) < .001f,
                "Cancelling during descent must leave committed column data and visuals consistent.");
        }
        finally
        {
            (ripple as IDisposable)?.Dispose();
            EditorSceneManager.ClosePreviewScene(scene);
        }
        MarathonRewardPresentationValidation.Run();
        File.WriteAllText("Logs/SettingsEarthquake-validation.txt", "PASS: settings and reward layout; symmetric earthquake lift flowing directly into independent column falls; fixed craters; cancellation before/after crest; settled obstacles and explosive fuse data.\n");
    }
}
#endif
