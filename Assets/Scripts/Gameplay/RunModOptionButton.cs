using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunModOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Refs")]
    public Button button;
    public Image backgroundImage;
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text nameShadowText;
    public TMP_Text descText;
    public TMP_Text descShadowText;
    public Outline highlightOutline;

    [Header("Hover/Select Tint (multiplies prefab BG color)")]
    [Min(0.01f)] public float hoverTintMult = 1.12f;
    [Min(0.01f)] public float selectedTintMult = 0.85f;

    [Header("Selected Override (optional)")]
    public bool useSelectedOverrideColor = true;
    public Color selectedOverrideColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Button Transition")]
    public bool forceButtonTransitionNone = true;

    [Header("Rarity Text Colors")]
    public Color commonNameColor = new Color(0.85f, 0.85f, 0.85f);
    public Color uncommonNameColor = Color.green;
    public Color rareNameColor = new Color(0.3f, 0.6f, 1f);
    public Color epicNameColor = new Color(0.75f, 0.3f, 1f);
    public Color legendaryNameColor = new Color(1f, 0.75f, 0.2f);

    public Color commonDescColor = new Color(0.85f, 0.85f, 0.85f);
    public Color uncommonDescColor = Color.green;
    public Color rareDescColor = new Color(0.3f, 0.6f, 1f);
    public Color epicDescColor = new Color(0.75f, 0.3f, 1f);
    public Color legendaryDescColor = new Color(1f, 0.75f, 0.2f);

    RunModifierSO _mod;
    bool _selected;
    bool _isPointerHover;
    bool _isNavigationFocus;

    Image _bg;
    Color _bgDefaultColor;

    void Awake()
    {
        if (button && forceButtonTransitionNone)
            button.transition = Selectable.Transition.None;

        if (backgroundImage) _bg = backgroundImage;
        else if (button && button.targetGraphic is Graphic g && g is Image gi) _bg = gi;
        else _bg = GetComponent<Image>();

        if (_bg)
            _bgDefaultColor = _bg.color;

        ApplyBgVisual();
    }

    void Update()
    {
        if (_isPointerHover && !_isNavigationFocus && !UICursorController.IsPointerTargetMode)
        {
            _isPointerHover = false;
            ApplyBgVisual();
        }
    }

    public void Bind(RunModifierSO mod, Action<RunModifierSO> onSelected)
    {
        _mod = mod;

        string displayName = TetrabeastsLocalization.LocalizeText(mod.displayName);
        string description = TetrabeastsLocalization.LocalizeText(mod.description);

        if (icon) icon.sprite = mod.icon;
        if (nameText) nameText.text = displayName;
        if (nameShadowText) nameShadowText.text = displayName;
        if (descText) descText.text = description;
        if (descShadowText) descShadowText.text = description;

        if (mod is RunModifier generic)
        {
            if (nameText) nameText.color = GetNameColor(generic.rarity);
            if (descText) descText.color = GetDescColor(generic.rarity);
        }
        else
        {
            if (nameText) nameText.color = commonNameColor;
            if (descText) descText.color = commonDescColor;
        }

        SetSelected(false);

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(_mod));
        }
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (highlightOutline) highlightOutline.enabled = selected;
        ApplyBgVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerHover = true;
        ApplyBgVisual();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!UICursorController.IsPointerTargetMode && !_isNavigationFocus)
            return;

        _isPointerHover = true;
        ApplyBgVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerHover = false;
        ApplyBgVisual();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isNavigationFocus = true;
        ApplyBgVisual();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isNavigationFocus = false;
        ApplyBgVisual();
    }

    void ApplyBgVisual()
    {
        if (!_bg) return;

        if (_selected)
        {
            _bg.color = useSelectedOverrideColor ? selectedOverrideColor : MultiplyRgb(_bgDefaultColor, selectedTintMult);
            return;
        }

        bool pointerHover = _isPointerHover && UICursorController.IsPointerTargetMode;
        bool navigationFocus = _isNavigationFocus && UICursorController.IsButtonNavigationMode;
        _bg.color = pointerHover || navigationFocus ? MultiplyRgb(_bgDefaultColor, hoverTintMult) : _bgDefaultColor;
    }

    static Color MultiplyRgb(Color c, float mult)
    {
        return new Color(
            Mathf.Clamp01(c.r * mult),
            Mathf.Clamp01(c.g * mult),
            Mathf.Clamp01(c.b * mult),
            c.a
        );
    }

    Color GetNameColor(RunModRarity r)
    {
        switch (r)
        {
            case RunModRarity.Uncommon: return uncommonNameColor;
            case RunModRarity.Rare: return rareNameColor;
            case RunModRarity.Epic: return epicNameColor;
            case RunModRarity.Legendary: return legendaryNameColor;
            default: return commonNameColor;
        }
    }

    Color GetDescColor(RunModRarity r)
    {
        switch (r)
        {
            case RunModRarity.Uncommon: return uncommonDescColor;
            case RunModRarity.Rare: return rareDescColor;
            case RunModRarity.Epic: return epicDescColor;
            case RunModRarity.Legendary: return legendaryDescColor;
            default: return commonDescColor;
        }
    }
}
