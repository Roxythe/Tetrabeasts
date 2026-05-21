using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VolumePanelUI : MonoBehaviour
{
    static readonly string[] MusicModeOptionLabels = { "EDM", "Metal", "Random" };

    [Header("Refs")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider cursorSizeSlider;
    public UICursorController uiCursor;
    public Button closeButton;
    public TMP_Dropdown musicModeDropdown; // 0=EDM, 1=Metal, 2=Both
    public TMP_Dropdown languageDropdown;
    public TMP_Text languageLabel;
    public Toggle combatLogToggle;
    public GameObject combatLogRoot;
    public Toggle skipIntroToggle;
    public GameController gameplayController;

    [Header("Controls")]
    public Button controlsButton;
    public GameObject controlsPanelRoot;
    [SerializeField] TMP_Dropdown controlsProfileDropdown;
    [SerializeField] Transform controlsRowsRoot;
    [SerializeField] Button controlsResetButton;
    [SerializeField] Button controlsBackButton;
    [SerializeField] bool buildControlsPanelIfEmpty = true;
    [SerializeField] bool focusFirstSelectableWhenOpened = true;
    [SerializeField] bool trapNavigationWhileOpen = true;

    [Header("SFX Preview")]
    public bool previewOnChange = true;
    [Range(0.05f, 0.5f)] public float previewDelay = 0.15f;
    Coroutine sfxPreviewCo;

    [Header("Pause")]
    public bool pauseWhenOpen = true;

    CanvasGroup cg;
    readonly Dictionary<Selectable, bool> modalScopedSelectables = new();

    void Awake()
    {
        TetrabeastsLocalization.EnsureInitialized();
        EnsureControlsRefs();
        EnsureLanguageDropdown();
        RefreshMusicModeDropdown();
        EnsureControlsPanelUI();

        cg = GetComponent<CanvasGroup>();
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (controlsButton)
        {
            controlsButton.onClick.RemoveListener(ToggleControlsPanel);
            controlsButton.onClick.AddListener(ToggleControlsPanel);
        }

        // Wire slider callbacks once
        if (masterSlider) masterSlider.onValueChanged.AddListener(SetMaster);
        if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusic);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFX);
        if (cursorSizeSlider) cursorSizeSlider.onValueChanged.AddListener(SetCursorSize);
        if (musicModeDropdown) musicModeDropdown.onValueChanged.AddListener(SetMusicMode);
        if (languageDropdown) languageDropdown.onValueChanged.AddListener(SetLanguage);
        if (combatLogToggle) combatLogToggle.onValueChanged.AddListener(SetCombatLogVisible);
        if (skipIntroToggle) skipIntroToggle.onValueChanged.AddListener(SetSkipIntroEnabled);
        HookControlsPanelCallbacks();
    }

    void OnEnable()
    {
        // Apply saved settings first then snap sliders
        SettingsStore.ApplySavedVolumesToAudio();

        if (AudioManager.I)
        {
            masterSlider?.SetValueWithoutNotify(AudioManager.I.masterVolume);
            musicSlider?.SetValueWithoutNotify(AudioManager.I.musicVolume);
            sfxSlider?.SetValueWithoutNotify(AudioManager.I.sfxVolume);
        }

        // Cursor size uses SettingsStore
        cursorSizeSlider?.SetValueWithoutNotify(SettingsStore.LoadCursorScale());

        if (musicModeDropdown && AudioManager.I)
            musicModeDropdown.SetValueWithoutNotify((int)AudioManager.I.GetMusicMode());

        RefreshLanguageControls();
        TetrabeastsLocalization.LanguageChanged += RefreshLanguageControls;

        if (pauseWhenOpen) Time.timeScale = 0f;

        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

        if (combatLogRoot)
            combatLogRoot.SetActive(SettingsStore.LoadCombatLogEnabled());

        if (!combatLogRoot)
        {
            var log = FindFirstObjectByType<BattleLogUI>(FindObjectsInactive.Include);
            combatLogRoot = log ? log.gameObject : null;
        }

        if (combatLogToggle)
            combatLogToggle.SetIsOnWithoutNotify(SettingsStore.LoadCombatLogEnabled());

        if (skipIntroToggle)
            skipIntroToggle.SetIsOnWithoutNotify(SettingsStore.LoadSkipIntroEnabled());

        TetrabeastsControls.ProfileChanged += HandleControlsProfileChanged;
        RefreshControlsPanel();
        CloseControlsPanel(true);
        RefreshModalSelectableScope();

        if (focusFirstSelectableWhenOpened)
            StartCoroutine(SelectFirstVisibleControlNextFrame(gameObject));
    }

    void OnDisable()
    {
        TetrabeastsLocalization.LanguageChanged -= RefreshLanguageControls;
        TetrabeastsControls.ProfileChanged -= HandleControlsProfileChanged;
        RestoreModalSelectableScope();
        if (pauseWhenOpen) Time.timeScale = 1f;
    }

    void SetMaster(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMasterVolume(v);
        SettingsStore.SaveVolumes(v, SettingsStore.LoadMusic(), SettingsStore.LoadSFX());
    }

    void SetMusic(float v)
    {
        if (AudioManager.I) AudioManager.I.SetMusicVolume(v);
        SettingsStore.SaveVolumes(SettingsStore.LoadMaster(), v, SettingsStore.LoadSFX());
    }

    void SetSFX(float v)
    {
        if (AudioManager.I) AudioManager.I.SetSFXVolume(v);
        SettingsStore.SaveVolumes(SettingsStore.LoadMaster(), SettingsStore.LoadMusic(), v);

        if (!previewOnChange) return;

        // Play once after the slider settles
        if (sfxPreviewCo != null) StopCoroutine(sfxPreviewCo);
        sfxPreviewCo = StartCoroutine(PreviewAfterDelay());
    }

    void SetCursorSize(float v)
    {
        uiCursor?.SetScale(v);
        SettingsStore.SaveCursorScale(v);
    }

    void SetMusicMode(int index)
    {
        if (!AudioManager.I) return;

        var mode = (AudioManager.MusicMode)Mathf.Clamp(index, 0, 2);
        AudioManager.I.SetMusicMode(mode);
    }

    void SetLanguage(int index)
    {
        TetrabeastsLocalization.SetLanguageByDropdownIndex(index, persist: true);
        RefreshLanguageControls();
    }

    void RefreshLanguageControls()
    {
        if (languageLabel)
            languageLabel.text = TetrabeastsLocalization.GetText("settings_language_label");

        RefreshMusicModeDropdown();
        RefreshControlsPanel();

        if (languageDropdown)
            TetrabeastsLocalization.PopulateLanguageDropdown(languageDropdown);
    }

    void RefreshMusicModeDropdown()
    {
        if (!musicModeDropdown)
            return;

        int selectedIndex = AudioManager.I ? (int)AudioManager.I.GetMusicMode() : musicModeDropdown.value;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, MusicModeOptionLabels.Length - 1);

        var options = new List<TMP_Dropdown.OptionData>(MusicModeOptionLabels.Length);
        for (int i = 0; i < MusicModeOptionLabels.Length; i++)
            options.Add(new TMP_Dropdown.OptionData(TetrabeastsLocalization.LocalizeText(MusicModeOptionLabels[i])));

        musicModeDropdown.ClearOptions();
        musicModeDropdown.AddOptions(options);
        musicModeDropdown.SetValueWithoutNotify(selectedIndex);
        musicModeDropdown.RefreshShownValue();
    }

    void EnsureLanguageDropdown()
    {
        if (languageDropdown || !musicModeDropdown)
            return;

        var parent = musicModeDropdown.transform.parent;
        if (!parent)
            return;

        var languageDropdownInstance = Instantiate(musicModeDropdown, parent);
        languageDropdownInstance.name = "Language_Dropdown";
        languageDropdown = languageDropdownInstance;

        var dropdownRect = languageDropdown.GetComponent<RectTransform>();
        var sourceRect = musicModeDropdown.GetComponent<RectTransform>();
        if (dropdownRect && sourceRect)
            dropdownRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -40f);

        var musicLabel = parent.Find("MusicGenre_Text")?.GetComponent<TMP_Text>();
        if (musicLabel)
        {
            languageLabel = Instantiate(musicLabel, parent);
            languageLabel.name = "Language_Text";
            languageLabel.text = TetrabeastsLocalization.GetText("settings_language_label");

            var labelRect = languageLabel.GetComponent<RectTransform>();
            var musicLabelRect = musicLabel.GetComponent<RectTransform>();
            if (labelRect && musicLabelRect)
                labelRect.anchoredPosition = musicLabelRect.anchoredPosition + new Vector2(0f, -40f);
        }

        TetrabeastsLocalization.PopulateLanguageDropdown(languageDropdown);
    }

    void EnsureControlsRefs()
    {
        if (!controlsButton)
            controlsButton = FindChildComponent<Button>(transform, "Controls_Button");

        if (!controlsPanelRoot)
        {
            var panel = FindDeepChild(transform, "Controls_Panel");
            if (panel)
                controlsPanelRoot = panel.gameObject;
        }
    }

    void EnsureControlsPanelUI()
    {
        EnsureControlsRefs();

        if (!controlsPanelRoot || !buildControlsPanelIfEmpty)
            return;

        Transform existingRoot = FindDeepChild(controlsPanelRoot.transform, "Controls_RuntimeRoot");
        if (existingRoot)
        {
            if (!controlsRowsRoot)
                controlsRowsRoot = FindDeepChild(existingRoot, "Controls_Rows");

            if (!controlsProfileDropdown)
                controlsProfileDropdown = existingRoot.GetComponentInChildren<TMP_Dropdown>(true);

            if (!controlsResetButton)
                controlsResetButton = FindChildComponent<Button>(existingRoot, "Controls_Reset_Button");

            if (!controlsBackButton)
                controlsBackButton = FindChildComponent<Button>(existingRoot, "Controls_Back_Button");

            return;
        }

        var panelImage = controlsPanelRoot.GetComponent<Image>();
        if (panelImage && panelImage.color.a < 0.5f)
            panelImage.color = new Color(0.035f, 0.035f, 0.035f, 0.95f);

        var root = new GameObject("Controls_RuntimeRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(controlsPanelRoot.transform, false);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.08f, 0.08f);
        rootRect.anchorMax = new Vector2(0.92f, 0.86f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var rootLayout = root.GetComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 12, 12);
        rootLayout.spacing = 12f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        float profileRowHeight = 48f;
        var profileRow = CreateHorizontalLayout(root.transform, "Controls_Profile_Row", 10f, profileRowHeight);
        var profileLabel = CreateText(
            profileRow,
            "Controls_Profile_Label",
            "Control Profile",
            26f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            Color.white);
        AddLayout(profileLabel.gameObject, 280f, profileRowHeight, flexibleWidth: 0f);

        controlsProfileDropdown = CreateProfileDropdown(profileRow);
        AddLayout(controlsProfileDropdown.gameObject, 420f, profileRowHeight, flexibleWidth: 1f);

        var divider = CreateImage(root.transform, "Controls_Divider", new Color(1f, 0.62f, 0.08f, 0.65f));
        AddLayout(divider.gameObject, -1f, 2f, flexibleWidth: 1f);

        var rowsGo = new GameObject("Controls_Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rowsGo.transform.SetParent(root.transform, false);
        controlsRowsRoot = rowsGo.transform;

        var rowsLayout = rowsGo.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 5f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        AddLayout(rowsGo, -1f, -1f, flexibleWidth: 1f, flexibleHeight: 1f);

        float footerButtonHeight = 54f;
        var buttonRow = CreateHorizontalLayout(root.transform, "Controls_Button_Row", 12f, footerButtonHeight);
        controlsResetButton = CreateSimpleButton(buttonRow, "Controls_Reset_Button", "Reset Defaults");
        controlsBackButton = CreateSimpleButton(buttonRow, "Controls_Back_Button", "Back");
        AddLayout(controlsResetButton.gameObject, 210f, footerButtonHeight);
        AddLayout(controlsBackButton.gameObject, 160f, footerButtonHeight);
    }

    void HookControlsPanelCallbacks()
    {
        if (controlsProfileDropdown)
        {
            controlsProfileDropdown.onValueChanged.RemoveListener(SetControlsProfile);
            controlsProfileDropdown.onValueChanged.AddListener(SetControlsProfile);
        }

        if (controlsResetButton)
        {
            controlsResetButton.onClick.RemoveListener(ResetControlsToPlatformDefault);
            controlsResetButton.onClick.AddListener(ResetControlsToPlatformDefault);
        }

        if (controlsBackButton)
        {
            controlsBackButton.onClick.RemoveListener(CloseControlsPanel);
            controlsBackButton.onClick.AddListener(CloseControlsPanel);
        }
    }

    TMP_Dropdown CreateProfileDropdown(Transform parent)
    {
        TMP_Dropdown dropdown = null;
        TMP_Dropdown source = languageDropdown ? languageDropdown : musicModeDropdown;

        if (source)
        {
            dropdown = Instantiate(source, parent);
            dropdown.name = "Controls_Profile_Dropdown";
            dropdown.onValueChanged.RemoveAllListeners();
        }

        if (!dropdown)
        {
            var go = new GameObject("Controls_Profile_Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(parent, false);
            dropdown = go.GetComponent<TMP_Dropdown>();
            go.GetComponent<Image>().color = new Color(0.98f, 0.68f, 0.17f, 1f);
        }

        return dropdown;
    }

    void RefreshControlsPanel()
    {
        EnsureControlsPanelUI();
        PopulateControlsProfileDropdown();
        RefreshControlsRows();
    }

    void PopulateControlsProfileDropdown()
    {
        if (!controlsProfileDropdown)
            return;

        var profiles = TetrabeastsControls.SelectableProfiles;
        var options = new List<TMP_Dropdown.OptionData>(profiles.Count);

        for (int i = 0; i < profiles.Count; i++)
            options.Add(new TMP_Dropdown.OptionData(TetrabeastsControls.GetProfileLabel(profiles[i])));

        int selectedIndex = GetSavedControlsProfileIndex();

        controlsProfileDropdown.ClearOptions();
        controlsProfileDropdown.AddOptions(options);
        controlsProfileDropdown.SetValueWithoutNotify(selectedIndex);
        controlsProfileDropdown.RefreshShownValue();
    }

    void RefreshControlsRows()
    {
        if (!controlsRowsRoot)
            return;

        var rows = TetrabeastsControls.GetBindingRows(TetrabeastsControls.SavedProfile);
        var usedRows = new HashSet<Transform>();

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            Transform rowRoot = FindDirectChild(controlsRowsRoot, $"Controls_Row_{row.Action}");
            if (!rowRoot)
                rowRoot = CreateControlsBindingRow(controlsRowsRoot, row.Action);

            rowRoot.gameObject.SetActive(true);
            UpdateControlsBindingRow(rowRoot, row, i);
            usedRows.Add(rowRoot);
        }

        for (int i = 0; i < controlsRowsRoot.childCount; i++)
        {
            Transform child = controlsRowsRoot.GetChild(i);
            if (child && child.name.StartsWith("Controls_Row_") && !usedRows.Contains(child))
                child.gameObject.SetActive(false);
        }
    }

    Transform CreateControlsBindingRow(Transform parent, TetrabeastsControlAction action)
    {
        var rowRoot = CreateHorizontalLayout(parent, $"Controls_Row_{action}", 10f, 38f);
        var bg = rowRoot.gameObject.AddComponent<Image>();
        bg.raycastTarget = false;

        var actionText = CreateText(rowRoot, "Action_Text", string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        var bindingText = CreateText(rowRoot, "Binding_Text", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Right);

        AddLayout(actionText.gameObject, 300f, 38f, flexibleWidth: 0f);
        AddLayout(bindingText.gameObject, 360f, 38f, flexibleWidth: 1f);

        return rowRoot;
    }

    void UpdateControlsBindingRow(Transform rowRoot, TetrabeastsControlBindingRow row, int rowIndex)
    {
        var bg = rowRoot.GetComponent<Image>();
        if (bg)
        {
            bg.color = rowIndex % 2 == 0
                ? new Color(1f, 1f, 1f, 0.055f)
                : new Color(0f, 0f, 0f, 0.14f);
            bg.raycastTarget = false;
        }

        var actionText = FindChildComponent<TMP_Text>(rowRoot, "Action_Text");
        if (actionText)
            actionText.text = row.ActionLabel;

        var bindingText = FindChildComponent<TMP_Text>(rowRoot, "Binding_Text");
        if (bindingText)
            bindingText.text = row.BindingLabel;
    }

    Transform CreateHorizontalLayout(Transform parent, string name, float spacing, float preferredHeight = 42f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        AddLayout(go, -1f, preferredHeight, flexibleWidth: 1f);
        return go.transform;
    }

    TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        return CreateText(parent, name, text, fontSize, style, alignment, Color.white);
    }

    TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;

        return tmp;
    }

    Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    Button CreateSimpleButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.98f, 0.68f, 0.17f, 1f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var labelText = CreateText(
            go.transform,
            "Text (TMP)",
            label,
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Color(0.12f, 0.1f, 0.08f, 1f));

        var labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        return button;
    }

    void AddLayout(GameObject go, float preferredWidth, float preferredHeight, float flexibleWidth = 0f, float flexibleHeight = 0f)
    {
        var layout = go.GetComponent<LayoutElement>();
        if (!layout)
            layout = go.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f)
            layout.preferredWidth = preferredWidth;

        if (preferredHeight >= 0f)
            layout.preferredHeight = preferredHeight;

        layout.flexibleWidth = flexibleWidth;
        layout.flexibleHeight = flexibleHeight;
    }

    int GetSavedControlsProfileIndex()
    {
        var profiles = TetrabeastsControls.SelectableProfiles;
        var saved = TetrabeastsControls.SavedProfile;

        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i] == saved)
                return i;
        }

        return 0;
    }

    void SetControlsProfile(int index)
    {
        var profiles = TetrabeastsControls.SelectableProfiles;
        if (index < 0 || index >= profiles.Count)
            return;

        TetrabeastsControls.SaveProfile(profiles[index]);
        RefreshControlsPanel();
    }

    void ResetControlsToPlatformDefault()
    {
        TetrabeastsControls.ResetToPlatformDefault();
        RefreshControlsPanel();
        SelectSelectable(controlsProfileDropdown);
    }

    void HandleControlsProfileChanged(TetrabeastsControlProfile _)
    {
        RefreshControlsPanel();
    }

    public void ToggleControlsPanel()
    {
        if (!controlsPanelRoot)
            return;

        if (UIPanelTransition.IsVisible(controlsPanelRoot))
            CloseControlsPanel();
        else
            OpenControlsPanel();
    }

    public void OpenControlsPanel()
    {
        if (!controlsPanelRoot)
            return;

        RefreshControlsPanel();
        controlsPanelRoot.transform.SetAsLastSibling();
        UIPanelTransition.Show(controlsPanelRoot);
        RefreshModalSelectableScope();

        if (focusFirstSelectableWhenOpened)
            StartCoroutine(SelectFirstVisibleControlNextFrame(controlsPanelRoot));
    }

    public void CloseControlsPanel()
    {
        CloseControlsPanel(false);
    }

    public void CloseControlsPanel(bool instant)
    {
        if (!controlsPanelRoot)
            return;

        UIPanelTransition.Hide(controlsPanelRoot, instant);

        RefreshModalSelectableScope();

        if (!instant)
            SelectSelectable(controlsButton);
    }

    System.Collections.IEnumerator PreviewAfterDelay()
    {
        // Works while paused
        yield return new WaitForSecondsRealtime(previewDelay);

        if (AudioManager.I)
            AudioManager.I.PlayPreviewSFX();   // Plays a random line-clear 
    }

    void BringToFront() => transform.SetAsLastSibling();

    public void Open()
    {
        UIPanelTransition.Show(gameObject);
        RefreshModalSelectableScope();
    }
    public void Close() { Close(false); }
    public void Close(bool instant)
    {
        RestoreModalSelectableScope();
        UIPanelTransition.Hide(gameObject, instant);
    }

    public void Toggle()
    {
        if (UIPanelTransition.IsVisible(gameObject)) Close();
        else Open();
    }

    void SetCombatLogVisible(bool isOn)
    {
        SettingsStore.SaveCombatLogEnabled(isOn);

        var log = FindFirstObjectByType<BattleLogUI>(FindObjectsInactive.Include);
        if (log) log.SetVisible(isOn);
    }

    void SetSkipIntroEnabled(bool isOn)
    {
        SettingsStore.SaveSkipIntroEnabled(isOn);
    }

    public void OnSaveAndQuitPressed()
    {
        var controller = GetGameplayController();
        if (!controller)
        {
            Debug.LogWarning("VolumePanelUI: No GameController found for Save and Quit.");
            return;
        }

        controller.RequestSaveAndQuit();
    }

    GameController GetGameplayController()
    {
        if (!gameplayController)
            gameplayController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);

        return gameplayController;
    }

    void Update()
    {
        if (!UIPanelTransition.IsVisible(gameObject))
            return;

        HandleTabNavigation();

        bool pausePressed = TetrabeastsControls.WasPressed(TetrabeastsControlAction.Pause);
        bool cancelPressed = TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuCancel);

        if (!pausePressed && !cancelPressed)
            return;

        if (pausePressed && ShouldLetGameplayPauseHandleEscape())
            return;

        if (controlsPanelRoot && UIPanelTransition.IsVisible(controlsPanelRoot))
        {
            CloseControlsPanel();
            return;
        }

        if (!ConfirmationPopupUI.TryCancelShowingPopup())
            Toggle();
    }

    void HandleTabNavigation()
    {
        GameObject navigationRoot = GetCurrentNavigationRoot();
        if (!navigationRoot)
            return;

        if (WasTabPressedThisFrame())
        {
            SelectRelativeSelectable(navigationRoot, IsShiftHeld() ? -1 : 1);
            return;
        }

        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (ShouldPullSelectionBackToScope(current, navigationRoot))
            SelectFirstVisibleSelectable(navigationRoot);
    }

    GameObject GetCurrentNavigationRoot()
    {
        if (controlsPanelRoot && UIPanelTransition.IsVisible(controlsPanelRoot))
            return controlsPanelRoot;

        return gameObject;
    }

    void RefreshModalSelectableScope()
    {
        RestoreModalSelectableScope();

        if (!trapNavigationWhileOpen || !UIPanelTransition.IsVisible(gameObject))
            return;

        GameObject navigationRoot = GetCurrentNavigationRoot();
        if (!navigationRoot)
            return;

        var selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < selectables.Length; i++)
        {
            var selectable = selectables[i];
            if (!selectable || selectable.transform.IsChildOf(navigationRoot.transform))
                continue;

            modalScopedSelectables[selectable] = selectable.interactable;
            selectable.interactable = false;
        }

        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (ShouldPullSelectionBackToScope(current, navigationRoot))
            SelectFirstVisibleSelectable(navigationRoot);
    }

    void RestoreModalSelectableScope()
    {
        foreach (var pair in modalScopedSelectables)
        {
            if (pair.Key)
                pair.Key.interactable = pair.Value;
        }

        modalScopedSelectables.Clear();
    }

    bool ShouldPullSelectionBackToScope(GameObject current, GameObject navigationRoot)
    {
        if (!navigationRoot)
            return false;

        if (!current)
            return true;

        if (current.transform.IsChildOf(navigationRoot.transform))
            return false;

        var selectable = current.GetComponentInParent<Selectable>();
        return !selectable || modalScopedSelectables.ContainsKey(selectable);
    }

    IEnumerator SelectFirstVisibleControlNextFrame(GameObject root)
    {
        yield return null;

        if (!root || !root.activeInHierarchy)
            yield break;

        SelectFirstVisibleSelectable(root);
    }

    void SelectFirstVisibleSelectable(GameObject root)
    {
        var selectables = GetUsableSelectables(root);
        if (selectables.Count == 0)
            return;

        SelectSelectable(selectables[0]);
    }

    void SelectRelativeSelectable(GameObject root, int direction)
    {
        var selectables = GetUsableSelectables(root);
        if (selectables.Count == 0)
            return;

        var eventSystem = EventSystem.current;
        GameObject current = eventSystem ? eventSystem.currentSelectedGameObject : null;

        int currentIndex = -1;
        if (current)
        {
            for (int i = 0; i < selectables.Count; i++)
            {
                if (selectables[i] && selectables[i].gameObject == current)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int nextIndex = currentIndex < 0
            ? (direction >= 0 ? 0 : selectables.Count - 1)
            : (currentIndex + direction + selectables.Count) % selectables.Count;

        SelectSelectable(selectables[nextIndex]);
    }

    List<Selectable> GetUsableSelectables(GameObject root)
    {
        var usable = new List<Selectable>();
        if (!root)
            return usable;

        var selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            var selectable = selectables[i];
            if (!selectable || !selectable.isActiveAndEnabled || !selectable.interactable || !selectable.gameObject.activeInHierarchy)
                continue;

            usable.Add(selectable);
        }

        return usable;
    }

    void SelectSelectable(Selectable selectable)
    {
        if (!selectable || !selectable.gameObject.activeInHierarchy || !selectable.interactable || !EventSystem.current)
            return;

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    static bool WasTabPressedThisFrame()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        pressed |= keyboard != null && keyboard.tabKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.Tab);
#endif

        return pressed;
    }

    static bool IsShiftHeld()
    {
        bool held = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        held |= keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        held |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

        return held;
    }

    static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        var child = FindDeepChild(root, childName);
        return child ? child.GetComponent<T>() : null;
    }

    static Transform FindDirectChild(Transform root, string childName)
    {
        if (!root || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child && child.name == childName)
                return child;
        }

        return null;
    }

    static Transform FindDeepChild(Transform root, string childName)
    {
        if (!root || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), childName);
            if (found)
                return found;
        }

        return null;
    }

    bool ShouldLetGameplayPauseHandleEscape()
    {
        return !pauseWhenOpen && GetGameplayController() && GetGameplayController().IsPaused;
    }
}
