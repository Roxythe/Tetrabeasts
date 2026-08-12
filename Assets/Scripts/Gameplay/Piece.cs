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

    [Header("Hint Refresh")]
    [SerializeField, Min(0.01f)] float hintSafetyRefreshSeconds = 0.12f;

    [HideInInspector] public TetrominoData data; // Assigned by controller
    [HideInInspector] public Color color = Color.cyan;

    Vector2Int origin; // Rotation/translation origin
    readonly List<Vector2Int> cells = new();
    readonly List<RectTransform> visuals = new();
    readonly List<RectTransform> activeVisualPool = new();
    readonly HashSet<RectTransform> pooledActiveVisuals = new();
    readonly List<MonsterData> monstersForCells = new();
    readonly List<RectTransform> hintOverlays = new();
    readonly List<RectTransform> hintPool = new();
    readonly List<Vector2Int> hintCells = new();
    readonly List<Vector2Int> scratchHintCells = new();
    readonly List<Vector2Int> candidateCells = new();
    readonly List<Vector2Int> landingCells = new();
    readonly List<Vector2Int> landingTestCells = new();
    readonly List<Vector2> visualStartPositions = new();
    readonly List<Vector2> visualTargetPositions = new();
    readonly HashSet<Vector2Int> activeCellSet = new();
    readonly HashSet<Vector2Int> hardDropPreviewCells = new();
    readonly HashSet<Vector2Int> specialHintCells = new();
    static readonly Comparison<Vector2Int> HintCellComparison = CompareHintCells;
    static readonly Color hintColor = new Color(1f, 0f, 0f, 0.5f); // Light red
    GameController gameController;

    float fallTimer = 0f, lockTimer;
    Coroutine visualTransitionCoroutine;
    bool hardDropVisualLockPending;
    bool completingVisualTransition;
    bool zipPadDropInProgress;
    bool hintsDirty = true;
    float nextHintSafetyRefreshRealtime;
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

    void Awake()
    {
        gameController = GetComponent<GameController>();
    }

    void OnEnable()
    {
        fallTimer = 0f; lockTimer = 0f;
        ReleaseActiveVisuals();
        cells.Clear();
        ClearHints();
        hardDropVisualLockPending = false;
        visualTransitionCoroutine = null;
        completingVisualTransition = false;
        zipPadDropInProgress = false;
        MarkHintsDirty();
        nextHintSafetyRefreshRealtime = 0f;
    }

    void OnDisable()
    {
        ClearHints();
    }

    void OnDestroy()
    {
        DestroyActiveVisuals(visuals);
        DestroyActiveVisuals(activeVisualPool);
        pooledActiveVisuals.Clear();
        DestroyHints(hintOverlays);
        DestroyHints(hintPool);
        hintCells.Clear();
        scratchHintCells.Clear();
        specialHintCells.Clear();
    }

    public void SpawnAtTop()
    {
        board.RecomputeCellMetrics();

        // Center top
        origin = new Vector2Int(board.width / 2, board.height);
        foreach (var c in data.cells) cells.Add(origin + c);

        // If blocked, try one row lower; else game over
        if (!board.Valid(cells))
        {
            Shift(Vector2Int.down);
            if (!board.Valid(cells)) { enabled = false; return; }
        }
        BuildVisuals();
        MarkHintsDirty();
        RefreshHintsIfNeeded(force: true);
    }

    private void Start()
    {
        
    }

    void Update()
    {
        var gc = GetGameController();

        bool tutorialPromptActive = gc != null && gc.IsTutorialPromptActive;
        bool gameplaySuspended = gc != null && gc.IsGameplaySuspended;

        if (ConfirmationPopupUI.IsAnyShowing)
            return;

        // Allow limited inputs during tutorial prompts, but still block on other suspension states.
        if (gameplaySuspended && !tutorialPromptActive)
            return;

        if (hardDropVisualLockPending)
            return;

        if (gc != null && gc.IsTutorialPieceInputBlocked)
        {
            fallTimer = 0f;
            lockTimer = 0f;
            return;
        }

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
                               (gc == null || !gc.IsTutorialHardDropInputGraceActive) &&
                               TetrabeastsControls.WasPressed(TetrabeastsControlAction.HardDrop);

        if (moveLeftPressed && TryMove(Vector2Int.left))
            NotifyTutorialEvent(TutorialGameplayEvent.MoveLeft);

        if (moveRightPressed && TryMove(Vector2Int.right))
            NotifyTutorialEvent(TutorialGameplayEvent.MoveRight);

        if (softDropPressed)
            SoftDrop(true);

        if (rotateCwPressed && rotateCcwPressed)
        {
            if (TetrabeastsControls.TryGetPreferredRotationAction(out var rotationAction))
            {
                rotateCwPressed = rotationAction == TetrabeastsControlAction.RotateClockwise;
                rotateCcwPressed = rotationAction == TetrabeastsControlAction.RotateCounterClockwise;
            }
            else
            {
                rotateCcwPressed = false;
            }
        }

        if (rotateCwPressed)
            RotateCW(true);
        else if (rotateCcwPressed)
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

        if (TryTriggerZipPadUnderActivePiece())
            return;

        if (!enabled || cells.Count == 0 || board == null)
            return;

        RefreshHintsIfNeeded();
    }

    public System.Collections.Generic.IReadOnlyList<RectTransform> GetTutorialHighlightTargets()
    {
        return visuals;
    }

    RectTransform GetOrCreateActiveVisual()
    {
        RectTransform rt = null;
        while (activeVisualPool.Count > 0 && !rt)
        {
            int last = activeVisualPool.Count - 1;
            rt = activeVisualPool[last];
            activeVisualPool.RemoveAt(last);
            pooledActiveVisuals.Remove(rt);
        }

        if (!rt)
            rt = Instantiate(activeTilePrefab, board.gridRoot).rectTransform;
        else
            rt.SetParent(board.gridRoot, false);

        rt.gameObject.SetActive(true);
        var img = rt.GetComponent<Image>();
        if (img)
        {
            img.enabled = true;
            img.sprite = null;
            img.raycastTarget = false;
            img.color = new Color(0f, 0f, 0f, 0f);
        }

        var anyOutline = rt.GetComponent<UnityEngine.UI.Outline>();
        if (anyOutline) Destroy(anyOutline);

        return rt;
    }

    Image GetOrCreateActiveChildImage(RectTransform parent, string childName)
    {
        var child = parent.Find(childName);
        Image img;
        if (child)
        {
            img = child.GetComponent<Image>();
            if (!img)
                img = child.gameObject.AddComponent<Image>();
        }
        else
        {
            var go = new GameObject(childName, typeof(Image));
            go.transform.SetParent(parent, false);
            img = go.GetComponent<Image>();
        }

        img.name = childName;
        img.enabled = true;
        img.raycastTarget = false;
        img.color = Color.white;
        img.gameObject.SetActive(true);
        return img;
    }

    void HideActiveChild(RectTransform parent, string childName)
    {
        var child = parent ? parent.Find(childName) : null;
        if (!child)
            return;

        if (child.TryGetComponent(out Image img))
        {
            img.sprite = null;
            img.enabled = true;
        }

        if (child.TryGetComponent(out TetrominoBackgroundPulse pulse))
            pulse.ResetPulseVisuals();

        child.gameObject.SetActive(false);
    }

    void ReleaseActiveVisual(RectTransform rt)
    {
        if (!rt || pooledActiveVisuals.Contains(rt))
            return;

        HideActiveChild(rt, "ActiveFill");
        HideActiveChild(rt, "MonsterPortrait");
        HideActiveChild(rt, "SpecialIcon");

        var img = rt.GetComponent<Image>();
        if (img)
        {
            img.sprite = null;
            img.raycastTarget = false;
            img.color = new Color(0f, 0f, 0f, 0f);
        }

        if (board && board.gridRoot)
            rt.SetParent(board.gridRoot, false);

        rt.gameObject.SetActive(false);
        pooledActiveVisuals.Add(rt);
        activeVisualPool.Add(rt);
    }

    void ReleaseActiveVisuals()
    {
        for (int i = 0; i < visuals.Count; i++)
            ReleaseActiveVisual(visuals[i]);

        visuals.Clear();
    }

    void DestroyActiveVisuals(List<RectTransform> targetVisuals)
    {
        for (int i = 0; i < targetVisuals.Count; i++)
        {
            if (targetVisuals[i])
                Destroy(targetVisuals[i].gameObject);
        }

        targetVisuals.Clear();
    }

    void BuildVisuals()
    {
        bool isSpecial = data.special != SpecialType.None;

        ReleaseActiveVisuals();

        if (board == null || board.gridRoot == null) return;

        var activeSet = RebuildActiveCellSet();
        var gc = GetGameController();
        Color borderColor = (gc && gc.immunityActive) ? board.immuneBorderColor : board.normalBorderColor;

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];

            // Base tile
            var rt = GetOrCreateActiveVisual();
            HideActiveChild(rt, "MonsterPortrait");
            HideActiveChild(rt, "SpecialIcon");

            // Size/position
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = board.CellToAnchoredPos(c);

            // Pick outline color (gold while immune, otherwise black)
            board.SetInlineBorderColor(rt, borderColor);

            // Build inner fill first so ApplySharedEdges can resize it correctly on shared edges
            var fill = GetOrCreateActiveChildImage(rt, "ActiveFill");
            fill.raycastTarget = false;
            fill.sprite = board.ResolveTetrominoFillSprite(data ? data.backgroundImage : null);
            fill.type = UnityEngine.UI.Image.Type.Simple;
            fill.preserveAspect = false;
            fill.color = color;

            var frt = fill.rectTransform;
            frt.SetParent(rt, false);
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta;
            frt.anchoredPosition = Vector2.zero;
            frt.SetAsFirstSibling(); // Icons/portraits sit on top
            board.ConfigureTetrominoBackgroundPulse(fill, color, data ? data.backgroundImage : null);

            // Halve thickness on shared edges
            bool L = IsVisualEdgeShared(c, c + Vector2Int.left, activeSet);
            bool R = IsVisualEdgeShared(c, c + Vector2Int.right, activeSet);
            bool U = IsVisualEdgeShared(c, c + Vector2Int.up, activeSet);
            bool D = IsVisualEdgeShared(c, c + Vector2Int.down, activeSet);

            board.ApplySharedEdges(rt, L, R, U, D);

            // Portrait/special icon
            if (isSpecial && data.specialSprite != null)
            {
                var p = GetOrCreateActiveChildImage(rt, "SpecialIcon");
                p.sprite = data.specialSprite; p.preserveAspect = true;
                p.raycastTarget = false;

                var prt = p.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = frt.sizeDelta - new Vector2(2f, 2f);
                prt.anchoredPosition = Vector2.zero;
                prt.localScale = Vector3.one;
                prt.SetAsLastSibling();
            }
            else if (!isSpecial && i < monstersForCells.Count && monstersForCells[i])
            {
                var portrait = GetCurrentMonsterPortrait(monstersForCells[i]);
                if (portrait)
                {
                    var p = GetOrCreateActiveChildImage(rt, "MonsterPortrait");
                    p.sprite = portrait; p.preserveAspect = true;
                    p.raycastTarget = false;

                    var prt = p.rectTransform;
                    prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                    prt.sizeDelta = frt.sizeDelta - new Vector2(2f, 2f);
                    prt.localScale = Vector3.one * 0.9f;
                    prt.anchoredPosition = Vector2.zero;
                    prt.SetAsLastSibling();
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

        FillCellTargetPositions(visualTargetPositions);
        SetVisualsToTargetPositions(visualTargetPositions);
        ApplyActiveVisualBorders();
        MarkHintsDirty();
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

    void FillCellTargetPositions(List<Vector2> targets)
    {
        targets.Clear();
        for (int i = 0; i < cells.Count; i++)
            targets.Add(board.CellToAnchoredPos(cells[i]));
    }

    void CaptureVisualPositions(List<Vector2> positions)
    {
        positions.Clear();
        for (int i = 0; i < visuals.Count; i++)
            positions.Add(visuals[i] ? visuals[i].anchoredPosition : Vector2.zero);
    }

    void SetVisualsToTargetPositions(IReadOnlyList<Vector2> targets)
    {
        int count = Mathf.Min(visuals.Count, targets.Count);
        for (int i = 0; i < count; i++)
            if (visuals[i])
                visuals[i].anchoredPosition = targets[i];
    }

    void ApplyActiveVisualBorders()
    {
        var activeSet = RebuildActiveCellSet();

        for (int i = 0; i < visuals.Count && i < cells.Count; i++)
        {
            if (!visuals[i])
                continue;

            var c = cells[i];
            bool L = IsVisualEdgeShared(c, c + Vector2Int.left, activeSet);
            bool R = IsVisualEdgeShared(c, c + Vector2Int.right, activeSet);
            bool U = IsVisualEdgeShared(c, c + Vector2Int.up, activeSet);
            bool D = IsVisualEdgeShared(c, c + Vector2Int.down, activeSet);

            board.ApplySharedEdges(visuals[i], L, R, U, D);
        }
    }

    HashSet<Vector2Int> RebuildActiveCellSet()
    {
        activeCellSet.Clear();
        for (int i = 0; i < cells.Count; i++)
            activeCellSet.Add(cells[i]);
        return activeCellSet;
    }

    bool IsVisualEdgeShared(Vector2Int cell, Vector2Int neighbor, HashSet<Vector2Int> activeSet)
    {
        if (!board)
            return false;

        if (activeSet.Contains(neighbor))
            return !board.IsObstacleCell(cell);

        return board.ShouldShareInlineBorderEdge(cell, neighbor);
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

        var activeSet = RebuildActiveCellSet();
        var previewCells = hardDropPreviewCells;
        previewCells.Clear();

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

            bool L = IsVisualEdgeShared(c, c + Vector2Int.left, activeSet);
            bool R = IsVisualEdgeShared(c, c + Vector2Int.right, activeSet);
            bool U = IsVisualEdgeShared(c, c + Vector2Int.up, activeSet);
            bool D = IsVisualEdgeShared(c, c + Vector2Int.down, activeSet);

            board.ApplySharedEdges(rt, L, R, U, D);
            hardDropPreviewBorderCells.Add(c);
        }

        previewCells.Clear();
    }

    void AddHardDropPreviewNeighbor(Vector2Int cell, HashSet<Vector2Int> activeSet, HashSet<Vector2Int> previewCells)
    {
        if (!board.InBounds(cell) || activeSet.Contains(cell) || board.IsFree(cell))
            return;

        previewCells.Add(cell);
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

        CaptureVisualPositions(visualStartPositions);
        FillCellTargetPositions(visualTargetPositions);
        CancelVisualTransition(false);

        if (previewSettledBorders)
            ApplyHardDropSettledBorderPreview();

        visualTransitionCoroutine = StartCoroutine(CoAnimateVisualTransition(
            visualStartPositions,
            visualTargetPositions,
            duration,
            rotateAroundPivot,
            pivot,
            rotationDegrees,
            onComplete));
    }

    IEnumerator CoAnimateVisualTransition(
        IReadOnlyList<Vector2> starts,
        IReadOnlyList<Vector2> targets,
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

            for (int i = 0; i < visuals.Count && i < starts.Count && i < targets.Count; i++)
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
        if (gameController)
            return gameController;

        gameController = GetComponent<GameController>();
        if (!gameController)
            gameController = FindFirstObjectByType<GameController>();

        return gameController;
    }

    bool TryMove(Vector2Int delta, bool syncVisuals = true)
    {
        candidateCells.Clear();
        for (int i = 0; i < cells.Count; i++)
            candidateCells.Add(cells[i] + delta);

        if (!board.Valid(candidateCells)) return false;

        for (int i = 0; i < cells.Count; i++) cells[i] = candidateCells[i];
        origin += delta;
        if (syncVisuals)
            SyncVisuals();
        MarkHintsDirty();
        return true;
    }

    void Shift(Vector2Int delta)
    {
        for (int i = 0; i < cells.Count; i++) cells[i] += delta;
        origin += delta;
        MarkHintsDirty();
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

    bool TryTriggerZipPadUnderActivePiece()
    {
        if (!enabled || zipPadDropInProgress || board == null || cells.Count == 0)
            return false;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (!board.InBounds(cell))
                continue;

            if (!board.TryTriggerZipPadForActivePiece(cell))
                continue;

            zipPadDropInProgress = true;
            ClearHints();
            HardDrop(false);
            return true;
        }

        return false;
    }

    void RotateCW(bool notifyTutorial = false)
    {
        Vector2Int rotationOrigin = origin;
        candidateCells.Clear();
        for (int i = 0; i < cells.Count; i++)
        {
            var r = cells[i] - origin; // Around origin
            var rot = new Vector2Int(r.y, -r.x);
            candidateCells.Add(origin + rot);
        }
        if (board.Valid(candidateCells))
        {
            for (int i = 0; i < cells.Count; i++) cells[i] = candidateCells[i];
            AnimateVisualsForRotation(rotationOrigin, -90f);
            MarkHintsDirty();
            if (notifyTutorial)
                NotifyTutorialEvent(TutorialGameplayEvent.RotateClockwise);
        }
    }

    void RotateCCW(bool notifyTutorial = false)
    {
        Vector2Int rotationOrigin = origin;
        candidateCells.Clear();
        for (int i = 0; i < cells.Count; i++)
        {
            var r = cells[i] - origin;
            var rot = new Vector2Int(-r.y, r.x);
            candidateCells.Add(origin + rot);
        }
        if (board.Valid(candidateCells))
        {
            for (int i = 0; i < cells.Count; i++) cells[i] = candidateCells[i];
            AnimateVisualsForRotation(rotationOrigin, 90f);
            MarkHintsDirty();
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
        zipPadDropInProgress = false;

        var gc = GetGameController();
        bool toppedOut = false;
        bool isSpecial = data.special != SpecialType.None;

        for (int i = 0; i < cells.Count; i++)
            if (cells[i].y >= board.height) { toppedOut = true; break; }

        if (toppedOut)
        {
            RestoreHardDropPreviewNeighborBorders();
            ClearHints();

            var topOutColsByRow = PlaceTopOutPieceTiles();
            gc?.PlayPieceLockSFX();

            ReleaseActiveVisuals();
            hardDropPreviewBorderCells.Clear();

            gc?.OnPieceLocked(0, new List<Vector2Int>(),
                              0, 0f, new Dictionary<int, int>(),
                              new Dictionary<int, MonsterData>(),
                              topOutColsByRow);

            if (gc)
                StartCoroutine(CoTriggerGameOverAfterTopOutPlacement(gc));

            enabled = false;
            return;
        }

        // ---------- SPECIAL PIECES ----------
        if (isSpecial)
        {
            RestoreHardDropPreviewNeighborBorders();

            // Clean up active visuals + any hint overlays
            ClearHints();
            ReleaseActiveVisuals();

            // Landing/center cell (clamped)
            var center = cells[0];
            center.x = Mathf.Clamp(center.x, 0, board.width - 1);
            center.y = Mathf.Clamp(center.y, 0, board.height - 1);

            var toRemove = new List<Vector2Int>(); // This list is used by Death/Bomb/Bolt only
            var envAffected = new List<Vector2Int>(); // This list is used to clear obstacles/traps even on empty cells

            if (data.special != SpecialType.SlowGravity)
                board.PlaySpecialBoardVFX(data.special);

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
            if (data.special != SpecialType.SlowGravity)
            {
                board.FlashCells(
                    toRemove,
                    data.specialFlashSprite,
                    data.flashOnlyOccupied); // Flash on affected cells using data flags
            }

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
            var sprite = data && data.special != SpecialType.None
                ? data.specialSprite
                : GetCurrentMonsterPortrait(md);
            var placed = board.InstantiateTileUI(color, sprite, data ? data.backgroundImage : null, portraitScale: 0.9f);

            placed.anchoredPosition = board.CellToAnchoredPos(cells[i]);
            board.Place(cells[i], placed);
            board.SetMonsterAt(cells[i], new Board.MonsterInstance(md, pieceGroupId));
        }

        gc?.levelModifierController?.OnNormalPiecePlaced(cells);

        for (int i = 0; i < cells.Count; i++)
            board.ApplyFloorEffectOnPlacement(cells[i]); // Apply any floor effects on the cell as the piece locks in, which may damage or heal the monster just placed

        gc?.PlayPieceLockSFX();

        // Recompute per-edge border thickness for the new blocks + neighbors
        for (int i = 0; i < cells.Count; i++)
            board.RefreshTileBordersAround(cells[i]);

        hardDropPreviewBorderCells.Clear();

        ReleaseActiveVisuals();

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
        }, clearOriginColumnsByRow: colsByRow);
    }

    Dictionary<int, List<int>> PlaceTopOutPieceTiles()
    {
        var colsByRow = new Dictionary<int, List<int>>();
        if (!board)
            return colsByRow;

        int pieceGroupId = board.AllocatePieceGroupId();

        for (int i = 0; i < cells.Count; i++)
        {
            MonsterData md = (i < monstersForCells.Count) ? monstersForCells[i] : null;
            var sprite = data && data.special != SpecialType.None
                ? data.specialSprite
                : GetCurrentMonsterPortrait(md);
            var placed = board.InstantiateTileUI(color, sprite, data ? data.backgroundImage : null, portraitScale: 0.9f);
            var cell = cells[i];

            placed.anchoredPosition = board.CellToAnchoredPos(cell);
            board.Place(cell, placed);

            if (board.InBounds(cell))
            {
                board.SetMonsterAt(cell, new Board.MonsterInstance(md, pieceGroupId));

                if (!colsByRow.TryGetValue(cell.y, out var columns))
                {
                    columns = new List<int>();
                    colsByRow[cell.y] = columns;
                }

                if (!columns.Contains(cell.x))
                    columns.Add(cell.x);
            }
        }

        for (int i = 0; i < cells.Count; i++)
            board.RefreshTileBordersAround(cells[i]);

        return colsByRow;
    }

    IEnumerator CoTriggerGameOverAfterTopOutPlacement(GameController gc)
    {
        yield return null;

        if (gc)
            gc.GameOver();
    }

    public void ResetPiece()
    {
        enabled = false;
        CancelVisualTransition();
        ClearHints();

        // Remove only the active falling tile visuals spawned
        ReleaseActiveVisuals();

        cells.Clear();
        fallTimer = 0f;
        lockTimer = 0f;
        zipPadDropInProgress = false;
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
        for (int i = hintOverlays.Count - 1; i >= 0; i--)
            ReleaseHintAt(i);

        hintCells.Clear();
        scratchHintCells.Clear();
        specialHintCells.Clear();
        MarkHintsDirty();
    }

    void MarkHintsDirty()
    {
        hintsDirty = true;
    }

    void RefreshHintsIfNeeded(bool force = false)
    {
        if (!force && !hintsDirty && Time.unscaledTime < nextHintSafetyRefreshRealtime)
            return;

        RefreshHintsNow(forceLayout: force);
        hintsDirty = false;
        nextHintSafetyRefreshRealtime = Time.unscaledTime + Mathf.Max(0.01f, hintSafetyRefreshSeconds);
    }

    void RefreshHintsNow(bool forceLayout = false)
    {
        if (data != null && data.special != SpecialType.None)
            UpdateSpecialHints(forceLayout);
        else
            UpdateNormalHints(forceLayout);
    }

    void UpdateSpecialHints(bool forceLayout = false)
    {
        scratchHintCells.Clear();
        specialHintCells.Clear();

        if (data == null || data.special == SpecialType.None || board == null || board.gridRoot == null || cells.Count == 0)
        {
            SetActiveHints(scratchHintCells, "SpecialHint", forceLayout);
            return;
        }

        var center = cells[0]; // Compute the landing cell for the special center
        var dropY = center.y; // Drop until blocked (or bottom)

        while (dropY > 0 && board.IsFree(new Vector2Int(center.x, dropY - 1))) dropY--;
        var landing = new Vector2Int(Mathf.Clamp(center.x, 0, board.width - 1),
                                     Mathf.Clamp(dropY, 0, board.height - 1));

        switch (data.special)
        {
            case SpecialType.Bomb:
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var c = new Vector2Int(landing.x + dx, landing.y + dy);
                        if (board.InBounds(c)) AddSpecialHintCell(c);
                    }
                break;

            case SpecialType.Bolt:
                for (int y = 0; y < board.height; y++)
                {
                    var c = new Vector2Int(landing.x, y);
                    if (board.InBounds(c)) AddSpecialHintCell(c);
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
                                AddSpecialHintCell(c);
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
                            if (y > 0 && board.IsFree(below)) AddSpecialHintCell(c);
                        }
                    }
                break;
        }

        scratchHintCells.Sort(HintCellComparison);
        SetActiveHints(scratchHintCells, "SpecialHint", forceLayout);
    }

    List<Vector2Int> ComputeLandingCells()
    {
        landingCells.Clear();
        for (int i = 0; i < cells.Count; i++)
            landingCells.Add(cells[i]);

        // Try dropping the copy straight down until it would collide
        bool canDrop = true;
        while (canDrop)
        {
            landingTestCells.Clear();
            for (int i = 0; i < landingCells.Count; i++)
                landingTestCells.Add(landingCells[i] + Vector2Int.down);

            if (board.Valid(landingTestCells))
            {
                for (int i = 0; i < landingCells.Count; i++)
                    landingCells[i] = landingTestCells[i];
            }
            else
            {
                canDrop = false;
            }
        }

        // Clamp to visible board only (ignore any cells above the top)
        for (int i = landingCells.Count - 1; i >= 0; i--)
            if (landingCells[i].y < 0 || landingCells[i].y >= board.height)
                landingCells.RemoveAt(i);

        return landingCells;
    }

    void UpdateNormalHints(bool forceLayout = false)
    {
        var gc = GetGameController();

        scratchHintCells.Clear();

        if (gc != null && gc.disableLandingHint)
        {
            SetActiveHints(scratchHintCells, "GhostHint", forceLayout);
            return; // Skip if disabled by run mods
        }

        var landing = ComputeLandingCells();
        for (int i = 0; i < landing.Count; i++)
            scratchHintCells.Add(landing[i]);

        SetActiveHints(scratchHintCells, "GhostHint", forceLayout);
    }

    static int CompareHintCells(Vector2Int a, Vector2Int b)
    {
        int y = a.y.CompareTo(b.y);
        return y != 0 ? y : a.x.CompareTo(b.x);
    }

    void AddSpecialHintCell(Vector2Int cell)
    {
        if (specialHintCells.Add(cell))
            scratchHintCells.Add(cell);
    }

    void SetActiveHints(List<Vector2Int> desiredCells, string objectName, bool forceLayout = false)
    {
        if (board == null || board.gridRoot == null)
        {
            ClearHints();
            return;
        }

        if (!forceLayout && HintsMatch(desiredCells, objectName))
            return;

        while (hintOverlays.Count > desiredCells.Count)
            ReleaseHintAt(hintOverlays.Count - 1);

        while (hintOverlays.Count < desiredCells.Count)
        {
            var rt = GetOrCreateHint(objectName);
            hintOverlays.Add(rt);
            hintCells.Add(default);
        }

        var cellSize = board.GetCellSize();
        for (int i = 0; i < desiredCells.Count; i++)
        {
            var rt = hintOverlays[i];
            if (!rt)
                rt = hintOverlays[i] = GetOrCreateHint(objectName);

            if (rt.parent != board.gridRoot)
                rt.SetParent(board.gridRoot, false);

            if (rt.name != objectName)
                rt.name = objectName;

            var img = rt.GetComponent<Image>();
            if (img)
            {
                img.sprite = OnePx();
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.color = hintColor;
            }

            rt.sizeDelta = cellSize;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = board.CellToAnchoredPos(desiredCells[i]);
            rt.gameObject.SetActive(true);
            rt.SetAsLastSibling();

            hintCells[i] = desiredCells[i];
        }
    }

    bool HintsMatch(List<Vector2Int> desiredCells, string objectName)
    {
        if (desiredCells == null)
            return hintOverlays.Count == 0;

        if (hintOverlays.Count != desiredCells.Count || hintCells.Count != desiredCells.Count)
            return false;

        for (int i = 0; i < desiredCells.Count; i++)
        {
            var rt = hintOverlays[i];
            if (!rt || !rt.gameObject.activeSelf || rt.parent != board.gridRoot)
                return false;

            if (rt.name != objectName || hintCells[i] != desiredCells[i])
                return false;
        }

        return true;
    }

    RectTransform GetOrCreateHint(string objectName)
    {
        RectTransform rt = null;
        while (hintPool.Count > 0 && !rt)
        {
            int last = hintPool.Count - 1;
            rt = hintPool[last];
            hintPool.RemoveAt(last);
        }

        if (rt)
            return rt;

        var img = new GameObject(objectName, typeof(Image)).GetComponent<Image>();
        img.sprite = OnePx();
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        img.color = hintColor;
        return img.rectTransform;
    }

    void ReleaseHintAt(int index)
    {
        var rt = hintOverlays[index];
        hintOverlays.RemoveAt(index);
        if (index < hintCells.Count)
            hintCells.RemoveAt(index);

        if (!rt)
            return;

        rt.gameObject.SetActive(false);
        hintPool.Add(rt);
    }

    void DestroyHints(List<RectTransform> hints)
    {
        for (int i = hints.Count - 1; i >= 0; i--)
            if (hints[i])
                Destroy(hints[i].gameObject);

        hints.Clear();
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
            var rt = visuals[i];
            if (!rt) continue;

            if (board)
            {
                board.SetInlineBorderColor(rt, c);
                continue;
            }

            var img = rt.GetComponent<UnityEngine.UI.Image>();
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

        RefreshHintsIfNeeded(force: true);
    }

    void NotifyTutorialEvent(TutorialGameplayEvent gameplayEvent)
    {
        var gc = GetGameController();
        gc?.NotifyTutorialGameplayEvent(gameplayEvent);
    }

}
