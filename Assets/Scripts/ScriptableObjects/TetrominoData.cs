using UnityEngine;

public enum SpecialType { None, Death, Bomb, Bolt, Earthquake }

[CreateAssetMenu(menuName = "Blocks/TetrominoData")]
public class TetrominoData : ScriptableObject
{
    public string id;                 // "I", "T", "L", etc. (later: monster name)
    public Color color = Color.white; // later: use monster portrait for UI
    public Vector2Int[] cells;        // relative cell offsets for the 4 tiles

    [Header("Special (optional)")]
    public SpecialType special = SpecialType.None;  // None = normal piece
    public Sprite specialSprite;                    // shown on the special block tile/preview
    public float spawnWeight = 0.05f;               // low chance (used below)
    public Sprite specialFlashSprite;  // flashed on affected cells at activation
    public bool flashOnlyOccupied = true; // true = flash only occupied cells, false = flash all affected cells
    public AudioClip specialSFX;       // played on activation
}