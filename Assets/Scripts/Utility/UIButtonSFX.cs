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
        if (hoverClip && AudioManager.I)
            AudioManager.I.PlayUISFX(hoverClip, 1f, 1f);
    }
}
