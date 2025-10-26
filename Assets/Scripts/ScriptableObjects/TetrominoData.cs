using UnityEngine;

[CreateAssetMenu(menuName = "Blocks/TetrominoData")]
public class TetrominoData : ScriptableObject
{
    public string id;                 // "I", "T", "L", etc. (later: monster name)
    public Color color = Color.white; // later: use monster portrait for UI
    public Vector2Int[] cells;        // relative cell offsets for the 4 tiles
}