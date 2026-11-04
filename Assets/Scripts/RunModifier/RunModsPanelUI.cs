// File: Assets/Scripts/UI/RunModsPanelUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Active level modifiers")]
    [SerializeField] Button levelModifiersButton;
    [SerializeField] LevelModifierDetails details;
    [SerializeField] Button previous, next, closeDetails;
    LevelModifierController controller;
    int page;
    public GameObject OpenDetailsRoot => details && details.gameObject.activeSelf ? details.gameObject : null;

    void Awake()
    {
        if (details) details.gameObject.SetActive(false);
        if (levelModifiersButton) levelModifiersButton.onClick.AddListener(OpenDetails);
        if (previous) previous.onClick.AddListener(() => ChangePage(-1));
        if (next) next.onClick.AddListener(() => ChangePage(1));
        if (closeDetails) closeDetails.onClick.AddListener(() => CloseDetails());
    }

    void OpenDetails()
    {
        if (!details || !controller || controller.ActiveModifierCount == 0) return;
        page = controller.ActiveModifierCount - 1;
        details.gameObject.SetActive(true);
        ShowPage();
        if (UICursorController.IsButtonNavigationMode) closeDetails.Select();
    }

    void ChangePage(int step)
    {
        int count = controller ? controller.ActiveModifierCount : 0;
        if (count == 0) { CloseDetails(); return; }
        page = (page + step + count) % count;
        ShowPage();
    }

    void ShowPage()
    {
        bool browse = controller.ActiveModifierCount > 1;
        previous.gameObject.SetActive(browse);
        next.gameObject.SetActive(browse);
        details.ShowModifier(controller.ActiveModifierAt(page));
    }

    public bool CloseDetails()
    {
        if (!OpenDetailsRoot) return false;
        details.gameObject.SetActive(false);
        if (UICursorController.IsButtonNavigationMode && levelModifiersButton) levelModifiersButton.Select();
        return true;
    }

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
        if (details) details.gameObject.SetActive(false);
    }

    void HandleLanguageChanged()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!controller)
        {
            var game = FindFirstObjectByType<GameController>();
            controller = game ? game.levelModifierController : null;
        }
        if (levelModifiersButton) levelModifiersButton.interactable = controller && controller.ActiveModifierCount > 0;
        if (OpenDetailsRoot && controller && controller.ActiveModifierCount > 0) ShowPage();
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
