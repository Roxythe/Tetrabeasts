using UnityEngine;

[CreateAssetMenu(menuName = "Run/Monster Piece", fileName = "NewMonsterPiece")]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    public string monsterName = "Devil";
    public Sprite portrait;
    public string monsterDescription;

    [Header("Unlock")]
    public bool startsLocked = false;
    public int unlockCost = 10;

    [Header("Skins")]
    public Sprite[] skinPortraits = new Sprite[5]; // [0]=default 
    public int[] skinCosts = new int[5];           // [0]=0, [1..4]=prices

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
    public Sprite attackSprite;
    public Sprite healSprite;

    [Header("SFX")]
    public AudioClip sfxHeal;    // played when this healer successfully heals someone
    public AudioClip[] sfxHealClips;
    public AudioClip sfxAttack;  // played when this monster's projectile hits the castle
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
