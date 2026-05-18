// File: Assets/Scripts/UI/RunModsPanelUI.cs
using System.Collections.Generic;
using UnityEngine;

public class RunModsPanelUI : MonoBehaviour
{
    [Header("Parents")]
    [SerializeField] Transform buffsContainer;
    [SerializeField] Transform debuffsContainer;

    [Header("Prefab (single row)")]
    [SerializeField] RunModRowUI buffRowPrefab;
    [SerializeField] RunModRowUI debuffRowPrefab;

    [Header("Behavior")]
    [SerializeField] bool refreshOnEnable = true;

    readonly struct ModKey
    {
        public readonly RunModifierSO mod;
        public readonly RunModRarity rarity;

        public ModKey(RunModifierSO mod, RunModRarity rarity)
        {
            this.mod = mod;
            this.rarity = rarity;
        }
    }

    sealed class ModKeyComparer : IEqualityComparer<ModKey>
    {
        public bool Equals(ModKey a, ModKey b)
        {
            return ReferenceEquals(a.mod, b.mod) && a.rarity == b.rarity;
        }

        public int GetHashCode(ModKey k)
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + (k.mod ? k.mod.GetInstanceID() : 0);
                h = (h * 31) + (int)k.rarity;
                return h;
            }
        }
    }

    static RunModRarity GetRarity(RunModifierSO mod)
    {
        return mod is RunModifier rm ? rm.rarity : RunModRarity.Common;
    }

    void OnEnable()
    {
        TetrabeastsLocalization.LanguageChanged += HandleLanguageChanged;
        if (refreshOnEnable) Refresh();
    }

    void OnDisable()
    {
        TetrabeastsLocalization.LanguageChanged -= HandleLanguageChanged;
    }

    void HandleLanguageChanged()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!buffsContainer || !debuffsContainer || !buffRowPrefab || !debuffRowPrefab)
        {
            Debug.LogWarning("RunModsPanelUI: Missing refs (containers or row prefabs).");
            return;
        }

        Rebuild(buffsContainer, RunModsStore.Buffs, true);
        Rebuild(debuffsContainer, RunModsStore.Debuffs, false);
    }

    void Rebuild(Transform parent, List<RunModifierSO> list, bool isBuff)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        var prefab = isBuff ? buffRowPrefab : debuffRowPrefab;
        if (list == null) return;

        // Preserve order while counting duplicates
        var comparer = new ModKeyComparer();
        var counts = new Dictionary<ModKey, int>(comparer);
        var order = new List<ModKey>();

        foreach (var mod in list)
        {
            if (!mod) continue;

            var key = new ModKey(mod, GetRarity(mod));
            if (counts.TryGetValue(key, out var c))
            {
                counts[key] = c + 1;
            }
            else
            {
                counts[key] = 1;
                order.Add(key);
            }
        }

        foreach (var key in order)
        {
            if (!key.mod) continue;

            var row = Instantiate(prefab, parent);
            row.Bind(key.mod, counts[key]); // Icon/Title/Description (+ xN)
        }
    }
}
