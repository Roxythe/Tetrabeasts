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
    public void Show(int level, Pair[] options, Action<RunModifierSO, RunModifierSO> callback)
    {
        pairs = options; onConfirm = callback; selected = -1;
        title.text = "Level " + level + " — Choose a pair";
        confirmButton.interactable = false;
        for (int i = 0; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(i < pairs.Length);
            if (i >= pairs.Length) continue;
            var p = pairs[i];
            choiceLabels[i].text = $"{Rarity(p.buff)}\n\n+ {p.buff.displayName}\n{p.buff.description}\n\n− {p.debuff.displayName}\n{p.debuff.description}";
        }
        gameObject.SetActive(true); transform.SetAsLastSibling();
        var navigator = GetComponent<ScopedMenuNavigator>() ?? gameObject.AddComponent<ScopedMenuNavigator>();
        navigator.SetNavigationRoot(gameObject);
        if (EventSystem.current && pairs.Length > 0) EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }
    void Select(int index)
    {
        selected = index; confirmButton.interactable = true;
        for (int i = 0; i < choices.Length; i++)
            choices[i].GetComponent<Image>().color = i == selected ? new Color(0.4f, 0.22f, 0.65f) : new Color(0.12f, 0.09f, 0.2f);
    }
    void Confirm()
    {
        if (selected < 0 || selected >= pairs.Length) return;
        var pair = pairs[selected]; gameObject.SetActive(false); onConfirm?.Invoke(pair.buff, pair.debuff);
    }
}
