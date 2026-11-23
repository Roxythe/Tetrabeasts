using UnityEngine;

// A pooled tile can represent a different unit later. Keep its popup identity
// across board movement, but change it when the tile is released/reused.
public sealed class DamageTextUnitIdentity : MonoBehaviour
{
    public int Generation { get; private set; }
    void OnDisable() { unchecked { Generation++; } }
}
