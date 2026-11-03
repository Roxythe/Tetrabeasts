#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SettingsPrefabAuthoring
{
    [MenuItem("Tetrabeasts/Build Editable Settings Tabs")]
    public static void Apply()
    {
        const string path = "Assets/Prefabs/Panels/Volume_Panel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var panel = root.GetComponent<VolumePanelUI>();
            var library = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab").GetComponent<AudioManager>();
            panel.BuildEditableSettings(library);
            var hover = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ButtonHoverUI_SFX.wav");
            var click = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ButtonPressedUI_SFX.wav");
            foreach (var feedback in root.GetComponentsInChildren<UIButtonSFX>(true))
            {
                if (!feedback.hoverClip) feedback.hoverClip = hover;
                if (!feedback.clickClip) feedback.clickClip = click;
            }
            EditorUtility.SetDirty(panel);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("Editable General, Playlist and Video tabs saved in Volume_Panel.prefab.");
    }
}
#endif
