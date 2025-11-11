using UnityEngine;

[CreateAssetMenu(menuName = "Run/Monster Piece", fileName = "NewMonsterPiece")]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    public string monsterName = "Devil";
    public Sprite portrait;
    public string monsterDescription;

    [Header("Stats")]
    public float maxHealth = 1.0f;
    public float minHealth;
    public float currentHealth;
    public float attackPower = 1.0f;
    public float weightedSpawnRate = 1.0f;
    
}
