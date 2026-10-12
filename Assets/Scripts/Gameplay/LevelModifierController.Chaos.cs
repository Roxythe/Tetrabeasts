using System.Collections.Generic;
using UnityEngine;

public partial class LevelModifierController
{
    // Each retained modifier keeps its own timers/coroutines. Only the primary
    // controller owns selection and board presentation; gameplay hooks compose.
    bool isChaosEffect;
    readonly List<LevelModifierSO> chaosModifiers = new();
    readonly List<LevelModifierController> chaosEffects = new();
    public IReadOnlyList<LevelModifierSO> ChaosModifiers => chaosModifiers;
    public bool HasModifier(LevelModifierKind kind) => GetModifier(kind) != null;
    public LevelModifierSO GetModifier(LevelModifierKind kind)
    {
        if (ActiveModifier && ActiveModifier.kind == kind) return ActiveModifier;
        foreach (var effect in chaosEffects)
            if (effect && effect.ActiveModifier && effect.ActiveModifier.kind == kind) return effect.ActiveModifier;
        return null;
    }
    public List<string> CaptureChaosModifiers()
    {
        var names = new List<string>();
        foreach (var modifier in chaosModifiers) if (modifier) names.Add(modifier.name);
        return names;
    }
    public void RestoreChaosModifiers(List<string> names)
    {
        chaosModifiers.Clear();
        var database = ChaosDatabase();
        if (!database || names == null) return;
        var pool = database.BuildPool(null);
        foreach (var name in names)
        {
            var modifier = pool.Find(m => m && m.name == name);
            if (modifier && !chaosModifiers.Contains(modifier)) chaosModifiers.Add(modifier);
        }
    }
    LevelModifierDatabaseSO ChaosDatabase()
    {
        if (_gc && _gc.castlesByLevel != null)
            foreach (var castle in _gc.castlesByLevel)
                if (castle && castle.levelModifierDatabase) return castle.levelModifierDatabase;
        return null;
    }
    void ClearChaosEffects()
    {
        foreach (var effect in chaosEffects)
        {
            if (!effect) continue;
            effect.StopAllCoroutines();
            effect.EndNewModifierEffects();
            effect.ClearOutFlankedProjectiles();
            effect.ClearVisualDictionary(effect._overgrowthVisuals);
            effect.ClearVisualDictionary(effect._contagionFloorVisuals);
            effect.ClearVisualDictionary(effect._infectionVisuals);
            effect.enabled = false;
            Destroy(effect);
        }
        chaosEffects.Clear();
    }
    void RebuildChaosEffects()
    {
        bool selectionWasRunning = IsSelectionRunning;
        ClearChaosEffects();
        ResetLevelState();
        IsSelectionRunning = selectionWasRunning;
        if (selectionWasRunning) SetSelectionCursorState(true);
        if (chaosModifiers.Count == 0) return;
        ActiveModifier = chaosModifiers[chaosModifiers.Count - 1];
        ApplyModifierStartupEffects();
        for (int i = 0; i < chaosModifiers.Count - 1; i++)
        {
            var effect = gameObject.AddComponent<LevelModifierController>();
            effect.isChaosEffect = true;
            effect.board = board; effect.piece = piece; effect._gc = _gc; effect.battleLog = battleLog;
            effect.ActiveModifier = chaosModifiers[i];
            chaosEffects.Add(effect);
            effect.ApplyModifierStartupEffects();
        }
        if (specialGaugeRoot) specialGaugeRoot.SetActive(!BlocksSpecialUsage);
        if (activeModifierTitleText)
            activeModifierTitleText.text = string.Join(" • ", chaosModifiers.ConvertAll(m => m.displayName));
    }
    public bool IsBoardFlipped
    {
        get
        {
            if (BoardFlipped) return true;
            foreach (var effect in chaosEffects) if (effect && effect.BoardFlipped) return true;
            return false;
        }
    }
}
