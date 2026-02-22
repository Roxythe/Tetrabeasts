using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Achievements/Achievement Database")]
public class AchievementDatabaseSO : ScriptableObject
{
    public List<AchievementDefSO> achievements = new();

    Dictionary<string, AchievementDefSO> _map;

    public void BuildLookup()
    {
        _map = new Dictionary<string, AchievementDefSO>(achievements.Count);
        foreach (var a in achievements)
        {
            if (!a || string.IsNullOrEmpty(a.achievementId)) continue;
            _map[a.achievementId] = a;
        }
    }

    public AchievementDefSO Get(string achievementId)
    {
        if (_map == null) BuildLookup();
        if (string.IsNullOrEmpty(achievementId)) return null;
        _map.TryGetValue(achievementId, out var def);
        return def;
    }
}