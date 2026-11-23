#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MarathonVisualRevisionAuthoring
{
    public const string PrefabPath = "Assets/Prefabs/GameModes/CommanderLevelUp.prefab";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    [MenuItem("Tetrabeasts/Build Commander Level Up Prefab")]
    public static void Build()
    {
        var config = GameModeAssets.Load();
        var saved = AssetDatabase.LoadAssetAtPath<CommanderLevelUpUI>(PrefabPath);
        if (!saved)
        {
            var root = new GameObject("CommanderLevelUp", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
                var canvas = root.GetComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 32700;
                var view = root.AddComponent<CommanderLevelUpUI>();
                view.group = root.GetComponent<CanvasGroup>();
                var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                dim.transform.SetParent(root.transform, false);
                dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.sizeDelta = Vector2.zero;
                dim.color = new Color(0, 0, 0, .35f);
                view.lights = new GameObject("Colorful Light Cones", typeof(RectTransform), typeof(CanvasRenderer), typeof(RadialLightBurst)).GetComponent<RadialLightBurst>();
                view.lights.transform.SetParent(root.transform, false);
                view.lights.rectTransform.sizeDelta = new Vector2(1400, 1400);
                view.lights.raycastTarget = false;
                view.label = new GameObject("Commander Level Up Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                view.label.transform.SetParent(root.transform, false);
                view.label.rectTransform.anchorMin = new Vector2(.05f, .5f);
                view.label.rectTransform.anchorMax = new Vector2(.95f, .5f);
                view.label.rectTransform.sizeDelta = new Vector2(0, 190);
                view.label.font = config.marathonRewardPrefab.title.font;
                view.label.text = "Commander Level Up";
                view.label.fontSize = view.label.fontSizeMax = 100;
                view.label.fontSizeMin = 28; view.label.enableAutoSizing = true;
                view.label.alignment = TextAlignmentOptions.Center;
                view.label.fontStyle = FontStyles.Bold;
                view.label.textWrappingMode = TextWrappingModes.NoWrap;
                view.label.raycastTarget = false;
                const string materialPath = "Assets/Prefabs/GameModes/CommanderLevelUpText.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material = new Material(view.label.font.material);
                    material.SetColor("_OutlineColor", Color.black); material.SetFloat("_OutlineWidth", .25f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                view.label.fontSharedMaterial = material;
                typeof(CommanderLevelUpUI).GetMethod("Present", Private).Invoke(view, new object[] { view.growSeconds });
                saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<CommanderLevelUpUI>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        config.commanderLevelUpPrefab = saved;
        EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
    }

    [MenuItem("Tetrabeasts/Validate Marathon Visual Revision")]
    public static void Validate()
    {
        Build();
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var config = GameModeAssets.Load();
            var list = Regex.Match(File.ReadAllText("Assets/Scenes/GameplayScene.unity"), @"  castlesByLevel:\r?\n((?:  - .*\r?\n)+)").Groups[1].Value;
            var castles = Regex.Matches(list, @"guid: (\w+)").Cast<Match>().Select(m => AssetDatabase.LoadAssetAtPath<CastleData>(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))).ToArray();
            var expected = AssetDatabase.LoadAssetAtPath<CastleData>("Assets/ScriptableObjects/Castle/TestCastle 0.asset");
            Check(castles.Length == 14 && castles[0] == expected && expected.levelBackgroundVideoClip, "First level must use the normal village and its animated background.");
            var inactive = new GameObject("Inactive castle check"); inactive.SetActive(false); SceneManager.MoveGameObjectToScene(inactive, scene);
            var game = inactive.AddComponent<GameController>(); game.castlesByLevel = castles;
            Check((CastleData)typeof(GameController).GetMethod("ResolveMarathonCastle", Private).Invoke(game, new object[] { 0 }) == expected, "Marathon must resolve the shared starting castle.");
            var castle = inactive.AddComponent<EnemyCastleUI>();
            castle.castleImage = new GameObject("Castle image", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); castle.castleImage.transform.SetParent(inactive.transform, false);
            castle.InitCastle(expected, 1, 1f, false, false, 1000);
            Check(castle.castleImage.sprite == expected.GetSpriteForHealth(1f), "Castle must show the correct undamaged sprite.");

            var canvas = new GameObject("Revision preview", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            var gauge = Object.Instantiate(config.marathonGaugePrefab, canvas.transform);
            gauge.Refresh(1, 0, 500);
            var motion = gauge.gauge.GetComponent<GaugeLiquidMotion>();
            typeof(GaugeLiquidMotion).GetMethod("OnEnable", Private).Invoke(motion, null);
            gauge.Refresh(1, 250, 500);
            Check(gauge.gauge.normalizedValue == 0, "XP target changes must not snap the fill.");
            var advance = typeof(GaugeLiquidMotion).GetMethod("AdvanceVisual", Private);
            advance.Invoke(motion, new object[] { .11f });
            float rising = gauge.gauge.normalizedValue;
            Check(rising > 0 && rising < .5f, "XP fill must interpolate upward.");
            for (int i = 0; i < 30; i++) { gauge.Refresh(1, 250, 500); advance.Invoke(motion, new object[] { .1f }); }
            Check(Mathf.Abs(gauge.gauge.normalizedValue - .5f) < .001f, "Repeated refresh must preserve smoothing.");
            gauge.Refresh(2, 0, 590); advance.Invoke(motion, new object[] { .11f });
            Check(gauge.gauge.normalizedValue > 0 && gauge.gauge.normalizedValue < .5f, "XP fill must interpolate downward.");
            var fill = gauge.gauge.fillRect.GetComponent<Image>();
            Check(fill.color.b > fill.color.g && fill.material.GetFloat("_FoamIntensity") == 0, "XP liquid must stay purple without white foam.");
            gauge.gameObject.SetActive(false);

            var view = Object.Instantiate(config.commanderLevelUpPrefab, canvas.transform);
            view.loopWhenIdle = false;
            var present = typeof(CommanderLevelUpUI).GetMethod("Present", Private);
            present.Invoke(view, new object[] { .9f });
            Check(view.lights.coneAngle > 360f / view.lights.coneCount, "Cones must overlap.");
            using (var mesh = new VertexHelper())
            {
                typeof(RadialLightBurst).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null).Invoke(view.lights, new object[] { mesh });
                var vertex = default(UIVertex);
                float alphaMin = 1, alphaMax = 0;
                for (int i = 0; i < mesh.currentVertCount; i++) { mesh.PopulateUIVertex(ref vertex, i); alphaMin = Mathf.Min(alphaMin, vertex.color.a / 255f); alphaMax = Mathf.Max(alphaMax, vertex.color.a / 255f); }
                Check(alphaMin == 0 && alphaMax > .1f && alphaMax < 1, "Cones must fade to transparent edges and remain translucent.");
                var populate = typeof(RadialLightBurst).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null);
                float shortest = float.MaxValue, longest = 0;
                bool independentShot = false;
                for (int frame = 0; frame < 100; frame++)
                {
                    view.lights.SetAnimationTime(frame * .03f); populate.Invoke(view.lights, new object[] { mesh });
                    mesh.PopulateUIVertex(ref vertex, 18 * 17 + 8);
                    shortest = Mathf.Min(shortest, vertex.position.magnitude);
                    longest = Mathf.Max(longest, vertex.position.magnitude);
                    mesh.PopulateUIVertex(ref vertex, 5 * 17 + 8); float firstAlpha = vertex.color.a / 255f;
                    mesh.PopulateUIVertex(ref vertex, 19 * 17 + 5 * 17 + 8); float secondAlpha = vertex.color.a / 255f;
                    independentShot |= firstAlpha > .2f && secondAlpha == 0;
                }
                Check(longest > shortest * 3f, "Light cones must shoot outward from near the center.");
                Check(independentShot, "Neighboring lights must fire and go dark independently.");
            }
            present.Invoke(view, new object[] { .9f });
            Capture(canvas, scene);
            File.WriteAllText("Logs/MarathonVisualRevision-validation.txt", "PASS: shared normal village sprite and video assignment; smooth XP rise/fall; purple liquid without white foam; authored celebration prefab; overlapping translucent cones; independent shots and outward travel.\n");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void Capture(Canvas canvas, Scene scene)
    {
        var camera = new GameObject("Preview camera", typeof(Camera)).GetComponent<Camera>(); SceneManager.MoveGameObjectToScene(camera.gameObject, scene); camera.scene = scene;
        camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .12f, .2f);
        var texture = new RenderTexture(960, 540, 24); camera.targetTexture = texture;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        var previous = RenderTexture.active; Texture2D capture = null;
        try
        {
            capture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var view = canvas.GetComponentInChildren<CommanderLevelUpUI>();
            for (int frame = 0; frame < 3; frame++)
            {
                view.lights.SetAnimationTime(.9f + frame * .2f);
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = texture;
                capture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); capture.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/CommanderLevelUp-cones-" + frame + ".png", capture.EncodeToPNG());
            }
        }
        finally { RenderTexture.active = previous; camera.targetTexture = null; texture.Release(); Object.DestroyImmediate(texture); if (capture) Object.DestroyImmediate(capture); }
    }
}
#endif
