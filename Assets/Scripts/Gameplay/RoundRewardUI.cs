using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundRewardUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject rootPanel;
    public GameObject buffPanel;
    public GameObject debuffPanel;

    [Header("Buff UI")]
    public Transform buffContainer;
    public Button confirmBuffButton;

    [Header("Debuff UI")]
    public Transform debuffContainer;
    public Button confirmDebuffButton;

    [Header("Prefabs")]
    public RunModOptionButton optionButtonPrefab;

    RunModifierSO _selectedBuff;
    RunModifierSO _selectedDebuff;

    Action<RunModifierSO, RunModifierSO> _onComplete;

    public void Show(
        RunModifierSO[] buffPool,
        RunModifierSO[] debuffPool,
        Action<RunModifierSO, RunModifierSO> onComplete)
    {
        _onComplete = onComplete;

        rootPanel.SetActive(true);
        buffPanel.SetActive(true);
        debuffPanel.SetActive(false);

        confirmBuffButton.interactable = false;
        confirmDebuffButton.interactable = false;

        _selectedBuff = null;
        _selectedDebuff = null;

        Populate(buffContainer, Pick3Unique(buffPool), isBuff: true);
        confirmBuffButton.onClick.RemoveAllListeners();
        confirmBuffButton.onClick.AddListener(() =>
        {
            buffPanel.SetActive(false);
            debuffPanel.SetActive(true);

            Populate(debuffContainer, Pick3Unique(debuffPool), isBuff: false);
        });

        confirmDebuffButton.onClick.RemoveAllListeners();
        confirmDebuffButton.onClick.AddListener(() =>
        {
            rootPanel.SetActive(false);
            _onComplete?.Invoke(_selectedBuff, _selectedDebuff);
        });
    }

    void Populate(Transform container, List<RunModifierSO> picks, bool isBuff)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        foreach (var mod in picks)
        {
            var btn = Instantiate(optionButtonPrefab, container);
            btn.Bind(mod, selected =>
            {
                // clear all highlights
                for (int i = 0; i < container.childCount; i++)
                    container.GetChild(i).GetComponent<RunModOptionButton>()?.SetSelected(false);

                btn.SetSelected(true);

                if (isBuff)
                {
                    _selectedBuff = mod;
                    confirmBuffButton.interactable = true;
                }
                else
                {
                    _selectedDebuff = mod;
                    confirmDebuffButton.interactable = true;
                }
            });
        }
    }

    static List<RunModifierSO> Pick3Unique(RunModifierSO[] pool)
    {
        var list = new List<RunModifierSO>();
        if (pool == null) return list;

        // naive unique pick
        int safety = 200;
        while (list.Count < 3 && safety-- > 0 && pool.Length > 0)
        {
            var pick = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (pick && !list.Contains(pick)) list.Add(pick);
        }
        return list;
    }
}
