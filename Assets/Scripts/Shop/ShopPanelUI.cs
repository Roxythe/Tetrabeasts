using UnityEngine;

public class ShopPanelUI : MonoBehaviour
{
    [Header("Optional")]
    public CurrencyUI currencyUI;

    [Header("Entries")]
    public ShopBuffEntryUI[] entries;

    private void OnEnable()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        currencyUI?.Refresh();

        if (entries == null) return;
        foreach (var e in entries)
            if (e) e.Refresh();
    }
}