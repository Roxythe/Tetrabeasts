#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MarathonCelebrationValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static object Field(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
    [MenuItem("Tetrabeasts/Validate Marathon Celebration and Damage Text")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run outside Play mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        var previousMode = GameModeStore.Current;
        var random = UnityEngine.Random.state;
        float previousTimeScale = Time.timeScale;
        try
        {
            var config = GameModeAssets.Load();
            Check(config.marathonCastleStartingHealth == 1000 && GameController.MarathonCastleHealth(0) == 1000, "Starting HP must be 1000.");
            Check(GameController.MarathonCastleHealth(1) == 1350, "Castle HP must continue growing.");
            var inactive = new GameObject("Inactive health check"); inactive.SetActive(false);
            SceneManager.MoveGameObjectToScene(inactive, scene);
            var game = inactive.AddComponent<GameController>();
            typeof(GameController).GetField("_starDifficultyModifiers", Private).SetValue(game, new StarDifficultyModifiers(3));
            GameModeStore.Current = RunGameMode.Marathon;
            game.enemyCastleHpMult = 1;
            Check(game.EnemyCastleHealthMultiplier == 1, "Three-star campaign scaling must not inflate Marathon HP.");
            game.enemyCastleHpMult = 1.2f;
            Check(Mathf.Approximately(game.EnemyCastleHealthMultiplier, 1.2f), "Marathon HP reward modifiers must still apply.");
            GameModeStore.Current = RunGameMode.Standard;
            Check(Mathf.Approximately(game.EnemyCastleHealthMultiplier, 3f), "Standard difficulty must remain unchanged.");

            var canvas = new GameObject("Preview canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
            var celebration = CommanderLevelUpUI.Create(canvas, config.marathonRewardPrefab.title.font);
            celebration.gameObject.SetActive(true);
            var present = typeof(CommanderLevelUpUI).GetMethod("Present", Private);
            var label = celebration.GetComponentInChildren<TMP_Text>();
            Time.timeScale = 0f;
            present.Invoke(celebration, new object[] { 0f });
            Check(label.rectTransform.localScale.x < .02f, "Announcement must start very small.");
            present.Invoke(celebration, new object[] { CommanderLevelUpUI.GrowSeconds });
            Check(Mathf.Approximately(label.rectTransform.localScale.x, 1), "Announcement must grow to full size.");
            var color = label.color;
            present.Invoke(celebration, new object[] { CommanderLevelUpUI.GrowSeconds + .125f });
            Check(label.color != color && Mathf.Approximately(label.rectTransform.localScale.x, 1), "Full-size announcement must animate its colors.");
            Check(CommanderLevelUpUI.HoldSeconds >= 2, "Full-size VFX must hold for two seconds.");
            Check(label.text == "Commander Level Up", "Announcement wording.");
            CaptureAnnouncement(canvas, scene);

            var popupRoot = new GameObject("Popup preview", typeof(RectTransform)); popupRoot.transform.SetParent(canvas.transform, false);
            var damage = popupRoot.AddComponent<FloatingDamageText>(); damage.SetFallbackRoot((RectTransform)popupRoot.transform);
            var first = new GameObject("Unit A", typeof(RectTransform)).GetComponent<RectTransform>(); first.SetParent(canvas.transform, false);
            var second = new GameObject("Unit B", typeof(RectTransform)).GetComponent<RectTransform>(); second.SetParent(canvas.transform, false);
            var active = (IList)Field(damage, "activePopups");
            void Hit(RectTransform unit, int amount, Board.DamageSource source, FloatingDamageText.DamageKind kind = FloatingDamageText.DamageKind.Poison, bool lethal = false)
                => damage.ShowUnitDamage(unit, amount, kind, lethal, source);
            Hit(first, 5, Board.DamageSource.FloorPoison);
            Hit(first, 8, Board.DamageSource.FloorPoison);
            Check(active.Count == 1, "Repeated poison must replace its previous popup.");
            var popup = active[0];
            var popupText = (TMP_Text)popup.GetType().GetField("text").GetValue(popup);
            Check(popupText.text == "-8", "Replacement must show the new tick, not an accumulated value.");
            first.anchoredPosition = Vector2.right * 80;
            Hit(first, 9, Board.DamageSource.FloorPoison);
            Check(active.Count == 1, "Moving a unit must retain its identity.");
            second.anchoredPosition = first.anchoredPosition;
            Hit(second, 4, Board.DamageSource.FloorPoison);
            Hit(first, 3, Board.DamageSource.FloorBurn, FloatingDamageText.DamageKind.Fire);
            Hit(first, 6, Board.DamageSource.VolcanicEruption, FloatingDamageText.DamageKind.Fire);
            Check(active.Count == 4, "Nearby units and different sources sharing a color must remain independent.");
            Hit(first, 12, Board.DamageSource.FloorPoison, lethal: true);
            Check(active.Count == 4, "Lethal poison must replace only its own source.");
            first.gameObject.SetActive(false);
            // Preview scenes do not dispatch Play-mode MonoBehaviour callbacks.
            typeof(DamageTextUnitIdentity).GetMethod("OnDisable", Private).Invoke(first.GetComponent<DamageTextUnitIdentity>(), null);
            first.gameObject.SetActive(true);
            Hit(first, 7, Board.DamageSource.FloorPoison);
            Check(active.Count == 5, "A reused tile must represent a new unit.");
            popupRoot.SetActive(false);
            typeof(FloatingDamageText).GetMethod("OnDisable", Private).Invoke(damage, null);
            Check(active.Count == 0, "Disabling damage text must clean up all active popups.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/MarathonCelebration-validation.txt", "PASS: 1000 starting HP; Marathon star isolation and reward modifiers; campaign scaling preserved; centered growth and color animation; two-second hold; source-specific replacement, movement, adjacent units, same-color distinct sources, lethal ticks, tile reuse and cleanup.\n");
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            GameModeStore.Current = previousMode;
            UnityEngine.Random.state = random;
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void CaptureAnnouncement(Canvas canvas, Scene scene)
    {
        var camera = new GameObject("Announcement preview camera", typeof(Camera)).GetComponent<Camera>();
        SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        camera.scene = scene;
        camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.12f, .12f, .2f);
        var texture = new RenderTexture(960, 540, 24);
        camera.targetTexture = texture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        var previous = RenderTexture.active;
        Texture2D capture = null;
        try
        {
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = texture;
            capture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); capture.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/CommanderLevelUp-preview.png", capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            texture.Release(); Object.DestroyImmediate(texture);
            if (capture) Object.DestroyImmediate(capture);
        }
    }
}
#endif
