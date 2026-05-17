using System.Collections.Generic;
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

    [Header("Hidden Summary Row")]
    public bool showHiddenSummaryRow = true;
    public Sprite hiddenSummaryIcon; // Assign a gray question-mark sprite
    public string hiddenSummaryTitle = "Secret Achievements";

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

    struct Entry
    {
        public AchievementDefSO def;
        public bool unlocked;
        public int originalIndex;
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

        // Build entries and compute hidden counts
        List<Entry> entries = new List<Entry>(database.achievements.Count);

        int hiddenTotal = 0;
        int hiddenUnlocked = 0;

        for (int i = 0; i < database.achievements.Count; i++)
        {
            var def = database.achievements[i];
            if (!def) continue;

            bool unlocked = (PlayerProgress.I != null) && PlayerProgress.I.IsUnlocked(def.achievementId);

            if (def.hiddenUntilUnlocked)
            {
                hiddenTotal++;
                if (unlocked) hiddenUnlocked++;
            }

            // If hidden and locked, optionally exclude from list
            if (hideHiddenUntilUnlocked && def.hiddenUntilUnlocked && !unlocked)
                continue;

            entries.Add(new Entry
            {
                def = def,
                unlocked = unlocked,
                originalIndex = i
            });
        }

        // Sort unlocked first, then keep original database order within each group
        entries.Sort((a, b) =>
        {
            int unlockedCompare = b.unlocked.CompareTo(a.unlocked); // True first
            if (unlockedCompare != 0) return unlockedCompare;
            return a.originalIndex.CompareTo(b.originalIndex);
        });

        // Build rows
        foreach (var e in entries)
        {
            var row = Instantiate(rowPrefab, contentParent);
            row.Set(e.def, e.unlocked);
        }

        // Add secret summary row at the bottom
        if (showHiddenSummaryRow && hiddenTotal > 0)
        {
            int remaining = Mathf.Max(0, hiddenTotal - hiddenUnlocked);

            string desc = remaining == 1
                ? TetrabeastsLocalization.LocalizeText("1 secret achievement remaining")
                : TetrabeastsLocalization.LocalizeFormat("{0} secret achievements remaining", remaining);

            var row = Instantiate(rowPrefab, contentParent);

            // Locked-style visuals 
            row.SetCustom(hiddenSummaryIcon, hiddenSummaryTitle, desc, unlocked: false, showLockedOverlay: false);
        }
    }
}
