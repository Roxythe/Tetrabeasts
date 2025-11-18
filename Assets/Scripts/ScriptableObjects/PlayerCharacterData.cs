using TMPro;
using UnityEngine;

public enum SpecialAbility
{
    ClearBottomRows // add more later (FreezeTime, NukeColumn, etc.)
}

[CreateAssetMenu(menuName = "Run/Player Character", fileName = "NewPlayerCharacter")]
public class PlayerCharacterData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Alyx";
    public Sprite portrait;
    public string specialDescription;

    [Header("Special")]
    public SpecialAbility ability = SpecialAbility.ClearBottomRows;
    [Range(1, 6)] public int clearRows = 3;     // for ClearBottomRows
    public float cooldownSeconds = 0f;          // optional, if you add a cooldown later
    public float specialGaugeMax = 100f;
}
