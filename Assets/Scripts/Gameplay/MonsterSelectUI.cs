using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MonsterSelectUI : MonoBehaviour
{
    [Header("Roster")]
    public MonsterData[] roster;                 // drag all MonsterData assets here (title scene)

    [Header("UI")]
    public Transform listParent;                 // Scroll View/Viewport/Content
    public Button itemButtonPrefab;              // prefab with child Image (portrait) + TMP_Text (name) + optional Image "Highlight"
    public Image previewImage;                   // Preview/Preview_Image
    public TMP_Text previewNameText;             // Preview/PreviewName_Text
    public TMP_Text previewStatText;             // Preview/PreviewStatText
    public TMP_Text counterText;                 // e.g., "Selected: 2 / 4"
    public Button startButton;                   // optional: disable until valid (2..4)
    public CurrencyUI currencyUI;

    MonsterData previewMonster;
    int previewSkinIndex = 0; // the skin currently being previewed (can be locked)

    [Header("Preview Unlock UI")]
    public Button previewUnlockButton;
    public TMP_Text previewUnlockCostText;
    public GameObject previewLockedImage; // optional overlay to show "locked"

    // runtime
    readonly Dictionary<MonsterData, Button> buttons = new();
    readonly HashSet<MonsterData> selected = new();

    const int MinSelect = 2;
    const int MaxSelect = 4;

    [Header("Audio")]
    public AudioClip selectSFX;
    public AudioClip hoverSFX;
    public AudioClip unlockSFX;
    public AudioClip errorSFX;

    void Awake()
    {
        BuildList();
        SeedDefaultsIfNeeded();    // ensure >= 2 selected (first two by default)

        var resolved = SelectedMonstersStore.ResolveFromRoster(roster);
        if (resolved.Count > 0)
        {
            selected.Clear();
            foreach (var md in resolved) if (md) selected.Add(md);
            SelectedMonstersStore.Active = new List<MonsterData>(selected);
        }

        SyncFromStore();
        RefreshAllUI();
    }

    void OnEnable()
    {
        // Pull current selections
        SyncFromStore();
        RefreshAllUI();
    }

    void BuildList()
    {
        // Clear any existing children
        for (int i = listParent.childCount - 1; i >= 0; i--)
            Destroy(listParent.GetChild(i).gameObject);
        buttons.Clear();

        if (roster == null) return;

        foreach (var md in roster)
        {
            if (!md) continue;

            var btn = Instantiate(itemButtonPrefab, listParent);
            var txt = btn.GetComponentInChildren<TMP_Text>();
            var img = btn.GetComponentInChildren<Image>();

            if (txt) txt.text = md.monsterName;
            if (img) img.sprite = MonsterSkinStore.GetPortrait(md, MonsterSkinStore.GetValidSelected(md));

            bool unlocked = UnlockStore.IsUnlocked(md);

            var lockedImgT = FindDeep(btn.transform, "Locked_Image");
            if (lockedImgT) lockedImgT.gameObject.SetActive(!unlocked);

            var unlockBtnT = FindDeep(btn.transform, "Unlock_Button");
            var unlockBtn = unlockBtnT ? unlockBtnT.GetComponent<Button>() : null;
            if (unlockBtnT) unlockBtnT.gameObject.SetActive(!unlocked);

            // Prevent selecting locked monsters
            var captured = md; // closure
            btn.interactable = true; // always clickable so we can play error SFX
            btn.onClick.AddListener(() =>
            {
                if (!UnlockStore.IsUnlocked(captured))
                {
                    if (AudioManager.I && errorSFX) AudioManager.I.PlaySFX(errorSFX);
                    return;
                }
                ToggleSelect(captured);
            });

            // Set cost text
            var costTextT = unlockBtnT ? FindDeep(unlockBtnT, "Cur_Text (TMP)") : null;
            var costText = costTextT ? costTextT.GetComponent<TMPro.TMP_Text>() : null;
            if (costText) costText.text = $"x{md.unlockCost}";

            if (unlockBtn)
            {
                unlockBtn.onClick.RemoveAllListeners();
                unlockBtn.onClick.AddListener(() =>
                {
                    if (CurrencyStore.Total < md.unlockCost)
                    {
                        if (AudioManager.I && errorSFX)
                            AudioManager.I.PlaySFX(errorSFX);
                        return;
                    }

                    CurrencyStore.Add(-md.unlockCost);  // spend
                    currencyUI?.Refresh();
                    UnlockStore.Unlock(md);

                    if (AudioManager.I && unlockSFX)
                        AudioManager.I.PlaySFX(unlockSFX);

                    // Update visuals immediately
                    if (lockedImgT) lockedImgT.gameObject.SetActive(false);
                    unlockBtnT.gameObject.SetActive(false);
                    btn.interactable = true;
                });
            }

            // show details on hover/click (optional: click already handled above)
            var evt = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // HOVER: preview + sound
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };

            enter.callback.AddListener(_ => 
            {
                if (AudioManager.I && hoverSFX) AudioManager.I.PlaySFX(hoverSFX); // play hover SFX
            });
            evt.triggers.Add(enter);

            buttons[captured] = btn;
            SetupSkinButtons(btn, md);
            SetButtonHighlight(btn, false); // default off; we’ll enable for selected later
        }
    }

    void ToggleSelect(MonsterData md)
    {
        bool changed = false;

        if (selected.Contains(md))
        {
            // Allow deselecting freely (even if this makes it < MinSelect)
            selected.Remove(md);
            changed = true;
        }
        else
        {
            // Enforce MAX at click time
            if (selected.Count < MaxSelect)
            {
                selected.Add(md);
                changed = true;
            }
            else
            {
                if (AudioManager.I && errorSFX)
                    AudioManager.I.PlaySFX(errorSFX);
                return; // don't play selectSFX / don't refresh
            }
        }

        if (AudioManager.I && selectSFX)
            AudioManager.I.PlaySFX(selectSFX);

        // Always resync UI so a 5th click never leaves a stray highlight
        SelectedMonstersStore.Active = new List<MonsterData>(selected);
        RefreshAllUI();
    }

    void SeedDefaultsIfNeeded()
    {
        // If nothing selected in store and roster has at least two, pick first two.
        if (SelectedMonstersStore.Active == null) SelectedMonstersStore.Active = new List<MonsterData>();
        if (SelectedMonstersStore.Active.Count >= MinSelect) return;

        if (roster != null && roster.Length >= MinSelect)
        {
            SelectedMonstersStore.Active.Clear();
            SelectedMonstersStore.Active.Add(roster[0]);
            SelectedMonstersStore.Active.Add(roster[1]);
        }
    }

    void SyncFromStore()
    {
        selected.Clear();
        if (SelectedMonstersStore.Active != null)
            foreach (var md in SelectedMonstersStore.Active)
                if (md != null) selected.Add(md);
    }

    public void RefreshAllUI()
    {
        // List highlights
        foreach (var kv in buttons)
            SetButtonHighlight(kv.Value, selected.Contains(kv.Key));

        // Counter + Start button gate
        if (counterText)
            counterText.text = $"Selected: {selected.Count} / {MaxSelect}";
        if (startButton)
            startButton.interactable = selected.Count >= MinSelect && selected.Count <= MaxSelect;

        // Preview panel
        if (previewMonster != null)
            ShowPreview(previewMonster, previewSkinIndex); // keep what user is previewing
        else
            ShowPreview(GetAnySelected());

        // Update all portraits to reflect current committed skin selection
        foreach (var kv in buttons)
        {
            // update main portrait to valid skin
            var img = kv.Value.GetComponentInChildren<Image>();
            if (img) img.sprite = MonsterSkinStore.GetPortrait(kv.Key, MonsterSkinStore.GetValidSelected(kv.Key));

            ApplyVariantOpacity(kv.Value, kv.Key);
            ApplyVariantLockVisuals(kv.Value, kv.Key);
        }
    }

    MonsterData GetAnySelected()
    {
        foreach (var md in selected) return md;
        return null;
    }

    void ShowPreview(MonsterData md)
    {
        // default behavior: show committed safe skin
        if (!md) { ClearPreview(); return; }

        int idx = MonsterSkinStore.GetValidSelected(md);
        previewMonster = md;
        previewSkinIndex = idx;
        ShowPreview(md, idx);
    }

    void ShowPreview(MonsterData md, int skinIdx)
    {
        if (!md) { ClearPreview(); return; }

        if (previewImage)
            previewImage.sprite = MonsterSkinStore.GetPortrait(md, skinIdx);

        if (previewNameText) previewNameText.text = md.monsterName;
        if (previewStatText) previewStatText.text = md.monsterDescription;

        SetupPreviewUnlockUI(md, skinIdx);
    }

    void ClearPreview()
    {
        if (previewImage) previewImage.sprite = null;
        if (previewNameText) previewNameText.text = "";
        if (previewStatText) previewStatText.text = "";
    }

    void SetButtonHighlight(Button btn, bool on)
    {
        // Optional overlay toggle (kept if you use it)
        var frame = btn.transform.Find("Highlight")?.GetComponent<Image>();
        if (frame) frame.enabled = on;

        // Force all states to same opacity so state machine can't fight you
        var colors = btn.colors;
        Color onCol = new Color(1f, 1f, 1f, 1.0f);
        Color offCol = new Color(1f, 1f, 1f, 0.5f);

        Color a = on ? onCol : offCol;

        colors.normalColor = a;
        colors.highlightedColor = a;
        colors.selectedColor = a;
        colors.pressedColor = a;   // keeps it consistent while mouse is down
        btn.colors = colors;

        // Optional: clear UI selection if this button just turned OFF and remains "selected"
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (!on && es && es.currentSelectedGameObject == btn.gameObject)
            es.SetSelectedGameObject(null);
    }

    public void ConfirmAndClose()
    {
        // Ensure at least MinSelect by auto-filling from roster order
        if (selected.Count < MinSelect && roster != null)
        {
            for (int i = 0; i < roster.Length && selected.Count < MinSelect; i++)
            {
                var md = roster[i];
                if (md != null && !selected.Contains(md))
                    selected.Add(md);
            }
        }

        foreach (var md in selected)
        {
            // If selected skin is invalid/locked, force fallback to last valid or default
            int valid = MonsterSkinStore.GetValidSelected(md);
            MonsterSkinStore.SetSelectedAndLastValid(md, valid);
        }

        // Persist & refresh one last time, then hide panel
        SelectedMonstersStore.Active = new List<MonsterData>(selected);
        SelectedMonstersStore.SaveNames(SelectedMonstersStore.Active); // Persist to PlayerPrefs
        RefreshAllUI();
        gameObject.SetActive(false);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (!root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    void SetupSkinButtons(Button monsterBtn, MonsterData md)
    {
        for (int i = 0; i < 5; i++)
        {
            int idx = i;

            var t = FindDeep(monsterBtn.transform, $"SkinVariant_Button{idx}");
            if (!t) continue;

            var b = t.GetComponent<Button>();
            if (!b) continue;

            
            var icon = t.GetComponent<Image>(); // Prefer an Image directly on SkinVariant_ButtonX

            // Optional fallback if a dedicated child image is added later
            if (!icon)
            {
                var iconT = FindDeep(t, "Icon_Image"); // look for child named "Icon_Image"
                icon = iconT ? iconT.GetComponent<Image>() : null;
            }

            if (icon)
                icon.sprite = MonsterSkinStore.GetPortrait(md, idx);

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                // If monster itself is locked, do nothing but error sound
                if (!UnlockStore.IsUnlocked(md))
                {
                    if (AudioManager.I && errorSFX) AudioManager.I.PlaySFX(errorSFX);
                    return;
                }

                previewMonster = md;
                previewSkinIndex = idx;

                // Show preview using that skin (even if locked)
                ShowPreview(md, idx);

                // If the skin is already unlocked, commit selection immediately
                if (MonsterSkinStore.IsUnlocked(md, idx))
                {
                    MonsterSkinStore.SetSelectedAndLastValid(md, idx);
                    RefreshAllUI(); // will update list portrait + opacity states
                }
            });
        }

        ApplyVariantOpacity(monsterBtn, md);
        ApplyVariantLockVisuals(monsterBtn, md);
    }

    void ApplyVariantOpacity(Button monsterBtn, MonsterData md)
    {
        int committed = MonsterSkinStore.GetValidSelected(md);

        for (int i = 0; i < 5; i++)
        {
            var t = FindDeep(monsterBtn.transform, $"SkinVariant_Button{i}");
            if (!t) continue;

            var b = t.GetComponent<Button>();
            if (!b) continue;

            bool selected = (i == committed);
            var colors = b.colors;
            float a = selected ? 1f : 0.5f;
            var c = new Color(1f, 1f, 1f, a);
            colors.normalColor = c;
            colors.highlightedColor = c;
            colors.selectedColor = c;
            colors.pressedColor = c;
            b.colors = colors;
        }
    }

    void SetupPreviewUnlockUI(MonsterData md, int skinIdx)
    {
        if (!previewUnlockButton) return;

        bool unlocked = MonsterSkinStore.IsUnlocked(md, skinIdx);

        // Default skin (0) is always unlocked -> hide unlock UI
        bool showUnlock = !unlocked && skinIdx > 0;

        previewUnlockButton.gameObject.SetActive(showUnlock);
        if (previewLockedImage) previewLockedImage.SetActive(showUnlock);

        if (!showUnlock) return;

        int cost = MonsterSkinStore.GetCost(md, skinIdx);
        if (previewUnlockCostText) previewUnlockCostText.text = $"x{cost}";

        previewUnlockButton.onClick.RemoveAllListeners();
        previewUnlockButton.onClick.AddListener(() =>
        {
            if (CurrencyStore.Total < cost)
            {
                if (AudioManager.I && errorSFX) AudioManager.I.PlaySFX(errorSFX);
                return;
            }

            CurrencyStore.Add(-cost);
            currencyUI?.Refresh();

            MonsterSkinStore.Unlock(md, skinIdx);
            MonsterSkinStore.SetSelectedAndLastValid(md, skinIdx); // commit after purchase

            if (AudioManager.I && unlockSFX) AudioManager.I.PlaySFX(unlockSFX);

            RefreshAllUI();
            ShowPreview(md, skinIdx); // refresh preview to hide unlock button + keep skin displayed
        });
    }

    void ApplyVariantLockVisuals(Button monsterBtn, MonsterData md)
    {
        for (int i = 0; i < 5; i++)
        {
            var t = FindDeep(monsterBtn.transform, $"SkinVariant_Button{i}");
            if (!t) continue;

            bool unlocked = MonsterSkinStore.IsUnlocked(md, i);

            var lockT = FindDeep(t, "Locked_Image");
            if (lockT) lockT.gameObject.SetActive(!unlocked);
        }
    }

}
