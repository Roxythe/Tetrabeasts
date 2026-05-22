using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TitleStarDifficultyUI : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] Button decreaseButton;
    [SerializeField] Button increaseButton;
    [SerializeField] TMP_Text selectionText;
    [SerializeField] TMP_Text shadowSelectionText;
    [SerializeField] TMP_Text unlockText;

    [Header("Stars")]
    [SerializeField] Image[] starImages = new Image[StarDifficultySystem.MaxStars];
    [SerializeField] Sprite filledStarSprite;
    [SerializeField] Sprite emptyStarSprite;
    [SerializeField] Color filledStarColor = Color.white;
    [SerializeField] Color emptyUnlockedStarColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] Color lockedStarColor = new Color(0.50f, 0.50f, 0.50f, 0.80f);

    [System.Serializable]
    struct DifficultyAnimationEntry
    {
        public GameObject prefab;
    }

    [Header("Difficulty Animation Prefabs")]
    [SerializeField] RectTransform leftDifficultyAnimationHolder;
    [SerializeField] RectTransform rightDifficultyAnimationHolder; // Currently not being used, code commented out
    [SerializeField] DifficultyAnimationEntry[] difficultyAnimationPrefabs = new DifficultyAnimationEntry[StarDifficultySystem.MaxStars + 1];

    GameObject _leftDifficultyAnimationInstance;
    GameObject _rightDifficultyAnimationInstance;
    int _lastAppliedDifficultyAnimation = -1;

    [Header("Tooltip")]
    [SerializeField] RectTransform tooltipRoot;
    [SerializeField] TMP_Text tooltipText;
    [SerializeField] RectTransform starDifficultyPanelRect;
    [SerializeField] float tooltipPanelTopGap = 18f;
    [SerializeField] float tooltipScreenEdgePadding = 8f;
    [SerializeField] Vector2 tooltipMinSize = new Vector2(220f, 80f);
    [SerializeField] Vector2 tooltipMaxSize = new Vector2(420f, 0f);
    [SerializeField] Vector2 tooltipTextInsetMin = new Vector2(12f, 10f);
    [SerializeField] Vector2 tooltipTextInsetMax = new Vector2(12f, 10f);
    [SerializeField] bool hideTooltipOnDisable = true;

    int _currentTooltipStars = -1;
    RectTransform _currentTooltipTarget;

    public void Initialize()
    {
        EnsurePlayerProgressExists();
        if (!starDifficultyPanelRect)
            starDifficultyPanelRect = transform as RectTransform;

        HookButtons();
        HookStarHoverTargets();
        RefreshNow();
        RefreshDifficultyAnimations(force: true);
    }

    public void RefreshNow()
    {
        if (PlayerProgress.I == null)
            return;

        int selectedStars = PlayerProgress.I.GetSelectedStarDifficulty();
        int maxUnlockedStars = PlayerProgress.I.GetMaxUnlockedStarDifficulty();

        if (decreaseButton)
            decreaseButton.interactable = selectedStars > 0;

        if (increaseButton)
            increaseButton.interactable = selectedStars < maxUnlockedStars;

        for (int i = 0; i < starImages.Length; i++)
        {
            Image star = starImages[i];
            if (!star)
                continue;

            int starCount = i + 1;
            bool isFilled = starCount <= selectedStars;
            bool isUnlocked = starCount <= maxUnlockedStars;

            if (filledStarSprite || emptyStarSprite)
                star.sprite = isFilled ? filledStarSprite : emptyStarSprite;

            star.color = isFilled
                ? filledStarColor
                : (isUnlocked ? emptyUnlockedStarColor : lockedStarColor);
        }

        string selectionLabel = TetrabeastsLocalization.LocalizeText(StarDifficultySystem.GetSelectionLabel(selectedStars));

        if (selectionText)
            selectionText.text = selectionLabel;

        if (shadowSelectionText)
            shadowSelectionText.text = selectionLabel;

        if (unlockText)
        {
            unlockText.text = maxUnlockedStars >= StarDifficultySystem.MaxStars
                ? TetrabeastsLocalization.LocalizeText("All star difficulties unlocked.")
                : TetrabeastsLocalization.LocalizeText(StarDifficultySystem.GetUnlockInstruction(maxUnlockedStars + 1));
        }

        RefreshDifficultyAnimations();

        if (_currentTooltipTarget && tooltipRoot && tooltipRoot.gameObject.activeSelf)
            ShowTooltip(_currentTooltipStars, _currentTooltipTarget);
        else
            HideTooltip();
    }

    void OnEnable()
    {
        TetrabeastsLocalization.LanguageChanged += RefreshNow;
        RefreshNow();
    }

    void OnDisable()
    {
        TetrabeastsLocalization.LanguageChanged -= RefreshNow;

        if (hideTooltipOnDisable)
            HideTooltip();

        if (_leftDifficultyAnimationInstance)
            Destroy(_leftDifficultyAnimationInstance);

        if (_rightDifficultyAnimationInstance)
            Destroy(_rightDifficultyAnimationInstance);

        _leftDifficultyAnimationInstance = null;
        _rightDifficultyAnimationInstance = null;
        _lastAppliedDifficultyAnimation = -1;
    }

    void OnValidate()
    {
        if (starImages != null && starImages.Length != StarDifficultySystem.MaxStars)
            System.Array.Resize(ref starImages, StarDifficultySystem.MaxStars);
    }

    void EnsurePlayerProgressExists()
    {
        if (PlayerProgress.I != null)
            return;

        GameObject go = new GameObject("PlayerProgress");
        go.AddComponent<PlayerProgress>();
    }

    void HookButtons()
    {
        if (decreaseButton)
        {
            decreaseButton.onClick.RemoveListener(OnDecreaseClicked);
            decreaseButton.onClick.AddListener(OnDecreaseClicked);
        }

        if (increaseButton)
        {
            increaseButton.onClick.RemoveListener(OnIncreaseClicked);
            increaseButton.onClick.AddListener(OnIncreaseClicked);
        }
    }

    void HookStarHoverTargets()
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            Image star = starImages[i];
            if (!star)
                continue;

            var hover = star.GetComponent<TitleStarDifficultyHoverTarget>();
            if (!hover)
                hover = star.gameObject.AddComponent<TitleStarDifficultyHoverTarget>();

            hover.Initialize(this, i + 1);
        }
    }

    void OnDecreaseClicked()
    {
        ChangeDifficulty(-1);
    }

    void OnIncreaseClicked()
    {
        ChangeDifficulty(1);
    }

    void ChangeDifficulty(int delta)
    {
        if (PlayerProgress.I == null)
            return;

        int current = PlayerProgress.I.GetSelectedStarDifficulty();
        int maxUnlocked = PlayerProgress.I.GetMaxUnlockedStarDifficulty();
        int next = Mathf.Clamp(current + delta, 0, maxUnlocked);

        if (next == current)
            return;

        PlayerProgress.I.SetSelectedStarDifficulty(next);
        RefreshNow();
    }

    public void ShowTooltip(int stars, RectTransform targetRect)
    {
        if (!tooltipRoot || !tooltipText || !targetRect || PlayerProgress.I == null)
            return;

        stars = StarDifficultySystem.ClampStars(stars);
        _currentTooltipStars = stars;
        _currentTooltipTarget = targetRect;

        RectTransform panelRect = GetTooltipAnchorPanel();
        RectTransform parentRect = tooltipRoot.parent as RectTransform;
        if (!panelRect || !parentRect)
            return;

        string text = TetrabeastsLocalization.LocalizeText(StarDifficultySystem.BuildTooltipText(
            stars,
            PlayerProgress.I.GetMaxUnlockedStarDifficulty()
        ));

        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.raycastTarget = false;
        tooltipText.text = text;

        var tooltipImage = tooltipRoot.GetComponent<Image>();
        if (tooltipImage)
            tooltipImage.raycastTarget = false;

        var layoutElement = tooltipRoot.GetComponent<LayoutElement>();
        if (!layoutElement)
            layoutElement = tooltipRoot.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        tooltipRoot.anchorMin = new Vector2(0.5f, 0f);
        tooltipRoot.anchorMax = new Vector2(0.5f, 0f);
        tooltipRoot.pivot = new Vector2(0.5f, 0f);

        RectTransform textRect = tooltipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(tooltipTextInsetMin.x, tooltipTextInsetMin.y);
        textRect.offsetMax = new Vector2(-tooltipTextInsetMax.x, -tooltipTextInsetMax.y);

        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();

        float horizontalInsets = tooltipTextInsetMin.x + tooltipTextInsetMax.x;
        float verticalInsets = tooltipTextInsetMin.y + tooltipTextInsetMax.y;

        float maxWidth = tooltipMaxSize.x > 0f
            ? tooltipMaxSize.x
            : parentRect.rect.width - (tooltipScreenEdgePadding * 2f);

        maxWidth = Mathf.Max(tooltipMinSize.x, maxWidth);

        Vector2 preferred = tooltipText.GetPreferredValues(
            text,
            float.PositiveInfinity,
            float.PositiveInfinity
        );

        float width = Mathf.Ceil(Mathf.Clamp(
            preferred.x + horizontalInsets,
            tooltipMinSize.x,
            maxWidth
        ));

        float wrappedTextWidth = Mathf.Max(1f, width - horizontalInsets);
        tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipMinSize.y);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        tooltipText.ForceMeshUpdate(true, true);

        preferred = tooltipText.GetPreferredValues(text, wrappedTextWidth, float.PositiveInfinity);
        float textHeight = Mathf.Max(preferred.y, tooltipText.preferredHeight, tooltipText.renderedHeight);

        float height = Mathf.Ceil(Mathf.Max(
            tooltipMinSize.y,
            textHeight + verticalInsets
        ));

        if (tooltipMaxSize.y > 0f)
            height = Mathf.Min(height, tooltipMaxSize.y);

        tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        tooltipText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        Vector2 starLocalPoint;
        Vector2 panelTopLocalPoint;

        Vector3 worldStarCenter = targetRect.TransformPoint(targetRect.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            RectTransformUtility.WorldToScreenPoint(null, worldStarCenter),
            null,
            out starLocalPoint
        );

        Vector3 worldPanelTopCenter = panelRect.TransformPoint(new Vector3(0f, panelRect.rect.yMax, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            RectTransformUtility.WorldToScreenPoint(null, worldPanelTopCenter),
            null,
            out panelTopLocalPoint
        );

        float x = starLocalPoint.x;
        float y = panelTopLocalPoint.y + tooltipPanelTopGap;

        float halfWidth = width * 0.5f;
        float minX = parentRect.rect.xMin + halfWidth + tooltipScreenEdgePadding;
        float maxX = parentRect.rect.xMax - halfWidth - tooltipScreenEdgePadding;
        x = Mathf.Clamp(x, minX, maxX);

        tooltipRoot.anchoredPosition = new Vector2(x, y);
    }

    public void HideTooltip()
    {
        _currentTooltipStars = -1;
        _currentTooltipTarget = null;

        if (tooltipRoot)
            tooltipRoot.gameObject.SetActive(false);
    }

    public void HideTooltipFor(RectTransform targetRect)
    {
        if (_currentTooltipTarget == targetRect)
            HideTooltip();
    }

    RectTransform GetTooltipAnchorPanel()
    {
        if (starDifficultyPanelRect)
            return starDifficultyPanelRect;

        return transform as RectTransform;
    }

    public void RefreshDifficultyAnimations(bool force = false)
    {
        if (PlayerProgress.I == null)
            return;

        int selectedStars = StarDifficultySystem.ClampStars(PlayerProgress.I.GetSelectedStarDifficulty());
        if (!force && _lastAppliedDifficultyAnimation == selectedStars)
            return;

        _lastAppliedDifficultyAnimation = selectedStars;

        GameObject prefab = null;
        if (difficultyAnimationPrefabs != null &&
            selectedStars >= 0 &&
            selectedStars < difficultyAnimationPrefabs.Length)
        {
            prefab = difficultyAnimationPrefabs[selectedStars].prefab;
        }

        ReplaceDifficultyAnimationInstance(ref _leftDifficultyAnimationInstance, leftDifficultyAnimationHolder, prefab);
        ReplaceDifficultyAnimationInstance(ref _rightDifficultyAnimationInstance, rightDifficultyAnimationHolder, prefab);
    }

    public void ReplaceDifficultyAnimationInstance(ref GameObject currentInstance, RectTransform holder, GameObject prefab)
    {
        if (currentInstance)
            Destroy(currentInstance);

        currentInstance = null;

        if (!holder || !prefab)
            return;

        currentInstance = Instantiate(prefab, holder);
        currentInstance.name = prefab.name;
        DisableRaycastTargets(currentInstance);

        RectTransform instanceRect = currentInstance.transform as RectTransform;
        if (instanceRect)
        {
            instanceRect.anchorMin = Vector2.zero;
            instanceRect.anchorMax = Vector2.one;
            instanceRect.offsetMin = Vector2.zero;
            instanceRect.offsetMax = Vector2.zero;
            instanceRect.localScale = Vector3.one;
            instanceRect.anchoredPosition = Vector2.zero;
        }

        RestartDifficultyAnimation(currentInstance);
    }

    void DisableRaycastTargets(GameObject root)
    {
        if (!root)
            return;

        var graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i])
                graphics[i].raycastTarget = false;
        }
    }

    void RestartDifficultyAnimation(GameObject instance)
    {
        if (!instance)
            return;

        var animators = instance.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (!animator || !animator.isActiveAndEnabled || !animator.runtimeAnimatorController)
                continue;

            animator.Rebind();
            animator.Play(0, 0, 0f);
            animator.Update(0f);
        }

        var pingPongAnimators = instance.GetComponentsInChildren<UIImagePingPongAnimator>(true);
        for (int i = 0; i < pingPongAnimators.Length; i++)
        {
            pingPongAnimators[i].ResetAnimation();
            pingPongAnimators[i].Play();
        }
    }
}

public class TitleStarDifficultyHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    TitleStarDifficultyUI owner;
    int stars;
    RectTransform rectTransform;
    Image image;

    public void Initialize(TitleStarDifficultyUI newOwner, int newStars)
    {
        owner = newOwner;
        stars = newStars;
        rectTransform = transform as RectTransform;
        image = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Show();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        Show();
    }

    void Show()
    {
        if (!owner || !rectTransform || !image || !image.raycastTarget)
            return;

        owner.ShowTooltip(stars, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!owner)
            return;

        owner.HideTooltipFor(rectTransform);
    }

}
