using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SteamLeaderboardUI : MonoBehaviour
{
    enum LeaderboardTab
    {
        Global,
        Friends,
        CurrentRank
    }

    [Header("Steam")]
    [SerializeField] string leaderboardName = SteamLeaderboardService.DefaultLeaderboardName;
    [SerializeField] PlayerCharacterData[] commanderRoster;

    [Header("Prefab Row")]
    [SerializeField] GameObject rowPrefab;
    [SerializeField] Sprite defaultAvatarSprite;
    [SerializeField] Sprite defaultCommanderSprite;

    [Header("Three Leaderboard Panels")]
    [SerializeField] GameObject globalPanel;
    [SerializeField] Transform globalRowsRoot;
    [SerializeField] GameObject friendsPanel;
    [SerializeField] Transform friendsRowsRoot;
    [SerializeField] GameObject currentRankPanel;
    [SerializeField] Transform currentRankRowsRoot;

    [Header("Tabs")]
    [SerializeField] Button globalTabButton;
    [SerializeField] Button friendsTabButton;
    [SerializeField] Button currentRankTabButton;
    [SerializeField] Button refreshButton;
    [SerializeField] Button closeButton;

    [Header("Text")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text statusShadowText;

    SteamLeaderboardSnapshot _snapshot;
    LeaderboardTab _activeTab = LeaderboardTab.Global;
    bool _isRefreshing;

    void Awake()
    {
        ResolveMissingReferences();
        HookButtons();
        SetActiveTab(_activeTab);
    }

    void OnEnable()
    {
        RefreshLeaderboard();
    }

    void OnDestroy()
    {
        var service = SteamLeaderboardService.Instance;
        if (service)
            service.AvatarsChanged -= HandleAvatarsChanged;
    }

    public void SetCommanderRoster(PlayerCharacterData[] roster)
    {
        if (roster != null && roster.Length > 0)
            commanderRoster = roster;
    }

    public void RefreshLeaderboard()
    {
        ResolveMissingReferences();
        ResolveCommanderRoster();

        if (_isRefreshing)
            return;

        _isRefreshing = true;
        SetStatus("Refreshing leaderboards...");
        ShowLoadingRows();

        var service = SteamLeaderboardService.Ensure(leaderboardName);
        service.AvatarsChanged -= HandleAvatarsChanged;
        service.AvatarsChanged += HandleAvatarsChanged;

        service.RefreshAll(commanderRoster, snapshot =>
        {
            _isRefreshing = false;
            _snapshot = snapshot;
            RebuildRows();
            SetStatus(snapshot != null ? snapshot.statusMessage : "Leaderboard refresh failed.");
        });
    }

    void HandleAvatarsChanged()
    {
        if (!isActiveAndEnabled || _snapshot == null)
            return;

        var service = SteamLeaderboardService.Instance;
        if (!service)
            return;

        ApplyAvatarSprites(_snapshot.global, service);
        ApplyAvatarSprites(_snapshot.friends, service);
        ApplyAvatarSprites(_snapshot.currentRank, service);
        RebuildRows();
    }

    void ApplyAvatarSprites(List<SteamLeaderboardEntry> entries, SteamLeaderboardService service)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            var sprite = service.GetAvatarSprite(entries[i].steamId);
            if (sprite)
                entries[i].avatarSprite = sprite;
        }
    }

    void RebuildRows()
    {
        if (_snapshot == null)
        {
            ShowMessageRows("No leaderboard data.");
            return;
        }

        BindRows(globalRowsRoot, _snapshot.global, "No global scores yet.");
        BindRows(friendsRowsRoot, _snapshot.friends, "No friend scores yet.");
        BindRows(currentRankRowsRoot, _snapshot.currentRank, "You are not ranked yet.");
    }

    void BindRows(Transform root, List<SteamLeaderboardEntry> entries, string emptyMessage)
    {
        ClearRows(root);
        if (!root)
            return;

        if (entries == null || entries.Count == 0)
        {
            CreateMessageRow(root, emptyMessage);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var row = CreateRow(root);
            if (!row)
                continue;

            row.Bind(entries[i], i, defaultAvatarSprite, defaultCommanderSprite);
        }
    }

    SteamLeaderboardRowUI CreateRow(Transform root)
    {
        if (!rowPrefab || !root)
            return null;

        var rowGo = Instantiate(rowPrefab, root);
        rowGo.SetActive(true);
        return rowGo.GetComponent<SteamLeaderboardRowUI>() ?? rowGo.AddComponent<SteamLeaderboardRowUI>();
    }

    void CreateMessageRow(Transform root, string message)
    {
        var row = CreateRow(root);
        if (row)
            row.BindMessage(message);
    }

    void ShowLoadingRows()
    {
        ShowMessageRows("Loading...");
    }

    void ShowMessageRows(string message)
    {
        ClearRows(globalRowsRoot);
        ClearRows(friendsRowsRoot);
        ClearRows(currentRankRowsRoot);
        CreateMessageRow(globalRowsRoot, message);
        CreateMessageRow(friendsRowsRoot, message);
        CreateMessageRow(currentRankRowsRoot, message);
    }

    void ClearRows(Transform root)
    {
        if (!root)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    void HookButtons()
    {
        if (globalTabButton)
        {
            globalTabButton.onClick.RemoveListener(ShowGlobal);
            globalTabButton.onClick.AddListener(ShowGlobal);
        }

        if (friendsTabButton)
        {
            friendsTabButton.onClick.RemoveListener(ShowFriends);
            friendsTabButton.onClick.AddListener(ShowFriends);
        }

        if (currentRankTabButton)
        {
            currentRankTabButton.onClick.RemoveListener(ShowCurrentRank);
            currentRankTabButton.onClick.AddListener(ShowCurrentRank);
        }

        if (refreshButton)
        {
            refreshButton.onClick.RemoveListener(RefreshLeaderboard);
            refreshButton.onClick.AddListener(RefreshLeaderboard);
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }
    }

    void ShowGlobal() => SetActiveTab(LeaderboardTab.Global);
    void ShowFriends() => SetActiveTab(LeaderboardTab.Friends);
    void ShowCurrentRank() => SetActiveTab(LeaderboardTab.CurrentRank);

    void SetActiveTab(LeaderboardTab tab)
    {
        _activeTab = tab;

        if (globalPanel) globalPanel.SetActive(tab == LeaderboardTab.Global);
        if (friendsPanel) friendsPanel.SetActive(tab == LeaderboardTab.Friends);
        if (currentRankPanel) currentRankPanel.SetActive(tab == LeaderboardTab.CurrentRank);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

    void SetStatus(string message)
    {
        if (statusText)
            statusText.text = message;

        if (statusShadowText)
            statusShadowText.text = message;
    }

    void ResolveMissingReferences()
    {
        if (!globalPanel)
            globalPanel = FindChildGameObject("globalpanel", "global_panel", "global");
        if (!friendsPanel)
            friendsPanel = FindChildGameObject("friendspanel", "friends_panel", "friends");
        if (!currentRankPanel)
            currentRankPanel = FindChildGameObject("currentrankpanel", "current_rank_panel", "rankpanel");

        if (!globalRowsRoot && globalPanel)
            globalRowsRoot = FindRowsRoot(globalPanel.transform);
        if (!friendsRowsRoot && friendsPanel)
            friendsRowsRoot = FindRowsRoot(friendsPanel.transform);
        if (!currentRankRowsRoot && currentRankPanel)
            currentRankRowsRoot = FindRowsRoot(currentRankPanel.transform);

        if (!statusText)
            statusText = FindText("status");

        if (!statusShadowText)
            statusShadowText = FindText("statusshadow", "status_shadow", "statusshadowtext", "status_shadow_text");

        if (!globalTabButton)
            globalTabButton = FindButton("global");
        if (!friendsTabButton)
            friendsTabButton = FindButton("friends");
        if (!currentRankTabButton)
            currentRankTabButton = FindButton("current", "rank");
        if (!refreshButton)
            refreshButton = FindButton("refresh", "reset");
        if (!closeButton)
            closeButton = FindButton("close");

        if (!globalPanel || !friendsPanel || !currentRankPanel || !globalRowsRoot || !friendsRowsRoot || !currentRankRowsRoot)
            BuildDefaultLayout();

        if (!rowPrefab)
        {
            var rectParent = transform as RectTransform;
            rowPrefab = CreateRuntimeRowTemplate(rectParent).gameObject;
        }
    }

    void BuildDefaultLayout()
    {
        var rootRect = transform as RectTransform;
        if (rootRect)
        {
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(680f, 760f);
        }

        var background = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.075f, 0.07f, 0.96f);

        var frame = CreateRect("Leaderboard_Frame", transform);
        Stretch(frame);

        var frameLayout = frame.gameObject.AddComponent<VerticalLayoutGroup>();
        frameLayout.padding = new RectOffset(18, 18, 16, 18);
        frameLayout.spacing = 10f;
        frameLayout.childControlWidth = true;
        frameLayout.childControlHeight = true;
        frameLayout.childForceExpandWidth = true;
        frameLayout.childForceExpandHeight = false;

        var header = CreateRect("Header", frame);
        AddLayoutElement(header.gameObject, preferredHeight: 72f, flexibleWidth: 1f);

        var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        var title = CreateText("Title_Text", "LEADERBOARDS", header, 34f, TextAlignmentOptions.MidlineLeft);
        AddLayoutElement(title.gameObject, preferredWidth: 330f, flexibleWidth: 1f);

        statusText = CreateText("Status_Text", "Refreshing leaderboards...", header, 18f, TextAlignmentOptions.MidlineRight);
        statusText.color = new Color(1f, 0.94f, 0.2f, 1f);
        AddLayoutElement(statusText.gameObject, preferredWidth: 210f, flexibleWidth: 1f);

        statusShadowText = CreateText("Status_Shadow_Text", "Refreshing leaderboards...", header, 18f, TextAlignmentOptions.MidlineRight);
        statusShadowText.color = new Color(0f, 0f, 0f);
        AddLayoutElement(statusShadowText.gameObject, preferredWidth: 210f, flexibleWidth: 1f);

        refreshButton = CreateButton("Refresh_Button", "Refresh", header, 86f);
        closeButton = CreateButton("Close_Button", "X", header, 46f);

        var tabs = CreateRect("Tabs", frame);
        AddLayoutElement(tabs.gameObject, preferredHeight: 54f, flexibleWidth: 1f);

        var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 6f;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;

        globalTabButton = CreateButton("Global_Tab_Button", "GLOBAL", tabs, 0f);
        friendsTabButton = CreateButton("Friends_Tab_Button", "FRIENDS", tabs, 0f);
        currentRankTabButton = CreateButton("CurrentRank_Tab_Button", "RANK", tabs, 0f);

        var columns = CreateRect("Column_Header", frame);
        AddLayoutElement(columns.gameObject, preferredHeight: 34f, flexibleWidth: 1f);

        var columnsLayout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
        columnsLayout.padding = new RectOffset(8, 8, 0, 0);
        columnsLayout.spacing = 8f;
        columnsLayout.childControlWidth = true;
        columnsLayout.childControlHeight = true;
        columnsLayout.childForceExpandWidth = false;
        columnsLayout.childForceExpandHeight = true;

        AddLayoutElement(CreateText("Rank_Header_Text", "RANK", columns, 19f, TextAlignmentOptions.MidlineLeft).gameObject, preferredWidth: 68f);
        AddLayoutElement(CreateText("Avatar_Header_Space", "", columns, 19f, TextAlignmentOptions.MidlineLeft).gameObject, preferredWidth: 38f);
        AddLayoutElement(CreateText("Player_Header_Text", "PLAYER", columns, 19f, TextAlignmentOptions.MidlineLeft).gameObject, flexibleWidth: 1f);
        AddLayoutElement(CreateText("Score_Header_Text", "SCORE", columns, 19f, TextAlignmentOptions.MidlineRight).gameObject, preferredWidth: 142f);
        AddLayoutElement(CreateText("Commander_Header_Space", "", columns, 19f, TextAlignmentOptions.MidlineRight).gameObject, preferredWidth: 40f);

        var panelHost = CreateRect("Panel_Host", frame);
        AddLayoutElement(panelHost.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);

        globalPanel = CreateLeaderboardPanel("GlobalPanel", "GlobalRows_Content", panelHost, out globalRowsRoot);
        friendsPanel = CreateLeaderboardPanel("FriendsPanel", "FriendsRows_Content", panelHost, out friendsRowsRoot);
        currentRankPanel = CreateLeaderboardPanel("CurrentRankPanel", "CurrentRankRows_Content", panelHost, out currentRankRowsRoot);

        if (!rowPrefab)
            rowPrefab = CreateRuntimeRowTemplate(frame).gameObject;
    }

    RectTransform CreateRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    GameObject CreateLeaderboardPanel(string panelName, string rowsName, RectTransform parent, out Transform rowsRoot)
    {
        var panel = CreateRect(panelName, parent);
        Stretch(panel);

        var rows = CreateRect(rowsName, panel);
        Stretch(rows);
        rowsRoot = rows;

        var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel.gameObject;
    }

    Button CreateButton(string objectName, string label, RectTransform parent, float preferredWidth)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.14f, 0.18f, 0.16f, 1f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.23f, 0.31f, 0.27f, 1f);
        colors.pressedColor = new Color(0.08f, 0.11f, 0.1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        AddLayoutElement(go, preferredWidth: preferredWidth, preferredHeight: 42f, flexibleWidth: preferredWidth <= 0f ? 1f : 0f);

        var text = CreateText("Label_Text", label, go.transform, 20f, TextAlignmentOptions.Center);
        text.color = Color.white;
        Stretch(text.rectTransform);

        return button;
    }

    TextMeshProUGUI CreateText(string objectName, string value, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, fontSize - 7f);
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    SteamLeaderboardRowUI CreateRuntimeRowTemplate(RectTransform parent)
    {
        var row = new GameObject("SteamLeaderboardRow_RuntimeTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup), typeof(SteamLeaderboardRowUI));
        row.transform.SetParent(parent ? parent : transform, false);
        row.SetActive(false);

        var rowUi = row.GetComponent<SteamLeaderboardRowUI>();
        rowUi.BindMessage("Loading...");
        return rowUi;
    }

    void AddLayoutElement(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
    {
        var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = flexibleWidth;
        layout.flexibleHeight = flexibleHeight;
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    GameObject FindChildGameObject(params string[] nameParts)
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string n = transforms[i].name.Replace(" ", "").ToLowerInvariant();
            for (int j = 0; j < nameParts.Length; j++)
            {
                if (n.Contains(nameParts[j]))
                    return transforms[i].gameObject;
            }
        }

        return null;
    }

    Transform FindRowsRoot(Transform panel)
    {
        var transforms = panel.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string n = transforms[i].name.Replace(" ", "").ToLowerInvariant();
            if (n.Contains("rows") || n.Contains("content"))
                return transforms[i];
        }

        return panel;
    }

    TMP_Text FindText(params string[] nameParts)
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string n = texts[i].name.ToLowerInvariant();
            for (int j = 0; j < nameParts.Length; j++)
            {
                if (n.Contains(nameParts[j]))
                    return texts[i];
            }
        }

        return null;
    }

    Button FindButton(params string[] nameParts)
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string n = buttons[i].name.ToLowerInvariant();
            bool matchedAll = true;
            for (int j = 0; j < nameParts.Length; j++)
            {
                if (!n.Contains(nameParts[j]))
                {
                    matchedAll = false;
                    break;
                }
            }

            if (matchedAll)
                return buttons[i];
        }

        return null;
    }

    void ResolveCommanderRoster()
    {
        if (commanderRoster != null && commanderRoster.Length > 0)
            return;

        var characterSelect = FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (characterSelect && characterSelect.roster != null && characterSelect.roster.Length > 0)
            commanderRoster = characterSelect.roster;
    }
}
