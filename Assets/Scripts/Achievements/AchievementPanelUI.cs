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
        // Ensure progress exists during play mode
        if (Application.isPlaying && PlayerProgress.I == null)
        {
            var go = new GameObject("PlayerProgress");
            go.AddComponent<PlayerProgress>();
        }

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
        Debug.Log($"[AchievementPanelUI] defs={database?.achievements?.Count ?? 0} contentChildren={contentParent.childCount}");

        if (!database || !contentParent || !rowPrefab)
            return;

        database.BuildLookup();

        // Clear
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        foreach (var def in database.achievements)
        {
            if (!def) continue;

            bool unlocked = (PlayerProgress.I != null) && PlayerProgress.I.IsUnlocked(def.achievementId);

            if (hideHiddenUntilUnlocked && def.hiddenUntilUnlocked && !unlocked)
                continue;

            var row = Instantiate(rowPrefab, contentParent);
            row.Set(def, unlocked);
        }
    }
}