using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    [Header("Combo")]
    public TMP_Text comboText;
    public TMP_Text staticComboText;
    [SerializeField] private ComboPulse comboPulse;
    [SerializeField] private ComboFireFX comboFireFX;
    private int _lastCombo;

    public void Set(int value)
    {
        if (scoreText) scoreText.text = $"Score: {value}";
    }

    public void SetCombo(int combo)
    {
        if (!comboText) return;

        bool increased = combo > _lastCombo;
        _lastCombo = combo;

        if (combo <= 0)
        {
            staticComboText.gameObject.SetActive(true);
            comboText.gameObject.SetActive(true);
            comboText.text = "x0";

            if (comboFireFX) comboFireFX.SetCombo(0);
            return;
        }

        staticComboText.gameObject.SetActive(true);
        comboText.gameObject.SetActive(true);
        comboText.text = $"x{combo}";

        if (increased && comboPulse) comboPulse.Pulse();
        if (comboFireFX) comboFireFX.SetCombo(combo);
    }

    public void ClearCombo()
    {
        _lastCombo = 0;

        if (!comboText) return;

        staticComboText.gameObject.SetActive(true);
        comboText.gameObject.SetActive(true);
        comboText.text = "x0";

        if (comboFireFX) comboFireFX.SetCombo(0);
    }
}
