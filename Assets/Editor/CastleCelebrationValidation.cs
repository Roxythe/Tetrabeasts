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

public static class CastleCelebrationValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static object Field(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

    [MenuItem("Tetrabeasts/Validate Castle Summary and Celebration Audio")]
    public static void Run()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode && !PlayerProgress.I, "Run outside Play mode with no live player progress instance.");
        var scene = EditorSceneManager.NewPreviewScene();
        var random = UnityEngine.Random.state;
        var mode = GameModeStore.Current;
        var roster = SelectedMonstersStore.Active;
        var run = RunMonsterProgress.RunActive ? RunMonsterProgress.GetSnapshot() : null;
        bool hadGold = PlayerPrefs.HasKey("CURRENCY_TOTAL");
        int gold = CurrencyStore.Total;
        float time = Time.timeScale;
        var fixtures = new List<MonsterData>();
        try
        {
            var config = GameModeAssets.Load();
            Check(config.commanderLevelUpSfx && config.commanderLevelUpSfx.name == "CommanderLevelUP_SFX", "Commander clip reference.");
            Check(config.curtainPulleySfx && config.curtainPulleySfx.name == "PulleyWheel_Squeak_SFX", "Pulley clip reference.");
            Check(config.squadFireworkSfx && config.squadFireworkPrefab && config.squadFireworkAnimation, "Firework assets must resolve.");
            Check(config.squadFireworkSfx.length <= 1f, "Each cue must use one short explosion, not an overlapping firework sequence.");
            Check(Mathf.Approximately(config.squadFireworkVolume, 1.2f), "Fireworks must use the requested 120 percent voice gain.");
            Check(config.squadFireworkExplosionTimes.All(t => t >= 0 && t < config.squadFireworkAnimation.length), "All audio cues must fall inside the looping clip.");
            var canvas = new GameObject("Celebration validation", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
            Time.timeScale = 0;
            var commander = CommanderLevelUpUI.Create(canvas, config.marathonRewardPrefab.title.font);
            var gauge = new GameObject("Concurrent gauge", typeof(RectTransform), typeof(MarathonGaugeUI)).GetComponent<MarathonGaugeUI>();
            gauge.transform.SetParent(canvas.transform, false);
            gauge.gauge = gauge.gameObject.AddComponent<Slider>();
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(gauge.transform, false); fill.color = Color.magenta; gauge.gauge.fillRect = fill.rectTransform;
            int oldSeed = commander.lights.seed;
            var play = commander.Play(gauge); Check(play.MoveNext(), "Commander celebration must start while paused.");
            Check(fill.color != Color.magenta && commander.gameObject.activeSelf, "Gauge and commander announcement must begin in the same frame.");
            int seed = commander.lights.seed;
            Check(seed != oldSeed, "Every Commander play must change the seed.");
            ((IDisposable)play).Dispose();
            Check(fill.color == Color.magenta, "Stopping the announcement must restore the gauge tint.");
            gauge.gameObject.SetActive(false);
            play = commander.Play(); play.MoveNext();
            Check(seed != commander.lights.seed, "Replaying must select another seed.");
            Check(JsonUtility.ToJson(random) == JsonUtility.ToJson(UnityEngine.Random.state), "Cosmetic seeds must not consume gameplay RNG.");
            ((IDisposable)play).Dispose();

            var curtain = MarathonCurtainTransition.Create(canvas, config.marathonCurtainPrefab, config.marathonCurtainOpening);
            var pulley = curtain.GetComponent<AudioSource>();
            Check(pulley && pulley.loop && pulley.ignoreListenerPause && !pulley.playOnAwake, "Pulley must loop on a pause-independent, explicitly controlled voice.");
            var close = curtain.Close(); close.MoveNext();
            var move = (IEnumerator)close.Current;
            curtain.AnimationPaused = true;
            move.MoveNext();
            var artwork = (GameObject)Field(curtain, "artwork");
            var sprite = artwork.GetComponentInChildren<Image>().sprite;
            move.MoveNext(); move.MoveNext();
            Check(sprite == artwork.GetComponentInChildren<Image>().sprite && !pulley.isPlaying, "Paused curtain must hold its visual and audio.");
            ((IDisposable)move).Dispose();
            ((IDisposable)close).Dispose();
            curtain.gameObject.SetActive(false);

            var effects = new List<SquadLevelUpFireworks>();
            foreach (var targetMode in new[] { RunGameMode.Standard, RunGameMode.Chaos, RunGameMode.Marathon })
            {
                GameModeStore.Current = targetMode;
                var row = new GameObject("XP row", typeof(RectTransform), typeof(XpMonsterRowUI)).GetComponent<XpMonsterRowUI>();
                row.transform.SetParent(canvas.transform, false);
                row.portraitImage = new GameObject("Portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                row.portraitImage.transform.SetParent(row.transform, false);
                row.BeginLevelUpSequence();
                var effect = (SquadLevelUpFireworks)Field(row, "_fireworks");
                Check((bool)effect == (targetMode != RunGameMode.Marathon), "Portrait fireworks should start only in Standard/Chaos.");
                if (effect)
                {
                    effects.Add(effect);
                    Check(effect.transform.GetSiblingIndex() < row.portraitImage.transform.GetSiblingIndex(), "Fireworks must draw behind the portrait.");
                    var effectRect = (RectTransform)effect.transform;
                    Check(Mathf.Approximately(effectRect.rect.width, row.portraitImage.rectTransform.rect.width * 1.9f), "Cropping must preserve firework width.");
                    Check(effectRect.anchoredPosition.y > row.portraitImage.rectTransform.anchoredPosition.y && effect.GetComponent<RectMask2D>(), "The visible window must crop the launch trails and sit higher.");
                    Check(Mathf.Approximately(effect.GetComponent<AudioSource>().volume,
                        AudioManager.I ? AudioManager.I.masterVolume * AudioManager.I.sfxVolume : .5f), "Source volume must respect settings; voice gain supplies the boost without source clamping.");
                    Call(effect, "Advance", .5f);
                    Check((int)Field(effect, "nextCue") == 0, "No explosion sound during initial rocket ascent.");
                    Call(effect, "Advance", .3f);
                    Check((int)Field(effect, "nextCue") == 1, "First sound must follow the first explosion cue.");
                    float elapsedBefore = (float)Field(effect, "elapsed");
                    effect.Begin();
                    row.BeginLevelUpSequence();
                    row.AppendLevelUpStatLine("+1 ATK");
                    row.AppendLevelUpStatLine("+1 HP");
                    Check((float)Field(effect, "elapsed") == elapsedBefore && (int)Field(effect, "nextCue") == 1,
                        "Repeated level-ups must not rewind visuals or replay their audio cues.");
                    Call(effect, "Advance", config.squadFireworkAnimation.length);
                    Check((int)Field(effect, "nextCue") == 1, "The cue sequence must restart each loop.");
                    row.HideLevelUps();
                    Check(!effect.gameObject.activeSelf, "Hiding level-up effects must stop fireworks.");
                    Set(row, "_fireworks", null);
                }
                Object.DestroyImmediate(row.gameObject);
            }

            var inactive = new GameObject("Reward calculation fixture"); inactive.SetActive(false);
            SceneManager.MoveGameObjectToScene(inactive, scene);
            var game = inactive.AddComponent<GameController>();
            GameModeStore.Current = RunGameMode.Marathon;
            foreach (string name in new[] { "Test squad member", "Max-level squad member" })
            {
                var monster = ScriptableObject.CreateInstance<MonsterData>(); monster.monsterName = name; fixtures.Add(monster);
            }
            SelectedMonstersStore.Active = fixtures;
            RunMonsterProgress.RestoreSnapshot(new Dictionary<string, RunMonsterProgress.RunState>
            {
                [fixtures[0].monsterName] = new RunMonsterProgress.RunState { level = 1, xpInto = 90 },
                [fixtures[1].monsterName] = new RunMonsterProgress.RunState { level = 100, xpInto = 0 }
            });
            Set(game, "_starDifficultyModifiers", new StarDifficultyModifiers(1));
            Set(game, "unitLives", game.MaxReserveUnits - 1);
            CurrencyStore.Total = 100;
            var lines = (List<MarathonCastleSummaryUI.RewardLine>)Call(game, "GrantMarathonCastleRewards");
            Check(lines.Count == 4 && lines[0].text.Contains("105"), "Gold and one XP line per squad member plus reserves.");
            Check(lines[2].Format(lines[2].afterXp).Contains("+0</color>") && lines[2].Format(lines[2].afterXp).Contains("0.0/100"), "A capped unit must show actual zero XP credited and its total.");
            Check(game.CurrentReserveUnits == game.MaxReserveUnits && lines[3].text.Contains("+1"), "Reinforcements must report actual capacity-limited gains.");
            Check(RunMonsterProgress.GetCurrentLevel(fixtures[0].monsterName) > 1, "Castle clear must actually grant unit XP.");

            var summary = MarathonCastleSummaryUI.Create(canvas, config.marathonRewardPrefab.title.font);
            var show = summary.Show(1, lines); show.MoveNext();
            Check(summary.GetComponent<Canvas>().sortingOrder > curtain.GetComponent<Canvas>().sortingOrder, "Summary must draw above the curtain.");
            var rows = (List<CanvasGroup>)Field(summary, "rows");
            Check(rows.Count == lines.Count && rows.All(r => r.alpha == 0), "Reward lines must begin hidden for their shared fade.");
            Call(summary, "AdvancePresentation", 1.5f);
            Check(rows.All(r => r.alpha == 1), "Every squad line must appear together.");
            Call(summary, "AdvancePresentation", 6f);
            var badges = (List<TMP_Text>)Field(summary, "levelUps");
            Check(badges[1].transform.parent.gameObject.activeSelf && !badges[2].transform.parent.gameObject.activeSelf, "Only units that gained levels should celebrate.");
            Color badgeColor = badges[1].color;
            Call(summary, "AdvancePresentation", .15f);
            Check(badges[1].color != badgeColor && badges[1].transform.parent.gameObject.activeSelf, "Level-up flashing must keep looping after XP stops counting.");
            Capture(canvas, scene);
            ((IDisposable)show).Dispose();
            var fade = summary.FadeOut(1); fade.MoveNext();
            Check(summary.gameObject.activeSelf && summary.GetComponent<CanvasGroup>().alpha == 1, "Summary fade must start visible alongside opening.");
            ((IDisposable)fade).Dispose();
            summary.gameObject.SetActive(false);
            var monsters = AssetDatabase.FindAssets("t:MonsterData").Select(g => AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m && m.portrait).Take(4).ToArray();
            var scroll = new GameObject("XP scroll preview", typeof(RectTransform), typeof(ScrollRect)).GetComponent<ScrollRect>();
            scroll.transform.SetParent(canvas.transform, false);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
            viewport.SetParent(scroll.transform, false); viewport.sizeDelta = new Vector2(1524, 857);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewport;
            for (int i = 0; i < monsters.Length; i++)
            {
                var portrait = new GameObject("Fireworks preview portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                portrait.transform.SetParent(viewport, false);
                portrait.sprite = monsters[i].portrait;
                portrait.rectTransform.sizeDelta = new Vector2(200, 200);
                portrait.rectTransform.anchoredPosition = new Vector2(-500, 328.5f - 200 * i);
                var fireworks = SquadLevelUpFireworks.Create(portrait);
                fireworks.Begin(); Call(fireworks, "Advance", new[] { 2.83f, .45f, 7.8f, 8.6f }[i]);
            }
            Check(viewport.GetComponent<SquadFireworkMaskHeadroom>(), "The list's mask must have extra headroom for the first row.");
            using (var mesh = new VertexHelper())
            {
                mesh.AddVert(new Vector3(-762, -428.5f), Color.white, Vector2.zero);
                mesh.AddVert(new Vector3(762, 428.5f), Color.white, Vector2.one);
                viewport.GetComponent<SquadFireworkMaskHeadroom>().ModifyMesh(mesh);
                var vertex = new UIVertex(); mesh.PopulateUIVertex(ref vertex, 1);
                Check(vertex.position.y == 528.5f && vertex.position.x == 762, "Raise the stencil top without changing its width.");
                Check(viewport.rect.height == 857, "Headroom must not change scrolling or row layout.");
            }
            Capture(canvas, scene, "Logs/SquadFireworks-preview.png");
            scroll.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                var popup = Object.Instantiate(config.marathonRewardPrefab.previewPanels[i], canvas.transform, false);
                popup.gameObject.SetActive(true);
                popup.localScale = Vector3.one;
                popup.anchoredPosition = new Vector2((i - 1) * 352f, 0);
                var text = popup.GetComponentsInChildren<TMP_Text>(true).First(t => t.GetComponent<TMPLabelShadow>());
                text.text = "Monster attack power\n1 -> <color=#" + ColorUtility.ToHtmlStringRGB(config.marathonRewardPrefab.previewBuffColor) + ">1.1</color>\nMaximum reserves\n22 -> <color=#" + ColorUtility.ToHtmlStringRGB(config.marathonRewardPrefab.previewDebuffColor) + ">16</color>";
                text.GetComponent<TMPLabelShadow>().Sync();
                Check(popup.GetComponent<Image>().sprite && text.color == Color.white && (text.fontStyle & FontStyles.Bold) != 0,
                    "Marathon previews must use the wooden backdrop and bold white text.");
            }
            Capture(canvas, scene, "Logs/MarathonRewardPreview-style.png");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/CastleCelebration-validation.txt", "PASS: audio assets, independent cosmetic seeds, paused curtain hold, Standard/Chaos portrait layering and looping cues, cleanup, actual castle awards/caps, sequential summary and sorting.\n");
        }
        finally
        {
            Time.timeScale = time; UnityEngine.Random.state = random; GameModeStore.Current = mode;
            SelectedMonstersStore.Active = roster; RunMonsterProgress.RestoreSnapshot(run);
            if (hadGold) CurrencyStore.Total = gold; else PlayerPrefs.DeleteKey("CURRENCY_TOTAL");
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var fixture in fixtures) Object.DestroyImmediate(fixture);
        }
    }

    static void Capture(Canvas canvas, Scene scene, string path = "Logs/MarathonCastleSummary-preview.png")
    {
        var camera = new GameObject("Summary preview camera", typeof(Camera)).GetComponent<Camera>();
        SceneManager.MoveGameObjectToScene(camera.gameObject, scene); camera.scene = scene;
        camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .06f, .1f);
        var texture = new RenderTexture(960, 540, 24); camera.targetTexture = texture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        var previous = RenderTexture.active; Texture2D capture = null;
        try
        {
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = texture;
            capture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); capture.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes(path, capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            texture.Release(); Object.DestroyImmediate(texture); if (capture) Object.DestroyImmediate(capture);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
#endif
