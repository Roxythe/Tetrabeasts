#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameplayFeedbackValidation
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Capture(Canvas canvas, Scene scene, string path) => typeof(CastleCelebrationValidation).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { canvas, scene, path });

    [MenuItem("Tetrabeasts/Validate Gameplay Feedback")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Validate outside Play mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        var run = RunMonsterProgress.RunActive ? RunMonsterProgress.GetSnapshot() : null;
        var random = UnityEngine.Random.state;
        float oldTime = Time.timeScale;
        bool audioPaused = AudioListener.pause;
        var assets = new List<Object>();
        IEnumerator animation = null;
        try
        {
            var line = new MarathonCastleSummaryUI.RewardLine { unitName = "Ram", roleColor = Color.red, beforeXp = 90, afterXp = 154.5f };
            Check(line.Format(90).Contains("90.0/100") && line.Format(100).Contains("0.0/100") && line.Format(154.5f).Contains("54.5/100"), "XP must roll across levels into the next 100 XP gauge.");
            Check(line.Level(100) == 2 && line.Format(154.5f).Contains("Lv. 2"), "Level rollover must reflect temporary run levels.");
            var buffs = new List<RunModifierSO>(); var debuffs = new List<RunModifierSO>();
            for (int tier = 0; tier < 5; tier++)
            {
                var buff = ScriptableObject.CreateInstance<RunModifier>(); var debuff = ScriptableObject.CreateInstance<RunModifier>();
                assets.Add(buff); assets.Add(debuff); buff.rarity = debuff.rarity = (RunModRarity)tier;
                buff.isBuff = true; buff.stat = RunModStat.MonsterDamageMult; debuff.stat = RunModStat.MaxReserveUnitsAdd;
                buffs.Add(buff); debuffs.Add(debuff);
            }
            bool mixed = false;
            for (int roll = 0; roll < 100; roll++)
                foreach (var pair in MarathonRewardUI.RollPairs(buffs.ToArray(), debuffs.ToArray()))
                {
                    int difference = Mathf.Abs((int)MarathonRewardUI.Rarity(pair.buff) - (int)MarathonRewardUI.Rarity(pair.debuff));
                    Check(difference <= 1 && !MarathonRewardUI.Conflicts(pair.buff, pair.debuff), "Rarity pairs must remain adjacent and non-conflicting.");
                    mixed |= difference == 1;
                }
            Check(mixed, "Adjacent rarities must actually be offered.");

            var canvas = new GameObject("Feedback preview", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
            var board = new GameObject("Board", typeof(RectTransform), typeof(Board)).GetComponent<Board>();
            board.transform.SetParent(canvas.transform, false);
            ((RectTransform)board.transform).sizeDelta = new Vector2(480, 720);
            board.width = 8; board.height = 12; Call(board, "Awake"); Set(board, "_gc", null); board.RecomputeCellMetrics();
            var placed = (Dictionary<Vector2Int, RectTransform>)Get(board, "placed");
            var cells = new List<Vector2Int> { new Vector2Int(3, 9), new Vector2Int(3, 5), new Vector2Int(3, 1) };
            Color[] colors = { Color.yellow, Color.magenta, Color.green };
            for (int i = 0; i < cells.Count; i++)
            {
                var tile = board.InstantiateTileUI(colors[i]); tile.anchoredPosition = board.CellToAnchoredPos(cells[i]); placed[cells[i]] = tile;
            }
            var monster = ScriptableObject.CreateInstance<MonsterData>(); assets.Add(monster);
            monster.monsterName = "Feedback Test Unit"; monster.attackPower = 10; monster.maxHealth = 20;
            RunMonsterProgress.RestoreSnapshot(new Dictionary<string, RunMonsterProgress.RunState> { [monster.monsterName] = new RunMonsterProgress.RunState { level = 1, xpInto = 90 } });
            var unit = new Board.MonsterInstance(monster); unit.hp = 7;
            board.SetMonsterAt(cells[0], unit);
            RunMonsterProgress.AddRunXp(monster.monsterName, 500);
            board.RefreshRunLevelStats(); board.TryGetMonster(cells[0], out var leveled);
            Check(leveled.attackPower == MonsterLeveling.GetLeveledStats(monster, 6).attackPower && leveled.hp == 7, "Existing units need new run stats without free healing or a permanent XP commit.");

            var obstacles = (Dictionary<Vector2Int, Board.ObstacleState>)Get(board, "obstacles");
            obstacles[cells[1]] = new Board.ObstacleState(Board.ObstacleType.Stone, 3);
            placed[cells[0]].Find("HealthFill").GetComponent<Image>().color = Color.white;
            Check(!(bool)Call(board, "SpecialWillDestroy", cells[1], SpecialType.Bolt), "A surviving multi-hit stone must not dissolve.");
            Check((bool)Call(board, "SpecialWillDestroy", cells[1], SpecialType.Bomb), "Bomb destruction should include stone rubble.");
            animation = (IEnumerator)Call(board, "AnimateSpecialDestruction", SpecialType.Bolt, new Vector2Int(3, 10), cells, cells, new Func<float>(() => .05f));
            for (int i = 0; i < 8; i++) Check(animation.MoveNext(), "Lightning animation should remain active during cascade.");
            Check(board.IsSpecialAnimating && placed[cells[0]].GetComponent<CanvasGroup>().alpha == 0 && placed[cells[2]].GetComponent<CanvasGroup>().alpha == 1,
                "Lightning must dissolve the highest unit before the lowest one.");
            var emittedPixels = (IList)Get(canvas.GetComponentInChildren<PixelDebrisVfx>(), "particles");
            Check((bool)emittedPixels[0].GetType().GetField("disintegrate").GetValue(emittedPixels[0]), "Lightning must use round disintegration particles.");
            Check(emittedPixels.Cast<object>().All(p => ((Vector2)p.GetType().GetField("velocity").GetValue(p)).y > 0), "Disintegration must drift upward.");
            Color originalBlockTint = (Color)emittedPixels[0].GetType().GetField("tint").GetValue(emittedPixels[0]);
            Check(originalBlockTint.g > .9f && originalBlockTint.b < .2f, "Temporary white tile flashes must not erase the original debris color.");
            Capture(canvas, scene, "Logs/LightningCascade-preview.png");
            ((IDisposable)animation).Dispose(); animation = null;
            Check(!board.GetComponentInChildren<LightningColumnSpark>(), "Cancelling lightning must remove the travelling spark.");
            Check(!board.IsSpecialAnimating && placed[cells[0]].GetComponent<CanvasGroup>().alpha == 1, "Cancellation must restore visual state and gameplay lock.");
            foreach (var fx in canvas.GetComponentsInChildren<PixelDebrisVfx>()) Object.DestroyImmediate(fx.gameObject);

            var debris = PixelDebrisVfx.Create(canvas);
            debris.Burst(Vector3.zero, 40, Color.yellow, Vector2.left, true);
            var particles = (IList)Get(debris, "particles");
            Check(particles.Count >= 5 && particles.Count <= 9, "Bombs need 3–6 directional fragments plus 2–3 arcing fragments per block.");
            foreach (var particle in particles) Check(((Vector2)particle.GetType().GetField("velocity").GetValue(particle)).x < 0, "Left-side bomb debris must initially travel left.");
            int yellowCount = particles.Count;
            Check(particles.Cast<object>().Count(p => ((Vector2)p.GetType().GetField("velocity").GetValue(p)).y > 300f) >= 2,
                "Each bomb burst must include extra upward arcing rubble.");
            debris.Burst(debris.rectTransform.TransformPoint(new Vector3(40, 0)), 40, Color.magenta, Vector2.right, true);
            Check(particles.Count - yellowCount >= 5 && particles.Count - yellowCount <= 9, "Each additional block needs its own directional and arcing burst.");
            for (int i = 0; i < particles.Count; i++)
            {
                Color tint = (Color)particles[i].GetType().GetField("tint").GetValue(particles[i]);
                Check(i < yellowCount ? tint.g > .9f && tint.b < .2f : tint.b > .9f && tint.g < .2f, "Each burst must retain its own block color.");
            }
            int count = particles.Count; Call(debris, "Advance", 0f); Check(particles.Count == count, "Debris must freeze with gameplay.");
            Call(debris, "Advance", .2f); Capture(canvas, scene, "Logs/BombDebris-preview.png");
            using (var mesh = new VertexHelper())
            {
                typeof(PixelDebrisVfx).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null).Invoke(debris, new object[] { mesh });
                Check(mesh.currentVertCount > particles.Count * 4, "Bomb fragments must have irregular multi-faced silhouettes, not square quads.");
                var v = new List<UIVertex>(); mesh.GetUIVertexStream(v);
                Check(v.Any(vertex => vertex.color.r == 0 && vertex.color.g == 0 && vertex.color.b == 0 && vertex.color.a == 255), "Debris needs an opaque black outline.");
            }
            Object.DestroyImmediate(debris.gameObject);

            animation = (IEnumerator)Call(board, "AnimateSpecialDestruction", SpecialType.Death, cells[0], new List<Vector2Int> { cells[0] }, new List<Vector2Int>(), new Func<float>(() => .1f));
            for (int i = 0; i < 5; i++) animation.MoveNext();
            Check(placed[cells[0]].localScale.y < .7f && placed[cells[0]].GetComponent<CanvasGroup>().alpha < .6f, "Death must wither and fade units.");
            Capture(canvas, scene, "Logs/DeathWither-preview.png");
            for (int step = 0; step < 20 && animation.MoveNext(); step++) { }
            Check(!board.IsSpecialAnimating && placed[cells[0]].GetComponent<CanvasGroup>().alpha == 0,
                "Completed destruction must stay hidden until board removal, without a one-frame flash.");
            ((IDisposable)animation).Dispose(); animation = null;
            var destroyedTile = placed[cells[0]];
            placed.Remove(cells[0]); Call(board, "ReleaseBoardTile", destroyedTile);
            var reusedTile = board.InstantiateTileUI(Color.yellow);
            Check(reusedTile == destroyedTile && reusedTile.GetComponent<CanvasGroup>().alpha == 1 && reusedTile.localScale == Vector3.one,
                "A pooled destroyed tile must return fully visible at its original scale.");
            placed[cells[0]] = reusedTile; reusedTile.anchoredPosition = board.CellToAnchoredPos(cells[0]);

            for (int i = 0; i < 3; i++)
            {
                var type = i == 0 ? Board.FloorEffectType.Poison : i == 1 ? Board.FloorEffectType.Burn : Board.FloorEffectType.Lightning;
                board.SetFloorEffect(new Vector2Int(i + 2, 3), type, 1, 1, 10);
            }
            foreach (var floor in board.GetComponentsInChildren<FloorAmbientVfx>()) Set(floor, "clock", .4f);
            Check(board.GetComponentsInChildren<FloorAmbientVfx>().Length >= 3, "Each damage floor type needs an ambient loop even without a border sprite.");
            foreach (var floor in board.GetComponentsInChildren<FloorAmbientVfx>())
                using (var mesh = new VertexHelper())
                {
                    typeof(FloorAmbientVfx).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null).Invoke(floor, new object[] { mesh });
                    var vertices = new List<UIVertex>(); mesh.GetUIVertexStream(vertices);
                    Check(vertices.All(v => floor.rectTransform.rect.Contains(v.position)), "Floor sparks must stay within their own cell.");
                    Check(vertices.Min(v => v.position.y) > floor.rectTransform.rect.yMin, "Floor particles should start above the bottom edge.");
                }
            Capture(canvas, scene, "Logs/FloorEffects-preview.png");
            animation = (IEnumerator)Call(board, "AnimateSpecialDestruction", SpecialType.Bolt, new Vector2Int(7, 10), new List<Vector2Int>(), new List<Vector2Int>(), new Func<float>(() => .1f));
            float furthestSpark = 0;
            for (int step = 0; animation.MoveNext(); step++)
            {
                Check(step < 30, "Empty-column lightning must finish.");
                var spark = board.GetComponentInChildren<LightningColumnSpark>();
                Check(spark, "Lightning must travel through empty cells too.");
                furthestSpark = Mathf.Max(furthestSpark, (float)Get(spark, "progress"));
                if (step == 3) Capture(canvas, scene, "Logs/LightningColumnSpark-preview.png");
            }
            Check(furthestSpark == 1 && !board.GetComponentInChildren<LightningColumnSpark>(), "The spark must reach the column bottom and clean up.");
            ((IDisposable)animation).Dispose(); animation = null;

            var firstSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            assets.Add(firstSprite);
            var image = new GameObject("Growth fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); image.transform.SetParent(canvas.transform, false);
            image.sprite = firstSprite;
            SpriteCrossfade.Swap(image, firstSprite, Color.green, firstSprite);
            var fade = image.GetComponent<SpriteCrossfade>(); Call(fade, "Advance", .2f);
            Check(image.color.a > 0 && image.color.a < 1 && image.transform.childCount == 1, "Growth stages must overlap during a fade.");

            var inactive = new GameObject("Focus test"); inactive.SetActive(false); SceneManager.MoveGameObjectToScene(inactive, scene);
            var game = inactive.AddComponent<GameController>();
            Set(game, "marathonRewardActive", true); Set(game, "_focusPausePending", true);
            Call(game, "TryApplyFocusPause"); Check(!(bool)Get(game, "_focusPausePending"), "A reward panel must discard delayed focus pauses.");
            Set(game, "marathonRewardActive", false); Time.timeScale = 0; Set(game, "_focusPausePending", true);
            Call(game, "TryApplyFocusPause"); Check(!(bool)Get(game, "_focusPausePending"), "Other already-paused screens must discard focus pauses too.");
            game.piece = inactive.AddComponent<Piece>();
            ((IList)Get(game.piece, "cells")).Add(Vector2Int.zero);
            Time.timeScale = 1; inactive.SetActive(true);
            Call(game, "PauseForLostFocus"); Check(game.IsPaused, "Focus loss during an active falling piece must still pause immediately.");
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/GameplayFeedback-validation.txt", "PASS: XP rollover/current stats, adjacent rarity pairs, lightning cascade/survivors/cancellation, outward bomb debris, death wither, floor loops, growth crossfade, and no deferred pause from suspended play.\n");
        }
        finally
        {
            (animation as IDisposable)?.Dispose();
            RunMonsterProgress.RestoreSnapshot(run); UnityEngine.Random.state = random; Time.timeScale = oldTime; AudioListener.pause = audioPaused;
            EditorSceneManager.ClosePreviewScene(scene); foreach (var asset in assets) if (asset) Object.DestroyImmediate(asset);
        }
        CastleCelebrationValidation.Run();
        MarathonRewardPresentationValidation.Run();
    }
}
#endif
