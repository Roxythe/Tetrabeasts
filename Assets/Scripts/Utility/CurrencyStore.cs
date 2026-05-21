using UnityEngine;

public static class CurrencyStore
{
    const string KEY = "CURRENCY_TOTAL";

    public static int Total
    {
        get => PlayerPrefs.GetInt(KEY, 0);
        set
        {
            PlayerPrefs.SetInt(KEY, Mathf.Max(0, value));
            PlayerPrefs.Save();
            SteamCloudSaveService.QueueUpload();
        }
    }

    public static void Add(int amount)
    {
        if (amount == 0) return;
        Total = Total + amount;
    }
}
