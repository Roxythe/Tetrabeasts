using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RoundTransitionSceneSetup
{
    static readonly string[] GameplayScenePaths =
    {
        "Assets/Scenes/GameplayScene.unity",
        "Assets/Scenes/TestGameplayScene.unity"
    };

    [MenuItem("Tools/Tetrabeasts/Create Round Transition UI In Gameplay Scenes")]
    public static void CreateRoundTransitionUIInGameplayScenes()
    {
        foreach (string scenePath in GameplayScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var controllers = Object.FindObjectsByType<GameController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var controller in controllers)
            {
                var serializedController = new SerializedObject(controller);
                var uiProperty = serializedController.FindProperty("roundTransitionUI");
                var fontProperty = serializedController.FindProperty("roundTransitionFont");
                var buttonProperty = serializedController.FindProperty("roundTransitionContinueButtonPrefab");

                var font = fontProperty?.objectReferenceValue as TMP_FontAsset;
                var buttonPrefab = buttonProperty?.objectReferenceValue as Button;
                var ui = uiProperty?.objectReferenceValue as RoundTransitionUI;

                if (!ui)
                    ui = Object.FindFirstObjectByType<RoundTransitionUI>(FindObjectsInactive.Include);

                if (!ui)
                {
                    ui = RoundTransitionUI.CreateRuntimeInstance(font, buttonPrefab);
                    ui.gameObject.name = "RoundTransitionUI";
                    Undo.RegisterCreatedObjectUndo(ui.gameObject, "Create Round Transition UI");
                }

                ui.Configure(font, buttonPrefab);
                ui.BuildMissingUiForInspector();

                if (uiProperty != null)
                {
                    uiProperty.objectReferenceValue = ui;
                    serializedController.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(controller);
                }

                EditorUtility.SetDirty(ui);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
