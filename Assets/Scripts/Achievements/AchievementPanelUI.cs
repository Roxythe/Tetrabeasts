using UnityEngine;

public class AchievementPanelUI : MonoBehaviour
{
    [Header("Data")]
    public AchievementDatabaseSO database;

    [Header("List")]
    public Transform contentParent;         // ScrollView Content
    public AchievementRowUI rowPrefab;

    [Header("Optional")]
    public bool hideHiddenUntilUnlocked = true;

    void OnEnable()
    {
        Rebuild();

        if (PlayerProgress.I != null)
            PlayerProgress.I.AchievementUnlocked += OnUnlocked;
    }

    void OnDisable()
    {
        if (PlayerProgress.I != null)
            PlayerProgress.I.AchievementUnlocked -= OnUnlocked;
    }

    void OnUnlocked(string id)
    {
        Rebuild();
    }

    public void Rebuild()
    {
        if (!database || !contentParent || !rowPrefab || PlayerProgress.I == null)
            return;

        database.BuildLookup();

        // Clear
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        foreach (var def in database.achievements)
        {
            if (!def) continue;

            bool unlocked = PlayerProgress.I.IsUnlocked(def.achievementId);

            if (hideHiddenUntilUnlocked && def.hiddenUntilUnlocked && !unlocked)
                continue;

            var row = Instantiate(rowPrefab, contentParent);
            row.Set(def, unlocked);
        }
    }
}