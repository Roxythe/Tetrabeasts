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

public class GameplayPolishFollowupTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Private).Invoke(o, args);
    static void Set(object o, string field, object value) => o.GetType().GetField(field, Private).SetValue(o, value);
    static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, Private).GetValue(o);

    [Test]
    public void ChaosArtCyclesAsASetAndKeepsTheSavedAnchor()
    {
        var previous = GameModeStore.Current; GameModeStore.Current = RunGameMode.Chaos;
        var go = new GameObject("Presentation test"); go.SetActive(false);
        var controller = go.AddComponent<LevelModifierController>();
        var created = new List<Object>();
        try
        {
            var mods = Get<List<LevelModifierSO>>(controller, "chaosModifiers");
            for (int i = 0; i < 3; i++)
            {
                var mod = ScriptableObject.CreateInstance<LevelModifierSO>(); created.Add(mod); mod.name = "Variant " + i;
                var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.one*.5f); created.Add(sprite);
                mod.backgroundOverrideSprite = mod.boardBackgroundOverrideSprite = sprite;
                mods.Add(mod);
            }
            var anchor = typeof(LevelModifierController).GetProperty("ChaosPresentationAnchorName");
            var level = typeof(LevelModifierController).GetProperty("ChaosPresentationAnchorLevel");
            anchor.SetValue(controller, mods[2].name); level.SetValue(controller, 6);
            for (int n = 0; n < 8; n++)
            {
                Call(controller, "SelectChaosPresentation", 6+n);
                Sprite expected = n%4 == 3 ? null : mods[2-n%4].backgroundOverrideSprite;
                Assert.AreSame(expected, Call(controller, "ResolveBackgroundSprite"));
                Assert.AreSame(expected, Call(controller, "ResolveBoardBackgroundSprite"));
                Call(controller, "SelectChaosPresentation", 6+n);
                Assert.AreSame(expected, Call(controller, "ResolveBackgroundSprite"), "Refreshing the same level must not advance the cycle.");
            }
            var save = new TempRunSaveStore.SaveData { chaosPresentationAnchorName = controller.ChaosPresentationAnchorName,
                chaosPresentationAnchorLevel = controller.ChaosPresentationAnchorLevel };
            save = JsonUtility.FromJson<TempRunSaveStore.SaveData>(JsonUtility.ToJson(save));
            anchor.SetValue(controller, save.chaosPresentationAnchorName); level.SetValue(controller, save.chaosPresentationAnchorLevel);
            Call(controller, "SelectChaosPresentation", 7);
            Assert.AreSame(mods[1].backgroundOverrideSprite, Call(controller, "ResolveBackgroundSprite"));
            Assert.IsNull(controller.ActiveModifier, "Presentation selection must not change gameplay modifier ownership.");
        }
        finally { Object.DestroyImmediate(go); foreach(var item in created) Object.DestroyImmediate(item); GameModeStore.Current=previous; }
    }

    [Test]
    public void FocusLossOpensPauseAndFocusReturnDoesNotResume()
    {
        float scale=Time.timeScale; bool audio=AudioListener.pause;
        var go=new GameObject("Focus test"); var gc=go.AddComponent<GameController>();
        try
        {
            Call(gc,"OnApplicationFocus",false);
            Assert.IsTrue(Get<bool>(gc,"isPaused")); Assert.AreEqual(0,Time.timeScale);
            Call(gc,"OnApplicationFocus",true);
            Assert.IsTrue(Get<bool>(gc,"isPaused"));
        }
        finally { Object.DestroyImmediate(go); Time.timeScale=scale; AudioListener.pause=audio; }
    }

    [Test]
    public void FocusLossDuringCinematicPausesWhenTheCinematicFinishes()
    {
        float scale=Time.timeScale; bool audio=AudioListener.pause;
        var go=new GameObject("Deferred focus test"); var gc=go.AddComponent<GameController>();
        try
        {
            Set(gc,"_specialAbilityCinematicActive",true);
            Call(gc,"OnApplicationFocus",false);
            Assert.IsFalse(Get<bool>(gc,"isPaused"));
            Set(gc,"_specialAbilityCinematicActive",false);
            Call(gc,"TryApplyFocusPause");
            Assert.IsTrue(Get<bool>(gc,"isPaused"));
        }
        finally { Object.DestroyImmediate(go); Time.timeScale=scale; AudioListener.pause=audio; }
    }

    [Test]
    public void PreviewUsesTheSuppliedTitleColors()
    {
        var go=new GameObject("Preview test"); go.SetActive(false);
        var gc=go.AddComponent<GameController>(); var mod=ScriptableObject.CreateInstance<RunModifier>();
        try
        {
            mod.stat=RunModStat.LuckAdd; mod.op=RunModOp.Add; mod.value=5;
            Color blue=new Color(.15641688f,.4393744f,.8962264f), red=new Color(.8679245f,0,0);
            mod.isBuff=true; StringAssert.Contains("#"+ColorUtility.ToHtmlStringRGB(blue),gc.RewardPreview(mod,null,blue,red));
            mod.isBuff=false; StringAssert.Contains("#"+ColorUtility.ToHtmlStringRGB(red),gc.RewardPreview(mod,null,blue,red));
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mod); }
    }

    [Test]
    public void ChaosPageCounterIsAuthoredBelowTitleWithOpaqueShadow()
    {
        var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Panels/RunModsUI_Panel.prefab"));
        try
        {
            var panel=go.GetComponent<RunModsPanelUI>();
            var label=Get<TMP_Text>(panel,"modifierPageText");
            Assert.IsFalse(label.gameObject.activeSelf); Assert.GreaterOrEqual(label.fontSize,44);
            var shadow=label.GetComponent<TMPLabelShadow>(); Assert.IsNotNull(shadow.shadow);
            label.gameObject.SetActive(true); shadow.Sync();
            Assert.AreEqual(Color.black,shadow.shadow.color); Assert.IsTrue(shadow.shadow.overrideColorTags);
            var details=Get<LevelModifierDetails>(panel,"details"); var title=Get<TMP_Text>(details,"title");
            Assert.AreEqual(title.rectTransform.anchoredPosition.x,label.rectTransform.anchoredPosition.x);
            Assert.Less(label.rectTransform.anchoredPosition.y,title.rectTransform.anchoredPosition.y);
            Assert.IsFalse(Get<Button>(panel,"previous").gameObject.activeSelf);
            Assert.IsFalse(Get<Button>(panel,"next").gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(go); }
    }
}

public class PausedGaugeAndDebrisTests
{
    [SetUp] public void SetUp() { LocalSaveTestScope.Begin(); LocalSaveTestScope.BackupScenePreferences(); }
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        LocalSaveTestScope.RestoreScenePreferences(); LocalSaveTestScope.End();
    }
    [UnityTest] public IEnumerator BubblesAndDebrisAdvanceAndExpireWhileTimeScaleIsZero()
    {
        yield return new EnterPlayMode();
        LocalSaveTestScope.Apply();
        var gauge=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab"));
        var root=new GameObject("Impact test",typeof(RectTransform));
        CastleImpactDebris.Spawn((RectTransform)root.transform,Vector2.zero,Color.yellow);
        var debris=root.GetComponentInChildren<CastleImpactDebris>();
        Time.timeScale=0f;
        yield return null;
        float before=Shader.GetGlobalFloat("_TetrabeastsLiquidTime");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(Shader.GetGlobalFloat("_TetrabeastsLiquidTime"),before);
        Assert.Greater((float)typeof(CastleImpactDebris).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(debris),0f);
        yield return new WaitForSecondsRealtime(2.5f);
        Assert.IsTrue(!debris,"Impact debris must expire even while gameplay is paused.");
        Object.Destroy(root); Object.Destroy(gauge); Time.timeScale=1f;
        yield return new ExitPlayMode();
    }
}
#endif
