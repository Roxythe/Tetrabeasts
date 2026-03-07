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

    private string B(string s) => $"<b>{E(s)}</b>";
    private string C(string s, Color32 c) => $"<color=#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}>{E(s)}</color>";
    private string Unit(string s) => $"<b>{C(s, unitName)}</b>";
    private string Enemy(string s) => $"<b>{C(s, enemyName)}</b>";
    private string Boss(string s) => $"<b>{C(s, bossName)}</b>";
    private string PlayerAbility(string s) => $"<b>{C(s, playerAbility)}</b>";

    private readonly Queue<TMP_Text> _active = new();

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

        if (linePrefab.gameObject.activeSelf)
            linePrefab.gameObject.SetActive(false);
    }

    public void LogDamage(string unitName, int amount) =>
        AddLine($"{B(unitName)} takes {C(amount.ToString(), damage)} damage.");

    public void LogHeal(string unitName, int amount) =>
        AddLine($"{B(unitName)} heals {C(amount.ToString(), heal)}.");

    public void LogDeath(string unitName) => AddLine($"{Unit(unitName)} dies.");

    public void LogAbilityUse(string actorName, string abilityName) =>
    AddLine($"{Unit(actorName)} uses {PlayerAbility(abilityName)}.");

    public void LogBossAbility(string abilityName) =>
    AddLine($"{Boss("Boss")} uses {B(HumanizePascal(abilityName))}.");

    public void LogPlain(string msg) =>
        AddLine(C(E(msg), normal));

    public void LogDamageFrom(string unitName, int amount, string source) =>
    LogDamageDetailed(unitName, amount, null, null, source);

    public void LogDamageDetailed(string unitName, int amount, string damageTypeWord, Color32? damageTypeColor, string fromWhoOrWhat)
    {
        string typePart = string.IsNullOrWhiteSpace(damageTypeWord)
            ? string.Empty
            : $" {ColorizeWord(damageTypeWord, damageTypeColor)}";

        string fromPart = string.IsNullOrWhiteSpace(fromWhoOrWhat)
            ? string.Empty
            : $" from {(fromWhoOrWhat == "Boss" ? Boss("Boss") : Enemy(fromWhoOrWhat))}";

        AddLine($"{Unit(unitName)} took {C(amount.ToString(), damage)}{typePart} damage{fromPart}.");
    }

    private string ColorizeWord(string word, Color32? c)
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;
        if (c == null) return E(word);
        return C(word, c.Value);
    }

    public void LogCastleHit(string attackerName, int amount, bool pylonsReduced)
    {
        string note = pylonsReduced ? " (shielded)" : string.Empty;
        AddLine($"{Unit(attackerName)} dealt {C(amount.ToString(), damage)} damage to {Enemy("Castle")}.{note}");
    }

    private void AddLine(string richText)
    {
        if (!enabled) return;

        var t = Instantiate(linePrefab, contentRoot);
        t.gameObject.SetActive(true);
        t.richText = true;
        t.text = richText;

        Canvas.ForceUpdateCanvases();

        float availableWidth = GetLineAvailableWidth();

        var fitter = t.GetComponent<BattleLogAutoFitLine>();
        if (fitter) fitter.Fit(availableWidth);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        Canvas.ForceUpdateCanvases();

        _active.Enqueue(t);

        while (_active.Count > maxLines)
        {
            var old = _active.Dequeue();
            if (old) Destroy(old.gameObject);
        }

        if (autoScroll)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
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
        while (_active.Count > 0)
        {
            var t = _active.Dequeue();
            if (t) Destroy(t.gameObject);
        }

        if (scrollRect)
            scrollRect.verticalNormalizedPosition = 0f;
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
}