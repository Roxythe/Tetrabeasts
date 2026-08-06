using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class EnemyCastleUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image castleImage; 
    public TMP_Text castleNameText; 
    public Slider healthBarSlider; 
    public TMP_Text healthBarText;  
    public TMP_Text levelNameText; 
    public Image levelBackgroundImage;
    public Image hpGaugeFrameImage;

    [Header("Damage Shake")]
    [SerializeField] bool shakeOnDamage = true;
    [SerializeField] RectTransform enemyHpShakeRoot;
    [SerializeField] RectTransform[] additionalDamageShakeTargets;
    [SerializeField, Min(0f)] float minDamageShakePixels = 1.5f;
    [SerializeField, Min(0f)] float maxDamageShakePixels = 18f;
    [SerializeField, Min(0.01f)] float minDamageShakeSeconds = 0.08f;
    [SerializeField, Min(0.01f)] float maxDamageShakeSeconds = 0.28f;
    [SerializeField, Min(1f)] float damageShakeSamplesPerSecond = 42f;
    [SerializeField, Min(0.01f)] float damageShakeAccumulationWindowSeconds = 0.35f;
    [SerializeField, Range(0.01f, 1f)] float damageForMaxShakeHpFraction = 0.25f;

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
    Sprite _defaultHpGaugeFrameSprite;
    bool _capturedHpGaugeFrameSprite = false;

    bool _magicShield = false;
    Coroutine _magicShieldCR;
    bool _infiniteHealth = false;
    bool _forceBossLevel = false;

    CastleData sourceData;
    int _lastStageIndex = -1;

    Coroutine _bossSpriteCR;
    Vector2 _bossOverlayBaseAnchoredPos;
    bool _bossOverlayBaseCaptured = false;

    Coroutine _damageShakeCR;
    List<DamageShakeTarget> _damageShakeTargets;
    readonly Queue<DamageShakeHit> _recentDamageShakeHits = new();

    public int CurrentHP => currentHP;
    public bool InfiniteHealth => _infiniteHealth;

    // New overload that accepts the level number
    public void InitCastle(CastleData data, int levelNumber)
    {
        InitCastle(data, levelNumber, 1f, false, false);
    }

    // Overload with HP multiplier (used for run modifiers)
    public void InitCastle(CastleData data, int levelNumber, float hpMult)
    {
        InitCastle(data, levelNumber, hpMult, false, false);
    }

    public void InitCastle(CastleData data, int levelNumber, float hpMult, bool forceInfiniteHealth, bool forceBossLevel)
    {
        if (data == null)
        {
            Debug.LogError("EnemyCastleUI.InitCastle called with null data");
            ApplyLevelBackground(null);
            ApplyHpGaugeFrame(null);
            return;
        }

        sourceData = data;
        _infiniteHealth = forceInfiniteHealth || data.infiniteHealth;
        _forceBossLevel = forceBossLevel;

        castleName = data.castleName;

        int baseMax = Mathf.Max(1, data.maxHP);
        maxHP = _infiniteHealth
            ? baseMax
            : Mathf.Max(1, Mathf.RoundToInt(baseMax * Mathf.Max(0.01f, hpMult)));
        currentHP = maxHP;
        StopDamageShake();
        ClearDamageShakeHistory();

        this.levelNumber = Mathf.Max(1, levelNumber);

        if (castleNameText) castleNameText.text = TetrabeastsLocalization.LocalizeText(castleName);
        if (levelNameText) levelNameText.text = TetrabeastsLocalization.LocalizeFormat("Level: {0}", this.levelNumber);
        ApplyLevelBackground(data);
        ApplyHpGaugeFrame(data);

        if (healthBarSlider)
        {
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = _infiniteHealth ? 1f : maxHP;
            healthBarSlider.value = _infiniteHealth ? 1f : currentHP;
        }

        CacheNormalHealthBarColorIfNeeded();
        SetInvulnerabilityVFX(false);
        SetMagicShieldActive(false);

        _lastStageIndex = -1;
        UpdateVisuals();
        SetupBossOverlay();
    }

    void OnDisable()
    {
        StopDamageShake();
        ClearDamageShakeHistory();
    }

    void ApplyLevelBackground(CastleData data)
    {
        if (!levelBackgroundImage)
            return;

        Sprite backgroundSprite = data ? data.levelBackgroundSprite : null;
        levelBackgroundImage.sprite = backgroundSprite;
        levelBackgroundImage.enabled = backgroundSprite != null;
    }

    void ApplyHpGaugeFrame(CastleData data)
    {
        Image frameImage = GetHpGaugeFrameImage();
        if (!frameImage)
            return;

        CacheDefaultHpGaugeFrameSpriteIfNeeded();

        Sprite frameSprite = data && data.hpGaugeFrameSprite
            ? data.hpGaugeFrameSprite
            : _defaultHpGaugeFrameSprite;

        if (!frameSprite)
            return;

        frameImage.sprite = frameSprite;
        frameImage.enabled = true;
    }

    // Call this whenever damage to the castle occurs
    public int ApplyDamage(int dmg)
    {
        if (dmg <= 0 || sourceData == null) return 0;

        if (_magicShield)
        {
            if (AudioManager.I && sourceData.bossPylonReducedHitSFX)
                AudioManager.I.PlaySFX(sourceData.bossPylonReducedHitSFX);
        }
        else if (_invulnerable)
        {
            // Play special invuln hit SFX (different from normal)
            if (AudioManager.I && sourceData.bossInvulnHitSFX)
                AudioManager.I.PlaySFX(sourceData.bossInvulnHitSFX);
            return 0;
        }

        if (_infiniteHealth)
        {
            TriggerBossDamageTakenSprite();
            TriggerDamageShake(dmg);
            UpdateVisuals();
            return dmg;
        }

        int appliedDamage = Mathf.Clamp(dmg, 0, currentHP);
        if (appliedDamage <= 0)
            return 0;

        currentHP = Mathf.Max(0, currentHP - appliedDamage);
        UpdateVisuals();

        TriggerBossDamageTakenSprite(); // Show damage sprite briefly, then return to correct idle
        TriggerDamageShake(appliedDamage);

        if (currentHP <= 0)
            OnCastleDestroyed();

        return appliedDamage;
    }

    void UpdateVisuals()
    {
        if (healthBarSlider)
            healthBarSlider.value = _infiniteHealth ? 1f : currentHP;

        if (healthBarText)
            healthBarText.text = _infiniteHealth ? "\u221E" : $"{currentHP} / {maxHP}";

        if (castleImage && sourceData != null)
        {
            float hpPercent = _infiniteHealth ? 1f : (float)currentHP / Mathf.Max(1, maxHP);

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

    void TriggerDamageShake(int appliedDamage)
    {
        if (!shakeOnDamage || appliedDamage <= 0)
            return;

        int recentDamage = RecordRecentDamageShakeHit(appliedDamage, Time.unscaledTime);

        StopDamageShake();

        _damageShakeTargets = BuildDamageShakeTargets();
        if (_damageShakeTargets == null || _damageShakeTargets.Count == 0)
            return;

        float maxDamageForShake = Mathf.Max(1f, maxHP * Mathf.Max(0.01f, damageForMaxShakeHpFraction));
        float intensity = Mathf.Clamp01(recentDamage / maxDamageForShake);
        float pixels = Mathf.Lerp(minDamageShakePixels, maxDamageShakePixels, intensity);
        float seconds = Mathf.Lerp(minDamageShakeSeconds, maxDamageShakeSeconds, intensity);

        _damageShakeCR = StartCoroutine(DamageShakeRoutine(seconds, pixels));
    }

    int RecordRecentDamageShakeHit(int appliedDamage, float now)
    {
        PruneRecentDamageShakeHits(now);
        _recentDamageShakeHits.Enqueue(new DamageShakeHit(now, appliedDamage));

        int total = 0;
        foreach (DamageShakeHit hit in _recentDamageShakeHits)
            total += hit.damage;

        return Mathf.Max(appliedDamage, total);
    }

    void PruneRecentDamageShakeHits(float now)
    {
        float window = Mathf.Max(0.01f, damageShakeAccumulationWindowSeconds);
        while (_recentDamageShakeHits.Count > 0 && now - _recentDamageShakeHits.Peek().time > window)
            _recentDamageShakeHits.Dequeue();
    }

    void ClearDamageShakeHistory()
    {
        _recentDamageShakeHits.Clear();
    }

    IEnumerator DamageShakeRoutine(float seconds, float pixels)
    {
        seconds = Mathf.Max(0.01f, seconds);
        pixels = Mathf.Max(0f, pixels);

        float elapsed = 0f;
        float sampleInterval = 1f / Mathf.Max(1f, damageShakeSamplesPerSecond);
        float nextSampleTime = 0f;
        Vector2 currentOffset = Vector2.zero;

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / seconds);
            float decay = 1f - normalizedTime;

            if (elapsed >= nextSampleTime)
            {
                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.001f)
                    direction = Vector2.right;

                direction.Normalize();
                currentOffset = direction * Random.Range(pixels * 0.35f, pixels) * decay;
                nextSampleTime = elapsed + sampleInterval;
            }

            ApplyDamageShakeOffset(currentOffset);
            yield return null;
        }

        ApplyDamageShakeOffset(Vector2.zero);
        _damageShakeCR = null;
        _damageShakeTargets = null;
    }

    List<DamageShakeTarget> BuildDamageShakeTargets()
    {
        var targets = new List<DamageShakeTarget>(5);

        AddDamageShakeTarget(targets, levelBackgroundImage ? levelBackgroundImage.rectTransform : null);
        AddDamageShakeTarget(targets, castleImage ? castleImage.rectTransform : null);
        AddDamageShakeTarget(targets, bossOverlayImage ? bossOverlayImage.rectTransform : null);
        AddDamageShakeTarget(targets, ResolveEnemyHpShakeRoot());

        if (additionalDamageShakeTargets != null)
        {
            for (int i = 0; i < additionalDamageShakeTargets.Length; i++)
                AddDamageShakeTarget(targets, additionalDamageShakeTargets[i]);
        }

        return targets;
    }

    RectTransform ResolveEnemyHpShakeRoot()
    {
        if (enemyHpShakeRoot)
            return enemyHpShakeRoot;

        enemyHpShakeRoot = FindChildRectTransform(transform, "EnemyHP_UI");

        Transform parent = transform.parent;
        while (!enemyHpShakeRoot && parent)
        {
            enemyHpShakeRoot = FindChildRectTransform(parent, "EnemyHP_UI");
            parent = parent.parent;
        }

        if (!enemyHpShakeRoot && healthBarSlider)
            enemyHpShakeRoot = healthBarSlider.transform as RectTransform;

        return enemyHpShakeRoot;
    }

    RectTransform FindChildRectTransform(Transform root, string targetName)
    {
        if (!root)
            return null;

        if (root.name == targetName)
            return root as RectTransform;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform match = FindChildRectTransform(root.GetChild(i), targetName);
            if (match)
                return match;
        }

        return null;
    }

    void AddDamageShakeTarget(List<DamageShakeTarget> targets, RectTransform rectTransform)
    {
        if (!rectTransform)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].rectTransform == rectTransform)
                return;
        }

        targets.Add(new DamageShakeTarget(rectTransform));
    }

    void ApplyDamageShakeOffset(Vector2 offset)
    {
        if (_damageShakeTargets == null)
            return;

        for (int i = 0; i < _damageShakeTargets.Count; i++)
        {
            DamageShakeTarget target = _damageShakeTargets[i];
            if (target == null || !target.rectTransform)
                continue;

            Vector2 expectedPosition = target.baseAnchoredPosition + target.previousOffset;
            if ((target.rectTransform.anchoredPosition - expectedPosition).sqrMagnitude > 0.25f)
                target.baseAnchoredPosition = target.rectTransform.anchoredPosition;

            target.previousOffset = offset;
            target.rectTransform.anchoredPosition = target.baseAnchoredPosition + offset;
        }
    }

    void StopDamageShake()
    {
        if (_damageShakeCR != null)
        {
            StopCoroutine(_damageShakeCR);
            _damageShakeCR = null;
        }

        ResetDamageShakeTargets();
    }

    void ResetDamageShakeTargets()
    {
        if (_damageShakeTargets == null)
            return;

        for (int i = 0; i < _damageShakeTargets.Count; i++)
        {
            DamageShakeTarget target = _damageShakeTargets[i];
            if (target == null || !target.rectTransform)
                continue;

            Vector2 expectedPosition = target.baseAnchoredPosition + target.previousOffset;
            if ((target.rectTransform.anchoredPosition - expectedPosition).sqrMagnitude <= 0.25f)
                target.rectTransform.anchoredPosition = target.baseAnchoredPosition;

            target.previousOffset = Vector2.zero;
        }

        _damageShakeTargets = null;
    }

    class DamageShakeTarget
    {
        public readonly RectTransform rectTransform;
        public Vector2 baseAnchoredPosition;
        public Vector2 previousOffset;

        public DamageShakeTarget(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            baseAnchoredPosition = rectTransform.anchoredPosition;
            previousOffset = Vector2.zero;
        }
    }

    struct DamageShakeHit
    {
        public readonly float time;
        public readonly int damage;

        public DamageShakeHit(float time, int damage)
        {
            this.time = time;
            this.damage = damage;
        }
    }

    void SetupBossOverlay()
    {
        if (!bossOverlayImage) return;

        bool on = IsBossLevelConfigured();
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

    bool IsBossLevelConfigured()
    {
        return sourceData != null && (sourceData.isBossLevel || _forceBossLevel);
    }

    public void StartInvulnerability(float seconds)
    {
        if (seconds <= 0f) return;

        if (_invulnCR != null) StopCoroutine(_invulnCR);
        _invulnCR = StartCoroutine(InvulnRoutine(seconds));
    }

    public void ClearInvulnerability()
    {
        if (_invulnCR != null)
        {
            StopCoroutine(_invulnCR);
            _invulnCR = null;
        }

        _invulnerable = false;
        SetInvulnerabilityVFX(false);
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

    Image GetHpGaugeFrameImage()
    {
        if (hpGaugeFrameImage) return hpGaugeFrameImage;

        Transform searchRoot = healthBarSlider ? healthBarSlider.transform : transform;
        var images = searchRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] && images[i].name == "BorderFrame_Image")
            {
                hpGaugeFrameImage = images[i];
                return hpGaugeFrameImage;
            }
        }

        return null;
    }

    void CacheDefaultHpGaugeFrameSpriteIfNeeded()
    {
        if (_capturedHpGaugeFrameSprite) return;

        var frameImage = GetHpGaugeFrameImage();
        _defaultHpGaugeFrameSprite = frameImage ? frameImage.sprite : null;
        _capturedHpGaugeFrameSprite = true;
    }

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
        return IsBossLevelConfigured() && bossOverlayImage != null && bossOverlayImage.enabled;
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
