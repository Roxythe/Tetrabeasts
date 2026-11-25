using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class Board
{
    public bool IsSpecialAnimating { get; private set; }
    int specialPresentationGeneration;
    sealed class SpecialTarget
    {
        public Vector2Int cell;
        public RectTransform rect;
        public CanvasGroup group;
        public float alpha;
        public Vector3 scale;
        public Color tint;
        public bool destroy, emitted;
        public Image[] images;
        public Color[] colors;
    }

    public void RefreshRunLevelStats()
    {
        foreach (var cell in new List<Vector2Int>(monsters.Keys))
        {
            var unit = monsters[cell]; unit.RecalcFromData(false); monsters[cell] = unit;
            UpdateTileHPVisual(cell, unit.hp, unit.maxHp);
            if (unit.healAmount > 0 && !healTimers.ContainsKey(cell)) healTimers[cell] = 0;
        }
    }

    bool SpecialWillDestroy(Vector2Int cell, SpecialType kind)
    {
        if (!obstacles.TryGetValue(cell, out var obstacle)) return true;
        if (obstacle.type == ObstacleType.SoldierWall) return false;
        if (kind == SpecialType.Bomb && (obstacle.type == ObstacleType.Stone || obstacle.type == ObstacleType.HardenedLava)) return true;
        if (kind == SpecialType.Bolt && obstacle.type == ObstacleType.HardenedLava) return true;
        if (obstacle.indestructible) return false;
        return obstacle.type != ObstacleType.Stone || obstacle.hitsRemaining <= 1;
    }

    public IEnumerator PlaySpecialDestruction(SpecialType kind, Vector2Int epicenter, List<Vector2Int> units, List<Vector2Int> environment)
        => AnimateSpecialDestruction(kind, epicenter, units, environment, () => Time.deltaTime);

    IEnumerator AnimateSpecialDestruction(SpecialType kind, Vector2Int epicenter, List<Vector2Int> units, List<Vector2Int> environment, System.Func<float> deltaTime)
    {
        int generation = specialPresentationGeneration;
        if (kind != SpecialType.Bomb && kind != SpecialType.Bolt && kind != SpecialType.Death) yield break;
        var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (!canvas) yield break;
        var targets = new List<SpecialTarget>();
        var seen = new HashSet<RectTransform>();
        void Add(Vector2Int cell, RectTransform rect, bool destroyed, Color fallback)
        {
            if (!rect || !seen.Add(rect)) return;
            var group = rect.GetComponent<CanvasGroup>();
            if (!group) group = rect.gameObject.AddComponent<CanvasGroup>();
            var fill = rect.Find("HealthFill")?.GetComponent<Image>();
            var owned = rect.GetComponent<BoardOwned>();
            var images = rect.GetComponentsInChildren<Image>(true);
            var colors = new Color[images.Length]; for (int i = 0; i < images.Length; i++) colors[i] = images[i].color;
            targets.Add(new SpecialTarget { cell = cell, rect = rect, group = group, alpha = group.alpha, scale = rect.localScale,
                tint = owned ? owned.tileColor : fill ? fill.color : fallback, destroy = destroyed, images = images, colors = colors });
        }
        foreach (var cell in units)
            if (placed.TryGetValue(cell, out var rect)) Add(cell, rect, SpecialWillDestroy(cell, kind), Color.gray);
        if (kind != SpecialType.Death)
            foreach (var cell in environment)
            {
                if (spikeVisuals.TryGetValue(cell, out var spike) && spike != null) Add(cell, spike.root, true, new Color(.65f, .65f, .7f));
                if (zipPadVisuals.TryGetValue(cell, out var zip) && zip != null) Add(cell, zip.root, true, Color.cyan);
                var growth = overlayRoot ? overlayRoot.Find($"Overgrowth_{cell.x}_{cell.y}") as RectTransform : null;
                if (growth) Add(cell, growth, true, new Color(.25f, .65f, .1f));
            }
        if (targets.Count == 0 && kind != SpecialType.Bolt) yield break;
        LightningColumnSpark columnSpark = null;
        float sparkSeconds = 0;
        if (kind == SpecialType.Bolt)
        {
            var contact = new Vector2Int(Mathf.Clamp(epicenter.x, 0, width - 1), Mathf.Clamp(epicenter.y, 0, height - 1));
            Vector2 bottom = CellToAnchoredPos(new Vector2Int(contact.x, 0)) - Vector2.up * GetCellSize().y * .5f;
            columnSpark = LightningColumnSpark.Create(overlayRoot ? overlayRoot : gridRoot, CellToAnchoredPos(contact) + Vector2.up * GetCellSize().y * .5f, bottom, GetCellSize());
            sparkSeconds = Mathf.Max(.3f, (contact.y + 2) * .11f);
        }
        var debris = PixelDebrisVfx.Create(canvas);
        float size = GetCellSize().x * Mathf.Abs(gridRoot.lossyScale.x) / Mathf.Max(.0001f, Mathf.Abs(canvas.transform.lossyScale.x));
        float duration = kind == SpecialType.Bomb ? .23f : kind == SpecialType.Death ? .75f : .55f;
        float lastDelay = 0;
        float Delay(SpecialTarget target) => columnSpark ? columnSpark.ProgressPastCell(CellToAnchoredPos(target.cell).y - GetCellSize().y * .5f) * sparkSeconds : 0;
        foreach (var target in targets) lastDelay = Mathf.Max(lastDelay, Delay(target));
        IsSpecialAnimating = true;
        bool completed = false;
        try
        {
            for (float t = 0; t < Mathf.Max(duration + lastDelay, sparkSeconds + .18f); t += deltaTime())
            {
                if (generation != specialPresentationGeneration) yield break;
                if (columnSpark) columnSpark.SetProgress(t / sparkSeconds, 1f - Mathf.Clamp01((t - sparkSeconds) / .18f));
                foreach (var target in targets)
                {
                    if (!target.rect) continue;
                    float local = t - Delay(target);
                    if (local < 0) continue;
                    if (!target.destroy) continue;
                    float p = Mathf.Clamp01(local / duration);
                    if (kind == SpecialType.Death)
                    {
                        target.group.alpha = target.alpha * (1 - p);
                        target.rect.localScale = Vector3.Scale(target.scale, new Vector3(1 - .45f * p, 1 - .85f * p, 1));
                        for (int i = 0; i < target.images.Length; i++)
                            if (target.images[i]) target.images[i].color = Color.Lerp(target.colors[i], new Color(.22f, .13f, .08f), p);
                    }
                    if (!target.emitted && local >= (kind == SpecialType.Bolt ? 0f : kind == SpecialType.Death ? .5f : .03f))
                    {
                        target.emitted = true;
                        debris.Burst(target.rect.TransformPoint(target.rect.rect.center), size,
                            kind == SpecialType.Death ? new Color(.3f, .2f, .12f) : target.tint,
                            (Vector2)(target.cell - epicenter), kind == SpecialType.Bomb, disintegrate: kind == SpecialType.Bolt);
                        if (kind != SpecialType.Death) target.group.alpha = 0;
                    }
                }
                yield return null;
            }
            completed = true;
        }
        finally
        {
            foreach (var target in targets)
            {
                if (generation != specialPresentationGeneration) continue;
                if (target.rect) target.rect.localScale = target.scale;
                if (target.group) target.group.alpha = completed && target.destroy ? 0 : target.alpha;
                for (int i = 0; i < target.images.Length; i++) if (target.images[i]) target.images[i].color = target.colors[i];
            }
            if (debris) debris.keepAlive = false;
            if (columnSpark)
            {
                if (Application.isPlaying) Destroy(columnSpark.gameObject); else DestroyImmediate(columnSpark.gameObject);
            }
            IsSpecialAnimating = false;
        }
    }
}
