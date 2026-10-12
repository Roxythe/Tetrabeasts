#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MarathonUIPolishTests
{
    List<Object> temporary = new();
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);

    [UnityTest]
    public IEnumerator RewardSelectionLockedTooltipsAndCursorFocusWorkWhilePaused()
    {
        yield return new EnterPlayMode();
        VerifyPausedInteractions();
        Cleanup();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Create captured reward callbacks after EnterPlayMode's domain reload.
    void VerifyPausedInteractions()
    {
        temporary ??= new List<Object>();
        var canvas = new GameObject("UI test canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); temporary.Add(canvas);
        var events = new GameObject("UI test events", typeof(EventSystem)); temporary.Add(events);
        var config = GameModeAssets.Load();
        Assert.IsNotNull(config);
        var rewards = Object.Instantiate(config.marathonRewardPrefab, canvas.transform);
        Assert.IsNotNull(rewards);
        var buff = ScriptableObject.CreateInstance<RunModifier>(); temporary.Add(buff);
        var debuff = ScriptableObject.CreateInstance<RunModifier>(); temporary.Add(debuff);
        Assert.IsNotNull(buff); Assert.IsNotNull(debuff);
        buff.displayName = "Test buff"; debuff.displayName = "Test debuff";
        buff.rarity = debuff.rarity = RunModRarity.Rare;
        buff.icon = config.boulderSprite; debuff.icon = config.boulderSprite;
        var pair = new MarathonRewardUI.Pair { buff = buff, debuff = debuff };
        int confirmations = 0;
        rewards.Show(3, new[] { pair, pair, pair }, (b, d) => { Assert.AreSame(buff, b); Assert.AreSame(debuff, d); confirmations++; });
        Time.timeScale = 0;
        Assert.IsFalse(rewards.confirmButton.interactable);
        Assert.AreSame(buff.icon, rewards.buffIcons[0].sprite);
        Assert.AreSame(debuff.icon, rewards.debuffIcons[0].sprite);
        Assert.AreEqual(rewards.buffTextColor, rewards.buffLabels[0].color);
        Assert.AreEqual(rewards.debuffTextColor, rewards.debuffLabels[0].color);
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(rewards.choices[1].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.IsTrue(rewards.confirmButton.interactable);
        Assert.IsTrue(rewards.selectionOutlines[1].enabled);
        Assert.IsFalse(rewards.selectionOutlines[0].enabled);
        Assert.AreEqual(MarathonRewardUI.RarityColor(RunModRarity.Rare), rewards.choices[1].GetComponent<Image>().color, "Selection must preserve rarity.");
        ExecuteEvents.Execute(rewards.confirmButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.AreEqual(1, confirmations);
        Assert.IsFalse(rewards.gameObject.activeSelf);
        buff.icon = null;
        rewards.Show(4, new[] { pair, pair, pair }, null);
        Assert.IsFalse(rewards.buffIcons[0].enabled, "Reused choices must clear missing icons.");
        Assert.IsFalse(rewards.selectionOutlines[1].enabled);
        rewards.gameObject.SetActive(false);

        var menu = Object.Instantiate(config.modeMenuPrefab, canvas.transform);
        menu.chaosButton.interactable = false;
        ExecuteEvents.Execute(menu.chaosButton.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.IsTrue(menu.tooltip.gameObject.activeSelf);
        Assert.IsTrue(menu.tooltipLockNote.gameObject.activeSelf);
        Assert.AreEqual("Reach the final level in Standard to unlock this game mode.", menu.tooltipLockNote.text);
        var rect = (RectTransform)menu.chaosButton.transform;
        Assert.Greater(menu.tooltip.position.x, rect.TransformPoint(new Vector3(rect.rect.xMax, 0)).x);
        ExecuteEvents.Execute(menu.chaosButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.IsTrue(menu.gameObject.activeSelf, "Locked mode must not close or select.");
        ExecuteEvents.Execute(menu.chaosButton.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Assert.IsFalse(menu.tooltip.gameObject.activeSelf);
        ExecuteEvents.Execute(menu.standardButton.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Assert.IsFalse(menu.tooltipLockNote.gameObject.activeSelf);

        var controller = new GameObject("Inactive cursor test controller"); controller.SetActive(false); temporary.Add(controller);
        var gc = controller.AddComponent<GameController>();
        var rewardFlag = typeof(GameController).GetField("marathonRewardActive", Private);
        rewardFlag.SetValue(gc, true);
        Assert.IsTrue((bool)Call(gc, "ShouldUseUICursorModeForCurrentState"));
        Cursor.lockState = CursorLockMode.Locked;
        Call(gc, "ApplyCursorModeForCurrentState");
        Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Restoring focus during a reward must not lock the cursor.");
        Call(gc, "EnterGameplayCursorMode");
        Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Deferred gameplay cursor callbacks cannot lock an open reward panel.");
        rewardFlag.SetValue(gc, false);
        Assert.IsFalse((bool)Call(gc, "ShouldUseUICursorModeForCurrentState"));
    }

    [Test]
    public void GaugeSloshDeformsSurfaceAndKeepsLiquidInsideTheGauge()
    {
        var gauge = Object.Instantiate(GameModeAssets.Load().marathonGaugePrefab); temporary.Add(gauge.gameObject);
        gauge.Refresh(1, 0, 500);
        Assert.AreEqual(0f, gauge.gauge.normalizedValue);
        gauge.Refresh(1, 250, 500);
        Assert.Greater(gauge.liquidSlosh.WaveEnergy, 0f);
        float energy = gauge.liquidSlosh.WaveEnergy;
        gauge.Refresh(1, 250, 500);
        Assert.AreEqual(energy, gauge.liquidSlosh.WaveEnergy, "Repeated UI refreshes must not create new waves.");
        Canvas.ForceUpdateCanvases();
        var bounds = gauge.gauge.fillRect.rect;
        float top = Mathf.Lerp(bounds.yMin, bounds.yMax, 0.5f);
        using (var mesh = new VertexHelper())
        {
            var quad = new UIVertex[4];
            for (int i = 0; i < 4; i++) { quad[i] = UIVertex.simpleVert; }
            quad[0].position = new Vector3(bounds.xMin, bounds.yMin); quad[0].uv0 = Vector2.zero;
            quad[1].position = new Vector3(bounds.xMin, top); quad[1].uv0 = new Vector2(0, 0.5f);
            quad[2].position = new Vector3(bounds.xMax, top); quad[2].uv0 = new Vector2(1, 0.5f);
            quad[3].position = new Vector3(bounds.xMax, bounds.yMin); quad[3].uv0 = new Vector2(1, 0);
            mesh.AddUIVertexQuad(quad);
            gauge.liquidSlosh.ModifyMesh(mesh);
            Assert.Greater(mesh.currentVertCount, 4);
            float highest = float.MinValue, lowest = float.MaxValue;
            UIVertex vertex = default;
            for (int i = 1; i < mesh.currentVertCount; i += 2)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                highest = Mathf.Max(highest, vertex.position.y); lowest = Mathf.Min(lowest, vertex.position.y);
                Assert.That(vertex.position.y, Is.InRange(bounds.yMin, bounds.yMax));
            }
            Assert.Greater(highest - lowest, 1f, "Liquid surface should have an actual wave, not just a color animation.");
        }
        gauge.gameObject.SetActive(false);
        Assert.AreEqual(0f, gauge.liquidSlosh.WaveEnergy);
    }

    [Test]
    public void SavedPrefabsUseExistingGaugeArtworkAndButtonSounds()
    {
        var config = GameModeAssets.Load();
        var special = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab");
        foreach (var name in new[] { "Border_Image", "GlassOverlay" })
        {
            var expected = System.Array.Find(special.GetComponentsInChildren<Image>(), i => i.name == name);
            Assert.AreSame(expected.sprite, config.marathonGaugePrefab.transform.Find(name).GetComponent<Image>().sprite);
        }
        foreach (var root in new[] { config.modeMenuPrefab.gameObject, config.marathonRewardPrefab.gameObject })
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var sound = button.GetComponent<UIButtonSFX>();
                Assert.IsNotNull(sound, button.name);
                Assert.IsNotNull(sound.hoverClip, button.name);
            }
        Assert.IsNull(config.marathonGaugePrefab.levelLabel);
        Assert.Greater(config.marathonGaugePrefab.horizontalOffset, 0);
        Assert.AreEqual(1f, config.marathonGaugePrefab.transform.Find("Background").GetComponent<Image>().color.a);
    }

    [TearDown]
    public void Cleanup()
    {
        Time.timeScale = 1f; Cursor.lockState = CursorLockMode.None;
        foreach (var item in temporary) if (item) Object.DestroyImmediate(item);
        temporary.Clear();
    }
}
#endif
