using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HighScoreUI : MonoBehaviour
{
    [Header("Panel + Inputs")]
    public GameObject panel;
    public TMP_InputField nameInput;
    public Button submitButton;

    [Header("Table")]
    public RectTransform tableRoot;
    public GameObject rowPrefab; // three TMP_Text children: Rank, Name, Score

    int pendingScore = 0;

    void Awake()
    {
        if (!panel) panel = gameObject;
        if (submitButton) submitButton.onClick.AddListener(Submit);
        Hide();
    }

    public void TryShow(int score)
    {
        pendingScore = score;
        RefreshTable();
        bool qualifies = HighScoreManager.IsHighScore(score);
        Show(qualifies);
    }

    void Submit()
    {
        HighScoreManager.Add(nameInput ? nameInput.text : "Player", pendingScore);
        RefreshTable();
        Show(false);
    }

    void Show(bool askForName)
    {
        if (!panel) return;
        panel.SetActive(true);
        if (nameInput) nameInput.gameObject.SetActive(askForName);
        if (submitButton) submitButton.gameObject.SetActive(askForName);
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }

    public void RefreshTable()
    {
        foreach (Transform c in tableRoot) Destroy(c.gameObject);
        var list = HighScoreManager.LoadTop(10, pad: true);

        for (int i = 0; i < list.Count; i++)
        {
            var row = Instantiate(rowPrefab, tableRoot);
            var texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();        // Rank
                texts[1].text = list[i].name;              // Name
                texts[2].text = list[i].score.ToString();  // Score
            }
        }
    }
}
