using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Game Mode Assets")]
public sealed class GameModeAssets : ScriptableObject
{
    public GameModeSelectionUI modeMenuPrefab;
    public MarathonGaugeUI marathonGaugePrefab;
    public MarathonRewardUI marathonRewardPrefab;
    public Sprite boulderSprite;
    public AudioClip boulderImpactSfx;
    public int standardLevelCount = 24;
    [Min(1f)] public float marathonStartingDamage = 500f;
    [Min(1f)] public float marathonDamageGrowth = 1.18f;
    public static GameModeAssets Load() => Resources.Load<GameModeAssets>("GameModeAssets");
}
