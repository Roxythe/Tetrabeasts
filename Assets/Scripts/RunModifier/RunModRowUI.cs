using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunModRowUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text nameShadowText;
    public TMP_Text descText;
    public TMP_Text descShadowText;

    [Header("Optional rarity colors (copy from RunModOptionButton if you want)")]
    public Color commonNameColor = new Color(0.85f, 0.85f, 0.85f);
    public Color uncommonNameColor = Color.green;
    public Color rareNameColor = new Color(0.3f, 0.6f, 1f);
    public Color epicNameColor = new Color(0.75f, 0.3f, 1f);
    public Color legendaryNameColor = new Color(1f, 0.75f, 0.2f);

    public Color commonDescColor = new Color(0.85f, 0.85f, 0.85f);
    public Color uncommonDescColor = Color.green;
    public Color rareDescColor = new Color(0.3f, 0.6f, 1f);
    public Color epicDescColor = new Color(0.75f, 0.3f, 1f);
    public Color legendaryDescColor = new Color(1f, 0.75f, 0.2f);

    public void Bind(RunModifierSO mod) => Bind(mod, 1);

    public void Bind(RunModifierSO mod, int copies)
    {
        if (!mod) return;

        copies = Mathf.Max(1, copies);

        if (icon) icon.sprite = mod.icon;

        string displayName = TetrabeastsLocalization.LocalizeText(mod.displayName);
        string description = TetrabeastsLocalization.LocalizeText(mod.description);

        if (nameText)
        {
            nameText.text = copies > 1 ? $"{displayName} x{copies}" : displayName;
        }

        if (nameShadowText)
        {
            nameShadowText.text = nameText ? nameText.text : displayName;
        }

        if (descText)
        {
            descText.text = description;
        }

        if (descShadowText)
        {
            descShadowText.text = descText ? descText.text : description;
        }

        // Color by rarity if it's a generic mod, otherwise use common colors
        if (mod is RunModifier generic)
        {
            var nameCol = GetNameColor(generic.rarity);
            var descCol = GetDescColor(generic.rarity);
            if (nameText) nameText.color = nameCol;
            if (descText) descText.color = descCol;
        }
        else
        {
            if (nameText) nameText.color = commonNameColor;
            if (descText) descText.color = commonDescColor;
        }
    }

    Color GetNameColor(RunModRarity r)
    {
        switch (r)
        {
            case RunModRarity.Uncommon: return uncommonNameColor;
            case RunModRarity.Rare: return rareNameColor;
            case RunModRarity.Epic: return epicNameColor;
            case RunModRarity.Legendary: return legendaryNameColor;
            default: return commonNameColor;
        }
    }

    Color GetDescColor(RunModRarity r)
    {
        switch (r)
        {
            case RunModRarity.Uncommon: return uncommonDescColor;
            case RunModRarity.Rare: return rareDescColor;
            case RunModRarity.Epic: return epicDescColor;
            case RunModRarity.Legendary: return legendaryDescColor;
            default: return commonDescColor;
        }
    }
}
