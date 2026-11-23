using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Game Mode Assets")]
public sealed class GameModeAssets : ScriptableObject
{
    public GameModeSelectionUI modeMenuPrefab;
    public MarathonGaugeUI marathonGaugePrefab;
    public MarathonRewardUI marathonRewardPrefab;
    public CommanderLevelUpUI commanderLevelUpPrefab;
    public GameObject marathonCurtainPrefab;
    public AnimationClip marathonCurtainOpening;
    [Header("Celebration audio and fireworks")]
    public AudioClip commanderLevelUpSfx;
    public AudioClip curtainPulleySfx;
    public GameObject squadFireworkPrefab;
    public AnimationClip squadFireworkAnimation;
    public AudioClip squadFireworkSfx;
    [Range(0f, 2f)] public float squadFireworkVolume = 1.2f;
    public float[] squadFireworkExplosionTimes = { .75f, 2.3f, 3.45f, 4.6f, 5.95f, 6.95f, 7.4f, 7.95f };
    [Min(1f)] public float marathonCastleStartingHealth = 1000f;
    [Min(1f)] public float marathonCastleHealthGrowth = 1.35f;
    public Sprite boulderSprite;
    public AudioClip boulderImpactSfx;
    public AudioClip[] boulderImpactAlternates;
    public int standardLevelCount = 24;
    [Min(1f)] public float marathonStartingDamage = 500f;
    [Min(1f)] public float marathonDamageGrowth = 1.18f;
    public static GameModeAssets Load() => Resources.Load<GameModeAssets>("GameModeAssets");
}
