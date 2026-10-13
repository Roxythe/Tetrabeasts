using UnityEngine;

// Edit this component on GameModeMenu.prefab to change the mode tooltips used in game.
public sealed class GameModeDescriptions : MonoBehaviour
{
    [TextArea(3, 8)] public string standard = "Battle through the castles, choose rewards, and defeat the final boss. Level modifiers last for one level.";
    [TextArea(3, 8)] public string chaos = "Choose a level modifier every other level. All chosen modifiers stay active and stack for the entire run. Earn 20% additional monster experience.";
    [TextArea(3, 8)] public string marathon = "Battle endlessly. Deal damage to level up, gain reinforcements, and choose a buff and debuff of matching rarity. A boss appears every third level. After the final boss, the castles repeat while damage targets and difficulty keep rising.";
    public string Get(RunGameMode mode) => mode == RunGameMode.Chaos ? chaos : mode == RunGameMode.Marathon ? marathon : standard;
}
