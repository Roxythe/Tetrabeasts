using TMPro;
using UnityEngine;

// All typography and artwork are authored on GameModeTooltip.prefab.
[RequireComponent(typeof(GameModeDescriptions))]
public sealed class GameModeTooltip : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public TMP_Text lockNote;
    public GameModeDescriptions descriptions;
    [TextArea] public string lockedMessage = "Reach the final level in Standard to unlock this game mode.";

    public void ShowMode(RunGameMode mode, string modeName, bool locked)
    {
        if (!descriptions) descriptions = GetComponent<GameModeDescriptions>();
        if (title) title.text = TetrabeastsLocalization.LocalizeText(modeName);
        if (description && descriptions) description.text = TetrabeastsLocalization.LocalizeText(descriptions.Get(mode));
        if (lockNote)
        {
            lockNote.text = TetrabeastsLocalization.LocalizeText(lockedMessage);
            lockNote.gameObject.SetActive(locked);
        }
        title?.GetComponent<TMPLabelShadow>()?.Sync();
    }
}
