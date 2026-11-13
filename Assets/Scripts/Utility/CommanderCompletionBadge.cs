using UnityEngine;
using UnityEngine.UI;

// The position, size, and sprite are authored in the portrait/button prefab.
public sealed class CommanderCompletionBadge : MonoBehaviour
{
    public Image star;

    public void Bind(PlayerCharacterData commander)
    {
        if (star) star.gameObject.SetActive(GameModeStore.HasStandardCompletion(commander));
    }
}
