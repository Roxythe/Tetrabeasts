using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class LevelModifierController
{
    sealed class Crater
    {
        public Vector2Int origin;
        public float remaining = 30f;
        public Image image;
    }

    sealed class Hallucination
    {
        public Image image;
        public UIImagePingPongAnimator idleAnimator;
        public Vector2 position;
        public Vector2 velocity;
        public float diameter;
        public float consumeFlash;
        public float deathTime = -1f;
    }

    readonly List<Crater> _craters = new();
    readonly List<Image> _newModifierWarnings = new();
    readonly List<Hallucination> _hallucinations = new();
    readonly Dictionary<Sprite, Vector2[]> _hallucinationVertices = new();
    bool _newModifiersStarted;
    bool _boardTransformCaptured;
    Quaternion _boardDefaultRotation;
    Vector3 _boardDefaultPosition;
    float _flipCountdown;
    float _bombardmentCountdown;
    Vector2Int? _bombardmentTarget;
    float _hallucinationCountdown;
    float _hallucinationBaseDiameter;
    RectTransform _hallucinationRoot;
    RectTransform _modifierFeedbackRoot;
    Image _inversionImage;
    float _inversionRemaining;
    bool _inversionOn;
    static Sprite _fallbackCrater;

    public bool NewModifierEffectsActive { get; private set; }
    public bool BoardFlipped { get; private set; }
    public bool InvertsPieceControls
    {
        get
        {
            if (!NewModifierEffectsActive) return false;
            if (ActiveModifier && ActiveModifier.kind == LevelModifierKind.Inversion && _inversionOn) return true;
            foreach (var effect in chaosEffects) if (effect && effect.InvertsPieceControls) return true;
            return false;
        }
    }

    void PrepareNewModifierEffects()
    {
        EndNewModifierEffects();
        NewModifierEffectsActive = true;
        _inversionOn = true;
        _newModifiersStarted = false;
    }

    // Also called on victory, defeat, restart, controller disable, and run reset.
    // Keep ActiveModifier available for the results screen without leaving effects running.
    public void EndNewModifierEffects()
    {
        foreach (var effect in chaosEffects) if (effect) { effect.StopAllCoroutines(); effect.EndNewModifierEffects(); }
        NewModifierEffectsActive = false;
        _newModifiersStarted = false;
        ClearNewWarnings();
        if (_boardTransformCaptured && board)
        {
            ImpactShake.Stop(board.transform);
            board.transform.localRotation = _boardDefaultRotation;
            board.transform.localPosition = _boardDefaultPosition;
        }
        _boardTransformCaptured = false;
        BoardFlipped = false;
        if (board && !isChaosEffect)
        {
            board.ClearFeelGood();
            board.ClearCraterCells();
        }
        foreach (var crater in _craters)
            if (crater.image) Destroy(crater.image.gameObject);
        _craters.Clear();
        _bombardmentTarget = null;
        if (_hallucinationRoot) Destroy(_hallucinationRoot.gameObject);
        _hallucinationRoot = null;
        _hallucinations.Clear();
        _hallucinationVertices.Clear();
        if (_modifierFeedbackRoot) Destroy(_modifierFeedbackRoot.gameObject);
        _modifierFeedbackRoot = null;
        if (_inversionImage) Destroy(_inversionImage.gameObject);
        _inversionImage = null;
        _inversionOn = false;
        _inversionRemaining = 0f;
    }

    void OnDisable() => EndNewModifierEffects();

    void UpdateNewModifierEffects(float dt)
    {
        if (!NewModifierEffectsActive || !ActiveModifier || !board) return;
        if (!_newModifiersStarted)
        {
            _newModifiersStarted = true;
            switch (ActiveModifier.kind)
            {
                case LevelModifierKind.WatchYourStep: SpawnWatchYourStep(); break;
                case LevelModifierKind.Inversion: BeginInversion(); break;
                case LevelModifierKind.FlipTheBoard:
                    _boardDefaultPosition = board.transform.localPosition;
                    _boardDefaultRotation = board.transform.localRotation;
                    _boardTransformCaptured = true;
                    // Give the initial flip the same warning as later swaps.
                    _flipCountdown = 2f;
                    break;
                case LevelModifierKind.Bombardment: _bombardmentCountdown = 15f; break;
                case LevelModifierKind.Hallucination:
                    _hallucinationBaseDiameter = Mathf.Max(0.1f, ActiveModifier.hallucinationBaseSizeCells);
                    SpawnHallucination();
                    _hallucinationCountdown = 10f;
                    break;
            }
        }
        switch (ActiveModifier.kind)
        {
            case LevelModifierKind.FlipTheBoard: UpdateFlip(dt); break;
            case LevelModifierKind.Bombardment: UpdateBombardment(dt); break;
            case LevelModifierKind.Hallucination: UpdateHallucinations(dt); break;
            case LevelModifierKind.Inversion: UpdateInversion(dt); break;
        }
        foreach (var warning in _newModifierWarnings)
            if (warning) warning.enabled = Mathf.Repeat(Time.time, 0.24f) < 0.12f;
    }
    void BeginInversion()
    {
        _inversionOn = true;
        _inversionRemaining = Mathf.Max(.1f, ActiveModifier.inversionActiveSeconds);
        PlayModifierSound(ActiveModifier.inversionSfx, .8f);
        if (!_inversionImage) _inversionImage = board.CreateModifierBoardVFX(ActiveModifier.inversionBoardSprite);
        if (_inversionImage)
        {
            _inversionImage.type = Image.Type.Filled;
            _inversionImage.fillMethod = Image.FillMethod.Radial360;
            _inversionImage.fillOrigin = (int)Image.Origin360.Top;
            _inversionImage.fillClockwise = false;
            _inversionImage.fillAmount = 1f;
            _inversionImage.rectTransform.localRotation = Quaternion.identity;
            _inversionImage.color = new Color(1f, 1f, 1f, .6f);
            _inversionImage.gameObject.SetActive(true);
        }
    }
    void UpdateInversion(float dt)
    {
        _inversionRemaining -= dt;
        if (_inversionRemaining <= 0f)
        {
            if (_inversionOn)
            {
                _inversionOn = false;
                _inversionRemaining = Mathf.Max(.1f, ActiveModifier.inversionRestSeconds);
                if (_inversionImage) _inversionImage.gameObject.SetActive(false);
            }
            else BeginInversion();
        }
        if (_inversionOn && _inversionImage)
        {
            _inversionImage.fillAmount = Mathf.Clamp01(_inversionRemaining / Mathf.Max(.1f, ActiveModifier.inversionActiveSeconds));
            // Positive UI Z rotation is counterclockwise: one turn per active period.
            _inversionImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 360f * (1f - _inversionImage.fillAmount));
        }
    }
    static void PlayModifierSound(AudioClip clip, float volume = 1f)
    {
        if (AudioManager.I && clip) AudioManager.I.PlaySFX(clip, volume);
    }
    RectTransform FeedbackRoot()
    {
        if (!_modifierFeedbackRoot)
        {
            _modifierFeedbackRoot = new GameObject("Modifier impact feedback", typeof(RectTransform)).GetComponent<RectTransform>();
            _modifierFeedbackRoot.SetParent(board.overlayRoot, false);
            _modifierFeedbackRoot.sizeDelta = Vector2.zero;
        }
        return _modifierFeedbackRoot;
    }
    void BombardmentUnitFeedback(Vector2Int cell)
    {
        if (!board.TryGetMonster(cell, out _) || !board.TryGetTileRect(cell, out var tile)) return;
        var portrait = tile.Find("MonsterPortrait");
        var original = portrait ? portrait.GetComponent<Image>() : tile.GetComponent<Image>();
        if (!original || !original.sprite) return;
        // Preserve a short visual echo because the gameplay unit is removed on impact.
        var echo = CreateOverlayImage("Bombardment unit impact", FeedbackRoot(), original.sprite, original.color, board.GetCellSize());
        echo.rectTransform.position = tile.position;
        ImpactShake.Play(echo.transform, .25f, 9f);
        echo.gameObject.AddComponent<ImpactEchoVFX>();
    }

    void SpawnWatchYourStep()
    {
        int rows = Mathf.Max(0, board.height - 5);
        var candidates = new List<Vector2Int>();
        int existing = 0;
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < board.width; x++)
        {
            var cell = new Vector2Int(x, y);
            if (board.HasObstacle(cell) || board.HasFloorEffect(cell)) { existing++; continue; }
            if (board.IsFree(cell)) candidates.Add(cell);
        }
        int count = Mathf.Min(candidates.Count, Mathf.Max(0, Mathf.RoundToInt(board.width * rows * 0.33f) - existing));
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(i, candidates.Count);
            var cell = candidates[index];
            candidates[index] = candidates[i];
            candidates[i] = cell;
            int kind = Random.Range(0, 7);
            if (kind == 0) board.TrySpawnStoneObstacle(cell);
            else board.SetFloorEffect(cell, (Board.FloorEffectType)(kind - 1), 1f, 1f, -1);
        }
    }

    void ScheduleFlip()
    {
        float min = Mathf.Max(2f, Mathf.Min(ActiveModifier.flipIntervalMin, ActiveModifier.flipIntervalMax));
        float max = Mathf.Max(min, Mathf.Max(ActiveModifier.flipIntervalMin, ActiveModifier.flipIntervalMax));
        _flipCountdown = Random.Range(min, max);
    }

    void ToggleBoardOrientation()
    {
        ImpactShake.Stop(board.transform);
        BoardFlipped = !BoardFlipped;
        board.transform.localPosition = _boardDefaultPosition;
        board.transform.localRotation = _boardDefaultRotation;
        // All grid-space mechanics keep their coordinates. Rotating their shared
        // parent makes local downward gravity, spawn, ghosts, and settling rise on screen.
        if (BoardFlipped)
            board.transform.RotateAround(board.gridRoot.TransformPoint(Vector3.zero),
                board.transform.forward, 180f);
    }

    void UpdateFlip(float dt)
    {
        _flipCountdown -= dt;
        if (_flipCountdown <= 2f && _newModifierWarnings.Count == 0)
        {
            Vector2 size = board.GetCellSize();
            foreach (int side in new[] { -1, 1 })
            {
                var warning = CreateOverlayImage("FlipWarning", board.overlayRoot,
                    ActiveModifier.flipWarningSprite, new Color(1f, 0.25f, 0.05f, 0.85f),
                    new Vector2(size.x * 0.3f, size.y * board.height));
                warning.preserveAspect = false;
                warning.rectTransform.anchoredPosition = new Vector2(side * size.x * (board.width * 0.5f + 0.25f), 0f);
                _newModifierWarnings.Add(warning);
            }
        }
        if (_flipCountdown > 0f) return;
        ClearNewWarnings();
        ToggleBoardOrientation();
        PlayModifierSound(ActiveModifier.flipSfx, .85f);
        ImpactShake.Play(board.transform, .3f, 7f);
        ScheduleFlip();
    }

    void ClearNewWarnings()
    {
        foreach (var warning in _newModifierWarnings)
            if (warning) Destroy(warning.gameObject);
        _newModifierWarnings.Clear();
    }

    IEnumerable<Vector2Int> CraterArea(Vector2Int origin)
    {
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
            yield return origin + new Vector2Int(x, y);
    }

    void UpdateBombardment(float dt)
    {
        // Expire before impact so the third strike never creates a third crater.
        for (int i = _craters.Count - 1; i >= 0; i--)
        {
            var crater = _craters[i];
            if (crater.image)
            {
                crater.image.rectTransform.sizeDelta = board.GetCellSize() * 3f;
                crater.image.rectTransform.anchoredPosition = board.CellToAnchoredPos(crater.origin + Vector2Int.one);
            }
            crater.remaining -= dt;
            if (crater.remaining > 0.0001f) continue;
            foreach (var cell in CraterArea(crater.origin)) board.SetCraterCell(cell, false);
            if (crater.image) Destroy(crater.image.gameObject);
            _craters.RemoveAt(i);
        }
        _bombardmentCountdown -= dt;
        if (_bombardmentCountdown <= 2f && !_bombardmentTarget.HasValue)
        {
            var candidates = new List<Vector2Int>();
            for (int y = 0; y <= board.height - 8; y++)
            for (int x = 0; x <= board.width - 3; x++)
            {
                var origin = new Vector2Int(x, y);
                bool free = true;
                foreach (var cell in CraterArea(origin))
                    if (board.IsCraterCell(cell)) { free = false; break; }
                if (free) candidates.Add(origin);
            }
            if (candidates.Count > 0)
            {
                _bombardmentTarget = candidates[Random.Range(0, candidates.Count)];
                foreach (var cell in CraterArea(_bombardmentTarget.Value))
                {
                    var warning = CreateOverlayImage("BombardmentWarning", board.overlayRoot,
                        ActiveModifier.bombardmentWarningSprite, new Color(1f, 0.15f, 0f, 0.7f), board.GetCellSize());
                    warning.rectTransform.anchoredPosition = board.CellToAnchoredPos(cell);
                    _newModifierWarnings.Add(warning);
                }
            }
        }
        if (_bombardmentCountdown > 0f) return;
        if (_bombardmentTarget.HasValue)
        {
            // Floating-point frame boundaries must not let a third crater survive.
            if (_craters.Count >= 2)
            {
                var oldest = _craters[0];
                foreach (var cell in CraterArea(oldest.origin)) board.SetCraterCell(cell, false);
                if (oldest.image) Destroy(oldest.image.gameObject);
                _craters.RemoveAt(0);
            }
            var origin = _bombardmentTarget.Value;
            PlayModifierSound(ActiveModifier.bombardmentImpactSfx);
            ImpactShake.Play(board.transform, .3f, 6f);
            PixelSprayVFX.Play(FeedbackRoot(), board.gridRoot.TransformPoint(board.CellToAnchoredPos(origin + Vector2Int.one)), Color.gray, 65f);
            foreach (var cell in CraterArea(origin))
            {
                BombardmentUnitFeedback(cell);
                board.ForceClearEnvironmentTarget(cell);
                board.ForceKillMonsterAndRemove(cell, Board.DamageSource.Generic);
                board.RemovePlacedCellImmediate(cell);
                board.SetCraterCell(cell, true);
            }
            var img = CreateOverlayImage("BombardmentCrater", board.CraterVisualRoot,
                ActiveModifier.bombardmentCraterSprite ? ActiveModifier.bombardmentCraterSprite : FallbackCraterSprite(),
                Color.white, board.GetCellSize() * 3f);
            img.preserveAspect = false;
            img.rectTransform.anchoredPosition = board.CellToAnchoredPos(origin + Vector2Int.one);
            img.transform.SetAsFirstSibling();
            _craters.Add(new Crater { origin = origin, image = img });
            if (piece && piece.HasActiveCells) piece.TryResolvePlacedOverlapByShiftingUp();
        }
        ClearNewWarnings();
        _bombardmentTarget = null;
        _bombardmentCountdown += 15f;
    }

    static Sprite FallbackCraterSprite()
    {
        if (_fallbackCrater) return _fallbackCrater;
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
            Color color = Color.Lerp(new Color(0.08f, 0.045f, 0.025f), new Color(0.4f, 0.25f, 0.12f), Mathf.Clamp01(radius));
            color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, radius));
            texture.SetPixel(x, y, color);
        }
        texture.Apply(false, true);
        _fallbackCrater = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        return _fallbackCrater;
    }

    Sprite HallucinationDefaultSprite => ActiveModifier.hallucinationIdleFrames != null &&
        ActiveModifier.hallucinationIdleFrames.Length > 0 ? ActiveModifier.hallucinationIdleFrames[0] : HallucinationDeathSprite;

    Sprite HallucinationDeathSprite => ActiveModifier.hallucinationAnimation != null &&
        ActiveModifier.hallucinationAnimation.Length > 0 ? ActiveModifier.hallucinationAnimation[0] : null;

    void EnsureHallucinationRoot()
    {
        if (_hallucinationRoot) return;
        var canvas = board.GetComponentInParent<Canvas>()?.rootCanvas;
        _hallucinationRoot = new GameObject("Hallucinations", typeof(RectTransform)).GetComponent<RectTransform>();
        _hallucinationRoot.SetParent(canvas ? canvas.transform : board.transform, false);
        _hallucinationRoot.anchorMin = _hallucinationRoot.anchorMax = Vector2.one * 0.5f;
        _hallucinationRoot.sizeDelta = Vector2.zero;
    }

    void SpawnHallucination()
    {
        EnsureHallucinationRoot();
        float diameter = Mathf.Min(_hallucinationBaseDiameter, Mathf.Min(board.width, board.height) * 0.85f);
        float radius = diameter * 0.5f;
        float radians = Random.Range(195f, 345f) * Mathf.Deg2Rad;
        var h = new Hallucination
        {
            diameter = diameter,
            position = new Vector2(Random.Range(radius, board.width - radius), board.height - radius),
            velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * Mathf.Max(0.1f, ActiveModifier.hallucinationSpeedCellsPerSecond),
            image = CreateOverlayImage("Hallucination", _hallucinationRoot, HallucinationDefaultSprite, Color.white, Vector2.one)
        };
        h.idleAnimator = h.image.gameObject.AddComponent<UIImagePingPongAnimator>();
        h.idleAnimator.enabled = false; // Advanced by the same active-round clock as movement.
        h.idleAnimator.Configure(h.image, ActiveModifier.hallucinationIdleFrames,
            ActiveModifier.hallucinationIdleFramesPerSecond);
        _hallucinations.Add(h);
        SyncHallucinationVisual(h);
        PlayModifierSound(ActiveModifier.hallucinationSpawnSfx, .7f);
    }

    void UpdateHallucinations(float dt)
    {
        MaintainHallucinationLayer();
        _hallucinationCountdown -= dt;
        if (_hallucinationCountdown <= 0f) { SpawnHallucination(); _hallucinationCountdown += 10f; }
        for (int i = _hallucinations.Count - 1; i >= 0; i--)
        {
            var h = _hallucinations[i];
            if (h.deathTime >= 0f)
            {
                h.deathTime += dt;
                var frames = ActiveModifier.hallucinationAnimation;
                int frame = Mathf.FloorToInt(h.deathTime / Mathf.Max(0.01f, ActiveModifier.hallucinationDeathFrameSeconds));
                if (frame >= Mathf.Max(1, frames == null ? 0 : frames.Length))
                {
                    Destroy(h.image.gameObject);
                    _hallucinations.RemoveAt(i);
                    continue;
                }
                if (frames != null && frame < frames.Length) h.image.sprite = frames[frame];
                continue;
            }
            h.position += h.velocity * dt;
            h.consumeFlash = Mathf.Max(0f, h.consumeFlash - dt);
            if (h.consumeFlash > 0f && ActiveModifier.hallucinationConsumeSprite)
                h.image.sprite = ActiveModifier.hallucinationConsumeSprite;
            else
            {
                // Play reapplies the current frame after a consume flash without
                // restarting the ping-pong cycle.
                h.idleAnimator.Play();
                h.idleAnimator.Advance(dt);
                if (ActiveModifier.hallucinationIdleFrames == null || ActiveModifier.hallucinationIdleFrames.Length == 0)
                    h.image.sprite = HallucinationDefaultSprite;
            }
            ConstrainHallucination(h, reflect: true);
            SyncHallucinationVisual(h);
        }
        ResolveHallucinationMerges();
    }

    void LateUpdate() => MaintainHallucinationLayer();

    void MaintainHallucinationLayer()
    {
        if (!_hallucinationRoot) return;
        // Move only the effect layer, never the pause menu: promoting the menu
        // every frame puts it above its own settings and confirmation dialogs.
        if (_gc && _gc.pausePanel && _gc.pausePanel.activeInHierarchy)
        {
            Transform pauseBranch = _gc.pausePanel.transform;
            while (pauseBranch.parent && pauseBranch.parent != _hallucinationRoot.parent)
                pauseBranch = pauseBranch.parent;
            if (pauseBranch.parent == _hallucinationRoot.parent &&
                _hallucinationRoot.GetSiblingIndex() > pauseBranch.GetSiblingIndex())
                _hallucinationRoot.SetSiblingIndex(pauseBranch.GetSiblingIndex());
            return;
        }
        if (ConfirmationPopupUI.IsAnyShowing) return;
        if (_hallucinationRoot.GetSiblingIndex() != _hallucinationRoot.parent.childCount - 1)
            _hallucinationRoot.SetAsLastSibling();
    }

    void ResolveHallucinationMerges()
    {
        for (int i = 0; i < _hallucinations.Count; i++)
        for (int j = i + 1; j < _hallucinations.Count; j++)
        {
            var a = _hallucinations[i];
            var b = _hallucinations[j];
            if (a.deathTime >= 0f || b.deathTime >= 0f) continue;
            if ((a.position - b.position).sqrMagnitude > Mathf.Pow((a.diameter + b.diameter) * 0.5f, 2f)) continue;
            bool aWins = Mathf.Approximately(a.diameter, b.diameter) ? Random.value < 0.5f : a.diameter > b.diameter;
            var winner = aWins ? a : b;
            var loser = aWins ? b : a;
            winner.diameter = Mathf.Min(Mathf.Max(a.diameter, b.diameter) + Mathf.Min(a.diameter, b.diameter) * .65f,
                Mathf.Min(board.width, board.height) * 0.85f);
            winner.consumeFlash = 0.5f;
            winner.image.sprite = ActiveModifier.hallucinationConsumeSprite ? ActiveModifier.hallucinationConsumeSprite : HallucinationDefaultSprite;
            ConstrainHallucination(winner, reflect: false);
            SyncHallucinationVisual(winner);
            PlayModifierSound(ActiveModifier.hallucinationMergeSfx, .8f);
            PixelSprayVFX.Play(_hallucinationRoot, winner.image.transform.position, new Color(.55f, .3f, 1f), board.GetCellSize().x * winner.diameter * .35f);
            Destroy(loser.image.gameObject);
            _hallucinations.Remove(loser);
            // Restart after mutation; this also resolves growth into a third sprite.
            i = -1;
            break;
        }
    }

    static void ReflectAxis(ref float position, ref float velocity, float min, float max)
    {
        // Constant-time mirroring also handles large frame deltas and oversized art.
        float width = max - min;
        if (width <= .0001f) { position = (min + max) * .5f; velocity = 0f; return; }
        if (position >= min && position <= max) return;
        float wrapped = Mathf.Repeat(position - min, width * 2f);
        if (wrapped >= width) { position = max - (wrapped - width); velocity = -velocity; }
        else position = min + wrapped;
    }

    float HallucinationHeading(Hallucination h)
    {
        Vector2 direction = Vector2.Scale(h.velocity, board.GetCellSize());
        return direction.sqrMagnitude > .0001f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f : 0f;
    }

    static Vector2 HallucinationPulse(Hallucination h)
    {
        float pulse = h.consumeFlash > 0f
            ? Mathf.Round(Mathf.Sin((1f - h.consumeFlash / .5f) * Mathf.PI * 2f) * 4f) / 4f : 0f;
        return new Vector2(1f + pulse * .18f, 1f - pulse * .12f);
    }

    Rect HallucinationVisibleBounds(Hallucination h)
    {
        var sprite = h.image.sprite;
        if (!sprite) return new Rect(Vector2.one * (-h.diameter * .5f), Vector2.one * h.diameter);
        if (!_hallucinationVertices.TryGetValue(sprite, out var vertices))
        {
            // Unity's tight sprite mesh follows the alpha silhouette without CPU texture reads.
            vertices = sprite.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 pixel = vertices[i] * sprite.pixelsPerUnit + sprite.pivot;
                vertices[i] = new Vector2(pixel.x / sprite.rect.width - .5f, pixel.y / sprite.rect.height - .5f);
            }
            _hallucinationVertices.Add(sprite, vertices);
        }
        Vector2 cell = board.GetCellSize();
        float fit = Mathf.Min(cell.x / sprite.rect.width, cell.y / sprite.rect.height) * h.diameter;
        Vector2 size = Vector2.Scale(sprite.rect.size * fit, HallucinationPulse(h));
        Quaternion rotation = Quaternion.Euler(0, 0, HallucinationHeading(h));
        Vector2 min = Vector2.one * float.PositiveInfinity, max = Vector2.one * float.NegativeInfinity;
        foreach (var vertex in vertices)
        {
            Vector2 point = rotation * Vector2.Scale(vertex, size);
            point = new Vector2(point.x / cell.x, point.y / cell.y);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    void ConstrainHallucination(Hallucination h, bool reflect)
    {
        Rect bounds = HallucinationVisibleBounds(h);
        if (reflect)
        {
            ReflectAxis(ref h.position.x, ref h.velocity.x, -bounds.xMin, board.width - bounds.xMax);
            ReflectAxis(ref h.position.y, ref h.velocity.y, -bounds.yMin, board.height - bounds.yMax);
            // A bounce changes the facing and thus the asymmetric visible silhouette.
            bounds = HallucinationVisibleBounds(h);
        }
        h.position.x = Mathf.Clamp(h.position.x, -bounds.xMin, board.width - bounds.xMax);
        h.position.y = Mathf.Clamp(h.position.y, -bounds.yMin, board.height - bounds.yMax);
    }

    void SyncHallucinationVisual(Hallucination h)
    {
        Vector2 cellSize = board.GetCellSize();
        Vector2 local = board.CellToAnchoredPos(Vector2Int.zero) + Vector2.Scale(h.position - Vector2.one * 0.5f, cellSize);
        h.image.rectTransform.position = board.gridRoot.TransformPoint(local);
        h.image.rectTransform.sizeDelta = cellSize * h.diameter;
        Vector3 boardScale = board.gridRoot.lossyScale;
        Vector3 parentScale = _hallucinationRoot.lossyScale;
        h.image.rectTransform.localScale = new Vector3(boardScale.x / parentScale.x, boardScale.y / parentScale.y, 1f);
        h.image.rectTransform.rotation = board.gridRoot.rotation * Quaternion.Euler(0, 0, HallucinationHeading(h));
        // A stepped squash/stretch pulse makes the growth readable in pixel art.
        if (h.consumeFlash > 0f)
        {
            Vector2 pulse = HallucinationPulse(h);
            h.image.rectTransform.localScale = Vector3.Scale(h.image.rectTransform.localScale, new Vector3(pulse.x, pulse.y, 1f));
        }
    }

    public bool TryHitHallucination(Vector2 previousBoardLocal, Vector2 currentBoardLocal)
    {
        foreach (var effect in chaosEffects) if (effect && effect.TryHitHallucination(previousBoardLocal, currentBoardLocal)) return true;
        if (!NewModifierEffectsActive || !_gc.IsRoundActive || !ActiveModifier || ActiveModifier.kind != LevelModifierKind.Hallucination) return false;
        Vector2 size = board.GetCellSize();
        Vector2 origin = board.CellToAnchoredPos(Vector2Int.zero) - size * 0.5f;
        Vector2 from = new Vector2((previousBoardLocal.x - origin.x) / size.x, (previousBoardLocal.y - origin.y) / size.y);
        Vector2 to = new Vector2((currentBoardLocal.x - origin.x) / size.x, (currentBoardLocal.y - origin.y) / size.y);
        Vector2 travel = to - from;
        Hallucination hit = null;
        float nearest = float.PositiveInfinity;
        foreach (var h in _hallucinations)
        {
            if (h.deathTime >= 0f) continue;
            float t = travel.sqrMagnitude <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(h.position - from, travel) / travel.sqrMagnitude);
            if ((from + travel * t - h.position).sqrMagnitude <= h.diameter * h.diameter * 0.25f && t < nearest)
            { hit = h; nearest = t; }
        }
        if (hit == null) return false;
        hit.deathTime = 0f;
        hit.idleAnimator.Stop();
        hit.image.sprite = HallucinationDeathSprite;
        hit.consumeFlash = 0f;
        SyncHallucinationVisual(hit);
        PlayModifierSound(ActiveModifier.hallucinationDeathSfx, .8f);
        _hallucinationBaseDiameter += Mathf.Max(0.01f, ActiveModifier.hallucinationSizeIncreaseCells);
        SpawnHallucination();
        _hallucinationCountdown = 10f;
        return true;
    }
}
