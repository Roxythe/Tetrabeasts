using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyUI : MonoBehaviour
{
    [Header("Wiring")]
    public Image icon;           // your coin image
    public TMP_Text amountText;  // the text next to it, e.g., "X 123"

    public void Refresh()
    {
        if (amountText) amountText.text = $"X {CurrencyStore.Total}";
    }

    void OnEnable() => Refresh();
}
