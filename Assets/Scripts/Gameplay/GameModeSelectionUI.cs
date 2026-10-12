using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class GameModeSelectionUI : MonoBehaviour
{
    public Button standardButton, chaosButton, marathonButton, closeButton;
    public TMP_Text lockDescription;
    Action<RunGameMode> selected;
    void Awake()
    {
        standardButton.onClick.AddListener(() => Pick(RunGameMode.Standard));
        chaosButton.onClick.AddListener(() => Pick(RunGameMode.Chaos));
        marathonButton.onClick.AddListener(() => Pick(RunGameMode.Marathon));
        closeButton.onClick.AddListener(Close);
    }
    public void Show(int standardLevelCount, Action<RunGameMode> onSelected)
    {
        selected = onSelected;
        bool unlocked = GameModeStore.ExtraModesUnlocked(standardLevelCount);
        chaosButton.interactable = marathonButton.interactable = unlocked;
        lockDescription.gameObject.SetActive(!unlocked);
        transform.SetAsLastSibling();
        UIPanelTransition.Show(gameObject, true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(standardButton.gameObject);
    }
    void Pick(RunGameMode mode)
    {
        var callback = selected;
        Close(); callback?.Invoke(mode);
    }
    public void Close() { UIPanelTransition.Hide(gameObject, true); }
}
