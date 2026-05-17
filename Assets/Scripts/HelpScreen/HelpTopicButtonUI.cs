using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpTopicButtonUI : MonoBehaviour
{
    public TMP_Text label;
    public Button button;
    public RectTransform indentRoot; // Assign this to the button's main container
    public float indentPixels = 24f;

    public void SetTopic(HelpTopicSO topic, Action onClick)
    {
        if (label) label.text = TetrabeastsLocalization.LocalizeText(topic ? topic.title : "(Missing)");

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        if (indentRoot)
        {
            // If stretched horizontally, use offsets
            if (indentRoot.anchorMin.x == 0f && indentRoot.anchorMax.x == 1f)
            {
                var min = indentRoot.offsetMin;
                min.x = indentPixels;
                indentRoot.offsetMin = min;
            }
            else
            {
                // Non-stretch fallback
                var p = indentRoot.anchoredPosition;
                indentRoot.anchoredPosition = new Vector2(indentPixels, p.y);
            }
        }
    }
}
