using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunModRowUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;

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

    public void Bind(RunModifierSO mod)
    {
        if (!mod) return;

        if (icon) icon.sprite = mod.icon;
        if (nameText) nameText.text = mod.displayName;
        if (descText) descText.text = mod.description;

        // Optional: copy the rarity coloring behavior you already use
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
