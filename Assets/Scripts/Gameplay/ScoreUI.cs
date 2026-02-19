using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    [Header("Combo")]
    public TMP_Text comboText;

    public void Set(int value)
    {
        if (scoreText) scoreText.text = $"Score: {value}";
    }

    public void SetCombo(int combo)
    {
        if (!comboText) return;

        if (combo <= 0)
        {
            comboText.text = "";
            comboText.gameObject.SetActive(false);
            return;
        }

        comboText.gameObject.SetActive(true);
        comboText.text = $"Combo x{combo}";
    }

    public void ClearCombo()
    {
        if (!comboText) return;
        comboText.text = "";
        comboText.gameObject.SetActive(false);
    }
}
