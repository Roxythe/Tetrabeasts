using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TitleTipTextRotator : MonoBehaviour
{
    const string LastTipIndexPrefsKey = "TitleTipTextRotator_LastTipIndex";
    const string RequiredParentName = "Tips";

    [SerializeField] TMP_Text tipText;
    [SerializeField] RectTransform backgroundRect;
    [SerializeField] RectTransform backgroundFrameRect;
    [SerializeField] Vector2 backgroundPadding = new Vector2(24f, 18f);
    [SerializeField, Min(0f)] float minimumBackgroundHeight = 44f;
    [SerializeField, Min(0f)] float maximumBackgroundHeight;
    [SerializeField] bool avoidImmediateRepeat = true;
    [SerializeField, TextArea(2, 4)] string[] tips =
    {
        "Tip: Special blocks activate as soon as they are placed.",
        "Tip: Rerolls can be saved and used on future reward screens during the same run.",
        "Tip: Temporary monster copies earn EXP during a run, and some of it becomes permanent after the run ends.",
        "Tip: Full rows launch attacks at the enemy castle.",
        "Tip: Keep an eye on your unit reserve. If it reaches 0, the run is over.",
        "Tip: Level modifiers stack with your run buffs and debuffs."
    };

    int _currentTipIndex = -1;
    float _initialBackgroundHeight;
    bool _isActiveTipTarget;
    bool _subscribedToLocalization;

    void Awake()
    {
        if (!tipText)
            tipText = GetComponent<TMP_Text>();

        _isActiveTipTarget = IsActiveTipTarget();
        if (!_isActiveTipTarget)
            return;

        EnsureBackgroundReference();
        if (backgroundRect)
            _initialBackgroundHeight = backgroundRect.rect.height;
        else if (backgroundFrameRect)
            _initialBackgroundHeight = backgroundFrameRect.rect.height;
    }

    void OnEnable()
    {
        _isActiveTipTarget = IsActiveTipTarget();
        if (!_isActiveTipTarget)
            return;

        TetrabeastsLocalization.LanguageChanged += RefreshCurrentTipText;
        _subscribedToLocalization = true;
        PickNewTip();
    }

    void OnDisable()
    {
        if (_subscribedToLocalization)
        {
            TetrabeastsLocalization.LanguageChanged -= RefreshCurrentTipText;
            _subscribedToLocalization = false;
        }
    }

    [ContextMenu("Pick New Tip")]
    public void PickNewTip()
    {
        if (!_isActiveTipTarget || !tipText || tips == null || tips.Length == 0)
            return;

        int newIndex = Random.Range(0, tips.Length);
        int lastIndex = PlayerPrefs.GetInt(LastTipIndexPrefsKey, -1);

        if (avoidImmediateRepeat && tips.Length > 1 && newIndex == lastIndex)
            newIndex = (newIndex + Random.Range(1, tips.Length)) % tips.Length;

        _currentTipIndex = newIndex;
        PlayerPrefs.SetInt(LastTipIndexPrefsKey, _currentTipIndex);
        RefreshCurrentTipText();
    }

    void RefreshCurrentTipText()
    {
        if (!_isActiveTipTarget || !tipText || tips == null || tips.Length == 0)
            return;

        if (_currentTipIndex < 0 || _currentTipIndex >= tips.Length)
            _currentTipIndex = Mathf.Clamp(PlayerPrefs.GetInt(LastTipIndexPrefsKey, 0), 0, tips.Length - 1);

        string localized = TetrabeastsLocalization.LocalizeText(tips[_currentTipIndex] ?? string.Empty);
        tipText.richText = true;
        tipText.text = TetrabeastsControls.ResolveControlPromptTokens(localized);
        ResizeBackgroundToCurrentTip();
    }

    void ResizeBackgroundToCurrentTip()
    {
        if (!tipText)
            return;

        EnsureBackgroundReference();
        if (!backgroundRect && !backgroundFrameRect)
            return;

        RectTransform textRect = tipText.rectTransform;
        float textWidth = textRect ? textRect.rect.width : 0f;
        if (textWidth <= 0f)
            textWidth = tipText.preferredWidth;

        Canvas.ForceUpdateCanvases();
        tipText.ForceMeshUpdate(true, true);

        Vector2 preferred = tipText.GetPreferredValues(
            tipText.text,
            Mathf.Max(1f, textWidth),
            float.PositiveInfinity);

        float targetHeight = Mathf.Ceil(preferred.y + backgroundPadding.y);
        targetHeight = Mathf.Max(minimumBackgroundHeight, targetHeight);

        float maxHeight = maximumBackgroundHeight > 0f ? maximumBackgroundHeight : _initialBackgroundHeight;
        if (maxHeight > 0f)
            targetHeight = Mathf.Min(targetHeight, maxHeight);

        ResizeBackgrounds(targetHeight);
        CenterTextInBackground(targetHeight);
    }

    void ResizeBackgrounds(float targetHeight)
    {
        if (backgroundRect)
            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        if (!backgroundFrameRect)
            return;

        if (backgroundRect)
        {
            backgroundFrameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, backgroundRect.rect.width);
            backgroundFrameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, backgroundRect.rect.height);
        }
        else
        {
            backgroundFrameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }
    }

    void CenterTextInBackground(float backgroundHeight)
    {
        RectTransform textRect = tipText ? tipText.rectTransform : null;
        if (!textRect || !backgroundRect)
            return;

        float contentHeight = Mathf.Max(1f, backgroundHeight - backgroundPadding.y);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        textRect.anchoredPosition = new Vector2(
            textRect.anchoredPosition.x,
            backgroundRect.anchoredPosition.y);
    }

    void EnsureBackgroundReference()
    {
        if (backgroundRect && backgroundFrameRect)
            return;

        Transform parent = tipText ? tipText.transform.parent : transform.parent;
        if (!parent || parent.name != RequiredParentName)
            return;

        if (!backgroundRect)
        {
            Transform found = FindDirectChild(parent, "BG_Image");
            backgroundRect = found as RectTransform;
        }

        if (!backgroundFrameRect)
        {
            Transform foundFrame = FindDirectChild(parent, "BGFrame_Image");
            backgroundFrameRect = foundFrame as RectTransform;
        }
    }

    bool IsActiveTipTarget()
    {
        if (!tipText)
            tipText = GetComponent<TMP_Text>();

        Transform parent = tipText ? tipText.transform.parent : transform.parent;
        return parent && parent.name == RequiredParentName;
    }

    static Transform FindDirectChild(Transform parent, string childName)
    {
        if (!parent || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child && child.name == childName)
                return child;
        }

        return null;
    }
}
