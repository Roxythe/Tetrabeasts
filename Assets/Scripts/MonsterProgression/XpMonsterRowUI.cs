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
    public TMP_Text xpTransferInfoText;

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

    [Header("Level Up Stat Pulse")]
    [Min(1.01f)] public float levelUpStatPulseScale = 1.12f;
    [Min(0.01f)] public float levelUpStatPulseUpSeconds = 0.08f;
    [Min(0.01f)] public float levelUpStatPulseDownSeconds = 0.10f;

    Coroutine _statsCycleCR;
    Coroutine _pulseCR;

    Vector3 _statScale0;
    Coroutine _statPulseCR;

    readonly List<string> _statLines = new();
    int _levelUpCount = 0;

    Color _lvlText0;
    Color _lvlShadow0;
    Vector3 _lvlScale0;
    Vector3 _lvlShadowScale0;

    int _uiLevel = 1;
    float _uiXpInto = 0f;
    int _levelTransitionStartLevel = 1;
    bool _showingLevelTransition;

    public void BindStatic(Sprite portrait, string displayName)
    {
        if (portraitImage) portraitImage.sprite = portrait;
        if (nameText) nameText.text = displayName ?? "";
        HideDeltaTexts();
    }

    public void SetLevel(int level)
    {
        level = Mathf.Max(1, level);
        _levelTransitionStartLevel = level;
        _showingLevelTransition = false;

        if (levelText) levelText.text = $"Level {level}";
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

        if (xpTransferInfoText)
        {
            xpTransferInfoText.gameObject.SetActive(false);
            xpTransferInfoText.text = string.Empty;
        }

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

        if (levelUpStatsText)
            _statScale0 = levelUpStatsText.transform.localScale;

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
            {
                levelUpStatsText.text = _statLines[0];
                PulseLevelUpStatsOnce();
            }
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
        if (_statPulseCR != null) { StopCoroutine(_statPulseCR); _statPulseCR = null; }

        if (levelUpStatsText)
            levelUpStatsText.transform.localScale = _statScale0 == default ? Vector3.one : _statScale0;

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

            PulseLevelUpStatsOnce();

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

    static string FormatXp(float xp)
    {
        float r = Mathf.Round(xp);
        if (Mathf.Abs(xp - r) < 0.01f) return r.ToString("0");
        return xp.ToString("0.#");
    }

    public void InitXpState(int level, float xpInto, float xpPerLevel = 100f)
    {
        HideLevelUps(); // Reset level-up display 

        _uiLevel = Mathf.Max(1, level);
        _uiXpInto = Mathf.Clamp(xpInto, 0f, xpPerLevel - 0.0001f);
        _levelTransitionStartLevel = _uiLevel;
        _showingLevelTransition = false;

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
            ShowLevelTransition(_uiLevel);
            SetXp(0f, xpPerLevel);
        }

        SetXp(_uiXpInto, xpPerLevel);
        return levelsGained;
    }

    void ShowLevelTransition(int newLevel)
    {
        newLevel = Mathf.Max(1, newLevel);
        _showingLevelTransition = true;

        if (levelText)
            levelText.text = $"Level {_levelTransitionStartLevel} -> Level {newLevel}";
    }

    public int SubtractXpFromOrb(float deltaXp, float xpPerLevel = 100f)
    {
        int steps = Mathf.Max(0, Mathf.FloorToInt(deltaXp));
        if (steps <= 0) return 0;

        int levelsLost = 0;

        for (int s = 0; s < steps; s++)
        {
            // Already fully drained
            if (_uiLevel <= 1 && _uiXpInto <= 0f)
            {
                _uiLevel = 1;
                _uiXpInto = 0f;
                break;
            }

            // Normal decrement within current level
            if (_uiXpInto > 0f)
            {
                _uiXpInto -= 1f;
                if (_uiXpInto < 0f) _uiXpInto = 0f;
            }
            else
            {
                // At 0/100 drop a level and set to 99/100
                if (_uiLevel > 1)
                {
                    _uiLevel -= 1;
                    levelsLost += 1;
                    _uiXpInto = xpPerLevel - 1f; // 99 for 100-based XP
                }
                else
                {
                    _uiLevel = 1;
                    _uiXpInto = 0f;
                    break;
                }
            }
        }

        SetLevel(_uiLevel);
        SetXp(_uiXpInto, xpPerLevel);

        return levelsLost;
    }

    void PulseLevelUpStatsOnce()
    {
        if (!levelUpStatsText) return;

        if (_statPulseCR != null)
            StopCoroutine(_statPulseCR);

        _statPulseCR = StartCoroutine(CoPulseLevelUpStatsOnce());
    }

    IEnumerator CoPulseLevelUpStatsOnce()
    {
        if (!levelUpStatsText) yield break;

        if (_statScale0 == default)
            _statScale0 = levelUpStatsText.transform.localScale;

        Vector3 from = _statScale0;
        Vector3 to = _statScale0 * Mathf.Max(1.01f, levelUpStatPulseScale);

        float up = Mathf.Max(0.01f, levelUpStatPulseUpSeconds);
        float down = Mathf.Max(0.01f, levelUpStatPulseDownSeconds);

        float t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / up);
            levelUpStatsText.transform.localScale = Vector3.Lerp(from, to, a);
            yield return null;
        }

        t = 0f;
        while (t < down)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / down);
            levelUpStatsText.transform.localScale = Vector3.Lerp(to, from, a);
            yield return null;
        }

        levelUpStatsText.transform.localScale = from;
        _statPulseCR = null;
    }

    public void ShowXpDrainTransferInfo(float preservedXp, float drainableXp, float conversionFraction)
    {
        if (!xpTransferInfoText)
            return;

        xpTransferInfoText.gameObject.SetActive(true);

        int percent = Mathf.RoundToInt(Mathf.Clamp01(conversionFraction) * 100f);
        xpTransferInfoText.text = $"{FormatXp(preservedXp)} permanent EXP ({percent}% of {FormatXp(drainableXp)} transferable EXP)";
    }

    public void ShowXpCommitTransferInfo(float preservedXp, float drainableXp, float conversionFraction)
    {
        if (!xpTransferInfoText)
            return;

        xpTransferInfoText.gameObject.SetActive(true);

        int percent = Mathf.RoundToInt(Mathf.Clamp01(conversionFraction) * 100f);
        xpTransferInfoText.text = $"Converted from {FormatXp(drainableXp)} run EXP at {percent}%";
    }

    public void HideXpTransferInfo()
    {
        if (!xpTransferInfoText)
            return;

        xpTransferInfoText.gameObject.SetActive(false);
        xpTransferInfoText.text = string.Empty;
    }

    public int GetUiLevel() => _uiLevel;
    public float GetUiXpInto() => _uiXpInto;
    public bool IsShowingLevelTransition() => _showingLevelTransition;

    // Expose anchors for orbs to animate towards
    public RectTransform GetXpBarCenter() => xpBarCenterRect;
    public RectTransform GetDistOrbAnchor() => xpOrbDistRect;
    public RectTransform GetDrainOrbAnchor() => xpOrbDrainRect;
}
