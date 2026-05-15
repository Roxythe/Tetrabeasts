using System.Collections;
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

    RectTransform fallbackRoot;

    public void SetFallbackRoot(RectTransform root)
    {
        fallbackRoot = root;
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

        RectTransform root = ResolveRoot();
        if (!root)
            return;

        root.SetAsLastSibling();

        var go = new GameObject("FloatingDamageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        Vector3 localPosition = root.InverseTransformPoint(worldPosition);
        Vector2 jitter = spawnJitter > 0f ? Random.insideUnitCircle * spawnJitter : Vector2.zero;
        jitter.y = Mathf.Abs(jitter.y) * 0.35f;
        rt.anchoredPosition = new Vector2(localPosition.x, localPosition.y) + jitter;

        float fontSize = ResolveFontSize(amount);
        rt.sizeDelta = new Vector2(
            Mathf.Max(minTextWidth, fontSize * Mathf.Max(4f, amount.ToString().Length + 2f), sourceSize.x * 1.5f),
            fontSize + textHeightPadding);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontStyle = fontStyle;
        text.fontSize = fontSize;
        text.text = showMinusSign ? $"-{amount}" : amount.ToString();
        text.color = killingBlow ? killingBlowColor : ColorForKind(kind);

        if (fontAsset)
            text.font = fontAsset;

        text.outlineColor = outlineColor;
        text.outlineWidth = outlineWidth;

        StartCoroutine(AnimateAndDestroy(rt, text));
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

    IEnumerator AnimateAndDestroy(RectTransform rt, TMP_Text text)
    {
        if (!rt || !text)
            yield break;

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

        if (rt)
            Destroy(rt.gameObject);
    }
}
