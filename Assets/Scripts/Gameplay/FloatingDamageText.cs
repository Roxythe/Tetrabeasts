using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    public enum DamageKind
    {
        Normal,
        Fire,
        Poison,
        Lightning,
        Spike,
        Contagion,
        Rations,
        DeathExplosion,
        BossAbility,
        MagicExplosive,
        Overgrowth,
        RearAmbush
    }

    [Header("Refs")]
    [SerializeField] RectTransform textRoot;
    [SerializeField] TMP_FontAsset fontAsset;

    [Header("Text")]
    [SerializeField] bool enabledText = true;
    [SerializeField] bool showMinusSign = true;
    [SerializeField] FontStyles fontStyle = FontStyles.Bold;
    [SerializeField, Min(1f)] float baseFontSize = 34f;
    [SerializeField, Min(1f)] float minFontSize = 22f;
    [SerializeField, Min(1f)] float maxFontSize = 64f;
    [SerializeField, Min(1f)] float damageForMaxFontSize = 100f;
    [SerializeField, Min(1f)] float minTextWidth = 120f;
    [SerializeField, Min(0f)] float textHeightPadding = 18f;

    [Header("Motion")]
    [SerializeField, Min(0.05f)] float fadeSpeed = 1.15f;
    [SerializeField, Min(0f)] float floatSpeed = 72f;
    [SerializeField, Min(0f)] float spawnYOffset = 10f;
    [SerializeField, Min(0f)] float spawnJitter = 8f;

    [Header("Pulse")]
    [SerializeField, Min(1f)] float pulseScale = 1.3f;
    [SerializeField, Min(0.01f)] float pulseDuration = 0.18f;

    [Header("Performance")]
    [SerializeField, Min(0)] int prewarmCount = 32;
    [SerializeField, Min(1)] int maxActivePopups = 15;
    [SerializeField] bool aggregateBurstDamage = true;
    [SerializeField, Min(0f)] float burstAggregateSeconds = 0.04f;
    [SerializeField, Min(0f)] float burstAggregatePositionRadius = 28f;

    [Header("Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color fireColor = new(1f, 0.45f, 0.05f, 1f);
    [SerializeField] Color poisonColor = new(0.72f, 0.25f, 1f, 1f);
    [SerializeField] Color lightningColor = new(0.25f, 0.9f, 1f, 1f);
    [SerializeField] Color spikeColor = new(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] Color contagionColor = new(1f, 0.1f, 0.65f, 1f);
    [SerializeField] Color rationsColor = new(0.84f, 0.72f, 0.38f, 1f);
    [SerializeField] Color deathExplosionColor = new(1f, 0.35f, 0.35f, 1f);
    [SerializeField] Color bossAbilityColor = new(0.9f, 0.75f, 1f, 1f);
    [SerializeField] Color magicExplosiveColor = new(0.35f, 0.75f, 1f, 1f);
    [SerializeField] Color overgrowthColor = new(0.45f, 0.9f, 0.35f, 1f);
    [SerializeField] Color rearAmbushColor = new(1f, 0.85f, 0.55f, 1f);
    [SerializeField] Color killingBlowColor = new(1f, 0.05f, 0.05f, 1f);

    [Header("Outline")]
    [SerializeField] Color outlineColor = new(0f, 0f, 0f, 0.85f);
    [SerializeField, Range(0f, 1f)] float outlineWidth = 0.22f;

    sealed class PopupInstance
    {
        public RectTransform rect;
        public TextMeshProUGUI text;
        public Coroutine routine;
        public float activatedAt;
    }

    struct PendingPopup
    {
        public Vector3 worldPosition;
        public int amount;
        public DamageKind kind;
        public bool killingBlow;
        public Vector2 sourceSize;
    }

    readonly List<PopupInstance> pooledPopups = new();
    readonly List<PopupInstance> activePopups = new();
    readonly List<PendingPopup> pendingPopups = new();

    RectTransform fallbackRoot;
    Coroutine flushPendingCoroutine;
    bool poolPrewarmed;

    public void SetFallbackRoot(RectTransform root)
    {
        fallbackRoot = root;

        if (isActiveAndEnabled)
            Prewarm();
    }

    public void Prewarm()
    {
        RectTransform root = ResolveRoot();
        if (root)
            EnsurePoolPrewarmed(root);
    }

    void OnDisable()
    {
        if (flushPendingCoroutine != null)
        {
            StopCoroutine(flushPendingCoroutine);
            flushPendingCoroutine = null;
        }

        pendingPopups.Clear();

        while (activePopups.Count > 0)
            StopAndPool(activePopups[activePopups.Count - 1]);
    }

    public void Show(RectTransform target, int amount, DamageKind kind, bool killingBlow)
    {
        if (!target)
            return;

        Vector3 worldPosition = target.TransformPoint(new Vector3(0f, target.rect.height * 0.18f + spawnYOffset, 0f));
        ShowAtWorldPosition(worldPosition, amount, kind, killingBlow, target.rect.size);
    }

    public void ShowAtLocalPosition(RectTransform sourceRoot, Vector2 anchoredPosition, Vector2 sourceSize,
                                    int amount, DamageKind kind, bool killingBlow)
    {
        if (!sourceRoot)
            return;

        Vector3 worldPosition = sourceRoot.TransformPoint(new Vector3(
            anchoredPosition.x,
            anchoredPosition.y + sourceSize.y * 0.18f + spawnYOffset,
            0f));

        ShowAtWorldPosition(worldPosition, amount, kind, killingBlow, sourceSize);
    }

    void ShowAtWorldPosition(Vector3 worldPosition, int amount, DamageKind kind, bool killingBlow, Vector2 sourceSize)
    {
        if (!enabledText || amount <= 0)
            return;

        if (aggregateBurstDamage && burstAggregateSeconds > 0f)
        {
            QueuePendingPopup(worldPosition, amount, kind, killingBlow, sourceSize);
            return;
        }

        SpawnPopupAtWorldPosition(worldPosition, amount, kind, killingBlow, sourceSize);
    }

    void QueuePendingPopup(Vector3 worldPosition, int amount, DamageKind kind, bool killingBlow, Vector2 sourceSize)
    {
        int existingIndex = FindPendingPopup(worldPosition, kind);
        if (existingIndex >= 0)
        {
            PendingPopup pending = pendingPopups[existingIndex];
            pending.amount += amount;
            pending.killingBlow |= killingBlow;
            pending.sourceSize = new Vector2(
                Mathf.Max(pending.sourceSize.x, sourceSize.x),
                Mathf.Max(pending.sourceSize.y, sourceSize.y));
            pendingPopups[existingIndex] = pending;
        }
        else
        {
            pendingPopups.Add(new PendingPopup
            {
                worldPosition = worldPosition,
                amount = amount,
                kind = kind,
                killingBlow = killingBlow,
                sourceSize = sourceSize
            });
        }

        if (flushPendingCoroutine == null)
            flushPendingCoroutine = StartCoroutine(FlushPendingPopupsAfterDelay());
    }

    int FindPendingPopup(Vector3 worldPosition, DamageKind kind)
    {
        float radius = Mathf.Max(0f, burstAggregatePositionRadius);
        float radiusSqr = radius * radius;

        for (int i = 0; i < pendingPopups.Count; i++)
        {
            PendingPopup pending = pendingPopups[i];
            if (pending.kind != kind)
                continue;

            float distanceSqr = (pending.worldPosition - worldPosition).sqrMagnitude;
            if (radius <= 0f)
            {
                if (distanceSqr <= 0.01f)
                    return i;
            }
            else if (distanceSqr <= radiusSqr)
            {
                return i;
            }
        }

        return -1;
    }

    IEnumerator FlushPendingPopupsAfterDelay()
    {
        float endTime = Time.unscaledTime + Mathf.Max(0f, burstAggregateSeconds);
        while (Time.unscaledTime < endTime)
            yield return null;

        FlushPendingPopups();
        flushPendingCoroutine = null;
    }

    void FlushPendingPopups()
    {
        for (int i = 0; i < pendingPopups.Count; i++)
        {
            PendingPopup pending = pendingPopups[i];
            SpawnPopupAtWorldPosition(pending.worldPosition, pending.amount, pending.kind, pending.killingBlow, pending.sourceSize);
        }

        pendingPopups.Clear();
    }

    void SpawnPopupAtWorldPosition(Vector3 worldPosition, int amount, DamageKind kind, bool killingBlow, Vector2 sourceSize)
    {
        if (!enabledText || amount <= 0)
            return;

        RectTransform root = ResolveRoot();
        if (!root)
            return;

        root.SetAsLastSibling();
        EnsurePoolPrewarmed(root);

        PopupInstance popup = GetPopup(root);
        if (popup == null || !popup.rect || !popup.text)
            return;

        RectTransform rt = popup.rect;
        TextMeshProUGUI text = popup.text;

        rt.gameObject.SetActive(true);
        rt.SetAsLastSibling();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        Vector3 localPosition = root.InverseTransformPoint(worldPosition);
        Vector2 jitter = spawnJitter > 0f ? Random.insideUnitCircle * spawnJitter : Vector2.zero;
        jitter.y = Mathf.Abs(jitter.y) * 0.35f;
        rt.anchoredPosition = new Vector2(localPosition.x, localPosition.y) + jitter;

        float fontSize = ResolveFontSize(amount);
        string amountText = amount.ToString();
        rt.sizeDelta = new Vector2(
            Mathf.Max(minTextWidth, fontSize * Mathf.Max(4f, amountText.Length + 2f), sourceSize.x * 1.5f),
            fontSize + textHeightPadding);

        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontStyle = fontStyle;
        text.fontSize = fontSize;
        text.text = showMinusSign ? "-" + amountText : amountText;
        text.color = killingBlow ? killingBlowColor : ColorForKind(kind);

        if (fontAsset)
            text.font = fontAsset;

        text.outlineColor = outlineColor;
        text.outlineWidth = outlineWidth;

        popup.activatedAt = Time.unscaledTime;
        popup.routine = StartCoroutine(AnimateAndRelease(popup));
    }

    PopupInstance GetPopup(RectTransform root)
    {
        PopupInstance popup = null;

        while (pooledPopups.Count > 0 && popup == null)
        {
            int last = pooledPopups.Count - 1;
            popup = pooledPopups[last];
            pooledPopups.RemoveAt(last);

            if (popup == null || !popup.rect || !popup.text)
                popup = null;
        }

        if (popup == null)
        {
            if (activePopups.Count >= Mathf.Max(1, maxActivePopups))
                return null;

            popup = CreatePopup(root);
        }

        if (popup == null || !popup.rect)
            return null;

        popup.rect.SetParent(root, false);
        activePopups.Add(popup);
        return popup;
    }

    PopupInstance CreatePopup(RectTransform root)
    {
        var go = new GameObject("FloatingDamageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;

        go.SetActive(false);

        return new PopupInstance
        {
            rect = rt,
            text = text
        };
    }

    void EnsurePoolPrewarmed(RectTransform root)
    {
        if (poolPrewarmed)
            return;

        poolPrewarmed = true;

        int count = Mathf.Min(Mathf.Max(0, prewarmCount), Mathf.Max(1, maxActivePopups));
        for (int i = 0; i < count; i++)
        {
            PopupInstance popup = CreatePopup(root);
            if (popup != null)
                pooledPopups.Add(popup);
        }
    }

    void StopAndPool(PopupInstance popup)
    {
        if (popup == null)
            return;

        if (popup.routine != null)
        {
            StopCoroutine(popup.routine);
            popup.routine = null;
        }

        PoolPopup(popup);
    }

    void PoolPopup(PopupInstance popup)
    {
        if (popup == null)
            return;

        activePopups.Remove(popup);
        popup.routine = null;

        if (!popup.rect)
            return;

        popup.rect.gameObject.SetActive(false);

        if (!pooledPopups.Contains(popup))
            pooledPopups.Add(popup);
    }

    RectTransform ResolveRoot()
    {
        if (textRoot)
            return textRoot;

        Canvas canvas = null;
        if (fallbackRoot)
            canvas = fallbackRoot.GetComponentInParent<Canvas>();

        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        if (canvas)
        {
            var go = new GameObject("FloatingDamageTextRoot", typeof(RectTransform));
            textRoot = go.GetComponent<RectTransform>();
            textRoot.SetParent(canvas.transform, false);
            textRoot.anchorMin = Vector2.zero;
            textRoot.anchorMax = Vector2.one;
            textRoot.offsetMin = Vector2.zero;
            textRoot.offsetMax = Vector2.zero;
            textRoot.pivot = new Vector2(0.5f, 0.5f);
            textRoot.localScale = Vector3.one;
            textRoot.SetAsLastSibling();
            return textRoot;
        }

        return fallbackRoot;
    }

    float ResolveFontSize(int amount)
    {
        float maxDamage = Mathf.Max(1f, damageForMaxFontSize);
        float t = Mathf.Clamp01(Mathf.Max(1, amount) / maxDamage);
        float scaled = Mathf.Lerp(baseFontSize * 0.75f, maxFontSize, t);
        return Mathf.Clamp(scaled, minFontSize, maxFontSize);
    }

    Color ColorForKind(DamageKind kind)
    {
        return kind switch
        {
            DamageKind.Fire => fireColor,
            DamageKind.Poison => poisonColor,
            DamageKind.Lightning => lightningColor,
            DamageKind.Spike => spikeColor,
            DamageKind.Contagion => contagionColor,
            DamageKind.Rations => rationsColor,
            DamageKind.DeathExplosion => deathExplosionColor,
            DamageKind.BossAbility => bossAbilityColor,
            DamageKind.MagicExplosive => magicExplosiveColor,
            DamageKind.Overgrowth => overgrowthColor,
            DamageKind.RearAmbush => rearAmbushColor,
            _ => normalColor
        };
    }

    IEnumerator AnimateAndRelease(PopupInstance popup)
    {
        if (popup == null || !popup.rect || !popup.text)
        {
            activePopups.Remove(popup);
            yield break;
        }

        RectTransform rt = popup.rect;
        TMP_Text text = popup.text;

        float duration = 1f / Mathf.Max(0.05f, fadeSpeed);
        float elapsed = 0f;
        Vector2 start = rt.anchoredPosition;
        Color startColor = text.color;

        while (rt && text && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            rt.anchoredPosition = start + Vector2.up * (floatSpeed * elapsed);

            float scale = 1f;
            if (pulseScale > 1f && pulseDuration > 0f && elapsed < pulseDuration)
            {
                float p = Mathf.Clamp01(elapsed / pulseDuration);
                scale = Mathf.Lerp(pulseScale, 1f, Mathf.SmoothStep(0f, 1f, p));
            }

            rt.localScale = Vector3.one * scale;

            Color color = startColor;
            color.a = startColor.a * (1f - t);
            text.color = color;

            yield return null;
        }

        PoolPopup(popup);
    }
}
