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

    [Header("Post-Submit Buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button closeButton;

    [Header("Table")]
    public RectTransform tableRoot;
    public GameObject rowPrefab; // three TMP_Text children: Rank, Name, Score

    int pendingScore = 0;
    int _highlightRankIndex = -1;

    [Header("Colors")]
    public Color newHighScoreColor = new Color(1f, 0.84f, 0f, 1f); // Gold
    public Color normalRowColor = Color.white;

    void Awake()
    {
        if (!panel) panel = gameObject;
        if (submitButton) submitButton.onClick.AddListener(Submit);
    }

    public void TryShow(int score)
    {
        pendingScore = score;
        _highlightRankIndex = -1;
        RefreshTable();

        bool qualifies = HighScoreManager.IsHighScore(score);
        ForceActivate();

        if (qualifies && TryGetSteamPlayerName(out string steamPlayerName))
            SubmitScore(steamPlayerName);
        else
        {
            SetSubmissionUIActive(qualifies); // Name submission UI
            SetPostSubmitButtonsActive(!qualifies);
        }

        SetCloseButtonActive(false);

        Debug.Log($"[HS] TryShow: score={score}, qualifies={qualifies}, " +
                  $"panelActiveSelf={panel.activeSelf}, compGOActiveSelf={gameObject.activeSelf}, " +
                  $"panelID={panel.GetInstanceID()}, compGOID={gameObject.GetInstanceID()}");
    }

    void ForceActivate()
    {
        bool hasSeparatePanel = panel && panel != gameObject;
        if (!UIPanelTransition.IsVisible(gameObject)) UIPanelTransition.Show(gameObject, hasSeparatePanel);
        if (panel && !UIPanelTransition.IsVisible(panel)) UIPanelTransition.Show(panel);

        var cg = (panel ? panel : gameObject).GetComponent<CanvasGroup>();
        if (cg) { cg.interactable = true; cg.blocksRaycasts = true; }
    }

    void Submit()
    {
        SubmitScore(nameInput ? nameInput.text : "Player");
    }

    void SubmitScore(string playerName)
    {
        _highlightRankIndex = HighScoreManager.Add(playerName, pendingScore);
        RefreshTable();
        SetSubmissionUIActive(false);
        SetPostSubmitButtonsActive(true);
    }

    bool TryGetSteamPlayerName(out string playerName)
    {
        playerName = null;

        var steam = SteamLeaderboardService.Ensure();
        return steam && steam.TryGetLocalPlayerName(out playerName);
    }

    void SetSubmissionUIActive(bool active)
    {
        if (nameInput) nameInput.gameObject.SetActive(active);
        if (submitButton) submitButton.gameObject.SetActive(active);
    }

    void SetPostSubmitButtonsActive(bool active)
    {
        if (restartButton) restartButton.gameObject.SetActive(active);
        if (mainMenuButton) mainMenuButton.gameObject.SetActive(active);
    }

    public void Hide()
    {
        if (panel) UIPanelTransition.Hide(panel);
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

                bool highlight = (i == _highlightRankIndex);
                var c = highlight ? newHighScoreColor : normalRowColor;

                texts[0].color = c;
                texts[1].color = c;
                texts[2].color = c;
            }
        }
    }

    public void ShowReadOnly()
    {
        ForceActivate();
        RefreshTable();

        // Title menu read-only: no submit, no restart/main menu
        SetSubmissionUIActive(false);
        SetPostSubmitButtonsActive(false);
        SetCloseButtonActive(true);
    }

    void SetCloseButtonActive(bool active)
    {
        if (closeButton) closeButton.gameObject.SetActive(active);
    }
}
