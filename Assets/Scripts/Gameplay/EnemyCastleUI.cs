using UnityEngine;
using UnityEngine.UI;
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

    CastleData sourceData;
    int _lastStageIndex = -1;

    // Call this at the start of a level
    public void InitCastle(CastleData data)
    {
        InitCastle(data, 1);
    }

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

        _lastStageIndex = -1;
        UpdateVisuals();
        SetupBossOverlay();
    }

    // Call this whenever you deal damage to the castle
    public void ApplyDamage(int dmg)
    {
        if (dmg <= 0 || sourceData == null) return;

        currentHP = Mathf.Max(0, currentHP - dmg);
        UpdateVisuals();

        // optional: hit sfx/flash here
        // if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxCastleHit);

        if (currentHP <= 0)
        {
            OnCastleDestroyed();
        }
    }

    void UpdateVisuals()
    {
        if (healthBarSlider) healthBarSlider.value = currentHP;
        if (healthBarText) healthBarText.text = $"{currentHP} / {maxHP}";

        if (castleImage && sourceData != null)
        {
            float hpPercent = (float)currentHP / Mathf.Max(1, maxHP);

            // Compute stage index exactly like CastleData.GetSpriteForHealth does
            int stageIndex =
                (hpPercent >= 0.76f) ? 0 :
                (hpPercent >= 0.51f) ? 1 :
                (hpPercent >= 0.26f) ? 2 : 3;

            // If stage changed (and not the very first initialization), play SFX
            if (_lastStageIndex != -1 && stageIndex != _lastStageIndex)
            {
                var clip = sourceData.PickRandom(sourceData.sfxDamageStageClips, null);
                if (clip && AudioManager.I) AudioManager.I.PlaySFX(clip);
            }

            _lastStageIndex = stageIndex;

            // Update the sprite
            castleImage.sprite = sourceData.GetSpriteForHealth(hpPercent);
        }
    }

    void OnCastleDestroyed()
    {
        // We'll let GameController decide what to do on win
        Debug.Log("Castle destroyed! Player wins level.");
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


}
