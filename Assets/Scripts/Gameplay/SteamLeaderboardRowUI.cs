using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SteamLeaderboardRowUI : MonoBehaviour
{
    [Header("Required Row Fields")]
    [SerializeField] TMP_Text rankText;
    [SerializeField] Image profilePicImage;
    [SerializeField] TMP_Text userNameText;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] Image commanderImage;

    [Header("Visuals")]
    [SerializeField] Image rowBackground;
    [SerializeField] Color normalBackground = new Color(0.14f, 0.16f, 0.18f, 0.88f);
    [SerializeField] Color alternateBackground = new Color(0.22f, 0.24f, 0.28f, 0.88f);
    [SerializeField] Color localPlayerBackground = new Color(0.25f, 0.32f, 0.42f, 0.96f);
    [SerializeField] Color normalText = Color.white;
    [SerializeField] Color scoreTextColor = new Color(1f, 0.95f, 0f, 1f);
    [SerializeField] Color placeholderText = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Column Layout")]
    [SerializeField] float rowHeight = 42f;
    [SerializeField] float rankColumnWidth = 220f;
    [SerializeField] float profilePicColumnWidth = 34f;
    [SerializeField] float profilePicSize = 30f;
    [SerializeField] float playerNamePreferredWidth = 230f;
    [SerializeField] float playerNameMinWidth = 90f;
    [SerializeField] float scoreColumnWidth = 118f;
    [SerializeField] float commanderColumnWidth = 36f;
    [SerializeField] float commanderImageSize = 32f;
    [SerializeField] float columnSpacing = 6f;
    [SerializeField] int horizontalPadding = 8;
    [SerializeField] int verticalPadding = 3;
    [SerializeField] float textFontSize = 24f;

    void Awake()
    {
        ResolveMissingReferences();
    }

    void OnValidate()
    {
        ResolveMissingReferences();
    }

    public void Bind(SteamLeaderboardEntry entry, int rowIndex, Sprite defaultAvatar, Sprite defaultCommander)
    {
        ResolveMissingReferences();

        bool hasEntry = entry != null && entry.HasScore;
        bool local = entry != null && entry.isLocalPlayer;

        if (rankText)
        {
            rankText.text = hasEntry ? $"#{entry.rank}" : "-";
            rankText.color = hasEntry ? normalText : placeholderText;
        }

        if (userNameText)
        {
            userNameText.text = hasEntry ? entry.playerName : "No entries";
            userNameText.color = hasEntry ? normalText : placeholderText;
        }

        if (scoreText)
        {
            scoreText.text = hasEntry ? FormatScore(entry.score) : "-";
            scoreText.color = hasEntry ? scoreTextColor : placeholderText;
        }

        if (profilePicImage)
        {
            profilePicImage.sprite = hasEntry && entry.avatarSprite ? entry.avatarSprite : defaultAvatar;
            profilePicImage.enabled = profilePicImage.sprite != null;
            profilePicImage.preserveAspect = true;
        }

        if (commanderImage)
        {
            commanderImage.sprite = hasEntry && entry.commanderSprite ? entry.commanderSprite : defaultCommander;
            commanderImage.enabled = commanderImage.sprite != null;
            commanderImage.preserveAspect = true;
        }

        if (rowBackground)
            rowBackground.color = local ? localPlayerBackground : ((rowIndex % 2 == 0) ? alternateBackground : normalBackground);
    }

    public void BindMessage(string message)
    {
        ResolveMissingReferences();

        if (rankText) rankText.text = "-";
        if (userNameText) userNameText.text = TetrabeastsLocalization.LocalizeText(message);
        if (scoreText) scoreText.text = "-";
        if (profilePicImage) profilePicImage.enabled = false;
        if (commanderImage) commanderImage.enabled = false;
        if (rowBackground) rowBackground.color = normalBackground;
    }

    static string FormatScore(int score)
    {
        return score.ToString("N0");
    }

    void ResolveMissingReferences()
    {
        if (!rankText)
            rankText = FindText("rank");
        if (!userNameText)
            userNameText = FindText("name", "player", "user");
        if (!scoreText)
            scoreText = FindText("score", "kills");
        if (!profilePicImage)
            profilePicImage = FindImage("profile", "avatar", "pic");
        if (!commanderImage)
            commanderImage = FindImage("commander", "character");
        if (!rowBackground)
            rowBackground = GetComponent<Image>();

        if (!rankText || !profilePicImage || !userNameText || !scoreText || !commanderImage)
            BuildDefaultLayout();

        ApplyColumnLayout();
    }

    void BuildDefaultLayout()
    {
        if (!rowBackground)
            rowBackground = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();

        rowBackground.color = normalBackground;

        var layout = gameObject.GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();

        if (!rankText)
            rankText = CreateText("Rank_Text", "#1", rankColumnWidth, 0f, TextAlignmentOptions.MidlineLeft);

        if (!profilePicImage)
            profilePicImage = CreateImage("PlayerIcon_Image", profilePicColumnWidth, profilePicSize);

        if (!userNameText)
            userNameText = CreateText("PlayerName_Text", "Player", playerNamePreferredWidth, 1f, TextAlignmentOptions.MidlineLeft);

        if (!scoreText)
            scoreText = CreateText("Score_Text", "0", scoreColumnWidth, 0f, TextAlignmentOptions.MidlineRight);

        if (!commanderImage)
            commanderImage = CreateImage("CommanderIcon_Image", commanderColumnWidth, commanderImageSize);

        ApplyColumnLayout();
    }

    TMP_Text CreateText(string objectName, string initialText, float preferredWidth, float flexibleWidth, TextAlignmentOptions alignment)
    {
        var child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        child.transform.SetParent(transform, false);

        var text = child.GetComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = 24f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = textFontSize;
        text.alignment = alignment;
        text.color = normalText;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var layout = child.GetComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.flexibleWidth = flexibleWidth;
        layout.preferredHeight = 36f;

        return text;
    }

    Image CreateImage(string objectName, float columnWidth, float size)
    {
        var child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        child.transform.SetParent(transform, false);

        var image = child.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.88f);
        image.preserveAspect = true;
        image.raycastTarget = false;

        var layout = child.GetComponent<LayoutElement>();
        layout.preferredWidth = columnWidth;
        layout.preferredHeight = size;
        layout.minWidth = columnWidth;
        layout.minHeight = size;

        return image;
    }

    void ApplyColumnLayout()
    {
        var layout = gameObject.GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
        layout.spacing = columnSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var rootLayout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        rootLayout.preferredHeight = rowHeight;
        rootLayout.flexibleWidth = 1f;

        ConfigureText(rankText, rankColumnWidth, rankColumnWidth, 0f, TextAlignmentOptions.MidlineLeft);
        ConfigureImage(profilePicImage, profilePicColumnWidth, profilePicSize);
        ConfigureText(userNameText, playerNameMinWidth, playerNamePreferredWidth, 1f, TextAlignmentOptions.MidlineLeft);
        ConfigureText(scoreText, scoreColumnWidth, scoreColumnWidth, 0f, TextAlignmentOptions.MidlineRight);
        ConfigureImage(commanderImage, commanderColumnWidth, commanderImageSize);
    }

    void ConfigureText(TMP_Text text, float minWidth, float preferredWidth, float flexibleWidth, TextAlignmentOptions alignment)
    {
        if (!text)
            return;

        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = textFontSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.margin = Vector4.zero;

        var layout = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = minWidth;
        layout.preferredWidth = preferredWidth;
        layout.flexibleWidth = flexibleWidth;
        layout.preferredHeight = rowHeight - (verticalPadding * 2f);
        layout.flexibleHeight = 0f;

        var rect = text.rectTransform;
        rect.sizeDelta = new Vector2(preferredWidth, rowHeight - (verticalPadding * 2f));
    }

    void ConfigureImage(Image image, float columnWidth, float size)
    {
        if (!image)
            return;

        image.preserveAspect = true;
        image.raycastTarget = false;

        var layout = image.GetComponent<LayoutElement>() ?? image.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = columnWidth;
        layout.preferredWidth = columnWidth;
        layout.flexibleWidth = 0f;
        layout.minHeight = size;
        layout.preferredHeight = size;
        layout.flexibleHeight = 0f;

        image.rectTransform.sizeDelta = new Vector2(size, size);
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

    Image FindImage(params string[] nameParts)
    {
        var images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject == gameObject)
                continue;

            string n = images[i].name.ToLowerInvariant();
            for (int j = 0; j < nameParts.Length; j++)
            {
                if (n.Contains(nameParts[j]))
                    return images[i];
            }
        }

        return null;
    }
}
