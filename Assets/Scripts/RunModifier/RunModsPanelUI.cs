using System.Collections.Generic;
using UnityEngine;

public class RunModsPanelUI : MonoBehaviour
{
    [Header("Parents")]
    [SerializeField] Transform buffsContainer;
    [SerializeField] Transform debuffsContainer;

    [Header("Prefab (single row)")]
    [SerializeField] RunModRowUI rowPrefab;

    [Header("Behavior")]
    [SerializeField] bool refreshOnEnable = true;

    void OnEnable()
    {
        if (refreshOnEnable) Refresh();
    }

    public void Refresh()
    {
        if (!buffsContainer || !debuffsContainer || !rowPrefab)
        {
            Debug.LogWarning("RunModsPanelUI: Missing refs (containers or rowPrefab).");
            return;
        }

        Rebuild(buffsContainer, RunModsStore.Buffs);
        Rebuild(debuffsContainer, RunModsStore.Debuffs);
    }

    void Rebuild(Transform parent, List<RunModifierSO> list)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        foreach (var mod in list)
        {
            if (!mod) continue;
            var row = Instantiate(rowPrefab, parent);
            row.Bind(mod); // icon/name/tooltip
        }
    }
}
