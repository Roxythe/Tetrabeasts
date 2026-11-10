#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RewardAndModifierPresentationTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private | BindingFlags.DeclaredOnly).Invoke(target, args);
    static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/" + path);
    static LevelModifierSO Mod(string name) => Asset<LevelModifierSO>("ScriptableObjects/LevelModifier/" + name + ".asset");

    [Test]
    public void PreviewUsesPlayerImpactAndIncludesThePendingBuff()
    {
        var go = new GameObject("Reward projection");
        go.SetActive(false);
        try
        {
            var gc = go.AddComponent<GameController>();
            var reinforcements = Asset<RunModifier>("ScriptableObjects/RunModifier/Buff/Buff_RestoreReserveUnitsUp_4.asset");
            int before = gc.CurrentReinforcementsPerWinForStats;
            StringAssert.Contains($"{before}  ->  <color=#72C9FF>{before + 5}", gc.RewardPreview(reinforcements));
            var disable = Asset<RunModifier>("ScriptableObjects/RunModifier/Debuff/Debuff_DisableReinforcements.asset");
            StringAssert.Contains($"{before + 5}  ->  <color=#FF817A>0", gc.RewardPreview(disable, reinforcements));
            foreach (string path in new[] { "Debuff_EnmProjDmgMult_0", "Debuff_MonHPMult_0" })
            {
                var bad = Asset<RunModifier>($"ScriptableObjects/RunModifier/Debuff/{path}.asset");
                string result = gc.RewardPreview(bad);
                StringAssert.Contains("<color=#FF817A>", result);
            }
            Assert.AreEqual(before, gc.CurrentReinforcementsPerWinForStats);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void AuthoredRewardPanelShowsAndHidesPreviewForBothInputPaths()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameplayScene.unity");
        try
        {
            RoundRewardUI ui = null;
            GameController gc = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!ui) ui = root.GetComponentInChildren<RoundRewardUI>(true);
                if (!gc) gc = root.GetComponentInChildren<GameController>(true);
            }
            Assert.IsNotNull(ui);
            Assert.IsNotNull(gc);
            typeof(RoundRewardUI).GetField("previewGame",Private).SetValue(ui,gc);
            var preview = ui.transform.Find("RewardPreview_Box").gameObject;
            var label = preview.GetComponentInChildren<TMP_Text>(true);
            // Replace the scene's editor sample cards before invoking the normal reward builder.
            for (int i = ui.buffContainer.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(ui.buffContainer.GetChild(i).gameObject);
            var choices = new List<RunModifierSO>();
            foreach (string name in new[] { "Buff_RestoreReserveUnitsUp_4", "Buff_HealPower_2", "Buff_MonAtk_1" })
                choices.Add(Asset<RunModifier>($"ScriptableObjects/RunModifier/Buff/{name}.asset"));
            Call(ui, "Populate", ui.buffContainer, choices, true);
            var option = ui.buffContainer.GetComponentInChildren<RunModOptionButton>(true);
            var input = typeof(UICursorController);
            var source = input.GetField("s_targetInputSource",BindingFlags.NonPublic | BindingFlags.Static);
            var navigation = input.GetField("s_buttonNavigationMode",BindingFlags.NonPublic | BindingFlags.Static);
            object oldSource = source.GetValue(null), oldNav = navigation.GetValue(null);
            try
            {
                source.SetValue(null,UITargetInputSource.MousePointer);
                navigation.SetValue(null,false);
                option.OnPointerEnter(null);
                Call(option,"LateUpdate");
                Assert.IsTrue(preview.activeSelf);
                StringAssert.Contains("#72C9FF",label.text);
                ui.rootPanel.transform.localScale = Vector3.one;
                ui.GetComponentInParent<Canvas>(true).rootCanvas.transform.localScale = Vector3.one;
                Canvas.ForceUpdateCanvases();
                Call(ui, "PositionPreview");
                var optionRect = (RectTransform)option.transform;
                var previewRect = (RectTransform)preview.transform;
                Vector3 cardRight = optionRect.TransformPoint(new Vector3(optionRect.rect.xMax, optionRect.rect.center.y));
                Vector3 popupLeft = previewRect.TransformPoint(new Vector3(previewRect.rect.xMin, previewRect.rect.center.y));
                Assert.Greater(popupLeft.x, cardRight.x);
                Assert.AreEqual(cardRight.y, popupLeft.y, .01f);
                CaptureRewardPreview(ui);
                option.OnPointerExit(null);
                Call(option,"LateUpdate");
                Assert.IsFalse(preview.activeSelf);
                source.SetValue(null,UITargetInputSource.ButtonNavigation);
                navigation.SetValue(null,true);
                option.OnSelect(null);
                Call(option,"LateUpdate");
                Assert.IsTrue(preview.activeSelf);
                option.OnDeselect(null);
                Call(option,"LateUpdate");
                Assert.IsFalse(preview.activeSelf);
            }
            finally { source.SetValue(null,oldSource); navigation.SetValue(null,oldNav); }
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void CaptureRewardPreview(RoundRewardUI source)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1920, 1080, 24);
        try
        {
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.scene = scene;
            camera.targetTexture = texture;
            camera.clearFlags = CameraClearFlags.SolidColor;
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var sample = Object.Instantiate(source.rootPanel, canvas.transform).GetComponent<RoundRewardUI>();
            sample.gameObject.SetActive(true);
            sample.buffPanel.SetActive(true);
            sample.debuffPanel.SetActive(false);
            GameplayPlaylistTests.Capture(camera,"Round-Reward-Preview");
            sample.gameObject.SetActive(false);
            var gauge = Object.Instantiate(Asset<GameObject>("Prefabs/SpecialGauge_UI.prefab"), canvas.transform);
            gauge.SetActive(true);
            var slider = gauge.GetComponentInChildren<Slider>(true);
            slider.normalizedValue = 1f;
            slider.fillRect.GetComponent<Image>().fillAmount = 1f;
            Assert.Zero(slider.fillRect.GetComponent<Image>().material.GetFloat("_FoamIntensity"));
            GameplayPlaylistTests.Capture(camera,"Special-Gauge-Full");
            camera.targetTexture = null;
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(texture);
        }
    }

    [UnityTest]
    public IEnumerator RevealedAndAcceptedModifiersUseSeparateViews()
    {
        yield return new EnterPlayMode();
        var root = new GameObject("Test canvas", typeof(RectTransform), typeof(Canvas));
        var camera = new GameObject("Presentation capture", typeof(Camera)).GetComponent<Camera>();
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        camera.clearFlags = CameraClearFlags.SolidColor;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var gcRoot = new GameObject("Inactive controller");
        gcRoot.SetActive(false);
        var gc = gcRoot.AddComponent<GameController>();
        var controller = gcRoot.AddComponent<LevelModifierController>();
        gc.levelModifierController = controller;
        var older = Mod("WatchYourStep");
        var newest = Mod("Hallucination");
        typeof(LevelModifierController).GetField("<ActiveModifier>k__BackingField", Private).SetValue(controller, older);
        var list = (List<LevelModifierSO>)typeof(LevelModifierController).GetField("chaosModifiers", Private).GetValue(controller);
        list.Add(older);
        var label = new GameObject("HUD", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(root.transform, false);
        typeof(LevelModifierController).GetField("activeModifierTitleText", Private).SetValue(controller, label);
        var selection = Object.Instantiate(Asset<GameObject>("Prefabs/Panels/LevelModifier_Panel.prefab"), root.transform)
            .GetComponent<LevelModifierSelectionUI>();
        selection.SetActiveModifierInfo(newest);
        var selectedView = selection.GetComponentInChildren<LevelModifierDetails>(true);
        Assert.AreEqual(newest.icon, selectedView.transform.Find("LevelModIcon_Image").GetComponent<Image>().sprite);
        Assert.AreEqual(newest, Call(selection,"ResolveInfoModifier",newest));
        Assert.IsTrue((bool)Call(controller, "HasAcceptedChaosKind", older));
        Assert.IsFalse((bool)Call(controller, "HasAcceptedChaosKind", newest));
        list.Add(newest);
        label.text = older.displayName;
        Call(controller,"UpdateChaosTitle",2.9f);
        Assert.AreEqual(older.displayName,label.text);
        Call(controller,"UpdateChaosTitle",.11f);
        Assert.AreEqual(TetrabeastsLocalization.LocalizeText(newest.displayName),label.text);
        Call(controller,"UpdateChaosTitle",3f);
        Assert.AreEqual(TetrabeastsLocalization.LocalizeText(older.displayName),label.text);

        var pause = Object.Instantiate(Asset<GameObject>("Prefabs/Panels/PauseMenu_Panel.prefab"), root.transform);
        pause.SetActive(true);
        var run = pause.GetComponentInChildren<RunModsPanelUI>(true);
        typeof(RunModsPanelUI).GetField("controller",Private).SetValue(run,controller);
        run.gameObject.SetActive(true);
        run.Refresh();
        var open = run.transform.Find("LevelModifiers_Button").GetComponent<Button>();
        open.onClick.Invoke();
        Assert.IsNotNull(run.OpenDetailsRoot);
        yield return null;
        GameplayPlaylistTests.Capture(camera,"Pause-Active-Level-Modifier");
        var view = run.OpenDetailsRoot.transform;
        var icon = view.Find("LevelModIcon_Image").GetComponent<Image>();
        Assert.AreEqual(newest.icon,icon.sprite);
        view.Find("NextModifier_Button").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(older.icon,icon.sprite);
        view.Find("PreviousModifier_Button").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(newest.icon,icon.sprite);
        run.CloseDetails();
        list.Clear();
        typeof(LevelModifierController).GetField("<ActiveModifier>k__BackingField",Private).SetValue(controller,null);
        run.Refresh();
        Assert.IsFalse(open.interactable);

        var overlay = new GameObject("Special overlay",typeof(RectTransform));
        overlay.transform.SetParent(root.transform,false);
        Call(gc,"PrepareSpecialAbilityPopupRect",overlay);
        Assert.AreEqual(short.MaxValue,overlay.GetComponent<Canvas>().sortingOrder);
        var parent = (RectTransform)root.transform;
        CastleImpactDebris.Spawn(parent,new Vector2(12,19),Color.gray);
        CastleImpactDebris.Spawn(parent,new Vector2(-10,21),Color.blue);
        var debris = root.GetComponentsInChildren<CastleImpactDebris>();
        Assert.AreEqual(2,debris.Length);
        foreach (var burst in debris)
        {
            using var mesh = new VertexHelper();
            Call(burst, "OnPopulateMesh", mesh);
            Assert.Greater(mesh.currentVertCount,0);
            Assert.IsFalse(burst.raycastTarget);
        }
        Object.Destroy(root);
        Object.Destroy(gcRoot);
        camera.targetTexture = null;
        Object.Destroy(target);
        Object.Destroy(camera.gameObject);
        yield return null;
        yield return new ExitPlayMode();
    }
}
#endif
