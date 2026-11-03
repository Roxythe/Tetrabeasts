#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RowAttackOutlineTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private | BindingFlags.DeclaredOnly).Invoke(target, args);

    [Test] public void PaletteUsesEveryColorWithoutRepeatsOrChangingGameplayRandomness()
    {
        var go = new GameObject("Palette test", typeof(RectTransform)); go.SetActive(false);
        var previousRandom = Random.state;
        try
        {
            var board = go.AddComponent<Board>();
            var expected = new HashSet<Color>(board.rowAttackOutlineExtraColors) { board.rowAttackOutlineColor };
            Color previous = default;
            for (int cycle = 0; cycle < 4; cycle++)
            {
                var seen = new HashSet<Color>();
                for (int i = 0; i < expected.Count; i++)
                {
                    var shade = (Color)Call(board, "PickRowAttackOutlineColor");
                    Assert.AreNotEqual(previous, shade);
                    Assert.IsTrue(seen.Add(shade));
                    previous = shade;
                }
                CollectionAssert.AreEquivalent(expected, seen);
            }
            Assert.AreEqual(JsonUtility.ToJson(previousRandom), JsonUtility.ToJson(Random.state));
            board.varyRowAttackOutlineColors = false;
            Assert.AreEqual(board.rowAttackOutlineColor, Call(board, "PickRowAttackOutlineColor"));
        }
        finally { Random.state = previousRandom; Object.DestroyImmediate(go); }
    }

    [Test]
    public void GoldPerimeterSpreadsFromNewCellsAndLeavesTheInteriorOpen()
    {
        var go = new GameObject("Outline mesh test", typeof(RectTransform));
        try
        {
            var effect = go.AddComponent<RowAttackOutlineVFX>();
            effect.Initialize(new Vector2(400, 40), 10, new[] { 4, 5 }, .16f, .09f, .2f, 3, Color.yellow);
            Assert.IsFalse(effect.raycastTarget);
            Set(effect, "elapsed", .04f);
            using (var mesh = new VertexHelper())
            {
                Call(effect, "OnPopulateMesh", mesh);
                var vertices = new List<UIVertex>(); mesh.GetUIVertexStream(vertices);
                Assert.IsNotEmpty(vertices);
                foreach (var vertex in vertices) Assert.Less(Mathf.Abs(vertex.position.x), 100f, "The sweep begins around the completing cells.");
            }
            Set(effect, "elapsed", .17f);
            using (var mesh = new VertexHelper())
            {
                Call(effect, "OnPopulateMesh", mesh);
                var vertices = new List<UIVertex>(); mesh.GetUIVertexStream(vertices);
                bool left = false, right = false, top = false, bottom = false;
                foreach (var vertex in vertices)
                {
                    var p = vertex.position;
                    Assert.IsTrue(Mathf.Abs(p.x) >= 190f || Mathf.Abs(p.y) >= 12f, "No fill or internal vertical dividers.");
                    if (p.x <= -199) left = true;
                    if (p.x >= 199) right = true;
                    if (p.y >= 19) top = true;
                    if (p.y <= -19) bottom = true;
                }
                Assert.IsTrue(left && right && top && bottom, "The sweep closes a full row rectangle.");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    [UnityTest]
    public IEnumerator RowClearWaitsForOutlinePreservesDamageAndCleansUp()
    {
        yield return new EnterPlayMode();
        yield return RunClearChecks();
        yield return new ExitPlayMode();
    }

    IEnumerator RunClearChecks()
    {
        var root = new GameObject("Row outline test", typeof(RectTransform), typeof(Canvas));
        var boardObject = new GameObject("Board", typeof(RectTransform));
        boardObject.transform.SetParent(root.transform, false);
        ((RectTransform)boardObject.transform).sizeDelta = new Vector2(400, 640);
        var board = boardObject.AddComponent<Board>();
        board.width = 10; board.height = 16;
        board.animateTetrominoBackgrounds = false;
        board.RecomputeCellMetrics();
        var monster = ScriptableObject.CreateInstance<MonsterData>();
        var placed = (Dictionary<Vector2Int, RectTransform>)typeof(Board).GetField("placed", Private).GetValue(board);
        var monsters = (Dictionary<Vector2Int, Board.MonsterInstance>)typeof(Board).GetField("monsters", Private).GetValue(board);
        void Place(int x, int y, bool alive = true)
        {
            var tile = new GameObject("Monster", typeof(RectTransform)).GetComponent<RectTransform>();
            tile.SetParent(board.gridRoot, false);
            tile.anchoredPosition = board.CellToAnchoredPos(new Vector2Int(x, y));
            var cell = new Vector2Int(x, y);
            placed[cell] = tile;
            monsters[cell] = new Board.MonsterInstance { data = monster, hp = alive ? 10 : 0, maxHp = 10, attackPower = 2, specialGaugeGain = 1 };
        }
        try
        {
            yield return null;
            for (int x = 0; x < 10; x++) Place(x, 0);
            // A second staggered row will become a cascade after the first clears.
            for (int x = 0; x < 10; x++) Place(x, 2 + x % 2);
            int completedRows = -1, damage = -1;
            board.ClearFullLinesAnimated((rows, cells, dmg, special, rowDamage, dominant) => { completedRows = rows; damage = dmg; },
                0f, new Dictionary<int, List<int>> { [0] = new List<int> { 1, 2 } });
            Assert.AreEqual(20, placed.Count, "Tiles remain until the outline has spread.");
            Assert.AreEqual(1, root.GetComponentsInChildren<RowAttackOutlineVFX>().Length);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(-1, completedRows);
            Assert.AreEqual(20, placed.Count);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (completedRows < 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(2, completedRows);
            Assert.AreEqual(40, damage, "The effect must not change row attack damage.");
            Assert.IsEmpty(placed);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsEmpty(root.GetComponentsInChildren<RowAttackOutlineVFX>());

            for (int x = 0; x < 10; x++) Place(x, 0, false);
            board.ClearFullLinesAnimated(null, 0f);
            Assert.IsEmpty(root.GetComponentsInChildren<RowAttackOutlineVFX>(), "Rows with no attack damage have no attack outline.");
            yield return null;
            yield return null;
            for (int x = 0; x < 10; x++) Place(x, 0);
            board.ClearFullLinesAnimated(null, 0f);
            Assert.IsNotEmpty(root.GetComponentsInChildren<RowAttackOutlineVFX>());
            board.ClearRoundTransientEffects();
            Assert.IsEmpty(root.GetComponentsInChildren<RowAttackOutlineVFX>(), "Level reset immediately hides unfinished outlines.");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(monster); }
    }

    [UnityTest]
    public IEnumerator RenderGoldOutlineStagesWithGameArtwork()
    {
        yield return new EnterPlayMode();
        var root = new GameObject("Outline render canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var cameraObject = new GameObject("Outline render camera", typeof(Camera));
        var target = new RenderTexture(1920, 1080, 24);
        var camera = cameraObject.GetComponent<Camera>();
        camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .04f, .065f);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        try
        {
            var boardObject = new GameObject("Board", typeof(RectTransform), typeof(Image));
            boardObject.transform.SetParent(root.transform, false);
            ((RectTransform)boardObject.transform).sizeDelta = new Vector2(600, 960);
            boardObject.GetComponent<Image>().color = new Color(.08f, .065f, .09f);
            var board = boardObject.AddComponent<Board>();
            board.width = 10; board.height = 16; board.verticalBufferCells = 0;
            board.tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tile_Prefab.prefab").GetComponent<Image>();
            board.RecomputeCellMetrics();
            var monsterIds = AssetDatabase.FindAssets("t:MonsterData");
            yield return null;
            foreach (int row in new[] { 1, 3, 5, 7, 9, 11, 13 })
            {
                for (int x = 0; x < 10; x++)
                {
                    var data = AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(monsterIds[x % monsterIds.Length]));
                    var tile = board.InstantiateTileUI(new Color(.3f, .12f, .4f), data.portrait);
                    board.Place(new Vector2Int(x, row), tile);
                }
                var effect = (RowAttackOutlineVFX)Call(board, "PlayRowAttackOutline", row,
                    new Dictionary<int, List<int>> { [row] = new List<int> { 4, 5 } });
                Set(effect, "elapsed", .26f);
                effect.SetAllDirty();
            }
            GameplayPlaylistTests.Capture(camera, "Row-Attack-Palette");
            // The same local effect follows flipped boards without camera-space offsets.
            boardObject.transform.localRotation = Quaternion.Euler(0, 0, 180);
            GameplayPlaylistTests.Capture(camera, "Row-Attack-Palette-Flipped");
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(target); }
        yield return new ExitPlayMode();
    }
}
#endif
