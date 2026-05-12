using UnityEngine;

public static class DemoBuildGuardRails
{
    public const int DefaultMaxCompletedLevel = 6;

    const string DefaultPurchaseBlockedMessage =
        "Purchases are disabled in the demo. Your earned progress will still carry into the full game.";

    public static bool IsDemoBuild { get; private set; }
    public static int MaxCompletedLevel { get; private set; } = DefaultMaxCompletedLevel;
    public static string PurchaseBlockedMessage { get; private set; } = DefaultPurchaseBlockedMessage;

    public static bool ShouldDeferAchievementUnlocks => IsDemoBuild;

    public static void Configure(bool enabled, int maxCompletedLevel = DefaultMaxCompletedLevel, string purchaseBlockedMessage = null)
    {
        IsDemoBuild = enabled;
        MaxCompletedLevel = Mathf.Max(1, maxCompletedLevel);
        PurchaseBlockedMessage = string.IsNullOrWhiteSpace(purchaseBlockedMessage)
            ? DefaultPurchaseBlockedMessage
            : purchaseBlockedMessage;
    }

    public static bool HasReachedLevelLimit(int completedLevelNumber)
    {
        return IsDemoBuild && completedLevelNumber >= MaxCompletedLevel;
    }

    public static bool TryBlockPurchase(AudioClip errorSFX = null)
    {
        if (!IsDemoBuild)
            return false;

        if (AudioManager.I && errorSFX)
            AudioManager.I.PlaySFX(errorSFX);

        var popup = ConfirmationPopupUI.FindOrCreate();
        if (popup)
        {
            popup.ShowAlert(PurchaseBlockedMessage, continueText: "OK", showWarningVisual: true);
        }
        else
        {
            Debug.LogWarning(PurchaseBlockedMessage);
        }

        return true;
    }
}
