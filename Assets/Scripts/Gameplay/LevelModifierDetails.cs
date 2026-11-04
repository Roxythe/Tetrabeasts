using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared by the slot reveal and the pause-menu collection.
public sealed class LevelModifierDetails : MonoBehaviour
{
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text titleShadow;
    [SerializeField] TMP_Text description;
    [SerializeField] Image icon;

    public void ShowModifier(LevelModifierSO modifier)
    {
        if (!modifier) return;
        string nameText = TetrabeastsLocalization.LocalizeText(modifier.displayName);
        LevelModifierSelectionUI.SetDynamicText(title, nameText, true);
        LevelModifierSelectionUI.SetDynamicText(titleShadow, nameText, true);
        LevelModifierSelectionUI.SetDynamicText(description, TetrabeastsLocalization.LocalizeText(modifier.description), true);
        icon.sprite = modifier.icon;
    }
}
