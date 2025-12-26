using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Castle Data")]
public class CastleData : ScriptableObject
{
    [Header("Display / Identity")]
    public string castleName = "Castle";

    [Tooltip("0 = healthy, 1 = chipped, 2 = cracked, 3 = crumbling")]
    public Sprite[] damageStages = new Sprite[4];

    [Header("Stats")]
    public int maxHP = 100;

    [Header("Projectile Attack")]
    public Sprite projectileSprite;          // arrow sprite, etc.
    [Min(0.1f)] public float projectileInterval = 3f;
    [Min(1f)] public float projectileSpeed = 800f;
    [Min(1)] public int projectileDamage = 1;

    [Header("Boss Level")]
    public bool isBossLevel;
    public Sprite bossOverlaySprite;
    public AudioClip bossBGM;
    [Range(0f, 1f)] public float bossBGMVolume = 0.6f;

    // (optional) non-boss per-level music
    public AudioClip levelBGM;
    [Range(0f, 1f)] public float levelBGMVolume = 0.5f;

    public Sprite GetSpriteForHealth(float hpPercent)
    {
        // clamp
        if (hpPercent < 0f) hpPercent = 0f;
        if (hpPercent > 1f) hpPercent = 1f;

        int index;
        if (hpPercent >= 0.76f) index = 0; // 100-76%
        else if (hpPercent >= 0.51f) index = 1; // 75-51%
        else if (hpPercent >= 0.26f) index = 2; // 50-26%
        else index = 3; // 25-0%

        if (damageStages != null && index >= 0 && index < damageStages.Length)
            return damageStages[index];

        // safety fallback
        if (damageStages != null && damageStages.Length > 0)
            return damageStages[0];

        return null;
    }
}
