using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpCategoryHeaderUI : MonoBehaviour
{
    public TMP_Text label;
    public Button button;
    public RectTransform arrow;
    public float collapsedZ = -90f;
    public float expandedZ = 90f;

    bool _expanded = false;
    Action _onToggle;


    public bool IsExpanded => _expanded;

    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        UpdateArrow();
    }

    public void SetLabel(string text)
    {
        if (label) label.text = text;
    }

    public void SetOnToggle(Action onToggle)
    {
        _onToggle = onToggle;

        UpdateArrow();

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                _expanded = !_expanded;
                UpdateArrow();
                _onToggle?.Invoke();
            });
        }
    }

    void UpdateArrow()
    {
        if (!arrow) return;
        arrow.localEulerAngles = new Vector3(0f, 0f, _expanded ? expandedZ : collapsedZ);
    }
}
