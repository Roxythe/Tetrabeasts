using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanelUI : MonoBehaviour
{
    const string RuntimeTooltipRootName = "ShopTooltip_Panel";
    const string RuntimeTooltipTextName = "ShopTooltip_Text";

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

    RectTransform _currentTooltipTarget;

    private void OnEnable()
    {
        ResolveTooltipReferences();
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
        HideTooltip();
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
