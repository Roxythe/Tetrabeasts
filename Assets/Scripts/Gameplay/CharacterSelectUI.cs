using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Roster")]
    public PlayerCharacterData[] roster;

    [Header("UI")]
    public Transform listParent;
    public Button characterButtonPrefab; // with child TMP_Text + Image
    public TMP_Text selectedName;        // optional preview
    public Image selectedPortrait;       // optional preview
    public TMP_Text selectedSpecialDescription;

    [Header("Audio")]
    public AudioClip selectSFX;
    public AudioClip hoverSFX;

    void Awake()
    {
        BuildList();

        // If nothing picked yet and we have a roster, default to first
        if (SelectedCharacterStore.Current == null && roster != null && roster.Length > 0)
            SetCurrent(roster[0]);
        else
            RefreshPreview();
    }

    void BuildList()
    {
        foreach (var data in roster)
        {
            var btn = Instantiate(characterButtonPrefab, listParent);
            var txt = btn.GetComponentInChildren<TMP_Text>();
            var img = btn.GetComponentInChildren<Image>();

            if (txt) txt.text = data.displayName;
            if (img && data.portrait) img.sprite = data.portrait;

            btn.onClick.AddListener(() => { SetCurrent(data); }); // select

            // Hover SFX
            var evt = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enter.callback.AddListener(_ => {
                if (hoverSFX && AudioManager.I) AudioManager.I.PlaySFX(hoverSFX);
            });
            evt.triggers.Add(enter);
        }
    }

    void SetCurrent(PlayerCharacterData data)
    {
        SelectedCharacterStore.Current = data;

        if (AudioManager.I && selectSFX)
            AudioManager.I.PlaySFX(selectSFX);

        RefreshPreview();
    }

    public void RefreshPreview()
    {
        var cur = SelectedCharacterStore.Current;
        if (!cur) return;
        if (selectedName) selectedName.text = cur.displayName;
        if (selectedPortrait && cur.portrait) selectedPortrait.sprite = cur.portrait;
        if (selectedSpecialDescription) selectedSpecialDescription.text = cur.specialDescription;
    }
}
