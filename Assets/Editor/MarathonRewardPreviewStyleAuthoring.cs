#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MarathonRewardPreviewStyleAuthoring
{
    [MenuItem("Tetrabeasts/Match Marathon Reward Preview Style")]
    public static void Apply()
    {
        var root = PrefabUtility.LoadPrefabContents(MarathonRewardPresentationAuthoring.RewardPath);
        try
        {
            MatchStandardStyle(root.GetComponent<MarathonRewardUI>());
            PrefabUtility.SaveAsPrefabAsset(root, MarathonRewardPresentationAuthoring.RewardPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void MatchStandardStyle(MarathonRewardUI target)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameplayScene.unity");
        try
        {
            var standard = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RoundRewardUI>(true)).First();
            var serialized = new SerializedObject(standard);
            var sourceImage = ((GameObject)serialized.FindProperty("previewBox").objectReferenceValue).GetComponent<Image>();
            var sourceText = (TMP_Text)serialized.FindProperty("previewText").objectReferenceValue;
            target.previewBuffColor = ((TMP_Text)serialized.FindProperty("buffTitleText").objectReferenceValue).color;
            target.previewDebuffColor = ((TMP_Text)serialized.FindProperty("debuffTitleText").objectReferenceValue).color;
            for (int i = 0; i < target.previewPanels.Length; i++)
            {
                var panel = target.previewPanels[i];
                var background = panel.GetComponent<Image>();
                background.sprite = sourceImage.sprite;
                background.material = sourceImage.material;
                background.color = sourceImage.color;
                background.type = sourceImage.type;
                background.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
                background.fillCenter = sourceImage.fillCenter;
                background.preserveAspect = sourceImage.preserveAspect;
                background.raycastTarget = false;
                foreach (var outline in panel.GetComponents<Outline>()) Object.DestroyImmediate(outline);
                var text = target.previewLabels[i];
                text.font = sourceText.font;
                text.fontSharedMaterial = sourceText.fontSharedMaterial;
                text.fontStyle = sourceText.fontStyle;
                text.color = sourceText.color;
                text.alignment = sourceText.alignment;
                text.richText = true;
                text.enableVertexGradient = sourceText.enableVertexGradient;
                text.enableAutoSizing = true;
                text.fontSizeMin = 18;
                text.fontSizeMax = text.fontSize = sourceText.fontSize;
                text.margin = sourceText.margin;
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(16, 16);
                text.rectTransform.offsetMax = new Vector2(-16, -16);
                text.raycastTarget = false;
                var shadow = text.GetComponent<TMPLabelShadow>() ?? text.gameObject.AddComponent<TMPLabelShadow>();
                if (!shadow.shadow)
                {
                    shadow.shadow = new GameObject("Stat Changes Shadow", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                    shadow.shadow.transform.SetParent(panel, false);
                }
                var sourceShadow = sourceText.GetComponent<TMPLabelShadow>();
                shadow.shadow.fontSharedMaterial = sourceShadow && sourceShadow.shadow ? sourceShadow.shadow.fontSharedMaterial : sourceText.fontSharedMaterial;
                var rect = shadow.shadow.rectTransform;
                rect.anchorMin = text.rectTransform.anchorMin; rect.anchorMax = text.rectTransform.anchorMax;
                rect.pivot = text.rectTransform.pivot; rect.sizeDelta = text.rectTransform.sizeDelta;
                rect.anchoredPosition = text.rectTransform.anchoredPosition + (sourceShadow && sourceShadow.shadow
                    ? sourceShadow.shadow.rectTransform.anchoredPosition - sourceText.rectTransform.anchoredPosition : new Vector2(2, -2));
                rect.localScale = Vector3.one;
                rect.SetSiblingIndex(text.transform.GetSiblingIndex() -
                    (rect.GetSiblingIndex() < text.transform.GetSiblingIndex() ? 1 : 0));
                shadow.Sync();
                if (background.sprite != sourceImage.sprite || text.font != sourceText.font || text.color != sourceText.color ||
                    rect.GetSiblingIndex() >= text.transform.GetSiblingIndex())
                    throw new InvalidOperationException("Marathon preview must match the Standard/Chaos style.");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
#endif
