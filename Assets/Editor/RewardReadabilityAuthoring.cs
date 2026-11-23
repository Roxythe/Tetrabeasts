#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RewardReadabilityAuthoring
{
    [MenuItem("Tetrabeasts/Update Reward Backings and Icon Art")]
    public static void Apply()
    {
        string path = MarathonRewardPresentationAuthoring.RewardPath;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var ui = root.GetComponent<MarathonRewardUI>();
            for (int i = 0; i < ui.choices.Length; i++)
            {
                Color tint = ui.colors.GetRarity((RunModRarity)i);
                // Center the buff between the header bottom and the debuff top.
                var header = (RectTransform)ui.choices[i].transform.Find("Rarity Backing");
                var buff = ui.buffBackgrounds[i].rectTransform;
                var debuff = ui.debuffBackgrounds[i].rectTransform;
                float headerBottom = header.anchoredPosition.y + header.rect.yMin;
                float debuffTop = debuff.anchoredPosition.y + debuff.rect.yMax;
                buff.anchoredPosition = new Vector2(buff.anchoredPosition.x, (headerBottom + debuffTop) * .5f);
                ui.ConfigureCardArt(i, tint, tint);
                foreach (var effect in ui.choices[i].GetComponentsInChildren<RewardIconBackdrop>(true)) effect.rarity = (RunModRarity)i;
                ui.cards[i].ResetState();
                ui.buffTitles[i].color = ui.colors.buffTitle; ui.buffLabels[i].color = ui.colors.buffDescription;
                ui.debuffTitles[i].color = ui.colors.debuffTitle; ui.debuffLabels[i].color = ui.colors.debuffDescription;
            }
            foreach (var shadow in root.GetComponentsInChildren<TMPLabelShadow>(true)) shadow.Sync();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
