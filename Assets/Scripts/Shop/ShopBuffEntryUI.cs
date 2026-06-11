using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopBuffEntryUI : MonoBehaviour
{
    public ShopBuffType buffType;

    public TMP_Text currentLevelText;
    public TMP_Text costText;
    public Button levelUpButton;
    public ShopPanelUI shopPanel;

    [Header("Hover Tooltip")]
    [TextArea(2, 5)]
    public string hoverDescription;

    [Header("SFX")]
    public AudioClip successSFX;
    public AudioClip errorSFX;

    const string K_RunPurchasedAnyShopUpgrade = AchievementSystem.Stat.RunPurchasedAnyShopUpgrade;
    const string K_LifetimeShopLevelPrefix = "lt_shop_level_"; // + buffType

    RectTransform _levelUpButtonRect;
    RectTransform _shownTooltipTarget;
    bool _levelUpPointerInside;
    bool _levelUpSelected;

    private void OnEnable()
    {
        ConfigureLevelUpTooltipTarget();

        if (levelUpButton)
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(TryPurchase);
        }

        Refresh();
    }

    void OnDisable()
    {
        ClearTooltipState();
    }

    void LateUpdate()
    {
        SyncLevelUpSelectionState();
    }

    public string GetHoverDescription()
    {
        if (!string.IsNullOrWhiteSpace(hoverDescription))
            return hoverDescription;

        return GetDefaultHoverDescription(buffType);
    }

    public void Refresh()
    {
        int level = ShopBuffStore.GetLevel(buffType);
        int cost = ShopBuffStore.GetNextCost(buffType);

        if (currentLevelText)
            currentLevelText.text = TetrabeastsLocalization.LocalizeFormat("Current Level: {0}", level);

        if (costText)
            costText.text = TetrabeastsLocalization.LocalizeFormat(" x{0}", cost);

        if (levelUpButton)
            levelUpButton.interactable = CurrencyStore.Total >= cost;
    }

    void TryPurchase()
    {
        if (DemoBuildGuardRails.TryBlockPurchase(errorSFX))
        {
            shopPanel?.RefreshAll();
            Refresh();
            return;
        }

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
            if (afterLevel >= 5 && PlayerProgress.I.GetLifetimeInt(AchievementSystem.Stat.AnyShopUpgradeReached5) == 0)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.AnyShopUpgradeReached5, 1);

            PlayerProgress.I.AddRunInt(K_RunPurchasedAnyShopUpgrade, 1);
            string levelKey = K_LifetimeShopLevelPrefix + buffType;
            PlayerProgress.I.SetLifetimeBestInt(levelKey, afterLevel);
        }

        shopPanel?.RefreshAll();
        Refresh();
    }

    internal void NotifyLevelUpPointerEnter()
    {
        _levelUpPointerInside = true;
        RefreshTooltipVisibility();
    }

    internal void NotifyLevelUpPointerMove()
    {
        _levelUpPointerInside = true;
        RefreshTooltipVisibility();
    }

    internal void NotifyLevelUpPointerExit()
    {
        _levelUpPointerInside = false;
        RefreshTooltipVisibility();
    }

    internal void NotifyLevelUpSelected()
    {
        _levelUpSelected = true;
        RefreshTooltipVisibility();
    }

    internal void NotifyLevelUpDeselected()
    {
        _levelUpSelected = false;
        RefreshTooltipVisibility();
    }

    void ConfigureLevelUpTooltipTarget()
    {
        if (!levelUpButton)
        {
            _levelUpButtonRect = null;
            return;
        }

        _levelUpButtonRect = levelUpButton.transform as RectTransform;

        var target = levelUpButton.GetComponent<ShopBuffEntryTooltipTarget>();
        if (!target)
            target = levelUpButton.gameObject.AddComponent<ShopBuffEntryTooltipTarget>();

        target.Initialize(this);
        SyncLevelUpSelectionState();
    }

    void SyncLevelUpSelectionState()
    {
        bool selected = levelUpButton &&
            EventSystem.current &&
            EventSystem.current.currentSelectedGameObject == levelUpButton.gameObject;

        if (_levelUpSelected == selected)
            return;

        _levelUpSelected = selected;
        RefreshTooltipVisibility();
    }

    void RefreshTooltipVisibility()
    {
        if (!shopPanel)
            shopPanel = GetComponentInParent<ShopPanelUI>(true);

        bool shouldShow = _levelUpPointerInside ||
            _levelUpSelected;

        if (!shouldShow)
        {
            HideShownTooltip();
            return;
        }

        if (!_levelUpButtonRect)
            return;

        _shownTooltipTarget = _levelUpButtonRect;
        shopPanel?.ShowTooltip(this, _levelUpButtonRect);
    }

    void ClearTooltipState()
    {
        _levelUpPointerInside = false;
        _levelUpSelected = false;
        HideShownTooltip();
    }

    void HideShownTooltip()
    {
        if (shopPanel && _shownTooltipTarget)
            shopPanel.HideTooltipFor(_shownTooltipTarget);

        _shownTooltipTarget = null;
    }

    static string GetDefaultHoverDescription(ShopBuffType type)
    {
        switch (type)
        {
            case ShopBuffType.LuckUp:
                return "Increase luck, improving favorable random outcomes during runs.";
            case ShopBuffType.GravityDown:
                return "Reduce the starting gravity speed of falling pieces.";
            case ShopBuffType.VelocityDown:
                return "Reduce how quickly gravity ramps up during a level.";
            case ShopBuffType.GoldUp:
                return "Increase the chance to earn gold from cleared rows.";
            case ShopBuffType.AttackUp:
                return "Increase monster attack power.";
            case ShopBuffType.HpUp:
                return "Increase monster maximum HP.";
            case ShopBuffType.HealPower:
                return "Increase monster healing power.";
            case ShopBuffType.UnitLivesUp:
                return "Increase starting unit reserves.";
            default:
                return string.Empty;
        }
    }
}

[DisallowMultipleComponent]
class ShopBuffEntryTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    ShopBuffEntryUI owner;

    public void Initialize(ShopBuffEntryUI entry)
    {
        owner = entry;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.NotifyLevelUpPointerEnter();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        owner?.NotifyLevelUpPointerMove();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.NotifyLevelUpPointerExit();
    }

    public void OnSelect(BaseEventData eventData)
    {
        owner?.NotifyLevelUpSelected();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        owner?.NotifyLevelUpDeselected();
    }
}
