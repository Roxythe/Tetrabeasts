using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MonsterSelectUI : MonoBehaviour
{
    [Header("Roster")]
    public MonsterData[] roster;

    [Header("UI")]
    public Transform listParent;
    public Button itemButtonPrefab;
    public Image previewImage;
    public TMP_Text previewNameText;
    public TMP_Text previewDescriptionText;
    public TMP_Text previewStatsText;
    public TMP_Text counterText;
    public Button startButton;
    public CurrencyUI currencyUI;

    [Header("Counter Colors")]
    public Color counterValidColor = Color.green;
    public Color counterInvalidColor = Color.red;

    MonsterData previewMonster;
    int previewSkinIndex = 0; // The skin currently being previewed (can be locked)

    [Header("Preview Unlock UI")]
    public Button previewUnlockButton;
    public TMP_Text previewUnlockCostText;
    public GameObject previewLockedImage; // Overlay to show "locked"

    // Runtime
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

        var resolved = SelectedMonstersStore.ResolveFromRoster(roster);
        var sanitized = SanitizeSelection(resolved);

        if (sanitized.Count == 0)
        {
            sanitized = BuildDefaultUnlockedSelection();
            SelectedMonstersStore.Active = sanitized;
            SelectedMonstersStore.SaveNames(sanitized);
        }
        else
        {
            SelectedMonstersStore.Active = sanitized;
        }

        SyncFromStore();
        RefreshAllUI();
    }

    void OnEnable()
    {
        SyncFromStore();
        RefreshAllUI();
    }

    void BuildList()
    {
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

            // If unlocked, show committed valid skin. If locked, show default (0)
            bool monsterUnlocked = UnlockStore.IsUnlocked(md);
            int displaySkin = monsterUnlocked ? MonsterSkinStore.GetValidSelected(md) : 0;

            if (img) img.sprite = MonsterSkinStore.GetPortrait(md, displaySkin);

            var lockedImgT = FindDeep(btn.transform, "Locked_Image");
            if (lockedImgT) lockedImgT.gameObject.SetActive(!monsterUnlocked);

            var unlockBtnT = FindDeep(btn.transform, "Unlock_Button");
            var unlockBtn = unlockBtnT ? unlockBtnT.GetComponent<Button>() : null;
            if (unlockBtnT) unlockBtnT.gameObject.SetActive(!monsterUnlocked);

            var captured = md;

            // Always preview on click
            btn.interactable = true;
            btn.onClick.AddListener(() =>
            {
                previewMonster = captured;

                // If locked, default preview skin 0. If unlocked, use committed valid skin
                previewSkinIndex = UnlockStore.IsUnlocked(captured)
                    ? MonsterSkinStore.GetValidSelected(captured)
                    : 0;

                ShowPreview(captured, previewSkinIndex); // Always show preview

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

                    CurrencyStore.Add(-md.unlockCost);
                    currencyUI?.Refresh();
                    bool wasLocked = !UnlockStore.IsUnlocked(md);
                    UnlockStore.Unlock(md);

                    // If this is the first time unlocking any monster, also grant the related achievement stats
                    if (wasLocked && PlayerProgress.I)
                    {
                        PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.MonstersUnlocked, 1);
                        PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunUnlockedAnyMonster, 1);
                    }

                    if (AudioManager.I && unlockSFX)
                        AudioManager.I.PlaySFX(unlockSFX);

                    if (lockedImgT) lockedImgT.gameObject.SetActive(false);
                    unlockBtnT.gameObject.SetActive(false);
                    btn.interactable = true;

                    // After unlocking, refresh portrait + preview stays on this monster
                    previewMonster = md;
                    previewSkinIndex = MonsterSkinStore.GetValidSelected(md);
                    RefreshAllUI();
                    ShowPreview(md, previewSkinIndex);
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
                if (AudioManager.I && hoverSFX) AudioManager.I.PlaySFX(hoverSFX);
            });
            evt.triggers.Add(enter);

            buttons[captured] = btn;
            SetupSkinButtons(btn, md);
            SetButtonHighlight(btn, false);
        }
    }

    void ToggleSelect(MonsterData md)
    {
        bool changed = false;

        if (selected.Contains(md))
        {
            selected.Remove(md);
            changed = true;
        }
        else
        {
            if (selected.Count < MaxSelect)
            {
                selected.Add(md);
                changed = true;
            }
            else
            {
                if (AudioManager.I && errorSFX)
                    AudioManager.I.PlaySFX(errorSFX);
                return;
            }
        }

        if (AudioManager.I && selectSFX)
            AudioManager.I.PlaySFX(selectSFX);

        SelectedMonstersStore.Active = new List<MonsterData>(selected);
        RefreshAllUI();
    }

    List<MonsterData> BuildDefaultUnlockedSelection()
    {
        var outList = new List<MonsterData>();
        if (roster == null) return outList;

        for (int i = 0; i < roster.Length && outList.Count < MinSelect; i++)
        {
            var md = roster[i];
            if (md == null) continue;
            if (!UnlockStore.IsUnlocked(md)) continue;
            if (!outList.Contains(md)) outList.Add(md);
        }

        if (outList.Count < MinSelect)
        {
            for (int i = 0; i < roster.Length && outList.Count < MinSelect; i++)
            {
                var md = roster[i];
                if (md == null) continue;
                if (!outList.Contains(md)) outList.Add(md);
            }
        }

        return outList;
    }

    List<MonsterData> SanitizeSelection(List<MonsterData> input)
    {
        var outList = new List<MonsterData>();
        if (input == null) return outList;

        foreach (var md in input)
        {
            if (md == null) continue;
            if (!UnlockStore.IsUnlocked(md)) continue;
            if (outList.Contains(md)) continue;
            outList.Add(md);
            if (outList.Count >= MaxSelect) break;
        }

        if (outList.Count < MinSelect)
        {
            var defaults = BuildDefaultUnlockedSelection();
            foreach (var d in defaults)
            {
                if (outList.Count >= MinSelect) break;
                if (d != null && !outList.Contains(d)) outList.Add(d);
            }
        }

        return outList;
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
        foreach (var kv in buttons)
            SetButtonHighlight(kv.Value, selected.Contains(kv.Key));

        int count = selected.Count;
        bool validCount = count >= MinSelect && count <= MaxSelect;

        if (counterText)
        {
            counterText.text = $"Selected: {count} / {MaxSelect}";
            counterText.color = validCount ? counterValidColor : counterInvalidColor;
        }

        if (startButton)
            startButton.interactable = validCount;

        if (previewMonster != null)
            ShowPreview(previewMonster, previewSkinIndex);
        else
            ShowPreview(GetAnySelected());

        foreach (var kv in buttons)
        {
            var img = kv.Value.GetComponentInChildren<Image>();
            bool monsterUnlocked = UnlockStore.IsUnlocked(kv.Key);
            int skin = monsterUnlocked ? MonsterSkinStore.GetValidSelected(kv.Key) : 0;
            if (img) img.sprite = MonsterSkinStore.GetPortrait(kv.Key, skin);

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
        if (!md) { ClearPreview(); return; }

        int idx = UnlockStore.IsUnlocked(md) ? MonsterSkinStore.GetValidSelected(md) : 0;
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

        // Keep your description where it already is
        if (previewDescriptionText) previewDescriptionText.text = md.monsterDescription;

        // Show effective stats (base + shop buffs)
        if (previewStatsText) previewStatsText.text = BuildPreviewStatsString(md);

        SetupPreviewUnlockUI(md, skinIdx);

    }

    string BuildPreviewStatsString(MonsterData md)
    {
        if (!md) return "";

        int atkLvl = ShopBuffStore.GetLevel(ShopBuffType.AttackUp);
        int hpLvl = ShopBuffStore.GetLevel(ShopBuffType.HpUp);
        int healLvl = ShopBuffStore.GetLevel(ShopBuffType.HealPower);

        float effectiveAttack = md.attackPower + atkLvl;          // +1 per level
        float effectiveMaxHp = md.maxHealth + (hpLvl * 5f);       // +5 per level

        // Only healers should benefit from heal power
        float effectiveHeal = (md.healAmount > 0f) ? (md.healAmount + (healLvl * 2f)) : 0f;

        float range = md.healRange;
        float speed = md.healSpeed;

        string atkLine = $"Attack: {md.attackPower:0.#}  (+{atkLvl}) = {effectiveAttack:0.#}";
        string hpLine = $"Max HP: {md.maxHealth:0.#}  (+{hpLvl * 5}) = {effectiveMaxHp:0.#}";

        string healLine;
        if (md.healAmount > 0f)
        {
            healLine =
                $"Heal: {md.healAmount:0.#}  (+{healLvl * 2}) = {effectiveHeal:0.#}\n" +
                $"Heal Range: {range:0.#}\n" +
                $"Heal Speed: {speed:0.#}s";
        }
        else
        {
            healLine = "Heal: —";
        }

        return $"Base Stats + (Shop Buff) = Total Stats\n{hpLine}\n{atkLine}\n{healLine}";
    }

    void ClearPreview()
    {
        if (previewImage) previewImage.sprite = null;
        if (previewNameText) previewNameText.text = "";
        if (previewDescriptionText) previewDescriptionText.text = "";
        if (previewStatsText) previewStatsText.text = "";
    }

    void SetButtonHighlight(Button btn, bool on)
    {
        var frame = btn.transform.Find("Highlight")?.GetComponent<Image>();
        if (frame) frame.enabled = on;

        var colors = btn.colors;
        Color onCol = new Color(1f, 1f, 1f, 1.0f);
        Color offCol = new Color(1f, 1f, 1f, 0.5f);
        Color a = on ? onCol : offCol;

        colors.normalColor = a;
        colors.highlightedColor = a;
        colors.selectedColor = a;
        colors.pressedColor = a;
        btn.colors = colors;

        var es = UnityEngine.EventSystems.EventSystem.current;
        if (!on && es && es.currentSelectedGameObject == btn.gameObject)
            es.SetSelectedGameObject(null);
    }

    public void ConfirmAndClose()
    {
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
            int valid = MonsterSkinStore.GetValidSelected(md);
            MonsterSkinStore.SetSelectedAndLastValid(md, valid);
        }

        SelectedMonstersStore.Active = new List<MonsterData>(selected);
        SelectedMonstersStore.SaveNames(SelectedMonstersStore.Active);
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

            var icon = t.GetComponent<Image>();
            if (!icon)
            {
                var iconT = FindDeep(t, "Icon_Image");
                icon = iconT ? iconT.GetComponent<Image>() : null;
            }

            if (icon)
                icon.sprite = MonsterSkinStore.GetPortrait(md, idx);

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                // Always allow preview (even if monster is locked)
                previewMonster = md;
                previewSkinIndex = idx;
                ShowPreview(md, idx);

                // Only allow selection if both monster and skin are unlocked
                if (UnlockStore.IsUnlocked(md) && MonsterSkinStore.IsUnlocked(md, idx))
                {
                    MonsterSkinStore.SetSelectedAndLastValid(md, idx);
                    RefreshAllUI();
                }
            });
        }

        ApplyVariantOpacity(monsterBtn, md);
        ApplyVariantLockVisuals(monsterBtn, md);
    }

    void ApplyVariantOpacity(Button monsterBtn, MonsterData md)
    {
        int committed = UnlockStore.IsUnlocked(md) ? MonsterSkinStore.GetValidSelected(md) : 0;

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
        // If the monster itself is locked allow preview, but do not allow buying skins here
        if (!UnlockStore.IsUnlocked(md))
        {
            if (previewUnlockButton) previewUnlockButton.gameObject.SetActive(false);
            if (previewLockedImage) previewLockedImage.SetActive(true);
            if (previewUnlockCostText) previewUnlockCostText.text = "";
            return;
        }

        if (!previewUnlockButton) return;

        bool unlocked = MonsterSkinStore.IsUnlocked(md, skinIdx);
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
            bool wasUnlocked = MonsterSkinStore.IsUnlocked(md, skinIdx); // Shouldn't happen, but just in case
            MonsterSkinStore.Unlock(md, skinIdx);

            // If this is the first time unlocking this skin, also grant the related achievement stats
            if (!wasUnlocked && PlayerProgress.I)
            {
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.SkinsUnlocked, 1);
                PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunUnlockedAnySkin, 1);
            }

            MonsterSkinStore.SetSelectedAndLastValid(md, skinIdx);

            if (AudioManager.I && unlockSFX) AudioManager.I.PlaySFX(unlockSFX);

            RefreshAllUI();
            ShowPreview(md, skinIdx);
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
