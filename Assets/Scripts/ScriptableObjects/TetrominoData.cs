using UnityEngine;

public enum SpecialType { None, Death, Bomb, Bolt, Earthquake }

[CreateAssetMenu(menuName = "Blocks/TetrominoData")]
public class TetrominoData : ScriptableObject
{
    public string id;                 // "I", "T", "L", etc.
    public Color color = Color.white; 
    public Vector2Int[] cells;        // Relative cell offsets for the 4 tiles

    [Header("Special (optional)")]
    public SpecialType special = SpecialType.None;  // None = normal piece
    public Sprite specialSprite;                    // Shown on the special block tile/preview
    public float spawnWeight = 0.05f;
    public Sprite specialFlashSprite;  // Flashed on affected cells at activation
    public bool flashOnlyOccupied = true; // True = flash only occupied cells, false = flash all affected cells
    public AudioClip specialSFX; 
}