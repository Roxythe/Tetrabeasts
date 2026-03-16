using TMPro;
using UnityEngine;

public class BuffPopupView : MonoBehaviour
{
    public TMP_Text mainText;
    public TMP_Text shadowText;

    public RectTransform RectTransform => transform as RectTransform;
    public bool IsValid => mainText != null && shadowText != null;
}