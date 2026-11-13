#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ActiveSquadTests
{
    [Test]
    public void CompletionIsCommanderSpecificStandardOnlyAndSurvivesReload()
    {
        LocalSaveTestScope.Begin();
        System.IO.Directory.CreateDirectory(LocalSaveFiles.DirectoryPath);
        var singleton = typeof(PlayerProgress).GetProperty("I");
        var previous = PlayerProgress.I;
        var oldMode = GameModeStore.Current;
        var go = new GameObject("Isolated completion progress"); go.SetActive(false);
        var progress = go.AddComponent<PlayerProgress>();
        var first = ScriptableObject.CreateInstance<PlayerCharacterData>(); first.name = "First test commander";
        var second = ScriptableObject.CreateInstance<PlayerCharacterData>(); second.name = "Second test commander";
        singleton.SetValue(null, progress);
        try
        {
            foreach (var mode in new[] { RunGameMode.Chaos, RunGameMode.Marathon })
            {
                GameModeStore.Current = mode;
                GameModeStore.MarkFinalLevelReached(first);
                Assert.IsFalse(GameModeStore.HasStandardCompletion(first));
            }
            GameModeStore.Current = RunGameMode.Standard;
            GameModeStore.MarkFinalLevelReached();
            Assert.IsFalse(GameModeStore.HasStandardCompletion(first), "A generic historical unlock cannot identify a commander.");
            GameModeStore.MarkFinalLevelReached(first);
            GameModeStore.MarkFinalLevelReached(first);
            Assert.IsTrue(GameModeStore.HasStandardCompletion(first));
            Assert.IsFalse(GameModeStore.HasStandardCompletion(second));
            Assert.AreEqual(1, progress.GetLifetimeInt("lt_standard_final_level_char_" + first.name));
            typeof(PlayerProgress).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(progress, null);
            Assert.IsTrue(GameModeStore.HasStandardCompletion(first));
            progress.AddLifetimeInt("lt_final_win_char_" + second.name, 1);
            Assert.IsTrue(GameModeStore.HasStandardCompletion(second), "Older commander-specific victories retain their star.");
        }
        finally
        {
            Object.DestroyImmediate(go); singleton.SetValue(null, previous);
            Object.DestroyImmediate(first); Object.DestroyImmediate(second);
            GameModeStore.Current = oldMode; LocalSaveTestScope.End();
        }
    }

    [Test]
    public void AuthoredBadgesAndSquadLabelsRenderAndBind()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1920, 1080, 24);
        try
        {
            var camera = new GameObject("Squad preview camera", typeof(Camera)).GetComponent<Camera>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.targetTexture = texture; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.09f, .08f, .07f);
            var canvas = new GameObject("Squad preview", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MiniMonsterIcon_Prefab.prefab");
            string[] names = { "Ram", "Rem", "Bulrog", "Celest" };
            for (int i = 0; i < names.Length; i++)
            {
                MonsterData monster = null;
                foreach (string id in AssetDatabase.FindAssets(names[i] + " t:MonsterData"))
                {
                    var candidate = AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(id));
                    if (candidate.monsterName == names[i]) { monster = candidate; break; }
                }
                Assert.IsNotNull(monster);
                var go = Object.Instantiate(unitPrefab, canvas.transform);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(-330 + i * 220, 220); rect.localScale = Vector3.one * 2;
                var view = go.GetComponent<MonsterIconUIRefs>(); view.BindLabels(monster);
                view.iconImage.sprite = monster.portrait;
                Assert.IsNotNull(view.nameText.GetComponent<TMPLabelShadow>().shadow);
                Assert.AreEqual(Color.black, view.nameText.GetComponent<TMPLabelShadow>().shadow.color);
                Assert.IsTrue(view.levelText.text.Contains(MonsterProgressStore.GetPermanentLevel(monster.monsterName).ToString()));
            }
            string[] prefabs = { "Assets/Prefabs/MiniCommanderIcon_Prefab.prefab", "Assets/Prefabs/Buttons/CharacterButton_Prefab.prefab" };
            for (int i = 0; i < prefabs.Length; i++)
            {
                var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]), canvas.transform);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(i == 0 ? -220 : 220, -170);
                rect.localScale = Vector3.one * (i == 0 ? 2 : 1.2f);
                var badge = go.GetComponent<CommanderCompletionBadge>();
                Assert.IsNotNull(badge.star.sprite); Assert.IsFalse(badge.star.raycastTarget);
                badge.Bind(null); Assert.IsFalse(badge.star.gameObject.activeSelf);
                badge.star.gameObject.SetActive(true); // Preview appearance without awarding progress.
            }
            GameplayPlaylistTests.Capture(camera, "Active-Squad-Labels-And-Stars");
            camera.targetTexture = null;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(texture); }
    }
}
#endif
