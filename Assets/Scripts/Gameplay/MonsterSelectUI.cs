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

    void Awake()
    {
        BuildList();
        SeedDefaultsIfNeeded();    // ensure >= 2 selected (first two by default)
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
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            entry.callback.AddListener(_ => ShowPreview(captured));
            evt.triggers.Add(entry);

            buttons[captured] = btn;
            SetButtonHighlight(btn, false); // default off; we’ll enable for selected later
        }
    }

    void ToggleSelect(MonsterData md)
    {
        if (selected.Contains(md))
        {
            // Attempt to deselect
            if (selected.Count <= MinSelect) return; // enforce minimum
            selected.Remove(md);
        }
        else
        {
            // Attempt to add
            if (selected.Count >= MaxSelect) return; // enforce maximum
            selected.Add(md);
        }

        // Push to global store so Gameplay can read it later
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
        // Expect an optional child named "Highlight" (Image) that we toggle
        var frame = btn.transform.Find("Highlight")?.GetComponent<Image>();
        if (frame) frame.enabled = on;

        // Subtle fallback: change colors if no highlight child exists
        var colors = btn.colors;
        colors.normalColor = on ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.75f);
        colors.highlightedColor = on ? Color.white : colors.highlightedColor;
        btn.colors = colors;
    }

}
