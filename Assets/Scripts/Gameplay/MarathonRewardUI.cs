using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class MarathonRewardUI : MonoBehaviour
{
    public Button[] choices;
    public TMP_Text[] choiceLabels;
    public TMP_Text[] buffLabels, debuffLabels;
    public TMP_Text[] buffTitles, debuffTitles;
    public Image[] buffBackgrounds, debuffBackgrounds;
    public MarathonRewardCard[] cards;
    public MarathonRewardColors colors;
    public Image[] buffIcons, debuffIcons;
    public Outline[] selectionOutlines;
    [HideInInspector] public Color buffTextColor = new Color(0.3f, 0.6f, 1f);
    [HideInInspector] public Color debuffTextColor = new Color(1f, 0.22f, 0.25f);
    public Button confirmButton;
    public TMP_Text title;
    public struct Pair { public RunModifierSO buff, debuff; }
    Pair[] pairs;
    int selected = -1;
    Action<RunModifierSO, RunModifierSO> onConfirm;
    void Awake()
    {
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            choices[i].onClick.AddListener(() => Select(index));
        }
        confirmButton.onClick.AddListener(Confirm);
    }
    public static Pair[] RollPairs(RunModifierSO[] buffs, RunModifierSO[] debuffs)
    {
        var available = new List<Pair>();
        if (buffs != null && debuffs != null)
            foreach (var buff in buffs)
            foreach (var debuff in debuffs)
                if (buff && debuff && Rarity(buff) == Rarity(debuff)) available.Add(new Pair { buff = buff, debuff = debuff });
        var result = new List<Pair>();
        float[] weights = { 55f, 25f, 12f, 6f, 2f };
        for (int i = 0; i < 3 && available.Count > 0; i++)
        {
            var rarities = new List<RunModRarity>();
            foreach (var pair in available) if (!rarities.Contains(Rarity(pair.buff))) rarities.Add(Rarity(pair.buff));
            float total = 0f;
            foreach (var r in rarities) total += weights[(int)r];
            float roll = UnityEngine.Random.value * total;
            var rarity = rarities[rarities.Count - 1];
            foreach (var r in rarities) { roll -= weights[(int)r]; if (roll <= 0f) { rarity = r; break; } }
            var candidates = available.FindAll(p => Rarity(p.buff) == rarity);
            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            result.Add(chosen); available.Remove(chosen);
        }
        return result.ToArray();
    }
    public static RunModRarity Rarity(RunModifierSO modifier) => modifier is RunModifier rm ? rm.rarity : RunModRarity.Common;
    public static Color RarityColor(RunModRarity rarity)
    {
        switch (rarity)
        {
            case RunModRarity.Uncommon: return Color.green;
            case RunModRarity.Rare: return new Color(0.3f, 0.6f, 1f);
            case RunModRarity.Epic: return new Color(0.75f, 0.3f, 1f);
            case RunModRarity.Legendary: return new Color(1f, 0.75f, 0.2f);
            default: return Color.white;
        }
    }
    public void Show(int level, Pair[] options, Action<RunModifierSO, RunModifierSO> callback)
    {
        pairs = options; onConfirm = callback; selected = -1;
        title.text = "Level " + level + " — Choose a Modifier";
        confirmButton.interactable = false;
        for (int i = 0; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(i < pairs.Length);
            if (i >= pairs.Length) continue;
            var p = pairs[i];
            var color = colors ? colors.GetRarity(Rarity(p.buff)) : RarityColor(Rarity(p.buff));
            choiceLabels[i].text = Rarity(p.buff).ToString();
            choiceLabels[i].color = color;
            buffTitles[i].text = p.buff.displayName;
            debuffTitles[i].text = p.debuff.displayName;
            buffLabels[i].text = p.buff.description;
            debuffLabels[i].text = p.debuff.description;
            buffTitles[i].color = colors.buffTitle; debuffTitles[i].color = colors.debuffTitle;
            buffLabels[i].color = colors.buffDescription; debuffLabels[i].color = colors.debuffDescription;
            buffBackgrounds[i].color = colors.buffBackground; debuffBackgrounds[i].color = colors.debuffBackground;
            buffIcons[i].sprite = p.buff.icon; buffIcons[i].enabled = p.buff.icon;
            debuffIcons[i].sprite = p.debuff.icon; debuffIcons[i].enabled = p.debuff.icon;
            choices[i].GetComponent<Image>().color = color;
            selectionOutlines[i].enabled = false;
            cards[i].ResetState();
        }
        gameObject.SetActive(true); transform.SetAsLastSibling();
        foreach (var shadow in GetComponentsInChildren<TMPLabelShadow>(true)) shadow.Sync();
        var navigator = GetComponent<ScopedMenuNavigator>() ?? gameObject.AddComponent<ScopedMenuNavigator>();
        navigator.SetNavigationRoot(gameObject);
        if (EventSystem.current && pairs.Length > 0) EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }
    void Select(int index)
    {
        selected = index; confirmButton.interactable = true;
        for (int i = 0; i < choices.Length; i++)
            cards[i].SetSelected(i == selected);
    }
    void Confirm()
    {
        if (selected < 0 || selected >= pairs.Length) return;
        var pair = pairs[selected]; gameObject.SetActive(false); onConfirm?.Invoke(pair.buff, pair.debuff);
    }
}
