using UnityEngine;

public partial class Board
{
    public UnityEngine.UI.Image CreateModifierBoardVFX(Sprite sprite) => CreateBoardVfxImage("InversionBoard_VFX", sprite);
    public bool IsBoulderTarget(Vector2Int cell) =>
        (monsters.TryGetValue(cell, out var monster) && monster.hp > 0f) ||
        (obstacles.TryGetValue(cell, out var obstacle) && obstacle.type == ObstacleType.Stone);

    // Return true only when HP or a destructible stone's durability was reduced.
    public bool StrikeWithBoulder(Vector2Int cell)
    {
        if (obstacles.TryGetValue(cell, out var obstacle) && obstacle.type == ObstacleType.Stone)
        {
            TryHandleObstacleAt(cell, null, null, false);
            return !obstacle.indestructible && obstacle.hitsRemaining > 0;
        }
        else if (monsters.TryGetValue(cell, out var monster) && monster.hp > 0f)
        {
            float before = monster.hp;
            DamageTile(cell, BoulderController.CalculateHitDamage(monster.maxHp,
                Random.Range(BoulderController.MinHitFraction, BoulderController.MaxHitFraction)), DamageSource.BossAbility);
            if (TryGetTileRect(cell, out var tile))
                ImpactShake.Play(tile.Find("MonsterPortrait") ?? tile, 0.18f, 4f);
            return !monsters.TryGetValue(cell, out var after) || after.hp < before;
        }
        return false;
    }
}
