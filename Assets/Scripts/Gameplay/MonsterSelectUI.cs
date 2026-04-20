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
    public TMP_Text counterTextShadow;
    public Button startButton;
    public CurrencyUI currencyUI;

    [Header("Preview Toggle")]
    public Button previewToggleButton;

    [Header("Monster Button Portrait")]
    public string portraitImageChildName = "MonsterIcon_Image";

    [Header("Monster Button Banner")]
    public string bannerImageChildName = "MonsterClassBackground_Image";
    public Sprite attackBannerSprite;
    public Sprite defenseBannerSprite;
    public Sprite healerBannerSprite;

    [Header("Counter Colors")]
    public Color counterValidColor = Color.green;
    public Color counterInvalidColor = Color.red;

    [Header("Selection Visuals")]
    [Range(0f, 1f)] public float selectedAlpha = 1f;
    [Range(0f, 1f)] public float deselectedAlpha = 0.9f;
    [Range(0f, 1f)] public float deselectedBannerMultiplier = 0.9f;

    MonsterData previewMonster;
    int previewSkinIndex = 0;
    bool showPreviewDescription = true;

    [Header("Preview Unlock UI")]
    public Button previewUnlockButton;
    public TMP_Text previewUnlockCostText;
    public GameObject previewLockedImage;

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
        if (previewToggleButton)
        {
            previewToggleButton.onClick.RemoveAllListeners();
            previewToggleButton.onClick.AddListener(TogglePreviewText);
        }

        ApplyPreviewTextVisibility();
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

    void TogglePreviewText()
    {
        showPreviewDescription = !showPreviewDescription;
        ApplyPreviewTextVisibility();
    }

    void ApplyPreviewTextVisibility()
    {
        if (previewDescriptionText)
            previewDescriptionText.gameObject.SetActive(showPreviewDescription);

        if (previewStatsText)
            previewStatsText.gameObject.SetActive(!showPreviewDescription);
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

            var view = btn.GetComponent<MonsterButtonView>();
            if (view)
            {
                if (view.nameText) view.nameText.text = md.monsterName;
                if (view.nameShadowText) view.nameShadowText.text = md.monsterName;
            }

            bool monsterUnlocked = UnlockStore.IsUnlocked(md);
            int displaySkin = monsterUnlocked ? MonsterSkinStore.GetValidSelected(md) : 0;

            if (view && view.portraitImage)
                view.portraitImage.sprite = MonsterSkinStore.GetPortrait(md, displaySkin);

            var lockedImgT = FindDeep(btn.transform, "Locked_Image");
            if (lockedImgT) lockedImgT.gameObject.SetActive(!monsterUnlocked);

            var unlockBtnT = FindDeep(btn.transform, "Unlock_Button");
            var unlockBtn = unlockBtnT ? unlockBtnT.GetComponent<Button>() : null;
            if (unlockBtnT) unlockBtnT.gameObject.SetActive(!monsterUnlocked);

            var captured = md;
            ApplyBanner(btn, md);

            btn.interactable = true;
            btn.onClick.AddListener(() =>
            {
                previewMonster = captured;

                previewSkinIndex = UnlockStore.IsUnlocked(captured)
                    ? MonsterSkinStore.GetValidSelected(captured)
                    : 0;

                ShowPreview(captured, previewSkinIndex);

                if (!UnlockStore.IsUnlocked(captured))
                {
                    if (AudioManager.I && errorSFX) AudioManager.I.PlaySFX(errorSFX);
                    return;
                }

                ToggleSelect(captured);
            });

            var costTextT = unlockBtnT ? FindDeep(unlockBtnT, "Cur_Text (TMP)") : null;
            var costText = costTextT ? costTextT.GetComponent<TMP_Text>() : null;
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

                    previewMonster = md;
                    previewSkinIndex = MonsterSkinStore.GetValidSelected(md);
                    RefreshAllUI();
                    ShowPreview(md, previewSkinIndex);
                });
            }

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

        if (!changed)
            return;

        if (AudioManager.I && selectSFX)
            AudioManager.I.PlaySFX(selectSFX);

        SelectedMonstersStore.Active = new List<MonsterData>(selected);
        SelectedMonstersStore.SaveNames(SelectedMonstersStore.Active);

        RefreshAllUI();
        RefreshTitleMenuLoadoutUI();
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

        string counterStr = $"{count}/{MaxSelect}";

        if (counterText)
        {
            counterText.text = counterStr;
            counterText.color = validCount ? counterValidColor : counterInvalidColor;
        }

        if (counterTextShadow)
        {
            counterTextShadow.text = counterStr;
        }

        if (startButton)
            startButton.interactable = validCount;

        if (previewMonster != null)
            ShowPreview(previewMonster, previewSkinIndex);
        else
            ShowPreview(GetAnySelected());

        ApplyPreviewTextVisibility();

        foreach (var kv in buttons)
        {
            var view = kv.Value.GetComponent<MonsterButtonView>();
            bool monsterUnlocked = UnlockStore.IsUnlocked(kv.Key);
            int skin = monsterUnlocked ? MonsterSkinStore.GetValidSelected(kv.Key) : 0;

            if (view && view.portraitImage)
                view.portraitImage.sprite = MonsterSkinStore.GetPortrait(kv.Key, skin);

            ApplyVariantOpacity(kv.Value, kv.Key);
            ApplyVariantLockVisuals(kv.Value, kv.Key);
            ApplyBanner(kv.Value, kv.Key);
            ApplyLevelBadge(kv.Value, kv.Key);
        }
    }

    void ApplyLevelBadge(Button btn, MonsterData md)
    {
        if (!btn || !md) return;

        var view = btn.GetComponent<MonsterButtonView>();
        if (!view) return;

        bool unlocked = UnlockStore.IsUnlocked(md);
        int lvl = MonsterProgressStore.GetPermanentLevel(md.monsterName);

        if (view.levelBg)
        {
            view.levelBg.SetActive(view.LevelBgDefaultActive && unlocked);
        }

        string s = unlocked ? $"{lvl}" : "";

        if (view.levelText)
        {
            view.levelText.gameObject.SetActive(unlocked);
            view.levelText.text = s;
        }

        if (view.levelShadowText)
        {
            view.levelShadowText.gameObject.SetActive(unlocked);
            view.levelShadowText.text = s;
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

        if (previewNameText)
        {
            bool unlocked = UnlockStore.IsUnlocked(md);
            int lvl = MonsterProgressStore.GetPermanentLevel(md.monsterName);
            previewNameText.text = unlocked ? $"{md.monsterName}  Lv.{lvl}" : md.monsterName;
        }

        if (previewDescriptionText)
            previewDescriptionText.text = BuildPreviewDescription(md);

        if (previewStatsText)
            previewStatsText.text = BuildPreviewStatsString(md);

        ApplyPreviewTextVisibility();
        SetupPreviewUnlockUI(md, skinIdx);
    }

    string BuildPreviewDescription(MonsterData md)
    {
        if (!md) return "";

        int lvl = MonsterProgressStore.GetPermanentLevel(md.monsterName);
        string passiveBlock = MonsterPassiveSystem.GetPassivePreviewBlock(md, lvl);

        if (string.IsNullOrEmpty(passiveBlock))
            return md.monsterDescription;

        return $"{md.monsterDescription}\n\n{passiveBlock}";
    }

    string BuildPreviewStatsString(MonsterData md)
    {
        if (!md) return "";

        int lvl = MonsterProgressStore.GetPermanentLevel(md.monsterName);
        float xpInto = MonsterProgressStore.GetPermanentXpIntoLevel(md.monsterName);

        var baseStats = MonsterLeveling.GetLeveledStats(md, lvl);

        int atkLvl = ShopBuffStore.GetLevel(ShopBuffType.AttackUp);
        int hpLvl = ShopBuffStore.GetLevel(ShopBuffType.HpUp);
        int healLvl = ShopBuffStore.GetLevel(ShopBuffType.HealPower);

        float baseAttack = baseStats.attackPower;
        float baseMaxHp = baseStats.maxHealth;
        float baseHeal = baseStats.healAmount;
        float baseRange = baseStats.healRange;
        float baseSpeed = baseStats.healSpeed;
        float baseSpecial = baseStats.specialGaugeGain;

        float totalAttack = baseAttack + atkLvl;
        float totalMaxHp = baseMaxHp + (hpLvl * 5f);
        float totalHeal = (baseHeal > 0f) ? (baseHeal + (healLvl * 2f)) : 0f;

        string roleLine = $"Role: {md.role}";
        string levelLine = $"Level: {lvl}  ({xpInto:0.#}/{MonsterLeveling.XpPerLevel})";

        string hpLine = $"Max HP: {baseMaxHp:0.#}  (+{hpLvl * 5}) = {totalMaxHp:0.#}";
        string atkLine = $"Attack: {baseAttack:0.#}  (+{atkLvl}) = {totalAttack:0.#}";
        string specialLine = $"Special Gain: {baseSpecial:0.#}";

        string healBlock;
        if (baseHeal > 0f)
        {
            healBlock =
                $"Heal: {baseHeal:0.#}  (+{healLvl * 2}) = {totalHeal:0.#}\n" +
                $"Heal Range: {baseRange:0.#}\n" +
                $"Heal Speed: {baseSpeed:0.#}s";
        }
        else
        {
            healBlock = "Heal: -";
        }

        return
            $"Base Stats + Shop Buff = Total Stats\n" +
            $"{roleLine}\n" +
            $"{levelLine}\n" +
            $"{hpLine}\n" +
            $"{atkLine}\n" +
            $"{specialLine}\n" +
            $"{healBlock}";
    }

    void ClearPreview()
    {
        if (previewImage) previewImage.sprite = null;
        if (previewNameText) previewNameText.text = "";
        if (previewDescriptionText) previewDescriptionText.text = "";
        if (previewStatsText) previewStatsText.text = "";
        ApplyPreviewTextVisibility();
    }

    void SetButtonHighlight(Button btn, bool on)
    {
        if (!btn) return;

        var frame = btn.transform.Find("Highlight")?.GetComponent<Image>();
        if (frame) frame.enabled = on;

        ApplySelectionAlpha(btn, on);

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
        RefreshTitleMenuLoadoutUI();
        gameObject.SetActive(false);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (!root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    Image GetPortraitImage(Button btn)
    {
        if (!btn) return null;

        var t = FindDeep(btn.transform, portraitImageChildName);
        return t ? t.GetComponent<Image>() : null;
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
                previewMonster = md;
                previewSkinIndex = idx;
                ShowPreview(md, idx);

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
            bool wasUnlocked = MonsterSkinStore.IsUnlocked(md, skinIdx);
            MonsterSkinStore.Unlock(md, skinIdx);

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

    MonsterRole ResolveRole(MonsterData md)
    {
        return md ? md.role : MonsterRole.Attack;
    }

    Sprite GetBannerSpriteForRole(MonsterRole role)
    {
        return role switch
        {
            MonsterRole.Attack => attackBannerSprite,
            MonsterRole.Defense => defenseBannerSprite,
            MonsterRole.Healer => healerBannerSprite,
            _ => null
        };
    }

    void ApplyBanner(Button btn, MonsterData md)
    {
        if (!btn) return;

        var bannerT = FindDeep(btn.transform, bannerImageChildName);
        if (!bannerT) return;

        var bannerImg = bannerT.GetComponent<Image>();
        if (!bannerImg) return;

        var sprite = GetBannerSpriteForRole(ResolveRole(md));
        if (sprite)
        {
            bannerImg.sprite = sprite;
            bannerImg.enabled = true;
        }
        else
        {
            bannerImg.enabled = false;
        }
    }

    Image GetBannerImage(Button btn)
    {
        if (!btn) return null;

        var t = FindDeep(btn.transform, bannerImageChildName);
        return t ? t.GetComponent<Image>() : null;
    }

    static void SetImageAlpha(Image img, float alpha)
    {
        if (!img) return;

        var c = img.color;
        c.a = Mathf.Clamp01(alpha);
        img.color = c;
    }

    void ApplySelectionAlpha(Button btn, bool selectedOn)
    {
        float a = selectedOn ? selectedAlpha : deselectedAlpha;

        SetImageAlpha(GetPortraitImage(btn), a);

        float bannerA = selectedOn ? selectedAlpha : (deselectedAlpha * deselectedBannerMultiplier);
        SetImageAlpha(GetBannerImage(btn), bannerA);
    }

    void RefreshTitleMenuLoadoutUI()
    {
        var titleMenu = FindFirstObjectByType<TitleMenuUI>(FindObjectsInactive.Include);
        if (titleMenu)
            titleMenu.RefreshSelectedLoadoutUI();
    }
}