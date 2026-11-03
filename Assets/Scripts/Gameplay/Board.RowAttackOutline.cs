using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    [Header("Row Attack Outline")]
    public bool showRowAttackOutline = true;
    [Min(.01f)] public float rowAttackOutlineSweepSeconds = .16f;
    [Min(0f)] public float rowAttackOutlineHoldSeconds = .09f;
    [Min(.01f)] public float rowAttackOutlineFadeSeconds = .20f;
    [Min(1f)] public float rowAttackOutlineThickness = 3f;
    public Color rowAttackOutlineColor = new Color(1f, .76f, .08f, 1f);
    [Tooltip("Shuffle gold and the extra colors without repeating the previous color.")]
    public bool varyRowAttackOutlineColors = true;
    public Color[] rowAttackOutlineExtraColors =
    {
        new Color(.08f, .85f, 1f, 1f),
        new Color(.10f, 1f, .48f, 1f),
        new Color(.64f, .35f, 1f, 1f),
        new Color(1f, .25f, .67f, 1f),
        new Color(1f, .38f, .08f, 1f),
        new Color(.36f, .64f, 1f, 1f)
    };
    // Cosmetic randomness must not change monster, projectile or modifier rolls.
    readonly System.Random outlineColorRandom = new System.Random();
    readonly List<Color> outlineColorBag = new();
    Color previousOutlineColor;
    bool hasPreviousOutlineColor;
    readonly List<RowAttackOutlineVFX> rowAttackOutlines = new();

    RowAttackOutlineVFX PlayRowAttackOutline(int row, Dictionary<int, List<int>> origins)
    {
        if (!showRowAttackOutline || row < 0 || row >= height) return null;
        var parent = overlayRoot ? overlayRoot : gridRoot;
        if (!parent) return null;
        rowAttackOutlines.RemoveAll(effect => !effect);
        var root = new GameObject("Row Attack Outline", typeof(RectTransform), typeof(CanvasRenderer));
        root.layer = gameObject.layer;
        var rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        var first = CellToAnchoredPos(new Vector2Int(0, row));
        var last = CellToAnchoredPos(new Vector2Int(width - 1, row));
        rect.anchoredPosition = (first + last) * .5f;
        var effect = root.AddComponent<RowAttackOutlineVFX>();
        List<int> columns = null;
        origins?.TryGetValue(row, out columns);
        effect.Initialize(new Vector2(GetCellSize().x * width, GetCellSize().y), width, columns,
            rowAttackOutlineSweepSeconds, rowAttackOutlineHoldSeconds, rowAttackOutlineFadeSeconds,
            rowAttackOutlineThickness, PickRowAttackOutlineColor());
        rowAttackOutlines.Add(effect);
        return effect;
    }

    Color PickRowAttackOutlineColor()
    {
        if (!varyRowAttackOutlineColors) return rowAttackOutlineColor;
        if (outlineColorBag.Count == 0)
        {
            outlineColorBag.Add(rowAttackOutlineColor);
            if (rowAttackOutlineExtraColors != null)
                foreach (var shade in rowAttackOutlineExtraColors)
                    if (!outlineColorBag.Contains(shade)) outlineColorBag.Add(shade);
            for (int i = outlineColorBag.Count - 1; i > 0; i--)
            {
                int j = outlineColorRandom.Next(i + 1);
                (outlineColorBag[i], outlineColorBag[j]) = (outlineColorBag[j], outlineColorBag[i]);
            }
            int last = outlineColorBag.Count - 1;
            if (last > 0 && hasPreviousOutlineColor && outlineColorBag[last] == previousOutlineColor)
                (outlineColorBag[0], outlineColorBag[last]) = (outlineColorBag[last], outlineColorBag[0]);
        }
        int next = outlineColorBag.Count - 1;
        previousOutlineColor = outlineColorBag[next];
        outlineColorBag.RemoveAt(next);
        hasPreviousOutlineColor = true;
        return previousOutlineColor;
    }

    void ClearRowAttackOutlines()
    {
        foreach (var effect in rowAttackOutlines)
            if (effect) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); }
        rowAttackOutlines.Clear();
    }
}
