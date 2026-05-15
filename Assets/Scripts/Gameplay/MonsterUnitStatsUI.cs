using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MonsterUnitStatsUI : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] Image backgroundImage;
    [SerializeField] Image monsterBackgroundImage;
    [SerializeField] Image monsterImage;

    [Header("Text")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text nameShadowText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text levelShadowText;
    [SerializeField] TMP_Text stats1Text;
    [SerializeField] TMP_Text stats1ShadowText;
    [SerializeField] TMP_Text stats2Text;
    [SerializeField] TMP_Text stats2ShadowText;

    [Header("Attack Colors")]
    [SerializeField] Color attackBackgroundColor = new Color(0.55f, 0.12f, 0.10f, 1f);
    [SerializeField] Color attackMonsterBackgroundColor = new Color(0.95f, 0.25f, 0.20f, 1f);

    [Header("Defender Colors")]
    [SerializeField] Color defenderBackgroundColor = new Color(0.10f, 0.24f, 0.58f, 1f);
    [SerializeField] Color defenderMonsterBackgroundColor = new Color(0.22f, 0.46f, 0.95f, 1f);

    [Header("Healer Colors")]
    [SerializeField] Color healerBackgroundColor = new Color(0.10f, 0.42f, 0.18f, 1f);
    [SerializeField] Color healerMonsterBackgroundColor = new Color(0.18f, 0.82f, 0.34f, 1f);

    bool _bound;

    public void Bind(MonsterData monster, Sprite portrait, int level, string stats1, string stats2)
    {
        EnsureReferences();

        string monsterName = monster ? monster.monsterName : "Monster";
        SetText(nameText, nameShadowText, monsterName);
        SetText(levelText, levelShadowText, $"Level {Mathf.Max(1, level)}");
        SetText(stats1Text, stats1ShadowText, stats1);
        SetText(stats2Text, stats2ShadowText, stats2);

        if (monsterImage)
        {
            monsterImage.sprite = portrait;
            monsterImage.preserveAspect = true;
            monsterImage.raycastTarget = false;
        }

        ApplyRoleColors(monster ? monster.role : MonsterRole.Attack);
    }

    void EnsureReferences()
    {
        if (_bound)
            return;

        backgroundImage = backgroundImage ? backgroundImage : FindImage("Background_Image");
        monsterBackgroundImage = monsterBackgroundImage ? monsterBackgroundImage : FindImage("MonsterBG_Image");
        monsterImage = monsterImage ? monsterImage : FindImage("Monster_Image");

        nameText = nameText ? nameText : FindText("Name_Text");
        nameShadowText = nameShadowText ? nameShadowText : FindText("NameShadow_Text");
        levelText = levelText ? levelText : FindText("Level_Text");
        levelShadowText = levelShadowText ? levelShadowText : FindText("LevelShadow_Text");
        stats1Text = stats1Text ? stats1Text : FindText("Stats1_Text");
        stats1ShadowText = stats1ShadowText ? stats1ShadowText : FindText("Stats1Shadow_Text");
        stats2Text = stats2Text ? stats2Text : FindText("Stats2_Text");
        stats2ShadowText = stats2ShadowText ? stats2ShadowText : FindText("Stats2Shadow_Text");

        ConfigureText(nameText, TextWrappingModes.NoWrap, useAutoSizing: true);
        ConfigureText(nameShadowText, TextWrappingModes.NoWrap, useAutoSizing: true);
        ConfigureText(levelText, TextWrappingModes.NoWrap, useAutoSizing: true);
        ConfigureText(levelShadowText, TextWrappingModes.NoWrap, useAutoSizing: true);
        ConfigureText(stats1Text, TextWrappingModes.NoWrap, useAutoSizing: false);
        ConfigureText(stats1ShadowText, TextWrappingModes.NoWrap, useAutoSizing: false);
        ConfigureText(stats2Text, TextWrappingModes.NoWrap, useAutoSizing: false);
        ConfigureText(stats2ShadowText, TextWrappingModes.NoWrap, useAutoSizing: false);
        MatchStatTextSizing();

        if (backgroundImage) backgroundImage.raycastTarget = false;
        if (monsterBackgroundImage) monsterBackgroundImage.raycastTarget = false;

        _bound = true;
    }

    void ApplyRoleColors(MonsterRole role)
    {
        Color background = attackBackgroundColor;
        Color monsterBackground = attackMonsterBackgroundColor;

        switch (role)
        {
            case MonsterRole.Defense:
                background = defenderBackgroundColor;
                monsterBackground = defenderMonsterBackgroundColor;
                break;

            case MonsterRole.Healer:
                background = healerBackgroundColor;
                monsterBackground = healerMonsterBackgroundColor;
                break;
        }

        if (backgroundImage)
            backgroundImage.color = background;

        if (monsterBackgroundImage)
            monsterBackgroundImage.color = monsterBackground;
    }

    void SetText(TMP_Text main, TMP_Text shadow, string value)
    {
        if (main)
            main.text = value ?? string.Empty;

        if (shadow)
            shadow.text = value ?? string.Empty;
    }

    void ConfigureText(TMP_Text text, TextWrappingModes wrapping, bool useAutoSizing)
    {
        if (!text)
            return;

        text.raycastTarget = false;
        text.textWrappingMode = wrapping;
        text.enableAutoSizing = useAutoSizing;
        text.fontSizeMin = Mathf.Min(text.fontSizeMin, 18f);
        text.fontSizeMax = Mathf.Max(text.fontSizeMax, text.fontSize);
    }

    void MatchStatTextSizing()
    {
        TMP_Text source = stats1Text ? stats1Text : stats1ShadowText;
        if (!source)
            return;

        CopyStatTextSizing(source, stats1Text);
        CopyStatTextSizing(source, stats1ShadowText);
        CopyStatTextSizing(source, stats2Text);
        CopyStatTextSizing(source, stats2ShadowText);
    }

    void CopyStatTextSizing(TMP_Text source, TMP_Text target)
    {
        if (!source || !target)
            return;

        target.font = source.font;
        target.fontSize = source.fontSize;
        target.fontSizeMin = source.fontSizeMin;
        target.fontSizeMax = source.fontSizeMax;
        target.enableAutoSizing = false;
        target.fontStyle = source.fontStyle;
        target.lineSpacing = source.lineSpacing;
        target.paragraphSpacing = source.paragraphSpacing;
    }

    Image FindImage(string childName)
    {
        Transform child = FindChildByPrefix(childName);
        return child ? child.GetComponent<Image>() : null;
    }

    TMP_Text FindText(string childName)
    {
        Transform child = FindChildByPrefix(childName);
        return child ? child.GetComponent<TMP_Text>() : null;
    }

    Transform FindChildByPrefix(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child.name == childName || child.name.StartsWith(childName + " "))
                return child;
        }

        return null;
    }
}
