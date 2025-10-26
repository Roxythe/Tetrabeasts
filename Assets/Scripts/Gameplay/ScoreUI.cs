using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TMP_Text scoreText;

    public void Set(int value)
    {
        if (scoreText) scoreText.text = $"Score: {value}";
    }
}
