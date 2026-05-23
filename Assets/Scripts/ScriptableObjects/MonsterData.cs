using UnityEngine;


public enum AttackAnimType
{
    None = 0,

    MirrorToggle = 1, // Flips horizontally back and forth while moving by toggling 2 layered sprites
    SpinClockwise = 2 // Rotates clockwise continuously while moving
}

public enum MonsterRole
{
    Attack = 0,
    Defense = 1,
    Healer = 2
}

public enum MonsterPassiveType
{
    None = 0,
    ComboDuration = 1,
    BonusComboChance = 2,
    StoneBuffDropChance = 3,
    StartingReserveUnits = 4,
    ReserveUnitsRestoredOnWin = 5,
    AllyMonsterBulwark = 6,
    CurrencyGain = 7,
    PartyExperienceGain = 8,
    StoneForager = 9
}

[CreateAssetMenu(menuName = "Run/Monster Piece", fileName = "NewMonsterPiece")]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    public string monsterName = "Devil";
    public Sprite portrait;
    public string monsterDescription;

    [Header("Passive")]
    public MonsterPassiveType passiveType = MonsterPassiveType.None;

    [Header("Role")]
    public MonsterRole role = MonsterRole.Attack;

    [Header("Unlock")]
    public bool startsLocked = false;
    public int unlockCost = 10;

    [Header("Skins")]
    public Sprite[] skinPortraits = new Sprite[5]; // [0]=default 
    public int[] skinCosts = new int[5];           // [0]=0, [1-4]=prices

    [Header("Stats")]
    public float maxHealth = 1.0f; // Maximum health of the tile monster
    public float minHealth;
    public float currentHealth;
    public float attackPower = 1.0f; // Damage dealt to enemy on tile clear
    public float weightedSpawnRate = 1.0f; // How likely this monster is to appear in runs

    [Header("Healer Stats")]
    public float healAmount = 0.0f; // How much this monster heals allies on line clear
    public float healRange = 1.0f; // Range within which this monster heals allies
    public float healSpeed = 10.0f; // How often the healing effect occurs

    [Header("Special")]
    public float specialGaugeGain = 1.0f; // How much this monster adds to the gauge on line clear

    [Header("VFX")]
    public AttackAnimType attackAnim = AttackAnimType.None;

    public Sprite attackSprite; // Base attack sprite
    public Sprite attackSpriteAlt; // Optional secondary sprite for toggle animation

    // Per-skin attack sprites 
    public Sprite[] skinAttackSprites = new Sprite[5];
    public Sprite[] skinAttackSpritesAlt = new Sprite[5];

    public Sprite healSprite;

    // Animation tuning
    [Range(0.03f, 0.25f)] public float attackToggleInterval = 0.08f;
    public float spinDegreesPerSecond = 720f;

    [Header("SFX")]
    public AudioClip sfxHeal;    // Played when this healer successfully heals someone
    public AudioClip[] sfxHealClips;
    public AudioClip sfxAttack;  // Played when this monster's projectile hits the castle
    public AudioClip[] sfxAttackClips;

    AudioClip PickRandomFrom(AudioClip[] arr, AudioClip fallback)
    {
        if (arr != null && arr.Length > 0)
        {
            for (int tries = 0; tries < 8; tries++)
            {
                var c = arr[Random.Range(0, arr.Length)];
                if (c) return c;
            }
        }

        return fallback;
    }

    public AudioClip PickRandomHealSFX() => PickRandomFrom(sfxHealClips, sfxHeal);
    public AudioClip PickRandomAttackSFX() => PickRandomFrom(sfxAttackClips, sfxAttack);
}
