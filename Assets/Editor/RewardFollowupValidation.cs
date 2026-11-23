#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RewardFollowupValidation
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    [MenuItem("Tetrabeasts/Validate Reward and Settling Followup")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run outside Play mode.");
        float[] expected = { .03f, .05f, .07f, .10f, .15f };
        for (int i = 0; i < expected.Length; i++)
        {
            var buff = AssetDatabase.LoadAssetAtPath<RunModifier>($"Assets/ScriptableObjects/RunModifier/Buff/Buff_LineClearCurChance_{i}.asset");
            Check(buff.op == RunModOp.Add && Mathf.Approximately(buff.value, expected[i]), "Currency chance must use fractional probability.");
            Check(Mathf.Abs(buff.Project(.13f) - (.13f + expected[i])) < .0001f, "Currency reward preview and application must increase the chance by percentage points.");
        }
        var scene = EditorSceneManager.NewPreviewScene();
        IEnumerator fall = null;
        try
        {
            var root = new GameObject("Row settling validation", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);
            var board = root.AddComponent<Board>(); board.width = 3; board.height = 16;
            typeof(Board).GetMethod("Awake", Private).Invoke(board, null);
            typeof(Board).GetField("_gc", Private).SetValue(board, null);
            ((RectTransform)root.transform).sizeDelta = new Vector2(120, 640); board.RecomputeCellMetrics();
            var placed = (Dictionary<Vector2Int, RectTransform>)typeof(Board).GetField("placed", Private).GetValue(board);
            var blocks = (Dictionary<Vector2Int, Board.ObstacleState>)typeof(Board).GetField("obstacles", Private).GetValue(board);
            RectTransform Tile(int x, int y)
            {
                var rect = new GameObject("Tile", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(board.gridRoot, false); rect.anchoredPosition = board.CellToAnchoredPos(new Vector2Int(x, y));
                placed[new Vector2Int(x,y)] = rect; return rect;
            }
            var low = Tile(0, 1); var high = Tile(0, 4); var blocker = Tile(1, 2); var above = Tile(1, 5);
            blocks[new Vector2Int(1, 2)] = new Board.ObstacleState(Board.ObstacleType.Stone, 3);
            float dt = .02f; var origins = new Dictionary<int, List<int>>();
            fall = (IEnumerator)typeof(Board).GetMethod("AnimateRowClearSettle", Private).Invoke(board, new object[] { origins, new Func<float>(() => dt) });
            Check(fall.MoveNext() && board.IsColumnSettling, "Settling must animate before the cascade proceeds.");
            Check(placed[new Vector2Int(0,0)] == low && placed[new Vector2Int(0,1)] == high, "Logical occupancy must settle bottom-up.");
            Check(high.anchoredPosition.y < board.CellToAnchoredPos(new Vector2Int(0,4)).y && high.anchoredPosition.y > board.CellToAnchoredPos(new Vector2Int(0,1)).y, "Units must descend smoothly rather than snap.");
            Check(blocker.anchoredPosition == board.CellToAnchoredPos(new Vector2Int(1,2)) && placed[new Vector2Int(1,3)] == above, "Fixed obstacles must remain blockers.");
            var paused = high.anchoredPosition; dt = 0; fall.MoveNext(); Check(high.anchoredPosition == paused, "Zero gameplay time must freeze settling."); dt = .02f;
            int frames = 0; while (fall.MoveNext()) Check(++frames < 100, "Settling must finish promptly.");
            Check(!board.IsColumnSettling && high.anchoredPosition == board.CellToAnchoredPos(new Vector2Int(0,1)), "Settling must finish exactly on-grid.");
            Check(origins.ContainsKey(0) && origins.ContainsKey(1) && origins.ContainsKey(3), "Cascade origin tracking must retain all moved cells.");
        }
        finally { (fall as IDisposable)?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        GameplayFeedbackValidation.Run();
        File.WriteAllText("Logs/RewardFollowup-validation.txt", "PASS: fractional currency chances, accelerated row settling/fixed blockers/pause, rarity backings and gaps, selection-only icon animations, outlined arcing debris, summary presentation and existing gameplay checks.");
    }
}
#endif
