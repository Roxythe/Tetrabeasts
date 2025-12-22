using UnityEngine;

[CreateAssetMenu(menuName = "Run/Monster Piece", fileName = "NewMonsterPiece")]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    public string monsterName = "Devil";
    public Sprite portrait;
    public string monsterDescription;

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

}
