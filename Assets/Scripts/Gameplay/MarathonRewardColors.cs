using UnityEngine;

// Attach to the reward panel prefab; runtime binding reads these Inspector values.
public sealed class MarathonRewardColors : MonoBehaviour
{
    [Header("Rarity backgrounds and labels")]
    public Color common = Color.white;
    public Color uncommon = Color.green;
    public Color rare = new Color(0.3f, 0.6f, 1f);
    public Color epic = new Color(0.75f, 0.3f, 1f);
    public Color legendary = new Color(1f, 0.75f, 0.2f);
    [Header("Buff section")]
    public Color buffBackground = new Color(0.16f, 0.32f, 0.65f);
    public Color buffTitle = new Color(0.55f, 0.82f, 1f);
    public Color buffDescription = new Color(0.3f, 0.6f, 1f);
    [Header("Debuff section")]
    public Color debuffBackground = new Color(0.65f, 0.16f, 0.2f);
    public Color debuffTitle = new Color(1f, 0.65f, 0.68f);
    public Color debuffDescription = new Color(1f, 0.25f, 0.3f);
    [Header("Unfocused choices")]
    [Range(0.1f, 1f)] public float inactiveAlpha = 0.65f;
    public Color GetRarity(RunModRarity rarity)
    {
        switch (rarity)
        {
            case RunModRarity.Uncommon: return uncommon;
            case RunModRarity.Rare: return rare;
            case RunModRarity.Epic: return epic;
            case RunModRarity.Legendary: return legendary;
            default: return common;
        }
    }
}
