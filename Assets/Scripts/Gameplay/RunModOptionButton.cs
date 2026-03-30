// File: Assets/Scripts/UI/RunModOptionButton.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RunModOptionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    public Button button;
    public Image backgroundImage;
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text descText;
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
    bool _isHover;

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

    public void Bind(RunModifierSO mod, Action<RunModifierSO> onSelected)
    {
        _mod = mod;

        if (icon) icon.sprite = mod.icon;
        if (nameText) nameText.text = mod.displayName;
        if (descText) descText.text = mod.description;

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
        _isHover = true;
        ApplyBgVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHover = false;
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

        _bg.color = _isHover ? MultiplyRgb(_bgDefaultColor, hoverTintMult) : _bgDefaultColor;
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