using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBuffEntryUI : MonoBehaviour
{
    public ShopBuffType buffType;

    public TMP_Text currentLevelText;
    public TMP_Text costText;
    public Button levelUpButton;
    public ShopPanelUI shopPanel;

    [Header("SFX")]
    public AudioClip successSFX;
    public AudioClip errorSFX;

    const string K_RunPurchasedAnyShopUpgrade = AchievementSystem.Stat.RunPurchasedAnyShopUpgrade;
    const string K_LifetimeShopLevelPrefix = "lt_shop_level_"; // + buffType

    private void OnEnable()
    {
        if (levelUpButton)
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(TryPurchase);
        }

        Refresh();
    }

    public void Refresh()
    {
        int level = ShopBuffStore.GetLevel(buffType);
        int cost = ShopBuffStore.GetNextCost(buffType);

        if (currentLevelText)
            currentLevelText.text = $"Current Level: {level}";

        if (costText)
            costText.text = $" x{cost}";

        if (levelUpButton)
            levelUpButton.interactable = CurrencyStore.Total >= cost;
    }

    void TryPurchase()
    {
        int cost = ShopBuffStore.GetNextCost(buffType);

        // If currency is insufficient
        if (CurrencyStore.Total < cost)
        {
            if (AudioManager.I && errorSFX)
                AudioManager.I.PlaySFX(errorSFX);

            // Ensure interactable/values are correct
            shopPanel?.RefreshAll();
            Refresh();
            return;
        }

        int beforeLevel = ShopBuffStore.GetLevel(buffType); // Capture pre-level to detect actual level change
        CurrencyStore.Add(-cost); // Deduct currency

        if (AudioManager.I && successSFX)
            AudioManager.I.PlaySFX(successSFX);

        ShopBuffStore.Increment(buffType);

        int afterLevel = ShopBuffStore.GetLevel(buffType);

        // Stats/Achievements: Purchase any shop upgrade and reach upgrade level X
        if (PlayerProgress.I != null && afterLevel > beforeLevel)
        {
            PlayerProgress.I.AddRunInt(K_RunPurchasedAnyShopUpgrade, 1);
            string levelKey = K_LifetimeShopLevelPrefix + buffType;
            PlayerProgress.I.SetLifetimeBestInt(levelKey, afterLevel);
        }

        shopPanel?.RefreshAll();
        Refresh();
    }
}