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

    CastleData sourceData;

    // Call this at the start of a level
    public void InitCastle(CastleData data)
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

        if (castleNameText) castleNameText.text = castleName;
        if (levelNameText) levelNameText.text = castleName;

        // set up slider
        if (healthBarSlider)
        {
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = maxHP;
            healthBarSlider.value = currentHP;
        }

        UpdateVisuals();
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
        // Update health bar value & text
        if (healthBarSlider)
            healthBarSlider.value = currentHP;

        if (healthBarText)
            healthBarText.text = $"{currentHP} / {maxHP}";

        // Update sprite based on %HP
        if (castleImage && sourceData != null)
        {
            float hpPercent = (float)currentHP / (float)maxHP;
            castleImage.sprite = sourceData.GetSpriteForHealth(hpPercent);
        }
    }

    void OnCastleDestroyed()
    {
        // We'll let GameController decide what to do on win
        Debug.Log("Castle destroyed! Player wins level.");
    }
}
