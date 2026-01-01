using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunModOptionButton : MonoBehaviour
{
    public Button button;
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;
    public Outline highlightOutline;

    [Header("Rarity Text Colors")]
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


    RunModifierSO _mod;

    public void Bind(RunModifierSO mod, Action<RunModifierSO> onSelected)
    {
        _mod = mod;
        if (icon) icon.sprite = mod.icon;
        if (nameText) nameText.text = mod.displayName;
        if (descText) descText.text = mod.description;

        // Rarity-based coloring
        if (mod is RunModifier generic)
        {
            var nameCol = GetNameColor(generic.rarity);
            var descCol = GetDescColor(generic.rarity);

            if (nameText) nameText.color = nameCol;
            if (descText) descText.color = descCol;
        }
        else
        {
            // fallback
            if (nameText) nameText.color = commonNameColor;
            if (descText) descText.color = commonDescColor;
        }

        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(_mod));
    }

    public void SetSelected(bool selected)
    {
        if (highlightOutline) highlightOutline.enabled = selected;
    }

    // Helper accessor methods for rarity-based colors
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
