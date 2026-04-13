using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class EnemyCastleUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image castleImage; 
    public TMP_Text castleNameText; 
    public Slider healthBarSlider; 
    public TMP_Text healthBarText;  
    public TMP_Text levelNameText; 

    [Header("Runtime State (read-only for other scripts)")]
    public int maxHP { get; private set; }
    public int currentHP { get; private set; }
    public string castleName { get; private set; }
    public int levelNumber { get; private set; }

    [Header("Boss Overlay")]
    public Image bossOverlayImage;            
    public Vector2 bossOverlaySize = Vector2.zero; 
    public Vector2 bossOverlayOffset = Vector2.zero;
    public UISweep bossIdleSweep;

    [Header("Invulnerability VFX")]
    public Image invulnShieldImageA;
    public Image invulnShieldImageB; 
    public Image healthBarFillImage; 

    [Header("Magic Shield (Pylons)")]
    public Image magicShieldImage;

    [Tooltip("Health bar fill color while invulnerable (silver/gray).")]
    public Color invulnHealthBarColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    bool _invulnerable = false;
    Coroutine _invulnCR;
    Color _normalHealthBarColor = Color.white;
    bool _capturedHealthBarColor = false;

    bool _magicShield = false;
    Coroutine _magicShieldCR;

    CastleData sourceData;
    int _lastStageIndex = -1;

    Coroutine _bossSpriteCR;
    Vector2 _bossOverlayBaseAnchoredPos;
    bool _bossOverlayBaseCaptured = false;

    // New overload that accepts the level number
    public void InitCastle(CastleData data, int levelNumber)
    {
        if (data == null)
        {
            Debug.LogError("EnemyCastleUI.InitCastle called with null data");
            return;
        }

        sourceData = data;

        castleName = data.castleName;
        maxHP = Mathf.Max(1, data.maxHP);
        currentHP = maxHP;
        this.levelNumber = Mathf.Max(1, levelNumber);

        if (castleNameText) castleNameText.text = castleName;
        if (levelNameText) levelNameText.text = $"Level: {this.levelNumber}";

        if (healthBarSlider)
        {
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = maxHP;
            healthBarSlider.value = currentHP;
        }

        CacheNormalHealthBarColorIfNeeded();
        SetInvulnerabilityVFX(false);
        SetMagicShieldActive(false);

        _lastStageIndex = -1;
        UpdateVisuals();
        SetupBossOverlay();
    }

    // Overload with HP multiplier (used for run modifiers)
    public void InitCastle(CastleData data, int levelNumber, float hpMult)
    {
        if (data == null)
        {
            Debug.LogError("EnemyCastleUI.InitCastle called with null data");
            return;
        }

        sourceData = data;

        castleName = data.castleName;

        int baseMax = Mathf.Max(1, data.maxHP);
        maxHP = Mathf.Max(1, Mathf.RoundToInt(baseMax * Mathf.Max(0.01f, hpMult)));
        currentHP = maxHP;

        this.levelNumber = Mathf.Max(1, levelNumber);

        if (castleNameText) castleNameText.text = castleName;
        if (levelNameText) levelNameText.text = $"Level: {this.levelNumber}";

        if (healthBarSlider)
        {
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = maxHP;
            healthBarSlider.value = currentHP;
        }

        CacheNormalHealthBarColorIfNeeded();
        SetInvulnerabilityVFX(false);

        _lastStageIndex = -1;
        UpdateVisuals();
        SetupBossOverlay();
    }

    // Call this whenever damage to the castle occurs
    public int ApplyDamage(int dmg)
    {
        if (dmg <= 0 || sourceData == null) return 0;

        if (_invulnerable)
        {
            // Play special invuln hit SFX (different from normal)
            if (AudioManager.I && sourceData.bossInvulnHitSFX)
                AudioManager.I.PlaySFX(sourceData.bossInvulnHitSFX);
            return 0;
        }

        if (_magicShield)
        {
            if (AudioManager.I && sourceData.bossPylonReducedHitSFX)
                AudioManager.I.PlaySFX(sourceData.bossPylonReducedHitSFX);
        }

        int appliedDamage = Mathf.Clamp(dmg, 0, currentHP);
        if (appliedDamage <= 0)
            return 0;

        currentHP = Mathf.Max(0, currentHP - appliedDamage);
        UpdateVisuals();

        TriggerBossDamageTakenSprite(); // Show damage sprite briefly, then return to correct idle

        if (currentHP <= 0)
            OnCastleDestroyed();

        return appliedDamage;
    }

    void UpdateVisuals()
    {
        if (healthBarSlider) healthBarSlider.value = currentHP;
        if (healthBarText) healthBarText.text = $"{currentHP} / {maxHP}";

        if (castleImage && sourceData != null)
        {
            float hpPercent = (float)currentHP / Mathf.Max(1, maxHP);

            // Compute stage index
            int stageIndex =
                (hpPercent >= 0.76f) ? 0 :
                (hpPercent >= 0.51f) ? 1 :
                (hpPercent >= 0.26f) ? 2 : 3;

            // If stage changed play SFX
            if (_lastStageIndex != -1 && stageIndex != _lastStageIndex)
            {
                var clip = sourceData.PickRandom(sourceData.sfxDamageStageClips, null);
                if (clip && AudioManager.I) AudioManager.I.PlaySFX(clip);
            }

            _lastStageIndex = stageIndex;
            castleImage.sprite = sourceData.GetSpriteForHealth(hpPercent); // Update the sprite
        }
    }

    void OnCastleDestroyed()
    {
        Debug.Log("Castle destroyed! Player wins level.");  // GameController decides what to do on win
    }

    void SetupBossOverlay()
    {
        if (!bossOverlayImage) return;

        bool on = (sourceData != null && sourceData.isBossLevel);
        bossOverlayImage.enabled = on;

        if (!on)
        {
            StopBossSpriteRoutineIfAny();
            SetBossIdleMotionPaused(true);
            return;
        }

        var brt = bossOverlayImage.rectTransform;

        if (!_bossOverlayBaseCaptured)
        {
            _bossOverlayBaseAnchoredPos = brt.anchoredPosition;
            _bossOverlayBaseCaptured = true;
        }

        bossOverlayImage.preserveAspect = true;
        bossOverlayImage.raycastTarget = false;

        // Size override 
        if (bossOverlaySize != Vector2.zero)
            brt.sizeDelta = bossOverlaySize;

        // Always start in the correct idle 
        ResetBossOverlayPosition();
        var idle = GetBossIdleSprite();
        if (idle) bossOverlayImage.sprite = idle;

        SetBossIdleMotionPaused(false);
    }

    public void StartInvulnerability(float seconds)
    {
        if (seconds <= 0f) return;

        if (_invulnCR != null) StopCoroutine(_invulnCR);
        _invulnCR = StartCoroutine(InvulnRoutine(seconds));
    }

    IEnumerator InvulnRoutine(float seconds)
    {
        _invulnerable = true;
        SetInvulnerabilityVFX(true);

        yield return new WaitForSeconds(seconds);

        _invulnerable = false;
        SetInvulnerabilityVFX(false);
        _invulnCR = null;
    }


    // ================= Invulnerability UI VFX =================

    Image GetHealthFillImage()
    {
        if (healthBarFillImage) return healthBarFillImage;

        if (healthBarSlider && healthBarSlider.fillRect)
        {
            var img = healthBarSlider.fillRect.GetComponent<Image>();
            if (img)
            {
                healthBarFillImage = img; // cache for future
                return img;
            }
        }

        return null;
    }

    void CacheNormalHealthBarColorIfNeeded()
    {
        if (_capturedHealthBarColor) return;

        var fill = GetHealthFillImage();
        if (!fill) return;

        _normalHealthBarColor = fill.color;
        _capturedHealthBarColor = true;
    }

    void SetInvulnerabilityVFX(bool on)
    {
        // Turn on/off both shield images
        if (invulnShieldImageA) invulnShieldImageA.gameObject.SetActive(on);
        if (invulnShieldImageB) invulnShieldImageB.gameObject.SetActive(on);

        // Tint health bar fill
        var fill = GetHealthFillImage();
        if (fill)
        {
            CacheNormalHealthBarColorIfNeeded();
            fill.color = on ? invulnHealthBarColor : _normalHealthBarColor;
        }
    }

    public void SetMagicShieldActive(bool on)
    {
        _magicShield = on;

        if (magicShieldImage)
            magicShieldImage.gameObject.SetActive(on);
    }

    // ================= Boss Sprite Routines (attack shift, damage flash) =================

    void StopBossSpriteRoutineIfAny()
    {
        if (_bossSpriteCR != null)
        {
            StopCoroutine(_bossSpriteCR);
            _bossSpriteCR = null;
        }
    }

    bool IsBossLevelActive()
    {
        return sourceData != null && sourceData.isBossLevel && bossOverlayImage != null && bossOverlayImage.enabled;
    }

    bool IsBossCriticalHP()
    {
        if (sourceData == null) return false;

        float t = Mathf.Clamp01(sourceData.bossCriticalHpThreshold <= 0f ? 0.30f : sourceData.bossCriticalHpThreshold);
        int cutoff = Mathf.CeilToInt(maxHP * t);
        return currentHP <= cutoff;
    }

    Sprite GetBossIdleSprite()
    {
        if (sourceData == null) return null;

        if (IsBossCriticalHP() && sourceData.bossCriticalIdleSprite)
            return sourceData.bossCriticalIdleSprite;

        return sourceData.bossOverlaySprite;
    }

    Sprite GetBossAttackSprite()
    {
        if (sourceData == null) return null;

        if (IsBossCriticalHP() && sourceData.bossCriticalAttackSprite)
            return sourceData.bossCriticalAttackSprite;

        return sourceData.bossAttackSprite;
    }

    Sprite GetBossDamageTakenSprite()
    {
        if (sourceData == null) return null;

        if (IsBossCriticalHP() && sourceData.bossCriticalDamageTakenSprite)
            return sourceData.bossCriticalDamageTakenSprite;

        return sourceData.bossDamageTakenSprite;
    }

    void ResetBossOverlayPosition()
    {
        if (!bossOverlayImage) return;

        var brt = bossOverlayImage.rectTransform;

        if (!_bossOverlayBaseCaptured)
        {
            _bossOverlayBaseAnchoredPos = brt.anchoredPosition;
            _bossOverlayBaseCaptured = true;
        }

        brt.anchoredPosition = brt.anchoredPosition + bossOverlayOffset;
    }

    void SetBossOverlaySprite(Sprite sprite)
    {
        if (!bossOverlayImage || sprite == null) return;
        bossOverlayImage.sprite = sprite;
    }

    public void PlayBossAttackSprite()
    {
        if (!IsBossLevelActive()) return;

        var sprite = GetBossAttackSprite();
        if (sprite == null) return;

        StopBossSpriteRoutineIfAny();
        SetBossIdleMotionPaused(true);
        _bossSpriteCR = StartCoroutine(BossAttackRoutine(sprite));
    }

    void TriggerBossDamageTakenSprite()
    {
        if (!IsBossLevelActive()) return;

        var sprite = GetBossDamageTakenSprite();
        if (sprite == null) return;

        // Damage should interrupt attack 
        StopBossSpriteRoutineIfAny();
        SetBossIdleMotionPaused(true);
        _bossSpriteCR = StartCoroutine(BossDamageRoutine(sprite));
    }

    IEnumerator BossAttackRoutine(Sprite sprite)
    {
        var brt = bossOverlayImage.rectTransform;
        Vector2 startPos = brt.anchoredPosition;

        SetBossOverlaySprite(sprite);

        float dir = (sourceData != null && sourceData.bossAttackShiftRight) ? 1f : -1f;
        float dist = (sourceData != null) ? sourceData.bossAttackShiftDistance : 20f;

        brt.anchoredPosition = startPos + new Vector2(dir * dist, 0f);

        float seconds = (sourceData != null && sourceData.bossAttackSpriteSeconds > 0f)
            ? sourceData.bossAttackSpriteSeconds
            : 0.25f;

        yield return new WaitForSeconds(seconds);

        brt.anchoredPosition = startPos;
        SetBossOverlaySprite(GetBossIdleSprite());

        _bossSpriteCR = null;
        SetBossIdleMotionPaused(false);
    }

    IEnumerator BossDamageRoutine(Sprite sprite)
    {
        var brt = bossOverlayImage.rectTransform;

        Vector2 startPos = brt.anchoredPosition;

        SetBossOverlaySprite(sprite);

        float seconds = (sourceData != null && sourceData.bossDamageSpriteSeconds > 0f)
            ? sourceData.bossDamageSpriteSeconds
            : 0.18f;

        yield return new WaitForSeconds(seconds);

        brt.anchoredPosition = startPos;
        SetBossOverlaySprite(GetBossIdleSprite());

        _bossSpriteCR = null;
        SetBossIdleMotionPaused(false);
    }

    void SetBossIdleMotionPaused(bool paused)
    {
        if (!bossIdleSweep && bossOverlayImage)
            bossIdleSweep = bossOverlayImage.GetComponent<UISweep>();

        if (!bossIdleSweep) return;

        if (!paused)
            UpdateBossIdleSpeedForCurrentHP();

        bossIdleSweep.SetPaused(paused);
    }

    void UpdateBossIdleSpeedForCurrentHP()
    {
        if (!bossIdleSweep) return;

        bool critical = IsBossCriticalHP();

        bossIdleSweep.SetCriticalHobble(critical);
        bossIdleSweep.SetSpeedMultiplier(1f);
    }
}
