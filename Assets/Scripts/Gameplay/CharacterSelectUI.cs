using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Roster")]
    public PlayerCharacterData[] roster;

    [Header("UI")]
    public Transform listParent;
    public Button characterButtonPrefab; // Text + Image
    public TMP_Text selectedName;        // Preview
    public Image selectedPortrait;       // Preview
    public Image selectedBorder;
    public TMP_Text selectedSpecialAbilityName;
    public TMP_Text selectedSpecialDescription;
    public CurrencyUI currencyUI;

    [Header("Audio")]
    public AudioClip selectSFX;
    public AudioClip hoverSFX;
    public AudioClip unlockSFX;
    public AudioClip errorSFX;

    [Header("Selection Visuals")]
    [Range(0f, 1f)] public float selectedAlpha = 1f;
    [Range(0f, 1f)] public float deselectedAlpha = 0.65f;

    PlayerCharacterData previewCharacter;
    readonly System.Collections.Generic.Dictionary<PlayerCharacterData, Button> buttons = new();

    void Awake()
    {
        BuildList();

        var saved = SelectedCharacterStore.ResolveFromRoster(roster);
        if (saved != null && UnlockStore.IsUnlocked(saved))
            SelectedCharacterStore.Current = saved;

        if (SelectedCharacterStore.Current == null)
        {
            var fallback = GetFirstUnlockedCharacter();
            if (fallback != null)
            {
                SetCurrent(fallback);
            }
            else if (roster != null && roster.Length > 0)
            {
                SetCurrent(roster[0]);
            }
        }

        // Default preview is the current stored character
        previewCharacter = SelectedCharacterStore.Current;
        RefreshPreview();
        RefreshButtonAlphas();
    }

    void BuildList()
    {
        for (int i = listParent.childCount - 1; i >= 0; i--)
            Destroy(listParent.GetChild(i).gameObject);

        buttons.Clear();

        if (roster == null)
            return;

        foreach (var data in roster)
        {
            var btn = Instantiate(characterButtonPrefab, listParent);
            buttons[data] = btn;
            var txt = btn.GetComponentInChildren<TMP_Text>();
            var img = btn.GetComponentInChildren<Image>();
            var borderT = FindDeep(btn.transform, "Border_Image");
            var borderImg = borderT ? borderT.GetComponent<Image>() : null;

            if (txt) txt.text = data.displayName;
            if (img && data.portrait) img.sprite = data.portrait;
            if (borderImg) borderImg.sprite = data.defaultBorder;

            bool unlocked = UnlockStore.IsUnlocked(data);

            var lockedImgT = FindDeep(btn.transform, "Locked_Image");
            if (lockedImgT) lockedImgT.gameObject.SetActive(!unlocked);

            var unlockBtnT = FindDeep(btn.transform, "Unlock_Button");
            var unlockBtn = unlockBtnT ? unlockBtnT.GetComponent<Button>() : null;
            if (unlockBtnT) unlockBtnT.gameObject.SetActive(!unlocked);

            // Always preview on click
            btn.interactable = true;
            btn.onClick.AddListener(() =>
            {
                previewCharacter = data;
                RefreshPreview(); // Always show preview

                if (!UnlockStore.IsUnlocked(data))
                {
                    if (AudioManager.I && errorSFX) AudioManager.I.PlaySFX(errorSFX);
                    return;
                }

                SetCurrent(data);
                RefreshButtonAlphas();
            });

            // Cost text
            var costTextT = unlockBtnT ? FindDeep(unlockBtnT, "Cur_Text (TMP)") : null;
            var costText = costTextT ? costTextT.GetComponent<TMPro.TMP_Text>() : null;
            if (costText) costText.text = $"x{data.unlockCost}";

            if (unlockBtn)
            {
                unlockBtn.onClick.RemoveAllListeners();
                unlockBtn.onClick.AddListener(() =>
                {
                    if (CurrencyStore.Total < data.unlockCost)
                    {
                        if (AudioManager.I && errorSFX)
                            AudioManager.I.PlaySFX(errorSFX);
                        return;
                    }

                    CurrencyStore.Add(-data.unlockCost);
                    currencyUI?.Refresh();
                    bool wasLocked = !UnlockStore.IsUnlocked(data);
                    UnlockStore.Unlock(data);

                    // Only count the unlock if it was previously locked
                    if (wasLocked && PlayerProgress.I)
                    {
                        PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.CharactersUnlocked, 1);
                        PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunUnlockedAnyCharacter, 1);
                    }

                    if (AudioManager.I && unlockSFX)
                        AudioManager.I.PlaySFX(unlockSFX);

                    if (lockedImgT) lockedImgT.gameObject.SetActive(false);
                    unlockBtnT.gameObject.SetActive(false);
                    btn.interactable = true;

                    // After purchase, auto-equip the character
                    previewCharacter = data;
                    SetCurrent(data);
                });
            }

            // Hover SFX
            var evt = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enter.callback.AddListener(_ =>
            {
                if (hoverSFX && AudioManager.I) AudioManager.I.PlaySFX(hoverSFX);
            });
            evt.triggers.Add(enter);
        }
    }

    void RefreshButtonAlphas()
    {
        foreach (var kv in buttons)
        {
            var data = kv.Key;
            var button = kv.Value;
            if (!button)
                continue;

            bool unlocked = UnlockStore.IsUnlocked(data);
            bool isSelected = SelectedCharacterStore.Current == data;
            float alpha = !unlocked || isSelected ? selectedAlpha : deselectedAlpha;

            var portrait = button.GetComponentInChildren<Image>();
            var borderT = FindDeep(button.transform, "Border_Image");
            var border = borderT ? borderT.GetComponent<Image>() : null;

            if (portrait)
            {
                Color c = portrait.color;
                c.a = alpha;
                portrait.color = c;
            }

            if (border)
            {
                Color c = border.color;
                c.a = alpha;
                border.color = c;
            }
        }
    }

    void SetCurrent(PlayerCharacterData data)
    {
        if (!UnlockStore.IsUnlocked(data))
        {
            if (AudioManager.I && errorSFX)
                AudioManager.I.PlaySFX(errorSFX);
            return;
        }

        SelectedCharacterStore.Save(data);
        SelectedCharacterStore.Current = data;

        if (AudioManager.I && selectSFX)
            AudioManager.I.PlaySFX(selectSFX);

        RefreshPreview();
        RefreshButtonAlphas();
        RefreshTitleMenuLoadoutUI();
    }

    PlayerCharacterData GetFirstUnlockedCharacter()
    {
        if (roster == null) return null;
        foreach (var c in roster)
        {
            if (c != null && UnlockStore.IsUnlocked(c))
                return c;
        }
        return null;
    }

    public void RefreshPreview()
    {
        // If nothing has been previewed yet, fall back to current selection
        var cur = previewCharacter ? previewCharacter : SelectedCharacterStore.Current;
        if (!cur) return;

        if (selectedName) selectedName.text = cur.displayName;
        if (selectedPortrait && cur.portrait) selectedPortrait.sprite = cur.portrait;
        if (selectedBorder && cur.defaultBorder) selectedBorder.sprite = cur.defaultBorder;

        if (selectedSpecialAbilityName)
        {
            selectedSpecialAbilityName.text = $"Special Ability: {cur.specialAbilityName}";
        }

        if (selectedSpecialDescription)
            selectedSpecialDescription.text = cur.specialDescription;
    }

    void RefreshTitleMenuLoadoutUI()
    {
        var titleMenu = FindFirstObjectByType<TitleMenuUI>(FindObjectsInactive.Include);
        if (titleMenu)
            titleMenu.RefreshSelectedLoadoutUI();
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (!root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
