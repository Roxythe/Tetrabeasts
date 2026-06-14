using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanelUI : MonoBehaviour
{
    const string RuntimeTooltipRootName = "ShopTooltip_Panel";
    const string RuntimeTooltipTextName = "ShopTooltip_Text";

    enum ShopKeeperPromptType
    {
        Welcome,
        Purchase,
        Refund,
        Idle
    }

    [Header("Optional")]
    public CurrencyUI currencyUI;

    [Header("Entries")]
    public ShopBuffEntryUI[] entries;

    [Header("Buttons")]
    public Button refundAllButton;

    [Header("Tooltip")]
    [SerializeField] RectTransform tooltipRoot;
    [SerializeField] TMP_Text tooltipText;
    [SerializeField] Vector2 tooltipOffset = new Vector2(0f, 18f);
    [SerializeField] Vector2 tooltipMinSize = new Vector2(360f, 96f);
    [SerializeField] Vector2 tooltipMaxSize = new Vector2(560f, 260f);
    [SerializeField] Vector2 tooltipTextInsetMin = new Vector2(14f, 12f);
    [SerializeField] Vector2 tooltipTextInsetMax = new Vector2(14f, 12f);
    [SerializeField, Min(0f)] float tooltipScreenEdgePadding = 16f;

    [Header("Shop Keeper Popup")]
    [SerializeField] GameObject shopTextPopUp;
    [SerializeField] TMP_Text shopText;
    [SerializeField] Image shopKeeperImage;
    [SerializeField, Min(0f)] float popupSeconds = 3f;
    [SerializeField, Min(0f)] float idlePromptSeconds = 15f;
    [SerializeField, TextArea(2, 4)] string[] welcomeText =
    {
        "If you got the gaba, I got the goods."
    };
    [SerializeField, TextArea(2, 4)] string[] purchaseMadeText;
    [SerializeField, TextArea(2, 4)] string[] refundText;
    [SerializeField, TextArea(2, 4)] string[] idleText;
    [SerializeField] Sprite[] welcomeImages;
    [SerializeField] Sprite[] purchaseImages;
    [SerializeField] Sprite[] refundImages;
    [SerializeField] Sprite[] idleImages;

    RectTransform _currentTooltipTarget;
    Coroutine _shopKeeperPopupRoutine;
    bool _shopPanelOpen;
    float _lastShopActivityTime;
    int _lastWelcomeTextIndex = -1;
    int _lastPurchaseTextIndex = -1;
    int _lastRefundTextIndex = -1;
    int _lastIdleTextIndex = -1;
    int _lastWelcomeImageIndex = -1;
    int _lastPurchaseImageIndex = -1;
    int _lastRefundImageIndex = -1;
    int _lastIdleImageIndex = -1;

    void Awake()
    {
        ResolveShopKeeperReferences();
        HideShopKeeperPrompt();
    }

    private void OnEnable()
    {
        ResolveTooltipReferences();
        ResolveShopKeeperReferences();
        HideTooltip();

        if (refundAllButton)
        {
            refundAllButton.onClick.RemoveAllListeners();
            refundAllButton.onClick.AddListener(RefundAllUpgrades);
        }

        RefreshAll();
    }

    void OnDisable()
    {
        NotifyShopClosed();
        HideTooltip();
    }

    void Update()
    {
        if (!_shopPanelOpen)
            return;

        if (!UIPanelTransition.IsVisible(gameObject))
        {
            NotifyShopClosed();
            return;
        }

        if (idlePromptSeconds <= 0f)
            return;

        if (Time.unscaledTime - _lastShopActivityTime < idlePromptSeconds)
            return;

        _lastShopActivityTime = Time.unscaledTime;
        ShowShopKeeperPrompt(ShopKeeperPromptType.Idle);
    }

    public void NotifyShopOpened()
    {
        ResolveShopKeeperReferences();
        _shopPanelOpen = true;
        RegisterShopActivity();
        ShowShopKeeperPrompt(ShopKeeperPromptType.Welcome);
    }

    public void NotifyShopClosed()
    {
        _shopPanelOpen = false;
        HideShopKeeperPrompt();
    }

    public void NotifyShopActivity()
    {
        RegisterShopActivity();
    }

    public void NotifyPurchaseMade()
    {
        RegisterShopActivity();
        ShowShopKeeperPrompt(ShopKeeperPromptType.Purchase);
    }

    public void RefreshAll()
    {
        currencyUI?.Refresh();

        if (refundAllButton)
            refundAllButton.interactable = ShopBuffStore.GetTotalRefundAll() > 0;

        if (entries == null) return;
        foreach (var e in entries)
            if (e) e.Refresh();
    }

    public void RefundAllUpgrades()
    {
        RegisterShopActivity();

        int refund = ShopBuffStore.GetTotalRefundAll(); // Calculate refund before resetting levels
        ShopBuffStore.ResetAllToZero(); // Reset levels so buffs no longer apply

        if (refund > 0)
            CurrencyStore.Add(refund);

        // Clear any cached/recorded shop-level stats 
        if (PlayerProgress.I != null)
        {
            const string lifetimePrefix = "lt_shop_level_";
            foreach (var t in ShopBuffStore.AllTypes)
            {
                string key = lifetimePrefix + t;
                PlayerProgress.I.SetLifetimeBestInt(key, 0);
            }
        }

        RefreshAll();
        ShowShopKeeperPrompt(ShopKeeperPromptType.Refund);
    }

    void RegisterShopActivity()
    {
        _lastShopActivityTime = Time.unscaledTime;
    }

    void ShowShopKeeperPrompt(ShopKeeperPromptType promptType)
    {
        ResolveShopKeeperReferences();

        switch (promptType)
        {
            case ShopKeeperPromptType.Welcome:
                ShowShopKeeperPrompt(welcomeText, welcomeImages, ref _lastWelcomeTextIndex, ref _lastWelcomeImageIndex);
                break;
            case ShopKeeperPromptType.Purchase:
                ShowShopKeeperPrompt(purchaseMadeText, purchaseImages, ref _lastPurchaseTextIndex, ref _lastPurchaseImageIndex);
                break;
            case ShopKeeperPromptType.Refund:
                ShowShopKeeperPrompt(refundText, refundImages, ref _lastRefundTextIndex, ref _lastRefundImageIndex);
                break;
            case ShopKeeperPromptType.Idle:
                ShowShopKeeperPrompt(idleText, idleImages, ref _lastIdleTextIndex, ref _lastIdleImageIndex);
                break;
        }
    }

    void ShowShopKeeperPrompt(string[] textOptions, Sprite[] imageOptions, ref int lastTextIndex, ref int lastImageIndex)
    {
        if (!shopTextPopUp)
            return;

        string selectedText = PickRandomText(textOptions, ref lastTextIndex);
        Sprite selectedSprite = PickRandomSprite(imageOptions, ref lastImageIndex);

        if (string.IsNullOrWhiteSpace(selectedText) && !selectedSprite)
            return;

        if (shopText)
        {
            shopText.text = string.IsNullOrWhiteSpace(selectedText)
                ? string.Empty
                : TetrabeastsLocalization.LocalizeText(selectedText);
        }

        if (shopKeeperImage)
        {
            if (selectedSprite)
                shopKeeperImage.sprite = selectedSprite;
        }

        shopTextPopUp.SetActive(true);

        if (_shopKeeperPopupRoutine != null)
            StopCoroutine(_shopKeeperPopupRoutine);

        _shopKeeperPopupRoutine = StartCoroutine(HideShopKeeperPromptAfterDelay());
    }

    IEnumerator HideShopKeeperPromptAfterDelay()
    {
        float delay = Mathf.Max(0f, popupSeconds);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        _shopKeeperPopupRoutine = null;
        HideShopKeeperPrompt();
    }

    void HideShopKeeperPrompt()
    {
        if (_shopKeeperPopupRoutine != null)
        {
            StopCoroutine(_shopKeeperPopupRoutine);
            _shopKeeperPopupRoutine = null;
        }

        if (shopTextPopUp)
            shopTextPopUp.SetActive(false);
    }

    static string PickRandomText(string[] options, ref int lastIndex)
    {
        int index = PickRandomIndex(options, lastIndex);
        if (index < 0)
            return string.Empty;

        lastIndex = index;
        return options[index];
    }

    static Sprite PickRandomSprite(Sprite[] options, ref int lastIndex)
    {
        int index = PickRandomIndex(options, lastIndex);
        if (index < 0)
            return null;

        lastIndex = index;
        return options[index];
    }

    static int PickRandomIndex<T>(T[] options, int lastIndex)
    {
        if (options == null || options.Length == 0)
            return -1;

        if (options.Length == 1)
            return 0;

        int index = Random.Range(0, options.Length);
        if (index == lastIndex)
            index = (index + Random.Range(1, options.Length)) % options.Length;

        return index;
    }

    public void ShowTooltip(ShopBuffEntryUI entry, RectTransform targetRect)
    {
        ResolveTooltipReferences();
        if (!tooltipRoot || !tooltipText || !entry || !targetRect)
            return;

        string text = TetrabeastsLocalization.LocalizeText(entry.GetHoverDescription());
        if (string.IsNullOrWhiteSpace(text))
            return;

        _currentTooltipTarget = targetRect;

        RectTransform parentRect = tooltipRoot.parent as RectTransform;
        if (!parentRect)
            return;

        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.raycastTarget = false;
        tooltipText.text = TetrabeastsControls.ResolveControlPromptTokens(text);

        var tooltipImage = tooltipRoot.GetComponent<Image>();
        if (tooltipImage)
            tooltipImage.raycastTarget = false;

        var layoutElement = tooltipRoot.GetComponent<LayoutElement>();
        if (!layoutElement)
            layoutElement = tooltipRoot.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
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
            : parentRect.rect.width - tooltipScreenEdgePadding * 2f;
        maxWidth = Mathf.Max(tooltipMinSize.x, maxWidth);

        Vector2 preferred = tooltipText.GetPreferredValues(
            tooltipText.text,
            float.PositiveInfinity,
            float.PositiveInfinity);

        float width = Mathf.Ceil(Mathf.Clamp(
            preferred.x + horizontalInsets,
            tooltipMinSize.x,
            maxWidth));

        float wrappedTextWidth = Mathf.Max(1f, width - horizontalInsets);
        tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipMinSize.y);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        tooltipText.ForceMeshUpdate(true, true);

        preferred = tooltipText.GetPreferredValues(tooltipText.text, wrappedTextWidth, float.PositiveInfinity);
        float textHeight = Mathf.Max(preferred.y, tooltipText.preferredHeight, tooltipText.renderedHeight);
        float height = Mathf.Ceil(Mathf.Max(tooltipMinSize.y, textHeight + verticalInsets));
        if (tooltipMaxSize.y > 0f)
            height = Mathf.Min(height, tooltipMaxSize.y);

        tooltipRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
        tooltipText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        Vector3 worldCenter = targetRect.TransformPoint(targetRect.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            RectTransformUtility.WorldToScreenPoint(null, worldCenter),
            null,
            out Vector2 localCenter);

        float x = localCenter.x + tooltipOffset.x;
        float y = localCenter.y + targetRect.rect.height * 0.5f + tooltipOffset.y;

        float halfWidth = width * 0.5f;
        float minX = parentRect.rect.xMin + halfWidth + tooltipScreenEdgePadding;
        float maxX = parentRect.rect.xMax - halfWidth - tooltipScreenEdgePadding;
        x = Mathf.Clamp(x, minX, maxX);

        float minY = parentRect.rect.yMin + tooltipScreenEdgePadding;
        float maxY = parentRect.rect.yMax - height - tooltipScreenEdgePadding;
        y = Mathf.Clamp(y, minY, maxY);

        tooltipRoot.anchoredPosition = new Vector2(x, y);
    }

    public void HideTooltipFor(RectTransform targetRect)
    {
        if (_currentTooltipTarget == targetRect)
            HideTooltip();
    }

    public void HideTooltip()
    {
        _currentTooltipTarget = null;

        if (tooltipRoot)
            tooltipRoot.gameObject.SetActive(false);
    }

    void ResolveTooltipReferences()
    {
        if (!tooltipRoot)
        {
            var foundPanel = FindChildByName(transform, "Tooltip_Panel") ??
                FindChildByName(transform, RuntimeTooltipRootName);
            if (foundPanel)
                tooltipRoot = foundPanel as RectTransform;
        }

        if (!tooltipText && tooltipRoot)
        {
            var foundText = FindChildByName(tooltipRoot, "Tooltip_Text (TMP)");
            if (!foundText)
                foundText = FindChildByName(tooltipRoot, RuntimeTooltipTextName);

            tooltipText = foundText ? foundText.GetComponent<TMP_Text>() : tooltipRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (!tooltipRoot || !tooltipText)
            CreateRuntimeTooltip();
    }

    void ResolveShopKeeperReferences()
    {
        if (!shopTextPopUp)
        {
            Transform foundPopUp = FindChildByName(transform, "ShopTextPopUp");
            if (foundPopUp)
                shopTextPopUp = foundPopUp.gameObject;
        }

        if (!shopText)
        {
            Transform textRoot = shopTextPopUp ? shopTextPopUp.transform : transform;
            Transform foundText = FindChildByName(textRoot, "Shop_Text (TMP)");
            shopText = foundText
                ? foundText.GetComponent<TMP_Text>()
                : textRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (!shopKeeperImage)
        {
            Transform foundImage = FindChildByName(transform, "ShopKeeper_Image");
            shopKeeperImage = foundImage ? foundImage.GetComponent<Image>() : null;
        }
    }

    void CreateRuntimeTooltip()
    {
        RectTransform parentRect = transform as RectTransform;
        if (!parentRect)
            return;

        if (!tooltipRoot)
        {
            var rootGo = new GameObject(RuntimeTooltipRootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootGo.layer = gameObject.layer;
            rootGo.transform.SetParent(parentRect, false);

            tooltipRoot = rootGo.transform as RectTransform;
            tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRoot.pivot = new Vector2(0.5f, 0f);
            tooltipRoot.sizeDelta = tooltipMinSize;

            var image = rootGo.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.92f);
            image.raycastTarget = false;

            rootGo.SetActive(false);
        }

        if (!tooltipText)
        {
            var textGo = new GameObject(RuntimeTooltipTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.layer = tooltipRoot.gameObject.layer;
            textGo.transform.SetParent(tooltipRoot, false);

            tooltipText = textGo.GetComponent<TMP_Text>();
            tooltipText.raycastTarget = false;
            tooltipText.color = Color.white;
            tooltipText.fontSize = 28f;
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.textWrappingMode = TextWrappingModes.Normal;
            tooltipText.overflowMode = TextOverflowModes.Overflow;

            TMP_Text source = GetTooltipTextStyleSource();
            if (source)
            {
                tooltipText.font = source.font;
                tooltipText.fontSharedMaterial = source.fontSharedMaterial;
                tooltipText.fontSize = source.fontSize;
            }
        }
    }

    TMP_Text GetTooltipTextStyleSource()
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (!entry)
                    continue;

                if (entry.currentLevelText)
                    return entry.currentLevelText;

                if (entry.costText)
                    return entry.costText;
            }
        }

        return GetComponentInChildren<TMP_Text>(true);
    }

    static Transform FindChildByName(Transform root, string childName)
    {
        if (!root || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found)
                return found;
        }

        return null;
    }
}
