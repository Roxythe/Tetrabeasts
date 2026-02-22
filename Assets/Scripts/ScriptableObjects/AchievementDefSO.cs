using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Achievements/Achievement Definition")]
public class AchievementDefSO : ScriptableObject
{
    [Header("IDs")]
    public string achievementId;     // Must match AchievementSystem.Ach.* (stable forever)
    public string steamApiName;      // Steamworks achievement API name

    [Header("Display")]
    public string title;
    [TextArea(2, 6)]
    public string description;
    public Sprite icon;

    [Header("Optional")]
    public bool hiddenUntilUnlocked = false; // Steam supports hidden achievements
}