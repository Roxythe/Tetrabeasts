using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [Header("Clips")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    Button _button;
    bool _clickWired;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void OnEnable()
    {
        // Wire click once per instance
        if (_button && !_clickWired)
        {
            _button.onClick.AddListener(PlayClick);
            _clickWired = true;
        }
    }

    void PlayClick()
    {
        if (clickClip && AudioManager.I)
            AudioManager.I.PlayUISFX(clickClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Don't play hover SFX for disabled/unclickable buttons
        if (!_button) return;
        if (!_button.isActiveAndEnabled) return;
        if (!_button.IsInteractable()) return;

        if (hoverClip && AudioManager.I)
            AudioManager.I.PlayUISFX(hoverClip, 1f, 1f);
    }

}
