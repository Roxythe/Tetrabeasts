using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonsterIconUIRefs : MonoBehaviour
{
    public Image iconImage;
    public Image roleBackgroundImage;
    public TMP_Text nameText;
    public TMP_Text levelText;

    public void BindLabels(MonsterData monster)
    {
        if (nameText) nameText.text = monster ? TetrabeastsLocalization.LocalizeText(monster.monsterName) : string.Empty;
        if (levelText) levelText.text = monster
            ? TetrabeastsLocalization.LocalizeFormat("Lv. {0}", MonsterProgressStore.GetPermanentLevel(monster.monsterName))
            : string.Empty;
        if (nameText) nameText.GetComponent<TMPLabelShadow>()?.Sync();
        if (levelText) levelText.GetComponent<TMPLabelShadow>()?.Sync();
    }
}
