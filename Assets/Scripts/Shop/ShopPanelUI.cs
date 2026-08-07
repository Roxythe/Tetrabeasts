using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanelUI : MonoBehaviour
{
    const string RuntimeTooltipRootName = "ShopTooltip_Panel";
    const string RuntimeTooltipTextName = "ShopTooltip_Text";
    const string WelcomePromptPrefsKey = "Tetrabeasts.ShopPrompt.Welcome.Used";
    const string PurchasePromptPrefsKey = "Tetrabeasts.ShopPrompt.Purchase.Used";
    const string RefundPromptPrefsKey = "Tetrabeasts.ShopPrompt.Refund.Used";
    const string IdlePromptPrefsKey = "Tetrabeasts.ShopPrompt.Idle.Used";

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
    string _claimedWelcomeText = string.Empty;
    Sprite _claimedWelcomeSprite;
    bool _hasClaimedWelcomePrompt;
    static readonly Dictionary<string, HashSet<int>> s_sessionUsedPromptTextIndices = new();
    static readonly Dictionary<string, int> s_sessionLastPromptTextIndex = new();

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

        if (_hasClaimedWelcomePrompt)
            ShowSelectedShopKeeperPrompt(_claimedWelcomeText, _claimedWelcomeSprite);
        else
            ShowShopKeeperPrompt(ShopKeeperPromptType.Welcome);
    }

    public void ClaimWelcomePromptForShopButtonPress()
    {
        ResolveShopKeeperReferences();
        _claimedWelcomeText = PickRandomText(welcomeText, WelcomePromptPrefsKey, ref _lastWelcomeTextIndex);
        _claimedWelcomeSprite = PickRandomSprite(welcomeImages, ref _lastWelcomeImageIndex);
        _hasClaimedWelcomePrompt = true;
        ApplyShopKeeperPromptContent(_claimedWelcomeText, _claimedWelcomeSprite);
    }

    public void NotifyShopClosed()
    {
        _shopPanelOpen = false;
        _hasClaimedWelcomePrompt = false;
        _claimedWelcomeText = string.Empty;
        _claimedWelcomeSprite = null;
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
                ShowShopKeeperPrompt(welcomeText, welcomeImages, WelcomePromptPrefsKey, ref _lastWelcomeTextIndex, ref _lastWelcomeImageIndex);
                break;
            case ShopKeeperPromptType.Purchase:
                ShowShopKeeperPrompt(purchaseMadeText, purchaseImages, PurchasePromptPrefsKey, ref _lastPurchaseTextIndex, ref _lastPurchaseImageIndex);
                break;
            case ShopKeeperPromptType.Refund:
                ShowShopKeeperPrompt(refundText, refundImages, RefundPromptPrefsKey, ref _lastRefundTextIndex, ref _lastRefundImageIndex);
                break;
            case ShopKeeperPromptType.Idle:
                ShowShopKeeperPrompt(idleText, idleImages, IdlePromptPrefsKey, ref _lastIdleTextIndex, ref _lastIdleImageIndex);
                break;
        }
    }

    void ShowShopKeeperPrompt(string[] textOptions, Sprite[] imageOptions, string promptPrefsKey, ref int lastTextIndex, ref int lastImageIndex)
    {
        if (!shopTextPopUp)
            return;

        string selectedText = PickRandomText(textOptions, promptPrefsKey, ref lastTextIndex);
        Sprite selectedSprite = PickRandomSprite(imageOptions, ref lastImageIndex);

        ShowSelectedShopKeeperPrompt(selectedText, selectedSprite);
    }

    void ShowSelectedShopKeeperPrompt(string selectedText, Sprite selectedSprite)
    {
        if (!shopTextPopUp)
            return;

        if (string.IsNullOrWhiteSpace(selectedText) && !selectedSprite)
            return;

        shopTextPopUp.SetActive(true);
        ApplyShopKeeperPromptContent(selectedText, selectedSprite);

        if (_shopKeeperPopupRoutine != null)
            StopCoroutine(_shopKeeperPopupRoutine);

        _shopKeeperPopupRoutine = StartCoroutine(HideShopKeeperPromptAfterDelay());
    }

    void ApplyShopKeeperPromptContent(string selectedText, Sprite selectedSprite)
    {
        if (shopText)
        {
            string englishText = selectedText?.Trim() ?? string.Empty;
            BindPromptTextLocalizationFallback(shopText, englishText);

            string localizedText = string.IsNullOrWhiteSpace(englishText)
                ? string.Empty
                : TetrabeastsLocalization.LocalizeText(englishText);
            SetPromptText(shopText, localizedText);
        }

        if (shopKeeperImage)
        {
            if (selectedSprite)
                shopKeeperImage.sprite = selectedSprite;
        }

        Canvas.ForceUpdateCanvases();
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

    static string PickRandomText(string[] options, string prefsKey, ref int lastIndex)
    {
        int index = PickNonRepeatingTextIndex(options, prefsKey, lastIndex);
        if (index < 0)
            return string.Empty;

        lastIndex = index;
        return options[index].Trim();
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

    static int PickNonRepeatingTextIndex(string[] options, string prefsKey, int lastIndex)
    {
        if (options == null || options.Length == 0)
            return -1;

        var validIndices = new List<int>();
        var validLookup = new HashSet<int>();
        for (int i = 0; i < options.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(options[i]))
                continue;

            validIndices.Add(i);
            validLookup.Add(i);
        }

        if (validIndices.Count == 0)
            return -1;

        if (validIndices.Count == 1)
            return validIndices[0];

        if (!validLookup.Contains(lastIndex))
            lastIndex = LoadLastPromptTextIndex(prefsKey);
        if (!validLookup.Contains(lastIndex))
            lastIndex = -1;

        var usedIndices = LoadUsedPromptTextIndices(prefsKey);
        RemoveInvalidPromptTextIndices(usedIndices, validLookup);

        var candidates = BuildUnusedPromptTextCandidates(validIndices, usedIndices);
        if (candidates.Count == 0)
        {
            usedIndices.Clear();
            candidates.AddRange(validIndices);
        }

        if (candidates.Count > 1)
            candidates.Remove(lastIndex);

        int pickedIndex = candidates[Random.Range(0, candidates.Count)];
        usedIndices.Add(pickedIndex);
        SaveUsedPromptTextIndices(prefsKey, usedIndices, pickedIndex);
        return pickedIndex;
    }

    static List<int> BuildUnusedPromptTextCandidates(List<int> validIndices, HashSet<int> usedIndices)
    {
        var candidates = new List<int>();
        for (int i = 0; i < validIndices.Count; i++)
        {
            int index = validIndices[i];
            if (usedIndices == null || !usedIndices.Contains(index))
                candidates.Add(index);
        }

        return candidates;
    }

    static HashSet<int> LoadUsedPromptTextIndices(string prefsKey)
    {
        var usedIndices = new HashSet<int>();

        if (string.IsNullOrWhiteSpace(prefsKey))
            return usedIndices;

        string rawValue = PlayerPrefs.GetString(prefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            string[] parts = rawValue.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int index))
                    usedIndices.Add(index);
            }
        }

        if (s_sessionUsedPromptTextIndices.TryGetValue(prefsKey, out var sessionUsedIndices))
            usedIndices.UnionWith(sessionUsedIndices);

        return usedIndices;
    }

    static int LoadLastPromptTextIndex(string prefsKey)
    {
        if (string.IsNullOrWhiteSpace(prefsKey))
            return -1;

        if (s_sessionLastPromptTextIndex.TryGetValue(prefsKey, out int sessionLastIndex))
            return sessionLastIndex;

        return PlayerPrefs.GetInt(GetLastPromptTextPrefsKey(prefsKey), -1);
    }

    static void RemoveInvalidPromptTextIndices(HashSet<int> usedIndices, HashSet<int> validLookup)
    {
        if (usedIndices == null || usedIndices.Count == 0 || validLookup == null)
            return;

        var staleIndices = new List<int>();
        foreach (int index in usedIndices)
        {
            if (!validLookup.Contains(index))
                staleIndices.Add(index);
        }

        for (int i = 0; i < staleIndices.Count; i++)
            usedIndices.Remove(staleIndices[i]);
    }

    static void SaveUsedPromptTextIndices(string prefsKey, HashSet<int> usedIndices, int lastPickedIndex)
    {
        if (string.IsNullOrWhiteSpace(prefsKey))
            return;

        if (!s_sessionUsedPromptTextIndices.TryGetValue(prefsKey, out var sessionUsedIndices))
        {
            sessionUsedIndices = new HashSet<int>();
            s_sessionUsedPromptTextIndices[prefsKey] = sessionUsedIndices;
        }

        sessionUsedIndices.Clear();

        if (usedIndices == null || usedIndices.Count == 0)
        {
            PlayerPrefs.DeleteKey(prefsKey);
        }
        else
        {
            var values = new List<string>(usedIndices.Count);
            foreach (int index in usedIndices)
            {
                sessionUsedIndices.Add(index);
                values.Add(index.ToString());
            }

            PlayerPrefs.SetString(prefsKey, string.Join(",", values));
        }

        s_sessionLastPromptTextIndex[prefsKey] = lastPickedIndex;
        PlayerPrefs.SetInt(GetLastPromptTextPrefsKey(prefsKey), lastPickedIndex);
        PlayerPrefsSaveScheduler.QueueSave(queueCloudUpload: false);
    }

    static string GetLastPromptTextPrefsKey(string prefsKey)
    {
        return prefsKey + ".Last";
    }

    static void SetPromptText(TMP_Text target, string text)
    {
        if (!target)
            return;

        target.text = string.Empty;
        target.ForceMeshUpdate(true, true);
        target.text = text ?? string.Empty;
        target.SetVerticesDirty();
        target.SetLayoutDirty();
        target.ForceMeshUpdate(true, true);
    }

    static void BindPromptTextLocalizationFallback(TMP_Text target, string englishText)
    {
        if (!target || string.IsNullOrWhiteSpace(englishText))
            return;

        var staticBinding = target.GetComponent<TetrabeastsLocalizedText>();
        if (staticBinding)
            staticBinding.enabled = false;

        var rawBinding = target.GetComponent<TetrabeastsLocalizedRawText>();
        if (!rawBinding && TetrabeastsLocalization.HasRuntimeTranslation(englishText))
            rawBinding = target.gameObject.AddComponent<TetrabeastsLocalizedRawText>();

        if (rawBinding)
            rawBinding.Configure(target, englishText);
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
