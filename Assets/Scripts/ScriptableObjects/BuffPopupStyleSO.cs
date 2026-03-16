using System;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Buffs/Buff Popup Style", fileName = "BuffPopupStyle")]
public class BuffPopupStyleSO : ScriptableObject
{
    [Header("Prefab")]
    public GameObject popupPrefab;

    [Header("Layout")]
    public Vector2 sizeMultiplier = new Vector2(1.8f, 1.8f);
    public Vector2 shadowOffset = new Vector2(2f, -2f);

    [Header("Timing")]
    [Min(0.05f)] public float duration = 1.0f;
    [Min(0f)] public float floatPixels = 40f;
    [Min(0.03f)] public float flashInterval = 0.12f;

    [Header("Pulse")]
    [Min(0f)] public float pulseScaleAmplitude = 0.20f;
    [Min(0f)] public float pulseSpeedHz = 2.5f; // Cycles per second

    [Header("Overlap Avoidance")]
    public bool avoidOverlap = true;
    [Min(0f)] public float overlapMinX = 50f;
    [Min(0f)] public float overlapMinY = 26f;
    [Min(0f)] public float overlapStepY = 28f;
    [Min(0)] public int overlapMaxShifts = 6;

    [Header("Rarity Colors")]
    public RarityColorPair common = new RarityColorPair(
        new Color(1f, 1f, 1f, 1f), new Color(0f, 0f, 0f, 1f),
        new Color(0f, 0f, 0f, 0.85f), new Color(0.05f, 0.05f, 0.05f, 0.85f)
    );

    public RarityColorPair uncommon = new RarityColorPair(
        new Color(0.6f, 1f, 0.6f, 1f), new Color(0.2f, 1f, 0.9f, 1f),
        new Color(0f, 0f, 0f, 0.85f), new Color(0.05f, 0.05f, 0.05f, 0.85f)
    );

    public RarityColorPair rare = new RarityColorPair(
        new Color(0.6f, 0.8f, 1f, 1f), new Color(0.8f, 0.6f, 1f, 1f),
        new Color(0f, 0f, 0f, 0.85f), new Color(0.05f, 0.05f, 0.05f, 0.85f)
    );

    public RarityColorPair epic = new RarityColorPair(
        new Color(0.9f, 0.4f, 1f, 1f), new Color(0.4f, 0.9f, 1f, 1f),
        new Color(0f, 0f, 0f, 0.85f), new Color(0.05f, 0.05f, 0.05f, 0.85f)
    );

    public RarityColorPair legendary = new RarityColorPair(
        new Color(1f, 0.65f, 0.15f, 1f), new Color(1f, 0.95f, 0.25f, 1f),
        new Color(0f, 0f, 0f, 0.9f), new Color(0.05f, 0.05f, 0.05f, 0.9f)
    );

    public RarityColorPair GetColors(RunModRarity rarity)
    {
        return rarity switch
        {
            RunModRarity.Uncommon => uncommon,
            RunModRarity.Rare => rare,
            RunModRarity.Epic => epic,
            RunModRarity.Legendary => legendary,
            _ => common
        };
    }

    [Serializable]
    public struct RarityColorPair
    {
        public Color textA;
        public Color textB;
        public Color shadowA;
        public Color shadowB;

        public RarityColorPair(Color textA, Color textB, Color shadowA, Color shadowB)
        {
            this.textA = textA;
            this.textB = textB;
            this.shadowA = shadowA;
            this.shadowB = shadowB;
        }
    }
}