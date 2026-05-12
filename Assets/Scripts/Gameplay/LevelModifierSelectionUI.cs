using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelModifierSelectionUI : MonoBehaviour
{
    public delegate bool RerollHandler(LevelModifierSO currentModifier, out LevelModifierSO nextModifier);

    class SlotColumn
    {
        public RectTransform viewport;
        public RectTransform content;
        public readonly List<Image> icons = new();
        public float targetY;
        public float cycleHeight;
    }

    [Header("Root")]
    [SerializeField] RectTransform root;
    [SerializeField] TMP_Text modDisplayNameText;
    [SerializeField] TMP_Text modDisplayNameShadowText;

    [Header("Lever Prompt")]
    [SerializeField] RectTransform leverArrowVisual;
    [SerializeField] float leverArrowPulseScale = 1.12f;
    [SerializeField] float leverArrowPulseSpeed = 2.5f;

    [Header("Modifier Name Animation")]
    [SerializeField] BuffPopupStyleSO modNameStyle;
    [SerializeField] RunModRarity modNameRarity = RunModRarity.Legendary;

    [Header("Buttons")]
    [SerializeField] Button leverButton;
    [SerializeField] RectTransform leverVisual;
    [SerializeField] Button continueButton;
    [SerializeField] Button rerollButton;
    [SerializeField] Button modifierInfoButton;
    [SerializeField] TMP_Text rerollCountText;
    [SerializeField] TMP_Text shadowRerollCountText;

    [Header("Modifier Info Panel")]
    [SerializeField] GameObject modifierInfoPanel;
    [SerializeField] TMP_Text modifierInfoTitleText;
    [SerializeField] TMP_Text modifierInfoShadowTitleText;
    [SerializeField] Image modifierInfoIcon;
    [SerializeField] TMP_Text modifierInfoDescText;
    [SerializeField] Button closeInfoButton;

    [Header("Slot Columns")]
    [SerializeField] RectTransform[] viewports = new RectTransform[3];
    [SerializeField] RectTransform[] contents = new RectTransform[3];

    [Header("Icon Prefab")]
    [SerializeField] Image iconPrefab;

    [Header("Layout")]
    [SerializeField] Vector2 iconSize = new Vector2(108f, 108f);
    [SerializeField] float iconSpacing = 118f;

    [Header("Animation")]
    [SerializeField] float spinDuration = 2.2f;
    [SerializeField] float stopDelayPerColumn = 0.28f;
    [SerializeField] float spinSpeedMultiplier = 1f;
    [SerializeField] bool spinInReverse = true;
    [SerializeField] float spinCyclesPerSecond = 7f;
    [SerializeField] float settleDuration = 0.45f;
    [SerializeField, Range(2f, 8f)] float stopEaseExponent = 4f;
    [SerializeField] float leverPressAngle = 18f;
    [SerializeField] float leverPressDuration = 0.10f;
    [SerializeField] float leverReturnDuration = 0.14f;
    [SerializeField] float rerollRebuildDelaySeconds = 0.06f;

    [Header("Slot Machine Lights")]
    [SerializeField] SlotMachineLightUI[] slotMachineLights;
    [SerializeField] bool autoFindSlotMachineLights = true;
    [SerializeField, Min(0f)] float revealLightPulseSpeedMultiplier = 1.35f;
    [SerializeField, Min(0f)] float revealLightColorSpeedMultiplier = 1.2f;
    [SerializeField, Range(0f, 0.5f)] float revealLightSyncedPulseScale = 0.12f;
    [SerializeField, Range(0f, 1f)] float revealLightSyncedBrightness = 1f;
    [SerializeField, Range(0f, 1f)] float revealLightSyncedPhaseOffset;
    [SerializeField] float revealLightSyncedColorOffset;

    readonly List<SlotColumn> _columns = new();
    readonly List<SlotMachineLightUI> _runtimeSlotMachineLights = new();

    bool _spinClicked;
    bool _continueClicked;
    bool _rerollClicked;
    bool _isSpinning;

    LevelModifierSO _currentChosenModifier;
    Quaternion _leverDefaultRotation = Quaternion.identity;
    Vector3 _leverArrowBaseScale = Vector3.one;
    Vector3 _modNameBaseScale = Vector3.one;
    Vector3 _modNameShadowBaseScale = Vector3.one;
    Color _modNameBaseColor = Color.white;
    Color _modNameShadowBaseColor = Color.white;
    Coroutine _modNameAnimCR;

    public LevelModifierSO CurrentChosenModifier => _currentChosenModifier;
    public RectTransform TutorialTarget => root ? root : transform as RectTransform;

    public IEnumerator PlaySelection(
        IReadOnlyList<LevelModifierSO> pool,
        LevelModifierSO chosen,
        Func<int> getAvailableRerolls = null,
        RerollHandler rerollHandler = null)
    {
        if (!chosen)
            yield break;

        EnsureReferences();
        BuildColumns();

        if (root == null || _columns.Count == 0)
            yield break;

        _spinClicked = false;
        _continueClicked = false;
        _rerollClicked = false;
        _isSpinning = false;
        _currentChosenModifier = chosen;

        CacheSlotMachineLights();
        ResetSlotMachineLights();

        UIPanelTransition.Show(root.gameObject);

        var gameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
        gameController?.QueueFirstLevelModifierTutorialIfNeeded(TutorialTarget);

        if (leverVisual)
            _leverDefaultRotation = leverVisual.localRotation;

        if (leverArrowVisual)
        {
            _leverArrowBaseScale = leverArrowVisual.localScale;
            leverArrowVisual.localScale = _leverArrowBaseScale;
            leverArrowVisual.gameObject.SetActive(true);
        }

        if (modDisplayNameText)
        {
            _modNameBaseScale = modDisplayNameText.rectTransform.localScale;
            _modNameBaseColor = modDisplayNameText.color;
            modDisplayNameText.rectTransform.localScale = _modNameBaseScale;
            modDisplayNameText.text = string.Empty;
        }

        if (modDisplayNameShadowText)
        {
            _modNameShadowBaseScale = modDisplayNameShadowText.rectTransform.localScale;
            _modNameShadowBaseColor = modDisplayNameShadowText.color;
            modDisplayNameShadowText.rectTransform.localScale = _modNameShadowBaseScale;
            modDisplayNameShadowText.text = string.Empty;
        }

        StopModNameAnimation();

        ClearModifierReveal();
        RefreshRerollUI(GetAvailableRerolls(getAvailableRerolls), showButton: false, rerollHandler);

        if (leverButton)
        {
            leverButton.gameObject.SetActive(true);
            leverButton.interactable = true;
        }

        var sprites = BuildSpritePool(pool, chosen);
        bool pendingRebuildForNextSpin = false;

        for (int i = 0; i < _columns.Count; i++)
            PopulateColumn(_columns[i], sprites, chosen.icon);

        yield return new WaitUntil(() => _spinClicked);

        if (leverButton)
            leverButton.interactable = false;

        while (true)
        {
            yield return StartCoroutine(PlayLeverPullRoutine());
            yield return StartCoroutine(SpinColumnsRoutine(
                         rebuildColumnsAfterSpinStarts: pendingRebuildForNextSpin,
                         rebuildPool: sprites, rebuildChosenSprite: chosen.icon));

            SyncSlotMachineLightsForReveal();
            RevealModifier(chosen);

            if (leverArrowVisual)
                leverArrowVisual.localScale = _leverArrowBaseScale;

            _continueClicked = false;
            _rerollClicked = false;

            RefreshRerollUI(GetAvailableRerolls(getAvailableRerolls), showButton: true, rerollHandler);

            bool shouldSpinAgain = false;
            while (!_continueClicked)
            {
                yield return new WaitUntil(() => _continueClicked || _rerollClicked);

                if (_continueClicked)
                    break;

                if (!_rerollClicked || rerollHandler == null)
                {
                    _rerollClicked = false;
                    continue;
                }

                if (!rerollHandler(chosen, out var rerolledModifier) || !rerolledModifier)
                {
                    _rerollClicked = false;
                    RefreshRerollUI(GetAvailableRerolls(getAvailableRerolls), showButton: true, rerollHandler);
                    continue;
                }

                chosen = rerolledModifier;
                _currentChosenModifier = rerolledModifier;
                ResetSlotMachineLights();
                shouldSpinAgain = true;
                break;
            }

            if (_continueClicked)
                break;

            if (!shouldSpinAgain)
                continue;

            ClearModifierReveal();
            RefreshRerollUI(GetAvailableRerolls(getAvailableRerolls), showButton: false, rerollHandler);

            sprites = BuildSpritePool(pool, chosen);
            pendingRebuildForNextSpin = true;
        }

        if (modifierInfoPanel)
            UIPanelTransition.Hide(modifierInfoPanel);

        StopModNameAnimation();

        ResetSlotMachineLights();
        UIPanelTransition.Hide(root.gameObject);
    }

    void OnDisable()
    {
        ResetSlotMachineLights();
    }

    void Update()
    {
        if (!leverArrowVisual || !leverArrowVisual.gameObject.activeSelf)
            return;

        float pulse = 1f + (leverArrowPulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * leverArrowPulseSpeed * Mathf.PI * 2f));
        leverArrowVisual.localScale = _leverArrowBaseScale * pulse;
    }

    void EnsureReferences()
    {
        if (!root)
            root = GetComponent<RectTransform>();

        if (!root)
        {
            Debug.LogWarning("LevelModifierSelectionUI: Root is missing.");
            return;
        }

        if (!modDisplayNameText || !modDisplayNameShadowText)
            Debug.LogWarning("LevelModifierSelectionUI: Mod display name text or shadow text is missing.");

        if (!leverButton || !continueButton || !rerollButton || !modifierInfoButton)
            Debug.LogWarning("LevelModifierSelectionUI: LeverButton / ContinueButton / RerollButton / ModifierInfoButton is missing.");

        if (!rerollCountText)
            Debug.LogWarning("LevelModifierSelectionUI: Reroll count text is missing.");

        if (!shadowRerollCountText)
            Debug.LogWarning("LevelModifierSelectionUI: Shadow reroll count text is missing.");

        if (!modifierInfoPanel || !modifierInfoTitleText || !modifierInfoShadowTitleText || !modifierInfoIcon || !modifierInfoDescText || !closeInfoButton)
            Debug.LogWarning("LevelModifierSelectionUI: Modifier info panel references are missing.");

        if (viewports == null || viewports.Length != 3 || contents == null || contents.Length != 3)
            Debug.LogWarning("LevelModifierSelectionUI: Assign exactly 3 viewports and 3 contents.");
    }

    void CacheSlotMachineLights()
    {
        _runtimeSlotMachineLights.Clear();

        if (slotMachineLights != null)
        {
            for (int i = 0; i < slotMachineLights.Length; i++)
            {
                if (slotMachineLights[i] && !_runtimeSlotMachineLights.Contains(slotMachineLights[i]))
                    _runtimeSlotMachineLights.Add(slotMachineLights[i]);
            }
        }

        if (autoFindSlotMachineLights && _runtimeSlotMachineLights.Count == 0)
        {
            var searchRoot = root ? root : transform;
            var foundLights = searchRoot.GetComponentsInChildren<SlotMachineLightUI>(true);
            for (int i = 0; i < foundLights.Length; i++)
            {
                if (foundLights[i] && !_runtimeSlotMachineLights.Contains(foundLights[i]))
                    _runtimeSlotMachineLights.Add(foundLights[i]);
            }
        }

        for (int i = 0; i < _runtimeSlotMachineLights.Count; i++)
            _runtimeSlotMachineLights[i].CaptureStartingSettings();
    }

    void SyncSlotMachineLightsForReveal()
    {
        for (int i = 0; i < _runtimeSlotMachineLights.Count; i++)
        {
            if (!_runtimeSlotMachineLights[i])
                continue;

            _runtimeSlotMachineLights[i].PlaySyncedFlash(
                revealLightPulseSpeedMultiplier,
                revealLightColorSpeedMultiplier,
                revealLightSyncedPulseScale,
                revealLightSyncedBrightness,
                revealLightSyncedPhaseOffset,
                revealLightSyncedColorOffset);
        }
    }

    void ResetSlotMachineLights()
    {
        for (int i = 0; i < _runtimeSlotMachineLights.Count; i++)
        {
            if (_runtimeSlotMachineLights[i])
                _runtimeSlotMachineLights[i].RestoreStartingSettings();
        }
    }

    void BuildColumns()
    {
        _columns.Clear();

        if (viewports == null || contents == null)
            return;

        int count = Mathf.Min(viewports.Length, contents.Length);
        for (int i = 0; i < count; i++)
        {
            if (!viewports[i] || !contents[i])
                continue;

            PrepareColumnContent(contents[i]);

            _columns.Add(new SlotColumn
            {
                viewport = viewports[i],
                content = contents[i]
            });
        }

        WireButtons();
    }

    void WireButtons()
    {
        if (leverButton)
        {
            leverButton.onClick.RemoveListener(OnSpinClicked);
            leverButton.onClick.AddListener(OnSpinClicked);
        }

        if (continueButton)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (rerollButton)
        {
            rerollButton.onClick.RemoveListener(OnRerollClicked);
            rerollButton.onClick.AddListener(OnRerollClicked);
        }

        if (modifierInfoButton)
        {
            modifierInfoButton.onClick.RemoveListener(OnModifierInfoClicked);
            modifierInfoButton.onClick.AddListener(OnModifierInfoClicked);
        }

        if (closeInfoButton)
        {
            closeInfoButton.onClick.RemoveListener(OnCloseInfoClicked);
            closeInfoButton.onClick.AddListener(OnCloseInfoClicked);
        }
    }

    void OnSpinClicked()
    {
        if (_isSpinning)
            return;

        if (leverArrowVisual)
            leverArrowVisual.gameObject.SetActive(false);

        _spinClicked = true;
    }

    void OnContinueClicked()
    {
        _continueClicked = true;
    }

    void OnRerollClicked()
    {
        if (_isSpinning)
            return;

        ResetSlotMachineLights();
        _rerollClicked = true;
    }

    void PopulateColumn(SlotColumn column, List<Sprite> pool, Sprite chosenSprite)
    {
        for (int i = column.content.childCount - 1; i >= 0; i--)
            Destroy(column.content.GetChild(i).gameObject);

        column.icons.Clear();

        List<Sprite> uniquePool = BuildUniqueSpritePool(pool, chosenSprite);

        int uniqueCount = Mathf.Max(1, uniquePool.Count);
        int baseIconCount = Mathf.Max(uniqueCount, 12);
        int iconCount = baseIconCount * 2;
        float startY = (iconCount - 1) * iconSpacing * 0.5f;

        int poolIndex = 0;
        ShuffleSprites(uniquePool);

        Sprite previousSprite = null;

        for (int i = 0; i < iconCount; i++)
        {
            Image icon;
            if (iconPrefab)
                icon = Instantiate(iconPrefab, column.content);
            else
                icon = new GameObject($"Icon_{i}", typeof(Image)).GetComponent<Image>();

            icon.name = $"Icon_{i}";
            icon.transform.SetParent(column.content, false);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;

            Sprite spriteToUse;
            bool isFinalChosenIcon = (i == iconCount - 1);

            if (isFinalChosenIcon)
            {
                spriteToUse = chosenSprite;
            }
            else
            {
                if (uniquePool.Count == 0)
                {
                    spriteToUse = chosenSprite;
                }
                else
                {
                    if (poolIndex >= uniquePool.Count)
                    {
                        ShuffleSprites(uniquePool);
                        poolIndex = 0;

                        if (uniquePool.Count > 1 && uniquePool[0] == previousSprite)
                        {
                            int swapIndex = UnityEngine.Random.Range(1, uniquePool.Count);
                            (uniquePool[0], uniquePool[swapIndex]) = (uniquePool[swapIndex], uniquePool[0]);
                        }
                    }

                    spriteToUse = uniquePool[poolIndex];
                    poolIndex++;
                }
            }

            icon.sprite = spriteToUse;
            previousSprite = spriteToUse;

            var rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = iconSize;
            rt.anchoredPosition = new Vector2(0f, startY - i * iconSpacing);

            column.icons.Add(icon);
        }

        column.content.sizeDelta = new Vector2(iconSize.x, iconCount * iconSpacing);
        column.cycleHeight = baseIconCount * iconSpacing;
        column.targetY = startY;
    }

    List<Sprite> BuildSpritePool(IReadOnlyList<LevelModifierSO> pool, LevelModifierSO chosen)
    {
        var sprites = new List<Sprite>();
        if (pool != null)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] && pool[i].icon)
                    sprites.Add(pool[i].icon);
            }
        }

        if (sprites.Count == 0 && chosen.icon)
            sprites.Add(chosen.icon);

        if (sprites.Count == 0)
            sprites.Add(CreateFallbackSprite());

        return sprites;
    }

    static Sprite CreateFallbackSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, new Color(0.96f, 0.79f, 0.35f, 1f));
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    List<Sprite> BuildUniqueSpritePool(IReadOnlyList<Sprite> source, Sprite chosenSprite)
    {
        var unique = new List<Sprite>();
        var seen = new HashSet<Sprite>();

        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Sprite sprite = source[i];
                if (!sprite || !seen.Add(sprite))
                    continue;

                unique.Add(sprite);
            }
        }

        if (unique.Count == 0 && chosenSprite)
            unique.Add(chosenSprite);

        return unique;
    }

    void ShuffleSprites(List<Sprite> sprites)
    {
        if (sprites == null || sprites.Count <= 1)
            return;

        for (int i = sprites.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (sprites[i], sprites[j]) = (sprites[j], sprites[i]);
        }
    }

    void OnModifierInfoClicked()
    {
        if (modifierInfoPanel)
            UIPanelTransition.Show(modifierInfoPanel);
    }

    void OnCloseInfoClicked()
    {
        if (modifierInfoPanel)
            UIPanelTransition.Hide(modifierInfoPanel);
    }

    void PopulateModifierInfoPanel(LevelModifierSO modifier)
    {
        if (!modifier)
            return;

        if (modifierInfoTitleText)
            modifierInfoTitleText.text = modifier.displayName;

        if (modifierInfoShadowTitleText)
            modifierInfoShadowTitleText.text = modifier.displayName;

        if (modifierInfoIcon)
            modifierInfoIcon.sprite = modifier.icon;

        if (modifierInfoDescText)
            modifierInfoDescText.text = modifier.description;
    }

    void RevealModifier(LevelModifierSO chosen)
    {
        if (!chosen)
            return;

        if (modDisplayNameText)
            modDisplayNameText.text = chosen.displayName;

        if (modDisplayNameShadowText)
            modDisplayNameShadowText.text = chosen.displayName;

        PlayModNameAnimation();
        PopulateModifierInfoPanel(chosen);
        AudioManager.I?.PlaySlotReveal();

        if (continueButton)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }

        if (modifierInfoButton)
        {
            modifierInfoButton.gameObject.SetActive(true);
            modifierInfoButton.interactable = true;
        }
    }

    void ClearModifierReveal()
    {
        StopModNameAnimation();

        if (modDisplayNameText)
            modDisplayNameText.text = string.Empty;

        if (modDisplayNameShadowText)
            modDisplayNameShadowText.text = string.Empty;

        if (continueButton)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.interactable = false;
        }

        if (rerollButton)
        {
            rerollButton.gameObject.SetActive(false);
            rerollButton.interactable = false;
        }

        if (modifierInfoButton)
        {
            modifierInfoButton.gameObject.SetActive(false);
            modifierInfoButton.interactable = false;
        }

        if (modifierInfoPanel)
            UIPanelTransition.Hide(modifierInfoPanel, true);
    }

    void RefreshRerollUI(int availableRerolls, bool showButton, RerollHandler rerollHandler)
    {
        int clampedRerolls = Mathf.Max(0, availableRerolls);

        if (rerollCountText)
            rerollCountText.text = $"Rerolls: {clampedRerolls}";

        if (shadowRerollCountText)
            shadowRerollCountText.text = $"Rerolls: {clampedRerolls}";

        if (!rerollButton)
            return;

        rerollButton.gameObject.SetActive(showButton);
        rerollButton.interactable = showButton && rerollHandler != null && clampedRerolls > 0;
    }

    static int GetAvailableRerolls(Func<int> getAvailableRerolls)
    {
        return getAvailableRerolls != null ? Mathf.Max(0, getAvailableRerolls()) : 0;
    }

    IEnumerator PlayLeverPullRoutine()
    {
        AudioManager.I?.PlaySlotLever();

        if (!leverVisual)
            yield break;

        Quaternion startRot = _leverDefaultRotation;
        Quaternion pressedRot = _leverDefaultRotation * Quaternion.Euler(0f, 0f, -Mathf.Abs(leverPressAngle));

        float t = 0f;
        while (t < leverPressDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / Mathf.Max(0.01f, leverPressDuration));
            leverVisual.localRotation = Quaternion.Slerp(startRot, pressedRot, lerp);
            yield return null;
        }

        t = 0f;
        while (t < leverReturnDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / Mathf.Max(0.01f, leverReturnDuration));
            leverVisual.localRotation = Quaternion.Slerp(pressedRot, startRot, lerp);
            yield return null;
        }

        leverVisual.localRotation = startRot;
    }

    IEnumerator SpinColumnsRoutine(
    bool rebuildColumnsAfterSpinStarts = false,
    List<Sprite> rebuildPool = null,
    Sprite rebuildChosenSprite = null)
    {
        _isSpinning = true;

        if (AudioManager.I)
            AudioManager.I.PlaySlotSpinLoop();

        float speedMult = Mathf.Max(0.01f, spinSpeedMultiplier);
        float freeSpinDuration = Mathf.Max(0.05f, spinDuration / speedMult);
        float settleTime = Mathf.Max(0.05f, settleDuration);
        float cyclesPerSecond = Mathf.Max(0.1f, spinCyclesPerSecond * speedMult);
        float unitsPerSecond = iconSpacing * cyclesPerSecond;
        float totalDuration = freeSpinDuration + stopDelayPerColumn * (_columns.Count - 1) + settleTime;

        float elapsed = 0f;
        bool[] stopSfxPlayed = new bool[_columns.Count];
        bool[] settleInitialized = new bool[_columns.Count];
        float[] settleStartOffsets = new float[_columns.Count];
        float[] settleEndOffsets = new float[_columns.Count];
        bool rebuiltColumns = !rebuildColumnsAfterSpinStarts;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < _columns.Count; i++)
            {
                SlotColumn column = _columns[i];
                float settleStartTime = freeSpinDuration + stopDelayPerColumn * i;

                float y;
                if (elapsed < settleStartTime)
                {
                    float direction = spinInReverse ? -1f : 1f;
                    float rawY = elapsed * unitsPerSecond * direction;
                    y = WrapOffset(rawY, column.cycleHeight);
                }
                else
                {
                    if (!settleInitialized[i])
                    {
                        settleInitialized[i] = true;

                        float wrappedStart = WrapOffset(settleStartTime * unitsPerSecond, column.cycleHeight);
                        settleStartOffsets[i] = wrappedStart;

                        float wrappedTarget = WrapOffset(column.targetY, column.cycleHeight);
                        if (!spinInReverse)
                        {
                            if (wrappedTarget < wrappedStart)
                                wrappedTarget += column.cycleHeight;
                        }
                        else
                        {
                            if (wrappedTarget > wrappedStart)
                                wrappedTarget -= column.cycleHeight;
                        }

                        settleEndOffsets[i] = wrappedTarget;
                    }

                    float settleElapsed = elapsed - settleStartTime;
                    float t = Mathf.Clamp01(settleElapsed / settleTime);
                    float eased = 1f - Mathf.Pow(1f - t, Mathf.Max(2f, stopEaseExponent));

                    float unwrappedY = Mathf.Lerp(settleStartOffsets[i], settleEndOffsets[i], eased);
                    y = WrapOffset(unwrappedY, column.cycleHeight);

                    bool finishesThisFrame = !stopSfxPlayed[i] && settleElapsed + Time.unscaledDeltaTime >= settleTime;
                    if (finishesThisFrame)
                    {
                        stopSfxPlayed[i] = true;
                        y = WrapOffset(column.targetY, column.cycleHeight);
                        AudioManager.I?.PlaySlotStop();
                    }
                }

                column.content.anchoredPosition = new Vector2(0f, y);
            }

            if (!rebuiltColumns && elapsed >= Mathf.Max(0f, rerollRebuildDelaySeconds))
            {
                for (int i = 0; i < _columns.Count; i++)
                    PopulateColumn(_columns[i], rebuildPool, rebuildChosenSprite);

                rebuiltColumns = true;
            }

            yield return null;
        }

        for (int i = 0; i < _columns.Count; i++)
            _columns[i].content.anchoredPosition = new Vector2(0f, WrapOffset(_columns[i].targetY, _columns[i].cycleHeight));

        if (AudioManager.I)
            AudioManager.I.StopSlotSpinLoop();

        _isSpinning = false;
    }

    void PlayModNameAnimation()
    {
        StopModNameAnimation();

        if (!modNameStyle || !modDisplayNameText || !modDisplayNameShadowText)
            return;

        _modNameAnimCR = StartCoroutine(AnimateModNameRoutine());
    }

    void StopModNameAnimation()
    {
        if (_modNameAnimCR != null)
        {
            StopCoroutine(_modNameAnimCR);
            _modNameAnimCR = null;
        }

        if (modDisplayNameText)
        {
            modDisplayNameText.rectTransform.localScale = _modNameBaseScale;
            modDisplayNameText.color = _modNameBaseColor;
        }

        if (modDisplayNameShadowText)
        {
            modDisplayNameShadowText.rectTransform.localScale = _modNameShadowBaseScale;
            modDisplayNameShadowText.color = _modNameShadowBaseColor;
        }
    }

    IEnumerator AnimateModNameRoutine()
    {
        var colors = modNameStyle.GetColors(modNameRarity);

        float duration = Mathf.Max(0.05f, modNameStyle.duration);
        float flashInterval = Mathf.Max(0.03f, modNameStyle.flashInterval);
        float pulseAmp = Mathf.Max(0f, modNameStyle.pulseScaleAmplitude);
        float pulseHz = Mathf.Max(0f, modNameStyle.pulseSpeedHz);

        float elapsed = 0f;
        float flashTimer = 0f;
        bool flip = false;

        while (elapsed < duration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            flashTimer += dt;

            if (flashTimer >= flashInterval)
            {
                flashTimer = 0f;
                flip = !flip;
            }

            Color textColor = flip ? colors.textB : colors.textA;
            Color shadowColor = flip ? colors.shadowB : colors.shadowA;

            if (modDisplayNameText)
                modDisplayNameText.color = textColor;

            if (modDisplayNameShadowText)
                modDisplayNameShadowText.color = shadowColor;

            float pulse = 1f;
            if (pulseAmp > 0f && pulseHz > 0f)
                pulse += Mathf.Sin(elapsed * pulseHz * Mathf.PI * 2f) * pulseAmp;

            if (modDisplayNameText)
                modDisplayNameText.rectTransform.localScale = _modNameBaseScale * pulse;

            if (modDisplayNameShadowText)
                modDisplayNameShadowText.rectTransform.localScale = _modNameShadowBaseScale * pulse;

            yield return null;
        }

        if (modDisplayNameText)
        {
            modDisplayNameText.color = colors.textA;
            modDisplayNameText.rectTransform.localScale = _modNameBaseScale;
        }

        if (modDisplayNameShadowText)
        {
            modDisplayNameShadowText.color = colors.shadowA;
            modDisplayNameShadowText.rectTransform.localScale = _modNameShadowBaseScale;
        }

        _modNameAnimCR = null;
    }

    void PrepareColumnContent(RectTransform content)
    {
        if (!content)
            return;

        var layoutGroup = content.GetComponent<LayoutGroup>();
        if (layoutGroup)
            layoutGroup.enabled = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter)
            fitter.enabled = false;

        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;
        content.localRotation = Quaternion.identity;
    }

    float WrapOffset(float value, float cycleHeight)
    {
        if (cycleHeight <= 0f)
            return value;

        value %= cycleHeight;
        if (value < 0f)
            value += cycleHeight;
        return value;
    }
}
