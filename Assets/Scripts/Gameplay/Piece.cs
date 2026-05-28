using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    public Board board;
    public Image activeTilePrefab;

    [Header("Timing")]
    public float fallInterval = 0.8f;
    public float lockDelay = 0.3f;

    [Header("Input Repeat")]
    [SerializeField, Min(0.01f)] float moveRepeatDelay = 0.18f;
    [SerializeField, Min(0.01f)] float moveRepeatInterval = 0.07f;
    [SerializeField, Min(0.01f)] float rotateRepeatDelay = 0.22f;
    [SerializeField, Min(0.01f)] float rotateRepeatInterval = 0.12f;

    [HideInInspector] public TetrominoData data; // Assigned by controller
    [HideInInspector] public Color color = Color.cyan;

    Vector2Int origin; // Rotation/translation origin
    readonly List<Vector2Int> cells = new();
    readonly List<RectTransform> visuals = new();
    readonly List<MonsterData> monstersForCells = new();
    readonly List<RectTransform> hintOverlays = new();
    static readonly Color hintColor = new Color(1f, 0f, 0f, 0.5f); // Light red

    float fallTimer = 0f, lockTimer;
    Coroutine visualTransitionCoroutine;
    bool hardDropVisualLockPending;
    bool completingVisualTransition;
    readonly List<Vector2Int> hardDropPreviewBorderCells = new();


    // Single-pixel white sprite for UI fills (so Image actually renders)
    static Sprite _onePx;
    static Sprite OnePx()
    {
        if (_onePx) return _onePx;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _onePx = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _onePx;
    }

    void OnEnable()
    {
        fallTimer = 0f; lockTimer = 0f;
        visuals.Clear(); cells.Clear();
        hardDropVisualLockPending = false;
        visualTransitionCoroutine = null;
        completingVisualTransition = false;
    }

    public void SpawnAtTop()
    {
        board.RecomputeCellMetrics();

        // Center top
        origin = new Vector2Int(board.width / 2, board.height);
        foreach (var c in data.cells) cells.Add(origin + c);

        // If blocked, try one row lower; else game over
        if (!board.Valid(cells.ToArray()))
        {
            Shift(Vector2Int.down);
            if (!board.Valid(cells.ToArray())) { enabled = false; return; }
        }
        BuildVisuals();
    }

    private void Start()
    {
        
    }

    void Update()
    {
        var gc = FindFirstObjectByType<GameController>();

        bool tutorialPromptActive = gc != null && gc.IsTutorialPromptActive;
        bool gameplaySuspended = gc != null && gc.IsGameplaySuspended;

        // Allow limited inputs during tutorial prompts, but still block on other suspension states.
        if (gameplaySuspended && !tutorialPromptActive)
            return;

        if (hardDropVisualLockPending)
            return;

        bool blockHorizontal = gc != null &&
                               gc.levelModifierController &&
                               gc.levelModifierController.BlocksManualHorizontalShift;

        bool blockRotation = gc != null &&
                             gc.levelModifierController &&
                             gc.levelModifierController.BlocksManualRotation;

        bool allowSoftDrop = !tutorialPromptActive || (gc != null && gc.IsTutorialSoftDropAllowed);
        bool allowHardDrop = !tutorialPromptActive || (gc != null && gc.IsTutorialHardDropAllowed);

        bool moveLeftPressed = !blockHorizontal &&
                               TetrabeastsControls.WasPressedOrRepeated(
                                   TetrabeastsControlAction.MoveLeft,
                                   moveRepeatDelay,
                                   moveRepeatInterval);

        bool moveRightPressed = !blockHorizontal &&
                                TetrabeastsControls.WasPressedOrRepeated(
                                    TetrabeastsControlAction.MoveRight,
                                    moveRepeatDelay,
                                    moveRepeatInterval);

        bool softDropPressed = allowSoftDrop &&
                               TetrabeastsControls.WasPressedOrRepeated(
                                   TetrabeastsControlAction.SoftDrop,
                                   moveRepeatDelay,
                                   moveRepeatInterval);

        bool rotateCwPressed = !blockRotation &&
                               TetrabeastsControls.WasPressedOrRepeated(
                                   TetrabeastsControlAction.RotateClockwise,
                                   rotateRepeatDelay,
                                   rotateRepeatInterval);

        bool rotateCcwPressed = !blockRotation &&
                                TetrabeastsControls.WasPressedOrRepeated(
                                    TetrabeastsControlAction.RotateCounterClockwise,
                                    rotateRepeatDelay,
                                    rotateRepeatInterval);

        bool hardDropPressed = allowHardDrop &&
                               TetrabeastsControls.WasPressed(TetrabeastsControlAction.HardDrop);

        if (moveLeftPressed && TryMove(Vector2Int.left))
            NotifyTutorialEvent(TutorialGameplayEvent.MoveLeft);

        if (moveRightPressed && TryMove(Vector2Int.right))
            NotifyTutorialEvent(TutorialGameplayEvent.MoveRight);

        if (softDropPressed)
            SoftDrop(true);

        if (rotateCwPressed)
            RotateCW(true);

        if (rotateCcwPressed)
            RotateCCW(true);

        if (hardDropPressed)
        {
            HardDrop(true);
            if (hardDropVisualLockPending)
                return;
        }

        if (gc != null && gc.levelModifierController)
            gc.levelModifierController.HandlePieceAutomation(this, Time.deltaTime);

        if (cells.Count == 0 || visuals.Count == 0)
        {
            fallTimer = 0f;
            lockTimer = 0f;
            return;
        }

        bool freezeTutorialGravity = gc != null && gc.IsTutorialPieceGravityFrozen;

        if (!freezeTutorialGravity)
        {
            fallTimer += Time.deltaTime;
            if (fallTimer >= fallInterval)
            {
                fallTimer = 0f;
                if (!TryMove(Vector2Int.down))
                    lockTimer += fallInterval;
                else
                    lockTimer = 0f;
            }

            if (lockTimer >= lockDelay)
                Lock();
        }
        else
        {
            fallTimer = 0f;
            lockTimer = 0f;
        }

        if (data.special != SpecialType.None)
            UpdateSpecialHints();
        else
            UpdateNormalHints();
    }

    public System.Collections.Generic.IReadOnlyList<RectTransform> GetTutorialHighlightTargets()
    {
        return visuals;
    }

    void BuildVisuals()
    {
        bool isSpecial = data.special != SpecialType.None;

        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        if (board == null || board.gridRoot == null) return;

        var activeSet = new HashSet<Vector2Int>(cells);

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];

            // Base tile
            var img = Instantiate(activeTilePrefab, board.gridRoot);
            var rt = img.rectTransform;

            img.sprite = null;
            img.raycastTarget = false;
            img.color = new Color(0f, 0f, 0f, 0f);

            var anyOutline = img.GetComponent<UnityEngine.UI.Outline>();
            if (anyOutline) Destroy(anyOutline);

            // Size/position
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = board.CellToAnchoredPos(c);

            // Pick outline color (gold while immune, otherwise black)
            var gc = FindFirstObjectByType<GameController>();
            Color borderColor = (gc && gc.immunityActive) ? board.immuneBorderColor : board.normalBorderColor;
            board.SetInlineBorderColor(rt, borderColor);

            // Build inner fill first so ApplySharedEdges can resize it correctly on shared edges
            var fillGO = new GameObject("ActiveFill", typeof(UnityEngine.UI.Image));
            var fill = fillGO.GetComponent<UnityEngine.UI.Image>();
            fill.raycastTarget = false;
            fill.sprite = (data != null && data.backgroundImage != null) ? data.backgroundImage : OnePx();
            fill.type = UnityEngine.UI.Image.Type.Simple;
            fill.preserveAspect = false;
            fill.color = color;

            var frt = fill.rectTransform;
            frt.SetParent(rt, false);
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta;
            frt.anchoredPosition = Vector2.zero;
            frt.SetAsFirstSibling(); // Icons/portraits sit on top

            // Halve thickness on shared edges
            bool L = activeSet.Contains(c + Vector2Int.left) || (board.InBounds(c + Vector2Int.left) && !board.IsFree(c + Vector2Int.left));
            bool R = activeSet.Contains(c + Vector2Int.right) || (board.InBounds(c + Vector2Int.right) && !board.IsFree(c + Vector2Int.right));
            bool U = activeSet.Contains(c + Vector2Int.up) || (board.InBounds(c + Vector2Int.up) && !board.IsFree(c + Vector2Int.up));
            bool D = activeSet.Contains(c + Vector2Int.down) || (board.InBounds(c + Vector2Int.down) && !board.IsFree(c + Vector2Int.down));

            board.ApplySharedEdges(rt, L, R, U, D);

            // Portrait/special icon
            var innerRT = frt;
            if (isSpecial && data.specialSprite != null)
            {
                var go = new GameObject("SpecialIcon", typeof(UnityEngine.UI.Image));
                var p = go.GetComponent<UnityEngine.UI.Image>();
                p.sprite = data.specialSprite; p.preserveAspect = true;
                p.raycastTarget = false;

                var prt = p.rectTransform; prt.SetParent(innerRT, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = innerRT.sizeDelta - new Vector2(2f, 2f);
                prt.anchoredPosition = Vector2.zero;
            }
            else if (!isSpecial && i < monstersForCells.Count && monstersForCells[i])
            {
                var portrait = GetCurrentMonsterPortrait(monstersForCells[i]);
                if (portrait)
                {
                    var go = new GameObject("MonsterPortrait", typeof(UnityEngine.UI.Image));
                    var p = go.GetComponent<UnityEngine.UI.Image>();
                    p.sprite = portrait; p.preserveAspect = true;
                    p.raycastTarget = false;

                    var prt = p.rectTransform; prt.SetParent(innerRT, false);
                    prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                    prt.sizeDelta = innerRT.sizeDelta - new Vector2(2f, 2f);
                    prt.anchoredPosition = Vector2.zero;
                }
            }

            visuals.Add(rt);
        }
    }

    void SyncVisuals()
    {
        CancelVisualTransition();
        if (!EnsureVisualsReady())
            return;

        SetVisualsToTargetPositions(GetCellTargetPositions());
        ApplyActiveVisualBorders();
    }

    bool EnsureVisualsReady()
    {
        if (visuals.Count != cells.Count)
        {
            BuildVisuals();
            return visuals.Count == cells.Count;
        }

        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] == null)
            {
                BuildVisuals();
                return visuals.Count == cells.Count;
            }
        }

        return true;
    }

    Vector2[] GetCellTargetPositions()
    {
        var targets = new Vector2[cells.Count];
        for (int i = 0; i < cells.Count; i++)
            targets[i] = board.CellToAnchoredPos(cells[i]);
        return targets;
    }

    Vector2[] CaptureVisualPositions()
    {
        var positions = new Vector2[visuals.Count];
        for (int i = 0; i < visuals.Count; i++)
            positions[i] = visuals[i] ? visuals[i].anchoredPosition : Vector2.zero;
        return positions;
    }

    void SetVisualsToTargetPositions(Vector2[] targets)
    {
        int count = Mathf.Min(visuals.Count, targets.Length);
        for (int i = 0; i < count; i++)
            if (visuals[i])
                visuals[i].anchoredPosition = targets[i];
    }

    void ApplyActiveVisualBorders()
    {
        var activeSet = new HashSet<Vector2Int>(cells);

        for (int i = 0; i < visuals.Count && i < cells.Count; i++)
        {
            if (!visuals[i])
                continue;

            var c = cells[i];
            bool L = activeSet.Contains(c + Vector2Int.left) || (board.InBounds(c + Vector2Int.left) && !board.IsFree(c + Vector2Int.left));
            bool R = activeSet.Contains(c + Vector2Int.right) || (board.InBounds(c + Vector2Int.right) && !board.IsFree(c + Vector2Int.right));
            bool U = activeSet.Contains(c + Vector2Int.up) || (board.InBounds(c + Vector2Int.up) && !board.IsFree(c + Vector2Int.up));
            bool D = activeSet.Contains(c + Vector2Int.down) || (board.InBounds(c + Vector2Int.down) && !board.IsFree(c + Vector2Int.down));

            board.ApplySharedEdges(visuals[i], L, R, U, D);
        }
    }

    void ApplyHardDropSettledBorderPreview()
    {
        ApplyActiveVisualBorders();
        RefreshHardDropPreviewNeighborBorders();
    }

    void RefreshHardDropPreviewNeighborBorders()
    {
        RestoreHardDropPreviewNeighborBorders();

        if (data == null || data.special != SpecialType.None || board == null || cells.Count == 0)
            return;

        var activeSet = new HashSet<Vector2Int>(cells);
        var previewCells = new HashSet<Vector2Int>();

        for (int i = 0; i < cells.Count; i++)
        {
            AddHardDropPreviewNeighbor(cells[i] + Vector2Int.left, activeSet, previewCells);
            AddHardDropPreviewNeighbor(cells[i] + Vector2Int.right, activeSet, previewCells);
            AddHardDropPreviewNeighbor(cells[i] + Vector2Int.up, activeSet, previewCells);
            AddHardDropPreviewNeighbor(cells[i] + Vector2Int.down, activeSet, previewCells);
        }

        foreach (var c in previewCells)
        {
            if (!board.TryGetTileRect(c, out var rt))
                continue;

            bool L = IsHardDropPreviewShared(c + Vector2Int.left, activeSet);
            bool R = IsHardDropPreviewShared(c + Vector2Int.right, activeSet);
            bool U = IsHardDropPreviewShared(c + Vector2Int.up, activeSet);
            bool D = IsHardDropPreviewShared(c + Vector2Int.down, activeSet);

            board.ApplySharedEdges(rt, L, R, U, D);
            hardDropPreviewBorderCells.Add(c);
        }
    }

    void AddHardDropPreviewNeighbor(Vector2Int cell, HashSet<Vector2Int> activeSet, HashSet<Vector2Int> previewCells)
    {
        if (!board.InBounds(cell) || activeSet.Contains(cell) || board.IsFree(cell))
            return;

        previewCells.Add(cell);
    }

    bool IsHardDropPreviewShared(Vector2Int cell, HashSet<Vector2Int> activeSet)
    {
        return activeSet.Contains(cell) || (board.InBounds(cell) && !board.IsFree(cell));
    }

    void RestoreHardDropPreviewNeighborBorders()
    {
        if (hardDropPreviewBorderCells.Count == 0)
            return;

        for (int i = 0; i < hardDropPreviewBorderCells.Count; i++)
        {
            var c = hardDropPreviewBorderCells[i];
            if (board && board.InBounds(c))
                board.RefreshTileBordersAt(c);
        }

        hardDropPreviewBorderCells.Clear();
    }

    void AnimateVisualsForRotation(Vector2Int rotationOrigin, float rotationDegrees)
    {
        var gc = GetGameController();
        float duration = gc ? gc.PieceRotationVisualDuration : 0f;

        if (!ShouldSmoothPieceAction(gc, duration))
        {
            SyncVisuals();
            return;
        }

        StartVisualTransition(duration, true, board.CellToAnchoredPos(rotationOrigin), rotationDegrees, null);
    }

    bool TryStartHardDropVisualLock()
    {
        var gc = GetGameController();
        float duration = gc ? gc.PieceHardDropVisualDuration : 0f;

        if (!ShouldSmoothPieceAction(gc, duration))
            return false;

        hardDropVisualLockPending = true;
        ClearHints();
        StartVisualTransition(duration, false, Vector2.zero, 0f, FinishHardDropVisualLock, true);
        return true;
    }

    bool ShouldSmoothPieceAction(GameController gc, float duration)
    {
        return gc != null && gc.SmoothPieceActionVisuals && duration > 0f;
    }

    void StartVisualTransition(
        float duration,
        bool rotateAroundPivot,
        Vector2 pivot,
        float rotationDegrees,
        Action onComplete,
        bool previewSettledBorders = false)
    {
        if (!EnsureVisualsReady())
        {
            onComplete?.Invoke();
            return;
        }

        Vector2[] starts = CaptureVisualPositions();
        Vector2[] targets = GetCellTargetPositions();
        CancelVisualTransition(false);

        if (previewSettledBorders)
            ApplyHardDropSettledBorderPreview();

        visualTransitionCoroutine = StartCoroutine(CoAnimateVisualTransition(
            starts,
            targets,
            duration,
            rotateAroundPivot,
            pivot,
            rotationDegrees,
            onComplete));
    }

    IEnumerator CoAnimateVisualTransition(
        Vector2[] starts,
        Vector2[] targets,
        float duration,
        bool rotateAroundPivot,
        Vector2 pivot,
        float rotationDegrees,
        Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            for (int i = 0; i < visuals.Count && i < starts.Length && i < targets.Length; i++)
            {
                if (!visuals[i])
                    continue;

                visuals[i].anchoredPosition = rotateAroundPivot
                    ? RotatePointAroundPivot(starts[i], pivot, Mathf.Lerp(0f, rotationDegrees, eased))
                    : Vector2.LerpUnclamped(starts[i], targets[i], eased);
            }

            yield return null;
        }

        SetVisualsToTargetPositions(targets);
        ApplyActiveVisualBorders();

        visualTransitionCoroutine = null;

        if (onComplete != null)
        {
            completingVisualTransition = true;
            onComplete.Invoke();
            completingVisualTransition = false;
        }
    }

    Vector2 RotatePointAroundPivot(Vector2 point, Vector2 pivot, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        Vector2 offset = point - pivot;

        return pivot + new Vector2(
            offset.x * cos - offset.y * sin,
            offset.x * sin + offset.y * cos);
    }

    void FinishHardDropVisualLock()
    {
        hardDropVisualLockPending = false;
        Lock();
    }

    void CancelVisualTransition(bool clearHardDropPending = true)
    {
        if (visualTransitionCoroutine != null && !completingVisualTransition)
            StopCoroutine(visualTransitionCoroutine);

        visualTransitionCoroutine = null;
        if (clearHardDropPending)
        {
            RestoreHardDropPreviewNeighborBorders();
            hardDropVisualLockPending = false;
        }
    }

    GameController GetGameController()
    {
        var gc = GetComponent<GameController>();
        if (!gc)
            gc = FindFirstObjectByType<GameController>();
        return gc;
    }

    bool TryMove(Vector2Int delta, bool syncVisuals = true)
    {
        var next = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++) next[i] = cells[i] + delta;
        if (!board.Valid(next)) return false;
        for (int i = 0; i < cells.Count; i++) cells[i] = next[i];
        origin += delta;
        if (syncVisuals)
            SyncVisuals();
        return true;
    }

    void Shift(Vector2Int delta)
    {
        for (int i = 0; i < cells.Count; i++) cells[i] += delta;
        origin += delta;
    }

    void SoftDrop(bool notifyTutorial = false)
    {
        if (!TryMove(Vector2Int.down))
            return;

        lockTimer = 0f;
        if (notifyTutorial)
            NotifyTutorialEvent(TutorialGameplayEvent.SoftDrop);
    }

    void HardDrop(bool notifyTutorial = false)
    {
        bool moved = false;
        while (TryMove(Vector2Int.down, false))
            moved = true;

        if (notifyTutorial)
            NotifyTutorialEvent(TutorialGameplayEvent.HardDrop);

        if (moved && TryStartHardDropVisualLock())
            return;

        SyncVisuals();
        Lock();
    }

    void RotateCW(bool notifyTutorial = false)
    {
        Vector2Int rotationOrigin = origin;
        var next = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            var r = cells[i] - origin; // Around origin
            var rot = new Vector2Int(r.y, -r.x);
            next[i] = origin + rot;
        }
        if (board.Valid(next))
        {
            for (int i = 0; i < cells.Count; i++) cells[i] = next[i];
            AnimateVisualsForRotation(rotationOrigin, -90f);
            if (notifyTutorial)
                NotifyTutorialEvent(TutorialGameplayEvent.RotateClockwise);
        }
    }

    void RotateCCW(bool notifyTutorial = false)
    {
        Vector2Int rotationOrigin = origin;
        var next = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            var r = cells[i] - origin;
            var rot = new Vector2Int(-r.y, r.x);
            next[i] = origin + rot;
        }
        if (board.Valid(next))
        {
            for (int i = 0; i < cells.Count; i++) cells[i] = next[i];
            AnimateVisualsForRotation(rotationOrigin, 90f);
            if (notifyTutorial)
                NotifyTutorialEvent(TutorialGameplayEvent.RotateCounterClockwise);
        }
    }

    void Lock()
    {
        if (visualTransitionCoroutine != null && !completingVisualTransition)
            StopCoroutine(visualTransitionCoroutine);

        visualTransitionCoroutine = null;
        hardDropVisualLockPending = false;

        bool toppedOut = false;
        bool isSpecial = data.special != SpecialType.None;

        for (int i = 0; i < cells.Count; i++)
            if (cells[i].y >= board.height) { toppedOut = true; break; }

        if (toppedOut)
        {
            RestoreHardDropPreviewNeighborBorders();

            foreach (var v in visuals) if (v) Destroy(v.gameObject);
            visuals.Clear();

            GetComponent<GameController>().OnPieceLocked(0, new System.Collections.Generic.List<Vector2Int>(),
                                                         0, 0f, new System.Collections.Generic.Dictionary<int, int>(),
                                                         new System.Collections.Generic.Dictionary<int, MonsterData>(),
                                                         new System.Collections.Generic.Dictionary<int,
                                                         System.Collections.Generic.List<int>>());

            GetComponent<GameController>().GameOver();
            enabled = false;
            return;
        }

        var gc = GetComponent<GameController>();

        // ---------- SPECIAL PIECES ----------
        if (isSpecial)
        {
            RestoreHardDropPreviewNeighborBorders();

            // Clean up active visuals + any hint overlays
            ClearHints();
            foreach (var v in visuals) if (v) Destroy(v.gameObject);
            visuals.Clear();

            // Landing/center cell (clamped)
            var center = cells[0];
            center.x = Mathf.Clamp(center.x, 0, board.width - 1);
            center.y = Mathf.Clamp(center.y, 0, board.height - 1);

            var toRemove = new List<Vector2Int>(); // This list is used by Death/Bomb/Bolt only
            var envAffected = new List<Vector2Int>(); // This list is used to clear obstacles/traps even on empty cells

            switch (data.special)
            {
                case SpecialType.Death:
                    {
                        int dropY = center.y;
                        while (dropY > 0 && board.IsFree(new Vector2Int(center.x, dropY - 1)))
                            dropY--;

                        var landing = new Vector2Int(center.x, dropY);

                        // Find the first monster below the landing in that column
                        MonsterData chosen = null;
                        for (int y = landing.y - 1; y >= 0; y--)
                        {
                            var c = new Vector2Int(landing.x, y);
                            if (!board.InBounds(c)) break;
                            if (!board.IsFree(c))
                            {
                                if (board.TryGetMonster(c, out var inst) && inst.data)
                                    chosen = inst.data;
                                break;
                            }
                        }

                        // Mark all monsters of that type for removal
                        if (chosen)
                        {
                            for (int y = 0; y < board.height; y++)
                                for (int x = 0; x < board.width; x++)
                                {
                                    var c = new Vector2Int(x, y);
                                    if (board.TryGetMonster(c, out var inst) && inst.data == chosen)
                                        toRemove.Add(c);
                                }
                        }
                        break;
                    }

                case SpecialType.Bomb:
                    {
                        // 3x3 around landing
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                var c = new Vector2Int(center.x + dx, center.y + dy);
                                if (!board.InBounds(c)) continue;

                                envAffected.Add(c); // Always include for obstacle clearing
                                if (!board.IsFree(c)) toRemove.Add(c);
                            }
                        break;
                    }

                case SpecialType.Bolt:
                    {
                        // Entire column including empty cells (for spike clearing)
                        for (int y = 0; y < board.height; y++)
                        {
                            var c = new Vector2Int(center.x, y);
                            if (!board.InBounds(c)) continue;

                            envAffected.Add(c);
                            if (!board.IsFree(c)) toRemove.Add(c); // Removes any occupied tiles in the column
                        }
                        break;
                    }

                case SpecialType.Earthquake:
                    {
                        if (AudioManager.I && data.specialSFX) AudioManager.I.PlaySFX(data.specialSFX);

                        // Flash vfx on whole board (or only occupied, based on SO flag)
                        var affectedEQ = new List<Vector2Int>();
                        for (int y = 0; y < board.height; y++)
                            for (int x = 0; x < board.width; x++)
                                affectedEQ.Add(new Vector2Int(x, y));
                        board.FlashCells(affectedEQ, data.specialFlashSprite, data.flashOnlyOccupied);

                        board.SettleAllColumns(true);

                        enabled = false;
                        int levelBeforeEQ = gc.CurrentLevel;

                        gc.StartCoroutine(CoEarthquakeDelayedClear(board, gc, levelBeforeEQ));

                        return;
                    }

                case SpecialType.SlowGravity:
                    {
                        gc?.ActivateSlowGravitySpecial(
                            data.slowGravityMultiplier,
                            data.slowGravityRampRateMultiplier);

                        for (int y = 0; y < board.height; y++)
                            for (int x = 0; x < board.width; x++)
                                envAffected.Add(new Vector2Int(x, y));
                        break;
                    }
            }

            var affectedEnv = new List<Vector2Int>();

            if (data.special == SpecialType.Bomb)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var c = new Vector2Int(center.x + dx, center.y + dy);
                        if (board.InBounds(c)) affectedEnv.Add(c);
                    }
            }
            else if (data.special == SpecialType.Bolt)
            {
                for (int y = center.y - 1; y >= 0; y--)
                {
                    var c = new Vector2Int(center.x, y);
                    if (board.InBounds(c)) affectedEnv.Add(c);
                }
            }

            if (AudioManager.I && data.specialSFX) AudioManager.I.PlaySFX(data.specialSFX);

            board.ApplySpecialToEnvironment(envAffected, data.special);
            board.FlashCells(
                data.special == SpecialType.SlowGravity ? envAffected : toRemove,
                data.specialFlashSprite,
                data.flashOnlyOccupied); // Flash on affected cells using data flags

            // Remove targets and make only the directly-above tiles fall sparsely
            board.RemoveCellsAndFall(toRemove, out var removedA, out int dmgA, out float chargeA);

            // Track stats for Death special unit removal
            if (data.special == SpecialType.Death && PlayerProgress.I != null && removedA != null && removedA.Count > 0)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.DeathblockUnitsRemoved, removedA.Count);

            enabled = false;
            int levelBeforeSpecial = gc.CurrentLevel;

            board.ClearFullLinesAnimated((rowsAfter, removedB, dmgB, chargeB, rowDamageB, rowDomB) =>
            {
                var colsByRowSpecial = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();

                if (gc != null)
                    gc.OnPieceLocked(rowsAfter, removedB, dmgA + dmgB, chargeA + chargeB, rowDamageB, rowDomB, colsByRowSpecial);

                if (gc != null && gc.CurrentLevel == levelBeforeSpecial && gc.CanSpawnNewPiece())
                    gc.SpawnNextPiece();
            });

            return;
        }

        // ---------- NORMAL PIECES ----------

        ClearHints();
        int pieceGroupId = board.AllocatePieceGroupId();
        for (int i = 0; i < cells.Count; i++)
        {
            MonsterData md = (i < monstersForCells.Count) ? monstersForCells[i] : null;
            var sprite = GetCurrentMonsterPortrait(md);
            var placed = board.InstantiateTileUI(color, sprite, data ? data.backgroundImage : null);

            placed.anchoredPosition = board.CellToAnchoredPos(cells[i]);
            board.Place(cells[i], placed);
            board.SetMonsterAt(cells[i], new Board.MonsterInstance(md, pieceGroupId));
        }

        gc?.levelModifierController?.OnNormalPiecePlaced(cells);

        for (int i = 0; i < cells.Count; i++)
            board.ApplyFloorEffectOnPlacement(cells[i]); // Apply any floor effects on the cell as the piece locks in, which may damage or heal the monster just placed

        gc?.PlayPieceLockSFX();

        foreach (var v in visuals)

            // Recompute per-edge border thickness for the new blocks + neighbors
            for (int i = 0; i < cells.Count; i++)
                board.RefreshTileBordersAround(cells[i]);

        hardDropPreviewBorderCells.Clear();

        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        var colsByRow = new Dictionary<int, List<int>>();
        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            if (c.y < 0 || c.y >= board.height) continue;
            if (!colsByRow.TryGetValue(c.y, out var list)) { list = new List<int>(); colsByRow[c.y] = list; }
            if (!list.Contains(c.x)) list.Add(c.x);
        }

        // Call the extended Board method
        enabled = false;
        int levelBefore = gc.CurrentLevel;

        board.ClearFullLinesAnimated((rowsCleared, removedCells, damageFromMonsters, specialChargeFromMonsters, rowDamage, rowDominantMonster) =>
        {
            if (gc != null)
                gc.OnPieceLocked(rowsCleared, removedCells, damageFromMonsters, specialChargeFromMonsters, rowDamage, rowDominantMonster, colsByRow);

            if (gc != null && gc.CurrentLevel == levelBefore && gc.CanSpawnNewPiece())
                gc.SpawnNextPiece();
        });
    }

    public void ResetPiece()
    {
        enabled = false;
        CancelVisualTransition();
        ClearHints();

        // Remove only the active falling tile visuals spawned
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                Destroy(visuals[i].gameObject);
        }
        visuals.Clear();

        cells.Clear();
        fallTimer = 0f;
        lockTimer = 0f;
    }

    public bool HasActiveCells => enabled && cells.Count > 0;

    public bool TryTranslate(Vector2Int delta, bool resetLockTimer)
    {
        bool moved = TryMove(delta);
        if (moved && resetLockTimer)
            lockTimer = 0f;

        return moved;
    }

    public void RotateClockwiseExternal()
    {
        RotateCW();
    }

    public void ForceLockImmediate()
    {
        Lock();
    }

    public bool OverlapsPlacedCells()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (cell.y >= board.height)
                continue;

            if (cell.y < 0)
                return true;

            if (!board.IsFree(cell))
                return true;
        }

        return false;
    }

    public bool TryResolvePlacedOverlapByShiftingUp()
    {
        if (board == null || cells.Count == 0)
            return true;

        bool moved = false;
        int maxAttempts = Mathf.Max(1, board.height + 4);

        for (int attempt = 0; attempt <= maxAttempts; attempt++)
        {
            if (!OverlapsPlacedCells())
            {
                if (moved)
                {
                    fallTimer = 0f;
                    lockTimer = 0f;
                    SyncVisuals();
                }

                return true;
            }

            Shift(Vector2Int.up);
            moved = true;
        }

        if (moved)
            SyncVisuals();

        return !OverlapsPlacedCells();
    }

    public void SetMonsters(MonsterData[] arr)
    {
        monstersForCells.Clear();
        if (arr != null) monstersForCells.AddRange(arr);
    }

    void ClearHints()
    {
        for (int i = 0; i < hintOverlays.Count; i++)
            if (hintOverlays[i]) Destroy(hintOverlays[i].gameObject);
        hintOverlays.Clear();
    }

    void UpdateSpecialHints()
    {
        ClearHints();
        if (data.special == SpecialType.None) return;

        var center = cells[0]; // Compute the landing cell for the special center
        var dropY = center.y; // Drop until blocked (or bottom)

        while (dropY > 0 && board.IsFree(new Vector2Int(center.x, dropY - 1))) dropY--;
        var landing = new Vector2Int(Mathf.Clamp(center.x, 0, board.width - 1),
                                     Mathf.Clamp(dropY, 0, board.height - 1));

        // Build affected set based on the special behavior
        var affected = new HashSet<Vector2Int>();

        switch (data.special)
        {
            case SpecialType.Bomb:
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var c = new Vector2Int(landing.x + dx, landing.y + dy);
                        if (board.InBounds(c)) affected.Add(c);
                    }
                break;

            case SpecialType.Bolt:
                for (int y = 0; y < board.height; y++)
                {
                    var c = new Vector2Int(landing.x, y);
                    if (board.InBounds(c)) affected.Add(c);
                }
                break;

            case SpecialType.Death:
                MonsterData chosen = null;
                for (int y = landing.y - 1; y >= 0; y--)
                {
                    var c = new Vector2Int(landing.x, y);
                    if (!board.IsFree(c))
                    {
                        if (board.TryGetMonster(c, out var inst) && inst.data) chosen = inst.data;
                        break;
                    }
                }
                if (chosen)
                {
                    for (int y = 0; y < board.height; y++)
                        for (int x = 0; x < board.width; x++)
                        {
                            var c = new Vector2Int(x, y);
                            if (board.TryGetMonster(c, out var inst) && inst.data == chosen)
                                affected.Add(c);
                        }
                }
                break;

            case SpecialType.Earthquake:
                for (int y = 0; y < board.height; y++)
                    for (int x = 0; x < board.width; x++)
                    {
                        var c = new Vector2Int(x, y);
                        if (!board.IsFree(c))
                        {
                            var below = new Vector2Int(x, y - 1);
                            if (y > 0 && board.IsFree(below)) affected.Add(c);
                        }
                    }
                break;
        }

        // Draw the tinted overlays
        foreach (var c in affected)
        {
            var img = new GameObject("SpecialHint", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            img.color = hintColor;
            var rt = img.rectTransform;
            rt.SetParent(board.gridRoot, false);
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = board.CellToAnchoredPos(c);
            hintOverlays.Add(rt);
        }
    }

    List<Vector2Int> ComputeLandingCells()
    {
        var landing = new List<Vector2Int>(cells); // Make a working copy of the current cells

        // Try dropping the copy straight down until it would collide
        bool canDrop = true;
        while (canDrop)
        {
            // Build next test
            var next = new Vector2Int[landing.Count];
            for (int i = 0; i < landing.Count; i++)
                next[i] = landing[i] + Vector2Int.down;

            if (board.Valid(next))
            {
                for (int i = 0; i < landing.Count; i++) landing[i] = next[i];
            }
            else
            {
                canDrop = false;
            }
        }

        // Clamp to visible board only (ignore any cells above the top)
        for (int i = landing.Count - 1; i >= 0; i--)
            if (landing[i].y < 0 || landing[i].y >= board.height)
                landing.RemoveAt(i);

        return landing;
    }

    void UpdateNormalHints()
    {
        var gc = FindFirstObjectByType<GameController>();

        ClearHints();

        if (gc != null && gc.disableLandingHint)
            return; // Skip if disabled by run mods

        var landing = ComputeLandingCells();
        if (landing.Count == 0) return;

        foreach (var c in landing)
        {
            var img = new GameObject("GhostHint", typeof(Image)).GetComponent<Image>();
            img.sprite = OnePx();
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.color = hintColor;

            var rt = img.rectTransform;
            rt.SetParent(board.gridRoot, false);
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = board.CellToAnchoredPos(c);

            hintOverlays.Add(rt);
        }
    }

    public void SetFallInterval(float seconds, bool resetAccumulator)
    {
        fallInterval = Mathf.Max(0.01f, seconds);

        if (resetAccumulator)
        {
            fallTimer = 0f;
            lockTimer = 0f;
        }
    }

    IEnumerator CoEarthquakeDelayedClear(Board boardRef, GameController gcRef, int levelBeforeEQ)
    {
        if (!boardRef) yield break;

        // Same timing used for cascade clear visuals
        yield return null;
        if (boardRef.cascadeClearVisualDelay > 0f)
            yield return new WaitForSecondsRealtime(boardRef.cascadeClearVisualDelay);

        boardRef.ClearFullLinesAnimated((rowsEQ, removedEQ, dmgEQ, chargeEQ, rowDamageEQ, rowDomEQ) =>
        {
            if (PlayerProgress.I != null && rowsEQ > 0)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.EarthquakeRowClears, rowsEQ);

            var emptyCols = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();
            if (gcRef != null)
                gcRef.OnPieceLocked(rowsEQ, removedEQ, dmgEQ, chargeEQ, rowDamageEQ, rowDomEQ, emptyCols);

            if (gcRef != null && gcRef.CurrentLevel == levelBeforeEQ && gcRef.CanSpawnNewPiece())
                gcRef.SpawnNextPiece();
        });
    }

    // ========== Inline Border Color Change ==========

    public void SetInlineBorderColor(Color c)
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            var img = visuals[i] ? visuals[i].GetComponent<UnityEngine.UI.Image>() : null;
            if (img) img.color = c;
        }
    }

    public void ResetInlineBorderColor(Color c)
    {
        SetInlineBorderColor(c);
    }

    Sprite GetCurrentMonsterPortrait(MonsterData md)
    {
        if (!md) return null;
        int skin = MonsterSkinStore.GetValidSelected(md);
        return MonsterSkinStore.GetPortrait(md, skin);
    }

    public void RefreshVisualsExternal()
    {
        if (cells.Count == 0)
            return;

        SyncVisuals();
    }

    public void RefreshLandingHintsExternal()
    {
        if (cells.Count == 0 || board == null)
            return;

        if (data != null && data.special != SpecialType.None)
            UpdateSpecialHints();
        else
            UpdateNormalHints();
    }

    void NotifyTutorialEvent(TutorialGameplayEvent gameplayEvent)
    {
        var gc = GetComponent<GameController>();
        if (!gc) gc = FindFirstObjectByType<GameController>();
        gc?.NotifyTutorialGameplayEvent(gameplayEvent);
    }

}
