using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XpMonsterRowUI : MonoBehaviour
{
    [Header("Refs")]
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Slider xpSlider;
    public TMP_Text xpText;

    public TMP_Text xpDistText;
    public TMP_Text xpDrainText;

    public RectTransform xpBarCenterRect;
    public RectTransform xpOrbDistRect;
    public RectTransform xpOrbDrainRect;

    public TMP_Text levelUpText;
    public TMP_Text levelUpShadowText;
    public TMP_Text levelUpStatsText;

    [Header("Level Up Animation")]
    public BuffPopupStyleSO levelUpStyle;
    public RunModRarity levelUpRarity = RunModRarity.Common;
    [Min(0.1f)] public float statCycleSeconds = 1.0f;

    Coroutine _statsCycleCR;
    Coroutine _pulseCR;

    readonly List<string> _statLines = new();
    int _levelUpCount = 0;

    Color _lvlText0;
    Color _lvlShadow0;
    Vector3 _lvlScale0;
    Vector3 _lvlShadowScale0;

    int _uiLevel = 1;
    float _uiXpInto = 0f;

    public void BindStatic(Sprite portrait, string displayName)
    {
        if (portraitImage) portraitImage.sprite = portrait;
        if (nameText) nameText.text = displayName ?? "";
        HideDeltaTexts();
    }

    public void SetLevel(int level)
    {
        if (levelText) levelText.text = $"Lv.{level}";
    }

    public void SetXp(float xpInto, float xpPerLevel = 100f)
    {
        xpInto = Mathf.Clamp(xpInto, 0f, xpPerLevel);
        if (xpSlider) xpSlider.value = xpPerLevel > 0 ? (xpInto / xpPerLevel) : 0f;
        if (xpText) xpText.text = $"{xpInto:0.#}/{xpPerLevel:0}";
    }

    public void HideDeltaTexts()
    {
        if (xpDistText) xpDistText.gameObject.SetActive(false);
        if (xpDrainText) xpDrainText.gameObject.SetActive(false);

        HideLevelUps();
    }

    public void HideLevelUps()
    {
        StopLevelUpEffects();

        _levelUpCount = 0;
        _statLines.Clear();

        if (levelUpText) levelUpText.gameObject.SetActive(false);
        if (levelUpShadowText) levelUpShadowText.gameObject.SetActive(false);
        if (levelUpStatsText) levelUpStatsText.gameObject.SetActive(false);
    }

    public void BeginLevelUpSequence()
    {
        StopLevelUpEffects();

        _levelUpCount = 0;
        _statLines.Clear();

        if (levelUpText) levelUpText.gameObject.SetActive(true);
        if (levelUpShadowText) levelUpShadowText.gameObject.SetActive(true);
        if (levelUpStatsText) levelUpStatsText.gameObject.SetActive(true);

        UpdateLevelUpHeader();

        if (levelUpStatsText)
            levelUpStatsText.text = "";

        if (levelUpStyle && (levelUpText || levelUpShadowText))
            _pulseCR = StartCoroutine(CoPulseAndFlash());
    }

    public void AppendLevelUpStatLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (_levelUpCount == 0 && _statLines.Count == 0)
            BeginLevelUpSequence();

        _levelUpCount += 1;
        _statLines.Add(line);

        UpdateLevelUpHeader();

        if (levelUpStatsText)
        {
            if (_statLines.Count == 1)
                levelUpStatsText.text = _statLines[0];
        }

        if (_statLines.Count > 1 && _statsCycleCR == null)
            _statsCycleCR = StartCoroutine(CoCycleStatLines());
    }

    void UpdateLevelUpHeader()
    {
        string header = _levelUpCount <= 1 ? "LEVEL UP!" : $"LEVEL UP! x{_levelUpCount}";

        if (levelUpText) levelUpText.text = header;
        if (levelUpShadowText) levelUpShadowText.text = header;
    }

    public void ShowXpDist(float gainedXp)
    {
        if (xpDrainText) xpDrainText.gameObject.SetActive(false);

        if (!xpDistText) return;
        xpDistText.gameObject.SetActive(true);
        xpDistText.text = $"+{FormatXp(gainedXp)} Exp";
    }

    public void ShowXpDrainPreserved(float preservedXp)
    {
        if (xpDistText) xpDistText.gameObject.SetActive(false);

        if (!xpDrainText) return;
        xpDrainText.gameObject.SetActive(true);

        xpDrainText.text = $"+{FormatXp(preservedXp)} Exp";
    }

    void StopLevelUpEffects()
    {
        if (_statsCycleCR != null) { StopCoroutine(_statsCycleCR); _statsCycleCR = null; }
        if (_pulseCR != null) { StopCoroutine(_pulseCR); _pulseCR = null; }

        // Restore defaults if cached
        if (levelUpText)
        {
            levelUpText.color = _lvlText0;
            levelUpText.transform.localScale = _lvlScale0 == default ? Vector3.one : _lvlScale0;
        }

        if (levelUpShadowText)
        {
            levelUpShadowText.color = _lvlShadow0;
            levelUpShadowText.transform.localScale = _lvlShadowScale0 == default ? Vector3.one : _lvlShadowScale0;
        }
    }

    IEnumerator CoCycleStatLines()
    {
        int idx = 0;

        while (true)
        {
            if (_statLines.Count == 0 || !levelUpStatsText)
                yield break;

            levelUpStatsText.text = _statLines[idx % _statLines.Count];
            idx++;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, statCycleSeconds));
        }
    }

    IEnumerator CoPulseAndFlash()
    {
        var colors = levelUpStyle.GetColors(levelUpRarity);

        if (levelUpText)
        {
            _lvlText0 = levelUpText.color;
            _lvlScale0 = levelUpText.transform.localScale;
        }

        if (levelUpShadowText)
        {
            _lvlShadow0 = levelUpShadowText.color;
            _lvlShadowScale0 = levelUpShadowText.transform.localScale;
        }

        float flashTimer = 0f;
        bool flip = false;

        while (true)
        {
            float dt = Time.unscaledDeltaTime;

            // Pulse
            float amp = Mathf.Max(0f, levelUpStyle.pulseScaleAmplitude);
            float hz = Mathf.Max(0.01f, levelUpStyle.pulseSpeedHz);
            float s = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * hz) * amp;

            if (levelUpText) levelUpText.transform.localScale = _lvlScale0 * s;
            if (levelUpShadowText) levelUpShadowText.transform.localScale = _lvlShadowScale0 * s;

            // Flash colors
            flashTimer += dt;
            float interval = Mathf.Max(0.03f, levelUpStyle.flashInterval);
            if (flashTimer >= interval)
            {
                flashTimer = 0f;
                flip = !flip;

                if (levelUpText)
                    levelUpText.color = flip ? colors.textA : colors.textB;

                if (levelUpShadowText)
                    levelUpShadowText.color = flip ? colors.shadowA : colors.shadowB;
            }

            yield return null;
        }
    }

    static void BuildStatLinesFromSummary(string summary, List<string> outLines)
    {
        if (string.IsNullOrWhiteSpace(summary)) return;

        var parts = summary.Split(',');
        foreach (var raw in parts)
        {
            var p = raw.Trim();
            if (string.IsNullOrEmpty(p)) continue;

            int plusIdx = p.IndexOf('+');
            if (plusIdx <= 0 || plusIdx >= p.Length - 1)
            {
                outLines.Add(p);
                continue;
            }

            string label = p.Substring(0, plusIdx).Trim();
            string num = p.Substring(plusIdx + 1).Trim();

            outLines.Add($"+ {num} {label}");
        }
    }

    static string FormatXp(float xp)
    {
        float r = Mathf.Round(xp);
        if (Mathf.Abs(xp - r) < 0.01f) return r.ToString("0");
        return xp.ToString("0.#");
    }

    public void InitXpState(int level, float xpInto, float xpPerLevel = 100f)
    {
        _uiLevel = Mathf.Max(1, level);
        _uiXpInto = Mathf.Clamp(xpInto, 0f, xpPerLevel - 0.0001f);

        SetLevel(_uiLevel);
        SetXp(_uiXpInto, xpPerLevel);
    }

    public int AddXpFromOrb(float deltaXp, float xpPerLevel = 100f)
    {
        if (deltaXp <= 0f) return 0;

        _uiXpInto += deltaXp;

        int levelsGained = 0;

        while (_uiXpInto >= xpPerLevel)
        {
            _uiXpInto -= xpPerLevel;
            _uiLevel += 1;
            levelsGained += 1;

            // Reset bar immediately on level-up
            SetLevel(_uiLevel);
            SetXp(0f, xpPerLevel);
        }

        SetXp(_uiXpInto, xpPerLevel);
        return levelsGained;
    }

    public int GetUiLevel() => _uiLevel;
    public float GetUiXpInto() => _uiXpInto;

    // Expose anchors for orbs to animate towards
    public RectTransform GetXpBarCenter() => xpBarCenterRect;
    public RectTransform GetDistOrbAnchor() => xpOrbDistRect;
    public RectTransform GetDrainOrbAnchor() => xpOrbDrainRect;
}