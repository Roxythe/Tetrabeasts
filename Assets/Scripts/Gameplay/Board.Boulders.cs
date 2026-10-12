using UnityEngine;

public partial class Board
{
    public bool IsBoulderTarget(Vector2Int cell) =>
        (monsters.TryGetValue(cell, out var monster) && monster.hp > 0f) ||
        (obstacles.TryGetValue(cell, out var obstacle) && obstacle.type == ObstacleType.Stone);

    public void StrikeWithBoulder(Vector2Int cell)
    {
        if (obstacles.TryGetValue(cell, out var obstacle) && obstacle.type == ObstacleType.Stone)
            TryHandleObstacleAt(cell, null, null, false);
        else if (monsters.TryGetValue(cell, out var monster) && monster.hp > 0f)
        {
            DamageTile(cell, BoulderController.HitDamage, DamageSource.BossAbility);
            if (TryGetTileRect(cell, out var tile))
                ImpactShake.Play(tile.Find("MonsterPortrait") ?? tile, 0.18f, 4f);
        }
    }
}
