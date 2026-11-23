using UnityEngine;
public sealed class BoardOwned : MonoBehaviour
{
    // Retain the block's actual color independently of hit flashes and health tinting.
    [System.NonSerialized] public Color tileColor = Color.white;
}
