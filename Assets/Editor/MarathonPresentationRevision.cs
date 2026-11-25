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

public static class MarathonPresentationRevision
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Read(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    [MenuItem("Tetrabeasts/Apply Marathon Presentation Revision")]
    public static void Apply()
    {
        RewardReadabilityAuthoring.Apply();
        string path = "Assets/Prefabs/GameModes/GameModeTooltip.prefab";
        var tooltip = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = tooltip.GetComponent<GameModeTooltip>();
            view.fixedWidth = 350; view.ShowMode(RunGameMode.Standard, "Standard", false);
            PrefabUtility.SaveAsPrefabAsset(tooltip, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(tooltip); }
        path = "Assets/Prefabs/GameModes/MarathonLevelGauge.prefab";
        var gauge = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab").GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Special_Text (TMP)");
            gauge.GetComponent<MarathonGaugeUI>().MatchTypography(source);
            PrefabUtility.SaveAsPrefabAsset(gauge, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(gauge); }
    }

    [MenuItem("Tetrabeasts/Validate Marathon Presentation Revision")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run outside Play mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var canvas = new GameObject("Revision previews", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920,1080);
            void Capture(string name) => typeof(CastleCelebrationValidation).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] {canvas,scene,"Logs/"+name+".png"});
            var tip = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameModeTooltip>("Assets/Prefabs/GameModes/GameModeTooltip.prefab"),canvas.transform);
            float standardHeight = 0;
            foreach (RunGameMode mode in new[] {RunGameMode.Standard,RunGameMode.Chaos,RunGameMode.Marathon})
            {
                tip.ShowMode(mode,mode.ToString(),false);
                var rect = (RectTransform)tip.transform; rect.anchoredPosition = Vector2.zero;
                Check(rect.rect.width == 350, "All tooltips must have the same locked width.");
                if (mode == RunGameMode.Standard) standardHeight = rect.rect.height;
                if (mode == RunGameMode.Marathon) Check(rect.rect.height > standardHeight, "Longer mode text must grow the panel vertically.");
                Check(Mathf.Abs(-tip.description.rectTransform.anchoredPosition.y + tip.description.rectTransform.rect.height + tip.padding - rect.rect.height) < .1f, "Bottom text padding must equal top padding.");
                Capture("ModeTooltip-"+mode);
            }
            Object.DestroyImmediate(tip.gameObject);
            var gaugePrefab = AssetDatabase.LoadAssetAtPath<MarathonGaugeUI>("Assets/Prefabs/GameModes/MarathonLevelGauge.prefab");
            var special = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpecialGauge_UI.prefab").GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Special_Text (TMP)");
            Check(gaugePrefab.damageLabel.font == special.font && gaugePrefab.damageLabel.fontStyle == special.fontStyle, "Marathon and special gauges must share font/style.");
            for (int tier = 0; tier < 5; tier++)
            {
                var good = AssetDatabase.LoadAssetAtPath<RunModifier>($"Assets/ScriptableObjects/RunModifier/Buff/Buff_AddCurrency_{tier}.asset");
                var bad = AssetDatabase.LoadAssetAtPath<RunModifier>($"Assets/ScriptableObjects/RunModifier/Debuff/Debuff_ReducedCurrency_{tier}.asset");
                Check(good.op == RunModOp.Multiply && bad.op == RunModOp.Multiply && bad.ProjectInteger(15) > 0, "Gold modifiers must scale proportionally and not erase a 15-gold reward.");
                for (int row = 0; row < 2; row++)
                {
                    var holder = new GameObject("Rarity", typeof(RectTransform)).GetComponent<RectTransform>(); holder.SetParent(canvas.transform,false);
                    holder.anchoredPosition = new Vector2((tier-2)*330, row == 0 ? 150 : -150);
                    var image = new GameObject("Icon", typeof(RectTransform),typeof(Image)).GetComponent<Image>(); image.transform.SetParent(holder,false);
                    image.rectTransform.sizeDelta = new Vector2(160,160); image.sprite = row == 0 ? good.icon : bad.icon;
                    var effect = RewardIconBackdrop.Attach(image,MarathonRewardUI.RarityColor((RunModRarity)tier));
                    effect.rarity = (RunModRarity)tier; effect.isDebuff = row == 1; effect.SetHighlighted(true);
                    typeof(RewardIconBackdrop).GetField("clock",Private).SetValue(effect, .37f);
                    var label = new GameObject("Label", typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>(); label.transform.SetParent(holder,false);
                    label.font = gaugePrefab.damageLabel.font; label.fontSize = 28; label.alignment = TextAlignmentOptions.Center; label.text = ((RunModRarity)tier) + (row==0 ? " Buff" : " Debuff");
                    label.rectTransform.sizeDelta = new Vector2(300,50); label.rectTransform.anchoredPosition = new Vector2(0,-120);
                }
            }
            Capture("RewardIcons-AllRarities");
            while (canvas.transform.childCount > 0) Object.DestroyImmediate(canvas.transform.GetChild(0).gameObject);
            var board = new GameObject("Layered hazards",typeof(RectTransform),typeof(Board)).GetComponent<Board>(); board.transform.SetParent(canvas.transform,false);
            board.width=8;board.height=12; ((RectTransform)board.transform).sizeDelta = new Vector2(480,720);
            Call(board,"Awake"); typeof(Board).GetField("_gc",Private).SetValue(board,null); board.RecomputeCellMetrics();
            var cell = new Vector2Int(2,2);
            Check(board.SetFloorEffect(cell,Board.FloorEffectType.Burn,2,.5f,10), "Base fire setup.");
            Check(board.AddBossLightning(cell,3,.5f,2), "Boss lightning must overlay fire.");
            var floors = (Dictionary<Vector2Int,Board.FloorEffectState>)Read(board,"floorEffects");
            var lightning = (Dictionary<Vector2Int,Board.FloorEffectState>)Read(board,"bossLightningEffects");
            var saved = board.CaptureMarathonBoard();
            Check(saved.floors.Count == 2 && saved.floors.Count(f=>f.bossLightning)==1, "Both hazard layers must survive a save.");
            board.RestoreMarathonBoard(saved,new MatchSnapshotAssets());
            Check(lightning.ContainsKey(cell) && floors.ContainsKey(cell), "Loading must restore both hazard layers on one cell.");
            Call(board,"TickBossLightning",Time.time+1f); Call(board,"TickBossLightning",Time.time+2f);
            Check(!lightning.ContainsKey(cell) && floors[cell].type == Board.FloorEffectType.Burn && floors[cell].ticksRemaining == 10, "Lightning expiration must not remove or age the base effect.");
            Check(board.AddBossLightning(cell,3,.5f,2), "Lightning can be reapplied.");
            board.ClearFloorEffect(cell);
            Check(lightning.ContainsKey(cell), "Base expiration must not erase lightning either.");
            board.ClearAll(); Check(lightning.Count == 0, "Board reset must clear every hazard layer.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        RewardFollowupValidation.Run();
        File.WriteAllText("Logs/MarathonPresentationRevision-validation.txt", "PASS: tooltip sizing/padding, gauge typography, percentage gold, rarity effects/layering, layered lightning/save data/independent expiry, smooth column settling and gameplay/reward regression checks.");
    }
}
#endif
