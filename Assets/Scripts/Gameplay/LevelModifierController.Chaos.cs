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
    int chaosTitleIndex;
    float chaosTitleElapsed;
    public int ChaosPresentationAnchorLevel { get; private set; } = -1;
    public string ChaosPresentationAnchorName { get; private set; }
    LevelModifierSO chaosPresentationModifier;
    LevelModifierSO PresentationModifier => GameModeStore.Current == RunGameMode.Chaos && !isChaosEffect
        ? chaosPresentationModifier : ActiveModifier;

    bool HasPresentation(LevelModifierSO modifier) => modifier &&
        (modifier.backgroundOverrideSprite || modifier.backgroundOverrideVideoClip ||
         modifier.boardBackgroundOverrideSprite || modifier.enableRainOverlay || ResolveModifierBoardBorderRoot(modifier));

    void SelectChaosPresentation(int level)
    {
        var variants = chaosModifiers.FindAll(HasPresentation);
        chaosPresentationModifier = null;
        if (variants.Count == 0) return;
        if (variants.Count == 1) { chaosPresentationModifier = variants[0]; return; }
        int anchor = variants.FindIndex(m => m.name == ChaosPresentationAnchorName);
        if (anchor < 0)
        {
            anchor = variants.Count - 1;
            ChaosPresentationAnchorName = variants[anchor].name;
            ChaosPresentationAnchorLevel = level;
        }
        // Walk newest to oldest, followed by one level with the normal castle art.
        int count = variants.Count + 1;
        int offset = Mathf.Max(0, level - ChaosPresentationAnchorLevel) % count;
        int index = (anchor - offset + count) % count;
        if (index < variants.Count) chaosPresentationModifier = variants[index];
    }

    public int ActiveModifierCount => chaosModifiers.Count > 0 ? chaosModifiers.Count : ActiveModifier ? 1 : 0;
    public LevelModifierSO ActiveModifierAt(int index) =>
        chaosModifiers.Count > 0 ? chaosModifiers[index] : index == 0 ? ActiveModifier : null;

    bool HasAcceptedChaosKind(LevelModifierSO modifier) =>
        modifier && chaosModifiers.Exists(active => active && active.kind == modifier.kind);

    void UpdateChaosTitle(float dt)
    {
        if (chaosModifiers.Count < 2 || !activeModifierTitleText) return;
        chaosTitleElapsed += dt;
        if (chaosTitleElapsed < 3f) return;
        chaosTitleElapsed %= 3f;
        chaosTitleIndex = (chaosTitleIndex + 1) % chaosModifiers.Count;
        activeModifierTitleText.text = TetrabeastsLocalization.LocalizeText(chaosModifiers[chaosTitleIndex].displayName);
    }

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
    public void RestoreChaosModifiers(List<string> names, int presentationAnchorLevel = -1, string presentationAnchorName = null)
    {
        ChaosPresentationAnchorLevel = presentationAnchorLevel;
        ChaosPresentationAnchorName = presentationAnchorName;
        chaosPresentationModifier = null;
        chaosModifiers.Clear();
        var database = ChaosDatabase();
        if (!database || names == null) return;
        var pool = database.BuildPool(null);
        foreach (var name in names)
        {
            var modifier = pool.Find(m => m && m.name == name);
            if (modifier && !HasAcceptedChaosKind(modifier)) chaosModifiers.Add(modifier);
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
        SelectChaosPresentation(_gc ? _gc.CurrentLevel : 0);
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
        chaosTitleIndex = chaosModifiers.Count - 1;
        chaosTitleElapsed = 0f;
        RefreshModifierUI();
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
