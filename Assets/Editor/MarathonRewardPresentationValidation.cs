#if UNITY_EDITOR
using System;
using System.Collections;
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

public static class MarathonRewardPresentationValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);

    [MenuItem("Tetrabeasts/Validate Marathon Reward Presentation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run this validation outside Play mode.");
        bool hadPreference = PlayerPrefs.HasKey(SettingsStore.KeyMarathonCompareRewards);
        int previousPreference = PlayerPrefs.GetInt(SettingsStore.KeyMarathonCompareRewards);
        var randomState = UnityEngine.Random.state;
        var inputSource = typeof(UICursorController).GetField("s_targetInputSource", BindingFlags.Static | BindingFlags.NonPublic);
        var previousSource = inputSource.GetValue(null);
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        try
        {
            PlayerPrefs.DeleteKey(SettingsStore.KeyMarathonCompareRewards);
            inputSource.SetValue(null, UITargetInputSource.MousePointer);
            Check(!SettingsStore.LoadMarathonCompareRewards(), "Compare must default to off.");
            var mods = AssetDatabase.FindAssets("t:RunModifier").Select(g =>
                AssetDatabase.LoadAssetAtPath<RunModifier>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m).ToArray();
            var buffs = mods.Where(m => m.isBuff).Cast<RunModifierSO>().ToArray();
            var debuffs = mods.Where(m => !m.isBuff).Cast<RunModifierSO>().ToArray();
            for (int i = 0; i < 500; i++)
            {
                var rolled = MarathonRewardUI.RollPairs(buffs, debuffs);
                Check(rolled.Length == 3 && rolled.Distinct().Count() == 3, "Each roll needs three distinct legal pairs.");
                foreach (var pair in rolled)
                    Check(!MarathonRewardUI.Conflicts(pair.buff, pair.debuff) && Mathf.Abs((int)MarathonRewardUI.Rarity(pair.buff) - (int)MarathonRewardUI.Rarity(pair.debuff)) <= 1, "Conflicting or mismatched pair.");
            }
            var attackUp = mods.First(m => m.isBuff && m.stat == RunModStat.MonsterDamageMult);
            var attackDown = mods.First(m => !m.isBuff && m.stat == RunModStat.MonsterDamageMult);
            Check(MarathonRewardUI.Conflicts(attackUp, attackDown), "Opposing attack rewards must conflict.");
            Check(MarathonRewardUI.RollPairs(new[] { attackUp }, new[] { attackDown }).Length == 0, "An invalid pool must not fall back to cancelling rewards.");

            var camera = new GameObject("Reward preview camera", typeof(Camera)).GetComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.scene = scene;
            camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.magenta; // Any uncovered screen edge is obvious.
            target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            var canvas = new GameObject("Reward preview canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var controller = new GameObject("Inactive preview stats"); controller.SetActive(false);
            SceneManager.MoveGameObjectToScene(controller, scene);
            var game = controller.AddComponent<GameController>();
            game.monsterDamageMult = game.monsterMaxHpMult = game.enemyProjectileDamageMult = 1;
            var audio = controller.AddComponent<AudioManager>();
            var contextField = typeof(AudioManager).GetField("context", Private);
            var poolField = typeof(AudioManager).GetField("currentPool", Private);
            var activeField = typeof(AudioManager).GetField("levelMusicActive", Private);
            var bossField = typeof(AudioManager).GetField("levelIsBoss", Private);
            contextField.SetValue(audio, Enum.Parse(contextField.FieldType, "Level"));
            activeField.SetValue(audio, true);
            var playlist = new AudioClip[0];
            poolField.SetValue(audio, playlist);
            audio.PlayLevelMusic(false, continueCurrentPlaylist: true);
            Check(ReferenceEquals(playlist, poolField.GetValue(audio)), "Normal Marathon levels must preserve the current playlist.");
            bossField.SetValue(audio, true);
            audio.PlayLevelMusic(true, continueCurrentPlaylist: true);
            Check(ReferenceEquals(playlist, poolField.GetValue(audio)), "An unchanged boss context must preserve the current playlist.");
            var reward = Object.Instantiate(AssetDatabase.LoadAssetAtPath<MarathonRewardUI>(MarathonRewardPresentationAuthoring.RewardPath), canvas.transform);
            Call(reward, "Awake");
            var pairs = new MarathonRewardUI.Pair[3];
            var rarities = new[] { RunModRarity.Common, RunModRarity.Uncommon, RunModRarity.Rare };
            for (int i = 0; i < 3; i++)
            {
                var buff = mods.First(m => m.isBuff && m.rarity == rarities[i] && m.stat == RunModStat.MonsterDamageMult);
                var debuff = mods.First(m => !m.isBuff && m.rarity == (RunModRarity)((int)rarities[i] + 1) && m.stat == RunModStat.MonsterMaxHpMult);
                pairs[i] = new MarathonRewardUI.Pair { buff = buff, debuff = debuff };
            }
            int confirmations = 0;
            reward.Show(2, pairs, (b, d) => confirmations++, game, true);
            for (int i = 0; i < reward.cards.Length; i++)
            {
                var card = reward.cards[i].transform;
                var good = card.Find("Buff Rarity Backing").GetComponent<Image>();
                var bad = card.Find("Debuff Rarity Backing").GetComponent<Image>();
                Check(good.color.a == 1 && bad.color.a == 1 && good.rectTransform.anchorMin.y == bad.rectTransform.anchorMax.y,
                    "Rarity backings must meet without transparent gaps.");
                Check(good.color != bad.color, "Mixed rarities need distinct section backings.");
                Check(good.color == reward.colors.GetRarity(MarathonRewardUI.Rarity(pairs[i].buff)) &&
                    bad.color == reward.colors.GetRarity(MarathonRewardUI.Rarity(pairs[i].debuff)), "Backings must use full-brightness rarity colors.");
                var header = (RectTransform)card.Find("Rarity Backing");
                var top = reward.buffBackgrounds[i].rectTransform;
                var bottom = reward.debuffBackgrounds[i].rectTransform;
                float headerGap = header.anchoredPosition.y + header.rect.yMin - top.anchoredPosition.y - top.rect.yMax;
                float middleGap = top.anchoredPosition.y + top.rect.yMin - bottom.anchoredPosition.y - bottom.rect.yMax;
                Check(headerGap > 0 && Mathf.Abs(headerGap - middleGap) < .1f, "Header and inter-section gaps must match.");
                foreach (var icon in new[] { reward.buffIcons[i], reward.debuffIcons[i] })
                {
                    var backing = icon.transform.parent.GetComponentInChildren<RewardIconBackdrop>(true);
                    Check(backing && backing.transform.GetSiblingIndex() > icon.transform.GetSiblingIndex(), "Icon animation must draw above the icon.");
                    Check(backing.plate && backing.plate.transform.GetSiblingIndex() < icon.transform.GetSiblingIndex(), "Only the opaque plate belongs behind the icon.");
                    Check(!icon.transform.parent.Find("Icon Border").gameObject.activeSelf, "Gold frame must not cover the rarity-colored frame.");
                }
            }
            Check(reward.title.text == "Commander Level 2 - Choose Modifier", "Reward title must identify the commander level.");
            Check(reward.choiceLabels.All(label => label.text.Contains("Buff:") && label.text.Contains("Debuff:")), "Mixed rarity labels must identify both tiers.");
            Check(!reward.compareAllToggle.isOn, "Checkbox must start off.");
            reward.cards[0].OnPointerEnter(null);
            Check(reward.cards[0].GetComponent<CanvasGroup>().alpha == 1f, "Hovered cards must be opaque.");
            Check(reward.previewPanels[0].gameObject.activeSelf && !reward.previewPanels[1].gameObject.activeSelf, "Hover must show only its own preview.");
            Check(reward.previewLabels[0].text.Contains("->"), "Preview must show numeric before/after values.");
            Check(game.monsterDamageMult == 1 && game.monsterMaxHpMult == 1, "Previews must not mutate stats.");
            reward.cards[0].OnPointerExit(null);
            Check(!reward.previewPanels[0].gameObject.activeSelf, "Leaving a card must hide its preview.");
            reward.compareAllToggle.isOn = true;
            Check(SettingsStore.LoadMarathonCompareRewards(), "Comparison setting must persist.");
            Check(reward.previewPanels.All(p => p.gameObject.activeSelf), "All previews must be visible for comparison.");
            Call(reward, "Select", 1);
            Check(Mathf.Approximately(reward.cards[0].GetComponent<CanvasGroup>().alpha, 215f / 255f), "Inactive cards must fade uniformly to 215/255 alpha.");
            Check(reward.cards[1].GetComponent<CanvasGroup>().alpha == 1, "Selected card must be opaque.");
            foreach (var effect in reward.cards[0].GetComponentsInChildren<RewardIconBackdrop>())
                Check(!(bool)typeof(RewardIconBackdrop).GetField("highlighted", Private).GetValue(effect), "Idle icons must not animate.");
            foreach (var effect in reward.cards[1].GetComponentsInChildren<RewardIconBackdrop>())
                Check((bool)typeof(RewardIconBackdrop).GetField("highlighted", Private).GetValue(effect), "Selected icons must animate.");
            Check(reward.cards[1].selectionAnimation.GetComponent<MarathonSelectionBorder>(), "Selection must use the new pixel animation.");
            Check(reward.choices.All(c => c.GetComponent<Image>().color.a == 0f), "Card centers must not tint transparent contents.");
            Check(reward.confirmButton.GetComponentInChildren<TMP_Text>().color == Color.black, "Confirm text must be black.");
            Canvas.ForceUpdateCanvases(); Call(reward, "LateUpdate");
            foreach (var border in reward.GetComponentsInChildren<MarathonSelectionBorder>())
            {
                border.SetAllDirty();
                border.Rebuild(CanvasUpdate.PreRender);
                var mesh = border.canvasRenderer.GetMesh();
                Check(mesh && mesh.vertexCount > 0, "Border must render visible geometry.");
            }
            Capture(camera, "Logs/MarathonRewards-Compare.png");
            Call(reward, "Confirm"); Call(reward, "Confirm");
            Check(confirmations == 1 && reward.gameObject.activeSelf && !reward.confirmButton.interactable,
                "Confirmation must fire once and keep the level-transition cover visible.");
            reward.Dismiss();
            reward.Show(3, pairs, null, game, true);
            Check(reward.GetComponentsInChildren<RewardIconBackdrop>(true).Length == 6, "Reopening must reuse the six icon backgrounds.");
            Check(reward.compareAllToggle.isOn && reward.previewPanels.All(p => p.gameObject.activeSelf), "Reopening must restore comparison preference.");
            reward.compareAllToggle.isOn = false;
            reward.cards[1].OnPointerEnter(null);
            Call(reward, "Select", 1);
            Check(reward.previewPanels[1].gameObject.activeSelf, "Hovered selection must retain its preview.");
            Capture(camera, "Logs/MarathonRewards-Hover.png");

            var popup = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BuffPopUpText_Prefab.prefab"), canvas.transform);
            var view = popup.GetComponent<BuffPopupView>();
            Check(view && view.GetComponent<CanvasGroup>(), "Reinforcement prefab needs a CanvasGroup.");
            var routine = (IEnumerator)Call(game, "FloatReinforcements", view, 1.5f, 30f);
            Check(routine.MoveNext() && view.GetComponent<CanvasGroup>().alpha == 1, "Reinforcement fade must start without a MissingComponentException.");
            var city = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Animations/BoardBorder/ModernCityBorder_Animation.prefab");
            Check(city.GetComponent<Image>().color == Color.black && !city.GetComponent<Image>().sprite, "City animation needs an opaque black backing.");
            Check(city.transform.Find("Animation").GetComponent<Animator>(), "City animated artwork must remain present.");
            var cityInstance = Object.Instantiate(city, canvas.transform);
            cityInstance.SetActive(true);
            var cityRect = (RectTransform)cityInstance.transform;
            var backingFit = cityInstance.GetComponent<BoardBorderBackingFit>();
            Check(backingFit && backingFit.artwork, "City backing must fit the artwork, excluding transparent padding.");
            foreach (var size in new[] { new Vector2(633.69f, 1080f), new Vector2(476.7201f, 858.61f), new Vector2(900f, 600f) })
            {
                cityRect.sizeDelta = size;
                Canvas.ForceUpdateCanvases();
                Rect bounds = backingFit.ArtworkBackingRect();
                var spriteSize = backingFit.artwork.overrideSprite.rect.size;
                float fittedWidth = Mathf.Min(size.x, size.y * spriteSize.x / spriteSize.y);
                Check(Mathf.Abs(bounds.width - fittedWidth / 3f) < .5f,
                    $"Backing must cover the central strip: expected {fittedWidth / 3f}, actual {bounds.width}.");
                Check(Mathf.Abs(bounds.center.x) < .5f, "Backing must remain centered behind the animation.");
            }
            Object.DestroyImmediate(cityInstance);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/MarathonRewardPresentation-validation.txt",
                "PASS: 500 legal reward rolls; same-stat exclusion; hover previews; no stat mutation; persisted comparison; alpha and selection geometry; black confirm; deferred one-shot confirmation; music playlist continuity; reinforcement CanvasGroup; city backing.\n");
        }
        finally
        {
            if (hadPreference) PlayerPrefs.SetInt(SettingsStore.KeyMarathonCompareRewards, previousPreference);
            else PlayerPrefs.DeleteKey(SettingsStore.KeyMarathonCompareRewards);
            PlayerPrefs.Save();
            UnityEngine.Random.state = randomState;
            inputSource.SetValue(null, previousSource);
            EditorSceneManager.ClosePreviewScene(scene);
            if (target) { target.Release(); Object.DestroyImmediate(target); }
        }
    }

    static void Capture(Camera camera, string path)
    {
        Canvas.ForceUpdateCanvases();
        var previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            camera.Render(); RenderTexture.active = camera.targetTexture;
            image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; if (image) Object.DestroyImmediate(image); }
    }
}
#endif
