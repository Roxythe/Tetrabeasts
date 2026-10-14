#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Explicit one-time authoring command. Never rebuilds or overwrites edited prefabs on import.
public static class GameModeAssetBuilder
{
    const string Folder = "Assets/Prefabs/GameModes";
    static TMP_FontAsset font;
    static Image buttonStyle, closeStyle;
    [MenuItem("Tetrabeasts/Create Missing Game Mode Assets")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        font = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(f => f.name.Contains("Jersey10-Regular")) ?? TMP_Settings.defaultFontAsset;
        var titleScene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Additive);
        var buttons = titleScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Button>(true)).ToArray();
        buttonStyle = buttons.FirstOrDefault(b => b.name.Contains("New") || b.name.Contains("Start"))?.GetComponent<Image>();
        closeStyle = buttons.FirstOrDefault(b => b.name.Contains("Close"))?.GetComponent<Image>();
        string menuPath = Folder + "/GameModeMenu.prefab";
        if (!File.Exists(menuPath))
        {
            var root = Modal("GameModeMenu", out var panel, new Vector2(700, 640));
            var menu = root.AddComponent<GameModeSelectionUI>();
            Label(panel, "Title", "Choose Game Mode", new Vector2(0, 248), new Vector2(570, 65), 42);
            menu.standardButton = Button(panel, "Standard", "Standard\nThe original castle campaign", new Vector2(0, 120), new Vector2(530, 100));
            menu.chaosButton = Button(panel, "Chaos", "Chaos\nModifiers accumulate • +20% experience", new Vector2(0, 0), new Vector2(530, 100));
            menu.marathonButton = Button(panel, "Marathon", "Marathon\nEndless damage levels and boss battles", new Vector2(0, -120), new Vector2(530, 100));
            menu.lockDescription = Label(panel, "Unlock", "Reach the final Standard level to unlock Chaos and Marathon.", new Vector2(0, -245), new Vector2(580, 70), 25);
            menu.closeButton = Button(panel, "Close", "X", new Vector2(303, 277), new Vector2(52, 52), true);
            GameModeUIPolish.PolishNewPrefab(root);
            PrefabUtility.SaveAsPrefabAsset(root, menuPath); Object.DestroyImmediate(root);
        }
        EditorSceneManager.CloseScene(titleScene, true);
        string gaugePath = Folder + "/MarathonLevelGauge.prefab";
        if (!File.Exists(gaugePath))
        {
            var root = Rect("MarathonLevelGauge", null, Vector2.zero, new Vector2(48, 500));
            var ui = root.gameObject.AddComponent<MarathonGaugeUI>();
            var back = root.gameObject.AddComponent<Image>(); back.color = new Color(0.06f, 0.025f, 0.1f); back.raycastTarget = false;
            var area = Rect("Fill Area", root, Vector2.zero, Vector2.zero); Stretch(area, 5);
            var fill = Rect("Purple Liquid", area, Vector2.zero, Vector2.zero); Stretch(fill, 0);
            var image = fill.gameObject.AddComponent<Image>(); image.color = new Color(0.67f, 0.22f, 0.92f); image.raycastTarget = false;
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/UI_LiquidFill_EnemyHP_Mat.mat");
            string materialPath = Folder + "/MarathonPurpleLiquid.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material && source)
            {
                material = new Material(source); material.name = "MarathonPurpleLiquid";
                material.shader = Shader.Find("Tetrabeasts/UI/Marathon Liquid");
                material.SetColor("_BubbleColor", new Color(0.88f, 0.7f, 1f));
                material.SetFloat("_Aspect", 0.08f); material.SetFloat("_BubbleScale", 6f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            image.material = material;
            image.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/SpritePixel.png").OfType<Sprite>().First();
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Vertical; image.fillOrigin = 0;
            var scroll = fill.gameObject.AddComponent<UILiquidMaterialScroll>();
            var scrollSettings = new SerializedObject(scroll);
            scrollSettings.FindProperty("noiseSpeed").vector2Value = new Vector2(0.25f, 0.05f);
            scrollSettings.FindProperty("bubbleSpeed").vector2Value = new Vector2(0f, -0.2f);
            scrollSettings.ApplyModifiedPropertiesWithoutUndo();
            ui.gauge = root.gameObject.AddComponent<Slider>(); ui.gauge.fillRect = fill; ui.gauge.direction = Slider.Direction.BottomToTop;
            ui.gauge.interactable = false; ui.gauge.minValue = 0; ui.gauge.maxValue = 1; ui.gauge.value = 0;
            ui.levelLabel = Label(root, "Level", "Level 1", new Vector2(0, 280), new Vector2(150, 50), 28);
            ui.damageLabel = Label(root, "Damage", "0 / 500", new Vector2(0, -282), new Vector2(150, 50), 23);
            GameModeUIPolish.PolishNewPrefab(root.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, gaugePath); Object.DestroyImmediate(root.gameObject);
        }
        string rewardPath = Folder + "/MarathonRewardPanel.prefab";
        if (!File.Exists(rewardPath))
        {
            var root = Modal("MarathonRewardPanel", out var panel, new Vector2(1120, 740));
            var ui = root.AddComponent<MarathonRewardUI>();
            ui.title = Label(panel, "Title", "Choose a pair", new Vector2(0, 310), new Vector2(940, 65), 38);
            ui.choices = new Button[3]; ui.choiceLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                ui.choices[i] = Button(panel, "Pair " + (i + 1), "Buff + Debuff", new Vector2((i - 1) * 352, 0), new Vector2(330, 490));
                ui.choiceLabels[i] = ui.choices[i].GetComponentInChildren<TMP_Text>();
                ui.choiceLabels[i].enableAutoSizing = true; ui.choiceLabels[i].fontSizeMin = 18; ui.choiceLabels[i].fontSizeMax = 28;
            }
            ui.confirmButton = Button(panel, "Confirm", "Confirm", new Vector2(0, -305), new Vector2(280, 70));
            GameModeUIPolish.PolishNewPrefab(root);
            PrefabUtility.SaveAsPrefabAsset(root, rewardPath); Object.DestroyImmediate(root);
        }
        const string configPath = "Assets/Resources/GameModeAssets.asset";
        var config = AssetDatabase.LoadAssetAtPath<GameModeAssets>(configPath);
        if (!config)
        {
            config = ScriptableObject.CreateInstance<GameModeAssets>();
            config.modeMenuPrefab = AssetDatabase.LoadAssetAtPath<GameModeSelectionUI>(menuPath);
            config.marathonGaugePrefab = AssetDatabase.LoadAssetAtPath<MarathonGaugeUI>(gaugePath);
            config.marathonRewardPrefab = AssetDatabase.LoadAssetAtPath<MarathonRewardUI>(rewardPath);
            config.boulderSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Traps_Obstacles/Boulder_Sprite.png").OfType<Sprite>().First();
            config.boulderImpactSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelModifier/BoulderImpact.wav");
            var gameplay = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
            var controller = gameplay.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameController>(true)).First();
            config.standardLevelCount = controller.castlesByLevel.Length;
            Debug.Log("Game mode Standard level count: " + config.standardLevelCount + "; bosses: " + string.Join(", ", controller.castlesByLevel.Where(c => c && c.isBossLevel).Select(c => c.name)));
            EditorSceneManager.CloseScene(gameplay, true);
            AssetDatabase.CreateAsset(config, configPath);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Game mode prefabs and runtime references created.");
    }
    static GameObject Modal(string name, out RectTransform panel, Vector2 size)
    {
        var root = Rect(name, null, Vector2.zero, Vector2.zero); Stretch(root, 0);
        var backdrop = root.gameObject.AddComponent<Image>(); backdrop.color = new Color(0, 0, 0, 0.8f);
        panel = Rect("Panel", root, Vector2.zero, size);
        panel.gameObject.AddComponent<Image>().color = new Color(0.075f, 0.05f, 0.12f);
        return root.gameObject;
    }

    public static void RenderPreviews()
    {
        RenderPreviews(false);
    }
    public static void RenderGameplayPreview()
    {
        RenderPreviews(true);
    }
    public static void RenderModifierFeedbackPreviews() => RenderPreviews(true, true);
    static void RenderPreviews(bool gameplayOnly, bool modifierFeedback = false)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var config = GameModeAssets.Load();
        var camera = new GameObject("Preview camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
        var canvas = new GameObject("Preview canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        var target = new RenderTexture(1600, 900, 24); camera.targetTexture = target;
        if (!gameplayOnly)
        {
            var menu = Object.Instantiate(config.modeMenuPrefab, canvas.transform); menu.lockDescription.gameObject.SetActive(false);
            Capture("Logs/GameModeMenu.png");
            menu.chaosButton.interactable = false;
            menu.ShowTooltip(menu.chaosButton, menu.chaosButton.GetComponent<GameModeButtonTooltip>().description);
            Capture("Logs/GameModeLockedTooltip.png");
            menu.marathonButton.interactable = false;
            menu.ShowTooltip(menu.marathonButton, "");
            Capture("Logs/MarathonModeTooltip.png"); Object.DestroyImmediate(menu.gameObject);
            var reward = Object.Instantiate(config.marathonRewardPrefab, canvas.transform);
            var modifiers = AssetDatabase.FindAssets("t:RunModifier").Select(g => AssetDatabase.LoadAssetAtPath<RunModifier>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m).ToArray();
            var pairs = new MarathonRewardUI.Pair[3];
            var rarities = new[] { RunModRarity.Common, RunModRarity.Rare, RunModRarity.Legendary };
            for (int i = 0; i < pairs.Length; i++)
            {
                var candidates = modifiers.Where(m => m.rarity == rarities[i] && m.icon).ToArray();
                pairs[i] = new MarathonRewardUI.Pair { buff = candidates.First(m => m.isBuff), debuff = candidates.First(m => !m.isBuff) };
            }
            reward.Show(2, pairs, null);
            reward.cards[1].SetSelected(true);
            foreach (var animator in reward.cards[1].selectedFlames.GetComponentsInChildren<Animator>(true))
                if (animator.runtimeAnimatorController)
                    animator.runtimeAnimatorController.animationClips[0].SampleAnimation(animator.gameObject, 0.25f);
            Capture("Logs/MarathonRewardPanel.png"); Object.DestroyImmediate(reward.gameObject);
            foreach (var path in new[] { "Assets/Resources/Prefabs/Panels/SteamLeaderboard_Panel.prefab", "Assets/Prefabs/Panels/HighScore_Panel.prefab" })
            {
                var panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), canvas.transform);
                panel.SetActive(true);
                Capture("Logs/" + panel.name + ".png");
                var dropdown = panel.GetComponentInChildren<TMP_Dropdown>(true);
                typeof(TMP_Dropdown).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(dropdown, null);
                dropdown.alphaFadeSpeed = 0;
                dropdown.Show();
                foreach (var group in dropdown.GetComponentsInChildren<CanvasGroup>()) group.alpha = 1;
                Capture("Logs/" + panel.name + "Dropdown.png");
                Object.DestroyImmediate(panel);
            }
            var gauge = Object.Instantiate(config.marathonGaugePrefab, canvas.transform); gauge.Refresh(5, 400, 750);
            Debug.Log("Gauge fill anchors " + gauge.gauge.fillRect.anchorMax + " rect " + gauge.gauge.fillRect.rect + " value " + gauge.gauge.value);
            Capture("Logs/MarathonLevelGauge.png");
            gauge.liquidSlosh.AddImpulse(0.15f);
            Capture("Logs/MarathonGaugeSlosh.png");
            camera.targetTexture = null; Object.DestroyImmediate(target); Object.DestroyImmediate(canvas.gameObject); Object.DestroyImmediate(camera.gameObject);
            return;
        }
        Object.DestroyImmediate(canvas.gameObject);
        var gameplay = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
        var gc = gameplay.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameController>(true)).First();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        gc.GetType().GetField("modeAssets", flags).SetValue(gc, config);
        GameModeStore.Current = RunGameMode.Marathon;
        gc.GetType().GetMethod("EnsureMarathonUI", flags).Invoke(gc, null);
        gc.GetType().GetField("marathonDamage", flags).SetValue(gc, 250f);
        gc.GetType().GetMethod("RefreshMarathonGauge", flags).Invoke(gc, null);
        gc.specialSlider.normalizedValue = 0.65f;
        gc.specialGauge = 65f;
        gc.GetType().GetMethod("UpdateSpecialGaugeFieryFill", flags).Invoke(gc, null);
        canvas = gc.gameBoard.GetComponentInParent<Canvas>().rootCanvas;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        if (modifierFeedback)
        {
            typeof(Board).GetField("boardRect", flags).SetValue(gc.gameBoard, (RectTransform)gc.gameBoard.transform);
            gc.gameBoard.RecomputeCellMetrics();
            typeof(Board).GetMethod("DrawGridOverlay", flags).Invoke(gc.gameBoard, null);
            var inversion = AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/Inversion.asset");
            var sigil = gc.gameBoard.CreateModifierBoardVFX(inversion.inversionBoardSprite);
            sigil.type = Image.Type.Filled; sigil.fillMethod = Image.FillMethod.Radial360;
            sigil.fillOrigin = (int)Image.Origin360.Top; sigil.fillClockwise = false;
            sigil.color = new Color(1, 1, 1, .6f);
            sigil.fillAmount = 1f; Capture("Logs/InversionActivePreview.png");
            sigil.fillAmount = .5f; Capture("Logs/InversionHalfPreview.png");
            Object.DestroyImmediate(sigil.gameObject);
            var boulders = gc.gameObject.AddComponent<BoulderController>();
            boulders.boulderSprite = config.boulderSprite;
            typeof(BoulderController).GetField("board", flags).SetValue(boulders, gc.gameBoard);
            typeof(BoulderController).GetField("gc", flags).SetValue(boulders, gc);
            typeof(BoulderController).GetMethod("Spawn", flags).Invoke(boulders, new object[] { 5f });
            var rockList = (System.Collections.IList)typeof(BoulderController).GetField("rocks", flags).GetValue(boulders);
            var rockImage = (Image)rockList[0].GetType().GetField("image").GetValue(rockList[0]);
            var contact = new Vector2(5f, 8f);
            var rockPosition = contact + Vector2.up * .42f;
            rockImage.rectTransform.anchoredPosition = (Vector2)typeof(BoulderController).GetMethod("ToLocal", flags).Invoke(boulders, new object[] { rockPosition });
            typeof(BoulderController).GetMethod("Burst", flags).Invoke(boulders, new object[] { contact, Vector2.up });
            var pixels = (System.Collections.IList)typeof(BoulderController).GetField("pixels", flags).GetValue(boulders);
            foreach (var pixel in pixels)
            {
                var image = (Image)pixel.GetType().GetField("image").GetValue(pixel);
                image.rectTransform.anchoredPosition += (Vector2)pixel.GetType().GetField("velocity").GetValue(pixel) * .18f + Vector2.down * (250f * .18f * .18f);
            }
            Capture("Logs/BoulderImpactPreview.png");
            // Scene close owns cleanup in edit mode; don't invoke runtime Destroy calls.
            rockList.Clear(); pixels.Clear();
        }
        Capture("Logs/MarathonGameplayLayout.png");
        GameModeStore.Current = RunGameMode.Standard;
        EditorSceneManager.CloseScene(gameplay, true);
        camera.targetTexture = null; Object.DestroyImmediate(target); Object.DestroyImmediate(camera.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);

        void Capture(string path)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var label in canvas.GetComponentsInChildren<TMP_Text>()) label.ForceMeshUpdate();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
                RenderPipeline.SubmitRenderRequest(camera, request);
            else camera.Render();
            Debug.Log("Preview render pipeline: " + GraphicsSettings.currentRenderPipeline + "; request supported: " + RenderPipeline.SupportsRenderRequest(camera, request));
            RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = null; Object.DestroyImmediate(image);
        }
    }
    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
    }
    static TMP_Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = Rect(name, parent, position, size); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size, bool close = false)
    {
        var rect = Rect(name, parent, position, size); var image = rect.gameObject.AddComponent<Image>();
        var style = close ? closeStyle : buttonStyle;
        if (style) { image.sprite = style.sprite; image.type = style.type; image.color = style.color; }
        else image.color = new Color(0.2f, 0.12f, 0.32f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var label = Label(rect, "Label", text, Vector2.zero, size - Vector2.one * 24, 28);
        if (style) label.color = new Color(0.08f, 0.09f, 0.13f);
        return button;
    }
}
#endif
