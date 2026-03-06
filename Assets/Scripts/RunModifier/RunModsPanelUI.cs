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

    void OnEnable()
    {
        if (refreshOnEnable) Refresh();
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

        foreach (var mod in list)
        {
            if (!mod) continue;

            var row = Instantiate(prefab, parent);
            row.Bind(mod); // Icon/Title/Description
        }
    }
}
