using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    [Header("Optional")]
    public CurrencyUI currencyUI;

    [Header("Entries")]
    public ShopBuffEntryUI[] entries;

    [Header("Buttons")]
    public Button refundAllButton;

    private void OnEnable()
    {
        if (refundAllButton)
        {
            refundAllButton.onClick.RemoveAllListeners();
            refundAllButton.onClick.AddListener(RefundAllUpgrades);
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        currencyUI?.Refresh();

        if (refundAllButton)
            refundAllButton.interactable = ShopBuffStore.GetTotalRefundAll() > 0;

        if (entries == null) return;
        foreach (var e in entries)
            if (e) e.Refresh();
    }

    public void RefundAllUpgrades()
    {
        int refund = ShopBuffStore.GetTotalRefundAll(); // Calculate refund before resetting levels
        ShopBuffStore.ResetAllToZero(); // Reset levels so buffs no longer apply

        if (refund > 0)
            CurrencyStore.Add(refund);

        // Clear any cached/recorded shop-level stats 
        if (PlayerProgress.I != null)
        {
            const string lifetimePrefix = "lt_shop_level_";
            foreach (var t in ShopBuffStore.AllTypes)
            {
                string key = lifetimePrefix + t;
                PlayerProgress.I.SetLifetimeBestInt(key, 0);
            }
        }

        RefreshAll();
    }
}