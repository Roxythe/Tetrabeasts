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

    // runtime
    readonly Dictionary<MonsterData, Button> buttons = new();
    readonly HashSet<MonsterData> selected = new();

    const int MinSelect = 2;
    const int MaxSelect = 4;

    [Header("Audio")]
    public AudioClip selectSFX;
    public AudioClip hoverSFX;

    void Awake()
    {
        BuildList();
        SeedDefaultsIfNeeded();    // ensure >= 2 selected (first two by default)
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
            if (img && md.portrait) img.sprite = md.portrait;

            var captured = md; // closure
            btn.onClick.AddListener(() => ToggleSelect(captured));

            // show details on hover/click (optional: click already handled above)
            var evt = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // HOVER: preview + sound
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enter.callback.AddListener(_ => {
                ShowPreview(captured);
                if (AudioManager.I && hoverSFX) AudioManager.I.PlaySFX(hoverSFX);
            });
            evt.triggers.Add(enter);

            buttons[captured] = btn;
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
                // Optional: play a "bump" SFX or flash the counter
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

        // Preview panel (show first selected or blank)
        var first = GetAnySelected();
        ShowPreview(first);
    }

    MonsterData GetAnySelected()
    {
        foreach (var md in selected) return md;
        return null;
    }

    void ShowPreview(MonsterData md)
    {
        if (!md)
        {
            if (previewImage) previewImage.sprite = null;
            if (previewNameText) previewNameText.text = "";
            if (previewStatText) previewStatText.text = "";
            return;
        }

        if (previewImage && md.portrait) previewImage.sprite = md.portrait;
        if (previewNameText) previewNameText.text = md.monsterName;
        if (previewStatText)
            previewStatText.text = md.monsterDescription; // your “stat information” string
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

        // Persist & refresh one last time, then hide panel
        SelectedMonstersStore.Active = new List<MonsterData>(selected);
        RefreshAllUI();
        gameObject.SetActive(false);
    }

}
