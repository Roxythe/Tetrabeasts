using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class EnemyCastleUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image castleImage;          // Castle_Image
    public TMP_Text castleNameText;    // CastleName_Text
    public Slider healthBarSlider;     // HealthBar_Slider
    public TMP_Text healthBarText;     // HealthBar_Text
    public TMP_Text levelNameText;     // (optional portrait label)

    [Header("Runtime State (read-only for other scripts)")]
    public int maxHP { get; private set; }
    public int currentHP { get; private set; }
    public string castleName { get; private set; }
    public int levelNumber { get; private set; }

    [Header("Boss Overlay")]
    public Image bossOverlayImage;             // Drag your Boss_Image here
    public Vector2 bossOverlaySize = Vector2.zero; // Optional fixed size; leave (0,0) to inherit castle size
    public Vector2 bossOverlayOffset = Vector2.zero; // Fine-tune placement

    [Header("Invulnerability VFX")]
    public Image invulnShieldImageA;
    public Image invulnShieldImageB; 
    public Image healthBarFillImage; // The Image component used to tint the health bar fill on invulnerability

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
    public void ApplyDamage(int dmg)
    {
        if (dmg <= 0 || sourceData == null) return;

        if (_invulnerable)
        {
            // Play special invuln hit SFX (different from normal)
            if (AudioManager.I && sourceData.bossInvulnHitSFX)
                AudioManager.I.PlaySFX(sourceData.bossInvulnHitSFX);
            return;
        }

        if (_magicShield)
        {
            if (AudioManager.I && sourceData.bossPylonReducedHitSFX)
                AudioManager.I.PlaySFX(sourceData.bossPylonReducedHitSFX);
        }

        currentHP = Mathf.Max(0, currentHP - dmg);
        UpdateVisuals();

        if (currentHP <= 0)
            OnCastleDestroyed();
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
        Debug.Log("Castle destroyed! Player wins level.");  // Let GameController decide what to do on win
    }

    void SetupBossOverlay()
    {
        if (!bossOverlayImage) return;

        bool on = (sourceData != null && sourceData.isBossLevel);
        bossOverlayImage.enabled = on;
        if (!on) return;

        if (sourceData.bossOverlaySprite)
            bossOverlayImage.sprite = sourceData.bossOverlaySprite;

        var brt = bossOverlayImage.rectTransform;
        bossOverlayImage.preserveAspect = true;
        bossOverlayImage.raycastTarget = false;

        // Optional size override (safe)
        if (bossOverlaySize != Vector2.zero)
            brt.sizeDelta = bossOverlaySize;
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

}
