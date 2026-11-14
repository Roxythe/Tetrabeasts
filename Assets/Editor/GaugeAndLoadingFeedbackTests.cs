#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GaugeAndLoadingFeedbackTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);

    [Test]
    public void ChargeChangesEaseBothWaysAndTheirWavesSettle()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab");
        var root = Object.Instantiate(prefab);
        try
        {
            root.SetActive(true);
            var slider = root.GetComponentInChildren<Slider>(true);
            slider.gameObject.SetActive(true);
            var motion = slider.GetComponent<GaugeLiquidMotion>();
            var slosh = slider.fillRect.GetComponent<MarathonLiquidSlosh>();
            // Edit-mode objects do not invoke ordinary MonoBehaviour lifecycle messages.
            Call(motion, "OnEnable");
            motion.SetTarget(0f, true);
            motion.SetTarget(1f);
            Assert.Zero(slider.normalizedValue, "Gameplay charge must not snap the visual fill.");
            Call(motion, "AdvanceVisual", .05f);
            Assert.That(slider.normalizedValue, Is.InRange(.01f, .99f));
            for (int i = 0; i < 120; i++) Call(motion, "AdvanceVisual", 1f / 60f);
            Assert.AreEqual(1f, slider.normalizedValue, .0001f);
            motion.SetTarget(0f);
            Call(motion, "AdvanceVisual", .05f);
            Assert.That(slider.normalizedValue, Is.InRange(.01f, .99f));
            Assert.Greater(slosh.WaveEnergy, 0f, "A large charge change should stir the liquid.");
            for (int i = 0; i < 240; i++) Call(slosh, "AdvanceWaves", 1f / 60f);
            Assert.Zero(slosh.WaveEnergy);
            GaugeLiquidMotion.NotifyScreenShake(.5f);
            Assert.Greater(slosh.WaveEnergy, 0f);
            for (int i = 0; i < 240; i++) Call(slosh, "AdvanceWaves", 1f / 60f);
            Assert.Zero(slosh.WaveEnergy);
        }
        finally
        {
            var motion = root.GetComponentInChildren<GaugeLiquidMotion>(true);
            if (motion) Call(motion, "OnDisable");
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(875f, 471f)]
    [TestCase(22f, 15f)]
    public void DirectHealthAndReserveUpdatesStayVisibleBetweenChanges(float maximum, float remaining)
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab"));
        var slider = root.GetComponentInChildren<Slider>(true);
        var motion = slider.GetComponent<GaugeLiquidMotion>();
        try
        {
            slider.minValue = 0f;
            slider.maxValue = maximum;
            slider.value = 0f;
            Call(motion, "OnEnable");
            slider.value = maximum;
            for (int i = 0; i < 120; i++) Call(motion, "AdvanceVisual", 1f / 60f);
            Assert.AreEqual(maximum, slider.value, "An idle liquid effect must not empty the gauge.");
            slider.value = remaining;
            for (int i = 0; i < 120; i++) Call(motion, "AdvanceVisual", 1f / 60f);
            Assert.AreEqual(remaining, slider.value);
            motion.SetTarget(.2f);
            slider.value = maximum;
            Call(motion, "AdvanceVisual", .5f);
            Assert.AreEqual(maximum, slider.value, "An explicit gameplay update must override any previous tween.");
        }
        finally { Call(motion, "OnDisable"); Object.DestroyImmediate(root); }
    }

    [Test]
    public void SmallChangesMakeSmallRipplesAndInterpolationDoesNotAmplifyThem()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab"));
        var slider = root.GetComponentInChildren<Slider>(true);
        var motion = slider.GetComponent<GaugeLiquidMotion>();
        var slosh = slider.fillRect.GetComponent<MarathonLiquidSlosh>();
        try
        {
            Call(motion, "OnEnable");
            motion.SetTarget(.5f, true);
            motion.SetTarget(.51f);
            float small = slosh.WaveEnergy;
            Assert.That(small, Is.InRange(.029f, .031f));
            for (int i = 0; i < 120; i++) Call(motion, "AdvanceVisual", 1f / 60f);
            Assert.AreEqual(small, slosh.WaveEnergy, .0001f);
            Call(slosh, "AdvanceWaves", 4f);
            motion.SetTarget(.71f);
            float large = slosh.WaveEnergy;
            Assert.Greater(large, small * 10f);
            Call(slosh, "AdvanceWaves", 4f);
            motion.SetTarget(.51f);
            Assert.AreEqual(large, slosh.WaveEnergy, .0001f, "An equal loss should stir as much as an equal gain.");
            Call(slosh, "AdvanceWaves", 4f);
            slider.normalizedValue = .52f;
            Assert.That(slosh.WaveEnergy, Is.GreaterThan(0f).And.LessThan(large));
        }
        finally { Call(motion, "OnDisable"); Object.DestroyImmediate(root); }
    }

    [Test]
    public void FullGaugeRipplesStayInsideTheGlass()
    {
        var root = new GameObject("Full liquid", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MarathonLiquidSlosh));
        try
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(50f, 300f);
            root.GetComponent<Image>().fillMethod = Image.FillMethod.Vertical;
            var slosh = root.GetComponent<MarathonLiquidSlosh>();
            slosh.AddImpulse(.2f);
            using (var mesh = new VertexHelper())
            {
                Rect bounds = rect.rect;
                mesh.AddVert(new Vector3(bounds.xMin, bounds.yMin), Color.white, Vector2.zero);
                mesh.AddVert(new Vector3(bounds.xMin, bounds.yMax), Color.white, Vector2.up);
                mesh.AddVert(new Vector3(bounds.xMax, bounds.yMax), Color.white, Vector2.one);
                mesh.AddVert(new Vector3(bounds.xMax, bounds.yMin), Color.white, Vector2.right);
                mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
                slosh.ModifyMesh(mesh);
                Assert.Greater(mesh.currentVertCount, 4);
                bool dipped = false;
                for (int i = 1; i < mesh.currentVertCount; i += 2)
                {
                    UIVertex vertex = default;
                    mesh.PopulateUIVertex(ref vertex, i);
                    Assert.LessOrEqual(vertex.position.y, bounds.yMax);
                    dipped |= vertex.position.y < bounds.yMax - .01f;
                }
                Assert.IsTrue(dipped, "Replenishing a full gauge must still show a visible inward ripple.");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void LoadingDotsCycleInOrderWhileGameplayTimeIsPaused()
    {
        var root = new GameObject("Loading feedback test");
        root.SetActive(false);
        var loading = root.AddComponent<LoadingScreen>();
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(root.transform);
        float oldTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 0f;
            Resources.Load<LoadingScreenSettings>("LoadingScreenSettings");
            Set(loading, "_loadingTexts", new[] { label });
            Set(loading, "_loadingTextBaseStrings", new[] { "Loading" });
            Set(loading, "_loadingTextBaseColors", new[] { Color.white });
            Set(loading, "_loadingTextVfxActive", true);
            for (int step = 0; step < 8; step++)
            {
                if (step == 0) Call(loading, "AdvanceLoadingAnimation", 0f);
                else for (int frame = 0; frame < 4; frame++) Call(loading, "AdvanceLoadingAnimation", .0625f);
                Assert.AreEqual("Loading" + new string('.', step % 4), label.text);
            }
        }
        finally { Time.timeScale = oldTimeScale; Object.DestroyImmediate(root); }
    }

    [Test]
    public void LoadingDotsKeepTheirCadenceAtEightFramesPerSecond()
    {
        var root = new GameObject("Slow loading feedback test");
        root.SetActive(false);
        var loading = root.AddComponent<LoadingScreen>();
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(root.transform);
        try
        {
            Set(loading, "_loadingTexts", new[] { label });
            Set(loading, "_loadingTextBaseStrings", new[] { "Loading" });
            Set(loading, "_loadingTextBaseColors", new[] { Color.white });
            Call(loading, "StartLoadingTextBlink");
            for (int step = 1; step <= 8; step++)
            {
                Call(loading, "AdvanceLoadingAnimation", .125f);
                Call(loading, "AdvanceLoadingAnimation", .125f);
                Assert.AreEqual("Loading" + new string('.', step % 4), label.text);
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void SceneTextBindingYieldsBeforeProcessingTheWholeScene()
    {
        var root = new GameObject("Batched localization test");
        root.SetActive(false);
        var labels = new Text[80];
        try
        {
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                labels[i].transform.SetParent(root.transform, false);
                labels[i].text = "New Game";
            }
            var method = typeof(TetrabeastsLocalizedTextScanner).GetMethod("BindTexts", BindingFlags.Static | BindingFlags.NonPublic);
            var scan = (System.Collections.IEnumerator)method.Invoke(null,
                new object[] { new TMP_Text[0], labels, true });
            Assert.IsTrue(scan.MoveNext());
            int firstBatch = root.GetComponentsInChildren<TetrabeastsLocalizedLegacyText>(true).Length;
            Assert.That(firstBatch, Is.InRange(1, 32));
            while (scan.MoveNext()) { }
            Assert.AreEqual(labels.Length, root.GetComponentsInChildren<TetrabeastsLocalizedLegacyText>(true).Length);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
#endif
