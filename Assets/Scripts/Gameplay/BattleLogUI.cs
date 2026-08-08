using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleLogUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private TMP_Text linePrefab;
    [SerializeField] private GameObject visualRoot;

    [Header("Behavior")]
    [SerializeField, Min(1)] private int maxLines = 80;
    [SerializeField] private bool autoScroll = true;

    [Header("Colors")]
    [SerializeField] private Color32 normal = new(255, 255, 255, 255);       // White
    [SerializeField] private Color32 unitName = new(120, 180, 255, 255);     // Blue
    [SerializeField] private Color32 enemyName = new(170, 170, 170, 255);    // Grey
    [SerializeField] private Color32 damage = new(255, 85, 85, 255);         // Red
    [SerializeField] private Color32 heal = new(85, 255, 85, 255);           // Green
    [SerializeField] private Color32 poison = new(190, 90, 255, 255);        // Purple
    [SerializeField] private Color32 fire = new(255, 150, 40, 255);          // Orange
    [SerializeField] private Color32 bossName = new(255, 205, 60, 255);      // Gold
    [SerializeField] private Color32 playerAbility = new(80, 230, 255, 255); // Cyan

    private string B(string s) => $"<b>{E(L(s))}</b>";
    private string C(string s, Color32 c) => $"<color=#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}>{E(s)}</color>";
    private string Unit(string s) => $"<b>{C(L(s), unitName)}</b>";
    private string Enemy(string s) => $"<b>{C(L(s), enemyName)}</b>";
    private string Boss(string s) => $"<b>{C(L(s), bossName)}</b>";
    private string PlayerAbility(string s) => $"<b>{C(L(s), playerAbility)}</b>";
    private string L(string s) => TetrabeastsLocalization.LocalizeText(s);
    private string LF(string englishFormat, params object[] args) => TetrabeastsLocalization.LocalizeFormat(englishFormat, args);

    private readonly Queue<TMP_Text> _active = new();
    private readonly Queue<TMP_Text> _pool = new();
    private readonly Queue<string> _buffer = new();
    private Coroutine _layoutRefreshCoroutine;
    private bool _layoutRefreshQueued;
    private static readonly Dictionary<string, string> BossAbilitySpellTitles = new(System.StringComparer.Ordinal)
    {
        { "RowBlast", "Skybreaker Edict" },
        { "RowBlastTop3", "Skybreaker Edict" },
        { "Boss_RowBlastTop3", "Skybreaker Edict" },
        { "FullBoardBlast", "Heaven's Judgement" },
        { "Boss_FullBoardBlast", "Heaven's Judgement" },
        { "LightningStrike", "Stormcaller's Verdict" },
        { "Boss_LightningStrike", "Stormcaller's Verdict" },
        { "SpawnTraps", "Hex of the Warped Ground" },
        { "Boss_SpawnTraps", "Hex of the Warped Ground" },
        { "Invulnerability", "Aegis of the Unbroken Crown" },
        { "Boss_Invulnerability", "Aegis of the Unbroken Crown" },
        { "GravityBoost", "Temporal Distortion" },
        { "Boss_GravityBoost", "Temporal Distortion" },
        { "PylonShield", "Ward of the Arcane Pylons" },
        { "Boss_PylonShield", "Ward of the Arcane Pylons" },
        { "MagicExplosive", "Rune of Ruin" },
        { "Boss_MagicExplosive", "Rune of Ruin" },
        { "ForcedDuplication", "Forced Duplication" },
        { "Boss_ForcedDuplication", "Forced Duplication" },
        { "SpecialSiphon", "Special Siphon" },
        { "Boss_SpecialSiphon", "Special Siphon" },
        { "Teleport", "Teleport" },
        { "Boss_Teleport", "Teleport" },
        { "ZipPad", "Zip Pad" },
        { "Boss_ZipPad", "Zip Pad" },
    };

    private void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (!contentRoot && scrollRect) contentRoot = scrollRect.content;

        if (!scrollRect || !contentRoot || !linePrefab)
        {
            Debug.LogError("BattleLogUI missing references (ScrollRect, ContentRoot, LinePrefab).", this);
            enabled = false;
            return;
        }

        if (!visualRoot && scrollRect)
            visualRoot = scrollRect.gameObject;

        if (linePrefab.gameObject.activeSelf)
            linePrefab.gameObject.SetActive(false);
    }

    public void LogDamage(string unitName, int amount) =>
        AddLine(LF("{0} takes {1} damage.", Unit(unitName), C(amount.ToString(), damage)));

    public void LogHeal(string unitName, int amount) =>
        AddLine(LF("{0} heals {1}.", Unit(unitName), C(amount.ToString(), heal)));

    public void LogDeath(string unitName) => AddLine(LF("{0} dies.", Unit(unitName)));

    public void LogAbilityUse(string actorName, string abilityName) =>
        AddLine(LF("{0} uses {1}.", Unit(actorName), PlayerAbility(abilityName)));

    public void LogBossAbility(string abilityName) =>
        AddLine(LF("{0} casts {1}.", Boss("Boss"), B(GetBossAbilitySpellTitle(abilityName))));

    public void LogBossTrapAbility(CastleData.BossTrapKind trapKind) =>
        AddLine(LF("{0} casts {1}.", Boss("Boss"), B(GetBossTrapSpellTitle(trapKind))));

    public void LogPlain(string msg) =>
        AddLine(C(L(msg), normal));

    public void LogDamageFrom(string unitName, int amount, string source) =>
    LogDamageDetailed(unitName, amount, null, null, source);

    public void LogDamageDetailed(string unitName, int amount, string damageTypeWord, Color32? damageTypeColor, string fromWhoOrWhat)
    {
        string typePart = string.IsNullOrWhiteSpace(damageTypeWord)
            ? string.Empty
            : $" {ColorizeWord(L(damageTypeWord), damageTypeColor)}";

        string fromPart = string.IsNullOrWhiteSpace(fromWhoOrWhat)
            ? string.Empty
            : LF(" from {0}", fromWhoOrWhat == "Boss" ? Boss("Boss") : Enemy(fromWhoOrWhat));

        AddLine(LF("{0} took {1}{2} damage{3}.", Unit(unitName), C(amount.ToString(), damage), typePart, fromPart));
    }

    public void LogHealDetailed(string healSourceName, int healedAmount, string targetName)
    {
        AddLine(LF("{0} restored {1} health for {2}.", Unit(healSourceName), C(healedAmount.ToString(), heal), Unit(targetName)));
    }

    private string ColorizeWord(string word, Color32? c)
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;
        if (c == null) return E(word);
        return C(word, c.Value);
    }

    public void LogCastleHit(string attackerName, int amount, bool pylonsReduced)
    {
        string note = pylonsReduced ? $" {L("(shielded)")}" : string.Empty;
        AddLine(LF("{0} dealt {1} damage to {2}.{3}", Unit(attackerName), C(amount.ToString(), damage), Enemy("Castle"), note));
    }

    private void AddLine(string richText)
    {
        // Always store, even when hidden
        _buffer.Enqueue(richText);
        while (_buffer.Count > maxLines)
            _buffer.Dequeue();

        // If visuals hidden stop here
        if (!visualRoot || !visualRoot.activeInHierarchy)
            return;

        AddLineVisualOnly(richText);
    }

    private void AddLineVisualOnly(string richText)
    {
        var t = GetLineFromPool();
        t.transform.SetParent(contentRoot, false);
        t.transform.SetAsLastSibling();
        t.gameObject.SetActive(true);
        t.richText = true;
        t.text = richText;

        float availableWidth = GetLineAvailableWidth();

        var fitter = t.GetComponent<BattleLogAutoFitLine>();
        if (fitter) fitter.Fit(availableWidth);

        _active.Enqueue(t);

        while (_active.Count > maxLines)
        {
            var old = _active.Dequeue();
            ReleaseLine(old);
        }

        QueueLayoutRefresh();
    }

    private static string E(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private float GetLineAvailableWidth()
    {
        var viewport = scrollRect.viewport ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        float width = viewport.rect.width;

        var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (vlg)
            width -= (vlg.padding.left + vlg.padding.right);

        return Mathf.Max(1f, width);
    }

    public void Clear()
    {
        _buffer.Clear();

        while (_active.Count > 0)
        {
            var t = _active.Dequeue();
            ReleaseLine(t);
        }

        if (scrollRect)
            scrollRect.verticalNormalizedPosition = 0f;

        QueueLayoutRefresh();
    }

    private static string HumanizePascal(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        var chars = new List<char>(s.Length + 8);
        chars.Add(s[0]);

        for (int i = 1; i < s.Length; i++)
        {
            char c = s[i];
            char prev = s[i - 1];

            bool isUpper = char.IsUpper(c);
            bool prevIsLower = char.IsLower(prev);
            bool nextIsLower = (i + 1 < s.Length) && char.IsLower(s[i + 1]);

            if (isUpper && (prevIsLower || nextIsLower && char.IsUpper(prev)))
                chars.Add(' ');

            chars.Add(c);
        }

        return new string(chars.ToArray());
    }

    private static string GetBossAbilitySpellTitle(string abilityName)
    {
        if (string.IsNullOrWhiteSpace(abilityName)) return string.Empty;
        if (BossAbilitySpellTitles.TryGetValue(abilityName, out string title)) return title;

        string trimmed = abilityName.StartsWith("Boss_", System.StringComparison.Ordinal)
            ? abilityName.Substring("Boss_".Length)
            : abilityName;

        return BossAbilitySpellTitles.TryGetValue(trimmed, out title)
            ? title
            : HumanizePascal(trimmed);
    }

    private static string GetBossTrapSpellTitle(CastleData.BossTrapKind trapKind)
    {
        switch (trapKind)
        {
            case CastleData.BossTrapKind.Stone: return "Summon Earthen Rampart";
            case CastleData.BossTrapKind.Spike: return "Raise Iron Thorns";
            case CastleData.BossTrapKind.Poison: return "Sow Venomous Miasma";
            case CastleData.BossTrapKind.Fire: return "Kindle Infernal Sigils";
            case CastleData.BossTrapKind.Lightning: return "Call Stormbound Sigils";
            default: return "Hex of the Warped Ground";
        }
    }

    public void SetVisible(bool visible)
    {
        if (!visualRoot) return;

        bool wasVisible = visualRoot.activeSelf;
        visualRoot.SetActive(visible);

        if (visible && !wasVisible)
            RebuildVisualFromBuffer();
    }

    public bool IsVisible() => visualRoot && visualRoot.activeSelf;

    private void RebuildVisualFromBuffer()
    {
        // Reuse existing visible lines when the log is rebuilt after being hidden.
        while (_active.Count > 0)
        {
            var t = _active.Dequeue();
            ReleaseLine(t);
        }

        if (!visualRoot || !visualRoot.activeInHierarchy) return;

        foreach (var line in _buffer)
            AddLineVisualOnly(line);

        if (autoScroll && scrollRect)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private TMP_Text GetLineFromPool()
    {
        while (_pool.Count > 0)
        {
            var pooled = _pool.Dequeue();
            if (pooled)
                return pooled;
        }

        return Instantiate(linePrefab, contentRoot);
    }

    private void ReleaseLine(TMP_Text line)
    {
        if (!line)
            return;

        line.text = string.Empty;
        line.gameObject.SetActive(false);
        _pool.Enqueue(line);
    }

    private void QueueLayoutRefresh()
    {
        if (_layoutRefreshQueued || !isActiveAndEnabled)
            return;

        _layoutRefreshQueued = true;
        _layoutRefreshCoroutine = StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;

        _layoutRefreshQueued = false;
        _layoutRefreshCoroutine = null;

        if (!contentRoot)
            yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (autoScroll && scrollRect)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void OnDisable()
    {
        if (_layoutRefreshCoroutine != null)
            StopCoroutine(_layoutRefreshCoroutine);

        _layoutRefreshCoroutine = null;
        _layoutRefreshQueued = false;
    }
}
