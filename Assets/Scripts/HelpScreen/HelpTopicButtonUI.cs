using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpTopicButtonUI : MonoBehaviour
{
    public TMP_Text label;
    public Button button;
    public RectTransform indentRoot; // Assign this to the button's main container

    public void SetTopic(HelpTopicSO topic, Action onClick)
    {
        if (label) label.text = topic ? topic.title : "(Missing)";

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        // Indent for sub-items
        if (indentRoot)
        {
            var p = indentRoot.anchoredPosition;
            indentRoot.anchoredPosition = new Vector2(24f, p.y); // Adjust indent here
        }
    }
}
