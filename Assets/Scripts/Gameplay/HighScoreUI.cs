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
    }

    public void TryShow(int score)
    {
        pendingScore = score;
        RefreshTable();

        bool qualifies = HighScoreManager.IsHighScore(score);
        ForceActivate();

        // Bring to front so nothing covers it
        //panel.transform.SetAsLastSibling();

        if (nameInput) nameInput.gameObject.SetActive(qualifies);
        if (submitButton) submitButton.gameObject.SetActive(qualifies);

        Debug.Log($"[HS] TryShow: score={score}, qualifies={qualifies}, " +
                  $"panelActiveSelf={panel.activeSelf}, compGOActiveSelf={gameObject.activeSelf}, " +
                  $"panelID={panel.GetInstanceID()}, compGOID={gameObject.GetInstanceID()}");
    }

    void ForceActivate()
    {
        // Activate the component's GO
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel && !panel.activeSelf) panel.SetActive(true);

        // If there's a CanvasGroup, make sure it's visible & clickable
        var cg = (panel ? panel : gameObject).GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
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
