using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SlotMachineLightPrefabFactory
{
    const string SpritePath = "Assets/Sprites/LevelModifier/SlotMachineLight.png";
    const string PrefabDirectory = "Assets/Prefabs/LevelModifier";
    const string PrefabPath = PrefabDirectory + "/SlotMachineLight_Prefab.prefab";

    [MenuItem("Tools/Tetrabeasts/Create Slot Machine Light Prefab")]
    public static void CreatePrefab()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (!sprite)
        {
            Debug.LogError($"SlotMachineLightPrefabFactory: Could not find sprite at {SpritePath}.");
            return;
        }

        if (!Directory.Exists(PrefabDirectory))
            Directory.CreateDirectory(PrefabDirectory);

        var go = new GameObject("SlotMachineLight_Prefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SlotMachineLightUI));

        try
        {
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(36f, 36f);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"Created slot machine light prefab at {PrefabPath}.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
