using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Roster")]
    public PlayerCharacterData[] roster;

    [Header("UI")]
    public Transform listParent;
    public Button characterButtonPrefab; // Text + Image
    public TMP_Text selectedName;        // Preview
    public Image selectedPortrait;       // Preview
    public Image selectedBorder;
    public TMP_Text selectedSpecialAbilityName;
    public TMP_Text selectedSpecialDescription;
    public CurrencyUI currencyUI;
    [SerializeField] ScrollRect listScrollRect;
    [SerializeField] Button frameSwapButton;
    [SerializeField] TMP_Text frameSwapButtonText;

    [Header("Audio")]
    public AudioClip selectSFX;
    public AudioClip hoverSFX;
    public AudioClip unlockSFX;
    public AudioClip errorSFX;

    [Header("Selection Visuals")]
    [Range(0f, 1f)] public float selectedAlpha = 1f;
    [Range(0f, 1f)] public float deselectedAlpha = 0.65f;
    [SerializeField] Color selectedFireBorderTint = new Color(0.55f, 0.9f, 1f, 1f);

    PlayerCharacterData previewCharacter;
    readonly System.Collections.Generic.Dictionary<PlayerCharacterData, Button> buttons = new();
    readonly System.Collections.Generic.Dictionary<PlayerCharacterData, UIButtonTargetVisual> buttonTargetVisuals = new();
    Coroutine listLayoutRebuildRoutine;

    void Awake()
    {
        ConfigureScrollInput();
        ResolveFrameSwapButton();
        WireFrameSwapButton();
        BuildList();

        var saved = SelectedCharacterStore.ResolveFromRoster(roster);
        if (saved != null && UnlockStore.IsUnlocked(saved))
            SelectedCharacterStore.Current = saved;

        if (SelectedCharacterStore.Current == null)
        {
            var fallback = GetFirstUnlockedCharacter();
            if (fallback != null)
            {
                SetCurrent(fallback);
            }
            else if (roster != null && roster.Length > 0)
            {
                SetCurrent(roster[0]);
            }
        }

        // Default preview is the current stored character
        previewCharacter = SelectedCharacterStore.Current;
        RefreshPreview();
        RefreshButtonAlphas();
        ScheduleListLayoutRebuild();
    }

    void OnEnable()
    {
        ConfigureScrollInput();
        ResolveFrameSwapButton();
        WireFrameSwapButton();
        RefreshPreview();
        RefreshButtonAlphas();
        ScheduleListLayoutRebuild();
    }

    void BuildList()
    {
        if (!listParent || !characterButtonPrefab)
            return;

        for (int i = listParent.childCount - 1; i >= 0; i--)
            Destroy(listParent.GetChild(i).gameObject);

        buttons.Clear();
        buttonTargetVisuals.Clear();

        if (roster == null)
            return;

        foreach (var data in roster)
        {
            var btn = Instantiate(characterButtonPrefab, listParent);
            var targetVisual = UIButtonTargetVisual.Ensure(btn.gameObject);

            buttons[data] = btn;
            var txt = btn.GetComponentInChildren<TMP_Text>();
            var portraitT = FindDeep(btn.transform, "CharacterPortrait_Image");
            var img = portraitT ? portraitT.GetComponent<Image>() : btn.GetComponentInChildren<Image>();
            var borderT = FindDeep(btn.transform, "Border_Image");
            var borderImg = borderT ? borderT.GetComponent<Image>() : null;
            var pulseTargets = GetCommanderButtonPulseTargets(portraitT, borderT);

            if (targetVisual)
            {
                targetVisual.Configure(hoverSFX, false, btn.transform);
                targetVisual.SetPulseTargets();
                targetVisual.SetTargetPulseEnabled(false);
                buttonTargetVisuals[data] = targetVisual;
            }

            ConfigureCommanderArtworkPulse(borderImg, borderT, pulseTargets);

            if (txt) txt.text = TetrabeastsLocalization.LocalizeText(data.displayName);
            if (img && data.portrait) img.sprite = data.portrait;
            if (borderImg) borderImg.sprite = CommanderBorderFrameStore.GetStaticBorderSprite(data);

            bool unlocked = UnlockStore.IsUnlocked(data);

            var lockedImgT = FindDeep(btn.transform, "Locked_Image");
            if (lockedImgT) lockedImgT.gameObject.SetActive(!unlocked);

            var unlockBtnT = FindDeep(btn.transform, "Unlock_Button");
            var unlockBtn = unlockBtnT ? unlockBtnT.GetComponent<Button>() : null;
            if (unlockBtnT) unlockBtnT.gameObject.SetActive(!unlocked);
            if (unlockBtn)
            {
                var unlockVisual = UIButtonTargetVisual.Ensure(unlockBtn.gameObject);
                if (unlockVisual)
                {
                    unlockVisual.Configure(null, false, btn.transform);
                    unlockVisual.SetTargetPulseEnabled(true);
                }
            }

            // Always preview on click
            btn.interactable = true;
            btn.onClick.AddListener(() =>
            {
                previewCharacter = data;
                RefreshPreview(); // Always show preview

                if (!UnlockStore.IsUnlocked(data))
                {
                    if (AudioManager.I && errorSFX) AudioManager.I.PlaySFX(errorSFX);
                    return;
                }

                SetCurrent(data);
                RefreshButtonAlphas();
            });

            // Cost text
            var costTextT = unlockBtnT ? FindDeep(unlockBtnT, "Cur_Text (TMP)") : null;
            var costText = costTextT ? costTextT.GetComponent<TMPro.TMP_Text>() : null;
            if (costText) costText.text = $"x{data.unlockCost}";

            if (unlockBtn)
            {
                unlockBtn.onClick.RemoveAllListeners();
                unlockBtn.onClick.AddListener(() =>
                {
                    if (DemoBuildGuardRails.TryBlockPurchase(errorSFX))
                        return;

                    if (CurrencyStore.Total < data.unlockCost)
                    {
                        if (AudioManager.I && errorSFX)
                            AudioManager.I.PlaySFX(errorSFX);
                        return;
                    }

                    CurrencyStore.Add(-data.unlockCost);
                    currencyUI?.Refresh();
                    bool wasLocked = !UnlockStore.IsUnlocked(data);
                    UnlockStore.Unlock(data);

                    // Only count the unlock if it was previously locked
                    if (wasLocked && PlayerProgress.I)
                    {
                        PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.CharactersUnlocked, 1);
                        PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunUnlockedAnyCharacter, 1);
                    }

                    if (AudioManager.I && unlockSFX)
                        AudioManager.I.PlaySFX(unlockSFX);

                    if (lockedImgT) lockedImgT.gameObject.SetActive(false);
                    unlockBtnT.gameObject.SetActive(false);
                    btn.interactable = true;

                    // After purchase, auto-equip the character
                    previewCharacter = data;
                    SetCurrent(data);
                });
            }

        }

        RebuildListLayout();
        ScheduleListLayoutRebuild();
    }

    static void ConfigureCommanderArtworkPulse(Image borderImage, Transform border, Transform[] pulseTargets)
    {
        if (!border)
            return;

        if (borderImage)
            borderImage.raycastTarget = true;

        var artworkVisual = UIButtonTargetVisual.Ensure(border.gameObject);
        if (!artworkVisual)
            return;

        artworkVisual.Configure(null, false, border);
        artworkVisual.SetPulseTargets(pulseTargets);
    }

    static Transform[] GetCommanderButtonPulseTargets(Transform portrait, Transform border)
    {
        if (portrait && border)
            return new[] { portrait, border };

        if (portrait)
            return new[] { portrait };

        if (border)
            return new[] { border };

        return null;
    }

    void ConfigureScrollInput()
    {
        var scroll = ResolveListScrollRect();
        if (!scroll)
            return;

        if (listParent is RectTransform content)
            scroll.content = content;

        scroll.horizontal = false;
        scroll.vertical = true;
        MenuScrollRectInput.Attach(scroll, gameObject, autoCenterSelected: true);
    }

    ScrollRect ResolveListScrollRect()
    {
        if (listScrollRect)
            return listScrollRect;

        if (listParent)
            listScrollRect = listParent.GetComponentInParent<ScrollRect>(true);

        return listScrollRect;
    }

    void ScheduleListLayoutRebuild()
    {
        if (!isActiveAndEnabled)
            return;

        if (listLayoutRebuildRoutine != null)
            StopCoroutine(listLayoutRebuildRoutine);

        listLayoutRebuildRoutine = StartCoroutine(RebuildListLayoutNextFrame());
    }

    IEnumerator RebuildListLayoutNextFrame()
    {
        yield return null;
        RebuildListLayout();
        listLayoutRebuildRoutine = null;
    }

    void RebuildListLayout()
    {
        if (listParent is not RectTransform content)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        ResizeGridContentToChildren(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        var scroll = ResolveListScrollRect();
        if (!scroll)
            return;

        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        MenuScrollRectInput.Attach(scroll, gameObject, autoCenterSelected: true);

        if (scroll.transform is RectTransform scrollRectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);

        scroll.verticalNormalizedPosition = 1f;
        scroll.velocity = Vector2.zero;
        Canvas.ForceUpdateCanvases();
    }

    void ResizeGridContentToChildren(RectTransform content)
    {
        var grid = content.GetComponent<GridLayoutGroup>();
        if (!grid)
            return;

        int childCount = GetActiveChildCount(content);
        int columns = GetGridColumnCount(grid, content, childCount);
        int rows = childCount > 0 ? Mathf.CeilToInt(childCount / (float)columns) : 0;

        float contentHeight = grid.padding.top + grid.padding.bottom;
        if (rows > 0)
            contentHeight += (rows * grid.cellSize.y) + ((rows - 1) * grid.spacing.y);

        if (TryGetContentVisualBounds(content, out var bounds))
            contentHeight = Mathf.Max(contentHeight, -bounds.min.y + grid.padding.bottom);

        var scroll = ResolveListScrollRect();
        float viewportHeight = 0f;
        if (scroll && scroll.viewport)
            viewportHeight = scroll.viewport.rect.height;
        else if (content.parent is RectTransform parent)
            viewportHeight = parent.rect.height;

        contentHeight = Mathf.Max(contentHeight, viewportHeight);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
    }

    static bool TryGetContentVisualBounds(RectTransform content, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        var corners = new Vector3[4];
        var children = content.GetComponentsInChildren<RectTransform>(false);

        foreach (var child in children)
        {
            if (!child || child == content)
                continue;

            child.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 localCorner = content.InverseTransformPoint(corners[i]);
                if (hasBounds)
                    bounds.Encapsulate(localCorner);
                else
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
            }
        }

        return hasBounds;
    }

    static int GetActiveChildCount(RectTransform content)
    {
        int count = 0;
        for (int i = 0; i < content.childCount; i++)
        {
            if (content.GetChild(i).gameObject.activeSelf)
                count++;
        }

        return count;
    }

    static int GetGridColumnCount(GridLayoutGroup grid, RectTransform content, int childCount)
    {
        if (childCount <= 0)
            return 1;

        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, grid.constraintCount);

        if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            int rows = Mathf.Max(1, grid.constraintCount);
            return Mathf.Max(1, Mathf.CeilToInt(childCount / (float)rows));
        }

        float width = content.rect.width;
        if (width <= 0f && content.parent is RectTransform parent)
            width = parent.rect.width;

        float stride = grid.cellSize.x + grid.spacing.x;
        if (width <= 0f || stride <= 0f)
            return Mathf.Max(1, grid.constraintCount);

        float availableWidth = Mathf.Max(0f, width - grid.padding.left - grid.padding.right);
        int columns = Mathf.FloorToInt((availableWidth + grid.spacing.x) / stride);
        return Mathf.Clamp(columns, 1, childCount);
    }

    void RefreshButtonAlphas()
    {
        foreach (var kv in buttons)
        {
            var data = kv.Key;
            var button = kv.Value;
            if (!button)
                continue;

            bool unlocked = UnlockStore.IsUnlocked(data);
            bool isSelected = SelectedCharacterStore.Current == data;
            float alpha = !unlocked || isSelected ? selectedAlpha : deselectedAlpha;

            var portrait = button.GetComponentInChildren<Image>();
            var borderT = FindDeep(button.transform, "Border_Image");
            var border = borderT ? borderT.GetComponent<Image>() : null;

            if (portrait)
            {
                Color c = portrait.color;
                c.a = alpha;
                portrait.color = c;
            }

            if (border)
            {
                Color c = border.color;
                c.a = alpha;
                border.color = c;
            }

            if (buttonTargetVisuals.TryGetValue(data, out var targetVisual) && targetVisual)
                targetVisual.SetPersistentFireBorder(unlocked && isSelected, selectedFireBorderTint);
        }
    }

    void SetCurrent(PlayerCharacterData data)
    {
        if (!UnlockStore.IsUnlocked(data))
        {
            if (AudioManager.I && errorSFX)
                AudioManager.I.PlaySFX(errorSFX);
            return;
        }

        SelectedCharacterStore.Save(data);
        SelectedCharacterStore.Current = data;

        if (AudioManager.I && selectSFX)
            AudioManager.I.PlaySFX(selectSFX);

        RefreshPreview();
        RefreshButtonAlphas();
        RefreshTitleMenuLoadoutUI();
    }

    PlayerCharacterData GetFirstUnlockedCharacter()
    {
        if (roster == null) return null;
        foreach (var c in roster)
        {
            if (c != null && UnlockStore.IsUnlocked(c))
                return c;
        }
        return null;
    }

    public void RefreshPreview()
    {
        // If nothing has been previewed yet, fall back to current selection
        var cur = previewCharacter ? previewCharacter : SelectedCharacterStore.Current;
        if (!cur) return;

        if (selectedName) selectedName.text = TetrabeastsLocalization.LocalizeText(cur.displayName);
        if (selectedPortrait && cur.portrait) selectedPortrait.sprite = cur.portrait;
        if (selectedBorder)
            CommanderBorderFrameStore.ApplyToImage(selectedBorder, cur);

        if (selectedSpecialAbilityName)
        {
            selectedSpecialAbilityName.text = TetrabeastsLocalization.LocalizeFormat(
                "Special Ability: {0}",
                TetrabeastsLocalization.LocalizeText(cur.specialAbilityName));
        }

        if (selectedSpecialDescription)
            selectedSpecialDescription.text = TetrabeastsLocalization.LocalizeText(cur.specialDescription);

        RefreshFrameSwapButton();
    }

    void ResolveFrameSwapButton()
    {
        if (!frameSwapButton)
        {
            var buttonTransform = FindDeep(transform, "FrameSwap_Button");
            if (buttonTransform)
                frameSwapButton = buttonTransform.GetComponent<Button>();
        }

        if (!frameSwapButtonText && frameSwapButton)
            frameSwapButtonText = frameSwapButton.GetComponentInChildren<TMP_Text>(true);
    }

    void WireFrameSwapButton()
    {
        if (!frameSwapButton)
            return;

        frameSwapButton.onClick.RemoveAllListeners();
        frameSwapButton.onClick.AddListener(OnFrameSwapClicked);
        UIButtonTargetVisual.Ensure(frameSwapButton.gameObject)?.Configure(hoverSFX, false, frameSwapButton.transform);
        RefreshFrameSwapButton();
    }

    void RefreshFrameSwapButton()
    {
        if (!frameSwapButton)
            return;

        var cur = previewCharacter ? previewCharacter : SelectedCharacterStore.Current;
        if (cur)
            CommanderBorderFrameStore.EnsureUnlockedFromExistingProgress(cur);

        bool available = cur &&
                         UnlockStore.IsUnlocked(cur) &&
                         CommanderBorderFrameStore.HasAnimatedBorder(cur) &&
                         CommanderBorderFrameStore.IsUnlocked(cur);

        frameSwapButton.interactable = available;

        if (frameSwapButtonText)
        {
            if (!cur || !CommanderBorderFrameStore.HasAnimatedBorder(cur))
                frameSwapButtonText.text = "Frame";
            else if (!CommanderBorderFrameStore.IsUnlocked(cur))
                frameSwapButtonText.text = "Locked";
            else
                frameSwapButtonText.text = CommanderBorderFrameStore.IsActive(cur) ? "Frame On" : "Frame Off";
        }
    }

    void OnFrameSwapClicked()
    {
        var cur = previewCharacter ? previewCharacter : SelectedCharacterStore.Current;
        bool available = cur &&
                         UnlockStore.IsUnlocked(cur) &&
                         CommanderBorderFrameStore.HasAnimatedBorder(cur) &&
                         CommanderBorderFrameStore.IsUnlocked(cur);

        if (!available)
        {
            if (AudioManager.I && errorSFX)
                AudioManager.I.PlaySFX(errorSFX);
            RefreshFrameSwapButton();
            return;
        }

        CommanderBorderFrameStore.ToggleActive(cur);
        RefreshPreview();
        RefreshButtonAlphas();
        RefreshTitleMenuLoadoutUI();
    }

    void RefreshTitleMenuLoadoutUI()
    {
        var titleMenu = FindFirstObjectByType<TitleMenuUI>(FindObjectsInactive.Include);
        if (titleMenu)
            titleMenu.RefreshSelectedLoadoutUI();
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (!root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
