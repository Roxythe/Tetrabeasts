using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Level Modifier Database")]
public class LevelModifierDatabaseSO : ScriptableObject
{
    [Header("Shared Modifier Pool")]
    public LevelModifierOption[] modifiers;

    public List<LevelModifierSO> BuildPool(LevelModifierKind[] allowedKinds = null)
    {
        var result = new List<LevelModifierSO>();
        if (modifiers == null || modifiers.Length == 0)
            return result;

        HashSet<LevelModifierKind> allowed = null;
        if (allowedKinds != null && allowedKinds.Length > 0)
            allowed = new HashSet<LevelModifierKind>(allowedKinds);

        for (int i = 0; i < modifiers.Length; i++)
        {
            var option = modifiers[i];
            if (!option.modifier)
                continue;

            if (allowed != null && !allowed.Contains(option.modifier.kind))
                continue;

            result.Add(option.modifier);
        }

        return result;
    }

    public LevelModifierSO PickWeighted(LevelModifierKind[] allowedKinds = null, LevelModifierSO excludeModifier = null)
    {
        if (modifiers == null || modifiers.Length == 0)
            return null;

        HashSet<LevelModifierKind> allowed = null;
        if (allowedKinds != null && allowedKinds.Length > 0)
            allowed = new HashSet<LevelModifierKind>(allowedKinds);

        int totalWeight = 0;
        for (int i = 0; i < modifiers.Length; i++)
        {
            var option = modifiers[i];
            if (!option.modifier)
                continue;

            if (allowed != null && !allowed.Contains(option.modifier.kind))
                continue;

            if (excludeModifier && option.modifier == excludeModifier)
                continue;

            totalWeight += Mathf.Max(1, option.weight);
        }

        if (totalWeight <= 0)
        {
            return excludeModifier ? PickWeighted(allowedKinds, null) : null;
        }

        int roll = Random.Range(0, totalWeight);

        for (int i = 0; i < modifiers.Length; i++)
        {
            var option = modifiers[i];
            if (!option.modifier)
                continue;

            if (allowed != null && !allowed.Contains(option.modifier.kind))
                continue;

            if (excludeModifier && option.modifier == excludeModifier)
                continue;

            roll -= Mathf.Max(1, option.weight);
            if (roll < 0)
                return option.modifier;
        }

        return null;
    }
}
