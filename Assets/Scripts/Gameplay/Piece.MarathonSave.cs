using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Piece
{
    public MarathonMatchSnapshot.PieceState CaptureMarathonPiece()
    {
        if (!HasActiveCells || !data) return null;
        var state = new MarathonMatchSnapshot.PieceState {
            asset = MatchSnapshotAssets.Key(data), origin = origin, cells = new List<Vector2Int>(cells), color = color,
            fallTimer = fallTimer, lockTimer = lockTimer, fallInterval = fallInterval, lockDelay = lockDelay,
            hardDropPending = hardDropVisualLockPending };
        foreach (var monster in monstersForCells) state.monsters.Add(MatchSnapshotAssets.Key(monster));
        return state;
    }

    public void RestoreMarathonPiece(MarathonMatchSnapshot.PieceState state, MatchSnapshotAssets assets)
    {
        ResetPiece();
        enabled = true; // OnEnable clears the old cells; populate after enabling.
        data = assets.Get<TetrominoData>(state.asset);
        color = state.color;
        origin = state.origin;
        cells.AddRange(state.cells);
        monstersForCells.Clear();
        foreach (string monster in state.monsters) monstersForCells.Add(assets.Get<MonsterData>(monster));
        fallTimer = state.fallTimer; lockTimer = state.lockTimer;
        fallInterval = state.fallInterval; lockDelay = state.lockDelay;
        BuildVisuals();
        MarkHintsDirty();
        RefreshHintsIfNeeded(force: true);
        if (state.hardDropPending)
        {
            hardDropVisualLockPending = true;
            StartCoroutine(CompleteRestoredHardDrop());
        }
    }

    IEnumerator CompleteRestoredHardDrop()
    {
        yield return null;
        while (GetGameController() && GetGameController().IsGameplaySuspended) yield return null;
        FinishHardDropVisualLock();
    }
}
