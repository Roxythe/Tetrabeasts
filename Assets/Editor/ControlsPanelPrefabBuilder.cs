using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ControlsPanelPrefabBuilder
{
    const string VolumePanelPrefabPath = "Assets/Prefabs/Panels/Volume_Panel.prefab";

    static readonly TetrabeastsControlAction[] DisplayedActions =
    {
        TetrabeastsControlAction.MoveLeft,
        TetrabeastsControlAction.MoveRight,
        TetrabeastsControlAction.SoftDrop,
        TetrabeastsControlAction.RotateClockwise,
        TetrabeastsControlAction.RotateCounterClockwise,
        TetrabeastsControlAction.HardDrop,
        TetrabeastsControlAction.Special,
        TetrabeastsControlAction.Pause,
        TetrabeastsControlAction.MenuSubmit,
        TetrabeastsControlAction.MenuCancel
    };

    [MenuItem("Tetrabeasts/UI/Rebuild Volume Controls Panel")]
    public static void RebuildVolumeControlsPanel()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(VolumePanelPrefabPath);
        try
        {
            var panelUI = prefabRoot.GetComponent<VolumePanelUI>();
            if (!panelUI)
                throw new System.InvalidOperationException("Volume_Panel prefab is missing VolumePanelUI.");

            Transform controlsPanel = panelUI.controlsPanelRoot
                ? panelUI.controlsPanelRoot.transform
                : FindDeepChild(prefabRoot.transform, "Controls_Panel");

            if (!controlsPanel)
                throw new System.InvalidOperationException("Volume_Panel prefab is missing Controls_Panel.");

            panelUI.controlsPanelRoot = controlsPanel.gameObject;

            Transform existingRoot = FindDeepChild(controlsPanel, "Controls_RuntimeRoot");
            if (existingRoot)
                Object.DestroyImmediate(existingRoot.gameObject);

            TMP_Text sourceText = prefabRoot.GetComponentInChildren<TMP_Text>(true);

            var runtimeRoot = CreateObject<VerticalLayoutGroup>("Controls_RuntimeRoot", controlsPanel);
            var runtimeRect = runtimeRoot.GetComponent<RectTransform>();
            runtimeRect.anchorMin = new Vector2(0.08f, 0.08f);
            runtimeRect.anchorMax = new Vector2(0.92f, 0.86f);
            runtimeRect.offsetMin = Vector2.zero;
            runtimeRect.offsetMax = Vector2.zero;

            var rootLayout = runtimeRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(18, 18, 12, 12);
            rootLayout.spacing = 12f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            var profileRow = CreateHorizontalLayout("Controls_Profile_Row", runtimeRoot.transform, 10f, 48f);
            var profileLabel = CreateText("Controls_Profile_Label", profileRow.transform, "Control Profile", 26f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, sourceText);
            AddLayout(profileLabel.gameObject, 280f, 48f);

            TMP_Dropdown sourceDropdown = panelUI.languageDropdown ? panelUI.languageDropdown : panelUI.musicModeDropdown;
            TMP_Dropdown profileDropdown = sourceDropdown
                ? Object.Instantiate(sourceDropdown, profileRow.transform)
                : CreateFallbackDropdown(profileRow.transform);
            profileDropdown.name = "Controls_Profile_Dropdown";
            profileDropdown.onValueChanged.RemoveAllListeners();
            AddLayout(profileDropdown.gameObject, 420f, 48f, 1f);

            var divider = CreateObject<Image>("Controls_Divider", runtimeRoot.transform);
            divider.color = new Color(1f, 0.62f, 0.08f, 0.65f);
            divider.raycastTarget = false;
            AddLayout(divider.gameObject, -1f, 2f, 1f);

            var rowsRoot = CreateObject<VerticalLayoutGroup>("Controls_Rows", runtimeRoot.transform);
            var rowsLayout = rowsRoot.GetComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 5f;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            AddLayout(rowsRoot.gameObject, -1f, -1f, 1f, 1f);

            for (int i = 0; i < DisplayedActions.Length; i++)
                CreateBindingRow(rowsRoot.transform, DisplayedActions[i], i, sourceText);

            var buttonRow = CreateHorizontalLayout("Controls_Button_Row", runtimeRoot.transform, 12f, 54f);
            Button resetButton = CreateButton("Controls_Reset_Button", buttonRow.transform, "Reset Defaults", sourceText);
            Button backButton = CreateButton("Controls_Back_Button", buttonRow.transform, "Back", sourceText);
            AddLayout(resetButton.gameObject, 210f, 54f);
            AddLayout(backButton.gameObject, 160f, 54f);

            var serializedPanel = new SerializedObject(panelUI);
            serializedPanel.FindProperty("controlsProfileDropdown").objectReferenceValue = profileDropdown;
            serializedPanel.FindProperty("controlsRowsRoot").objectReferenceValue = rowsRoot.transform;
            serializedPanel.FindProperty("controlsResetButton").objectReferenceValue = resetButton;
            serializedPanel.FindProperty("controlsBackButton").objectReferenceValue = backButton;
            serializedPanel.FindProperty("buildControlsPanelIfEmpty").boolValue = true;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, VolumePanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tetrabeasts/UI/Sync Existing Volume Controls Panel")]
    public static void SyncExistingVolumeControlsPanel()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(VolumePanelPrefabPath);
        try
        {
            var panelUI = prefabRoot.GetComponent<VolumePanelUI>();
            if (!panelUI)
                throw new System.InvalidOperationException("Volume_Panel prefab is missing VolumePanelUI.");

            Transform controlsPanel = panelUI.controlsPanelRoot
                ? panelUI.controlsPanelRoot.transform
                : FindDeepChild(prefabRoot.transform, "Controls_Panel");

            if (!controlsPanel)
                throw new System.InvalidOperationException("Volume_Panel prefab is missing Controls_Panel.");

            Transform runtimeRoot = FindDeepChild(controlsPanel, "Controls_RuntimeRoot");
            Transform rowsRoot = runtimeRoot ? FindDeepChild(runtimeRoot, "Controls_Rows") : null;
            if (!runtimeRoot || !rowsRoot)
                throw new System.InvalidOperationException("Volume_Panel prefab is missing Controls_RuntimeRoot or Controls_Rows. Use Rebuild Volume Controls Panel first.");

            TMP_Text sourceText = prefabRoot.GetComponentInChildren<TMP_Text>(true);
            SyncControlsRows(rowsRoot, sourceText);

            var serializedPanel = new SerializedObject(panelUI);
            serializedPanel.FindProperty("controlsProfileDropdown").objectReferenceValue = runtimeRoot.GetComponentInChildren<TMP_Dropdown>(true);
            serializedPanel.FindProperty("controlsRowsRoot").objectReferenceValue = rowsRoot;
            serializedPanel.FindProperty("controlsResetButton").objectReferenceValue = FindChildComponent<Button>(runtimeRoot, "Controls_Reset_Button");
            serializedPanel.FindProperty("controlsBackButton").objectReferenceValue = FindChildComponent<Button>(runtimeRoot, "Controls_Back_Button");
            serializedPanel.FindProperty("buildControlsPanelIfEmpty").boolValue = true;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, VolumePanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RebuildVolumeControlsPanelBatch()
    {
        RebuildVolumeControlsPanel();
    }

    public static void SyncExistingVolumeControlsPanelBatch()
    {
        SyncExistingVolumeControlsPanel();
    }

    static void SyncControlsRows(Transform rowsRoot, TMP_Text sourceText)
    {
        for (int i = rowsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rowsRoot.GetChild(i);
            if (!child || !child.name.StartsWith("Controls_Row_"))
                continue;

            string actionName = child.name.Substring("Controls_Row_".Length);
            if (!System.Enum.TryParse(actionName, out TetrabeastsControlAction action) ||
                System.Array.IndexOf(DisplayedActions, action) < 0)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        for (int i = 0; i < DisplayedActions.Length; i++)
        {
            var action = DisplayedActions[i];
            Transform row = FindDirectChild(rowsRoot, $"Controls_Row_{action}");
            if (!row)
            {
                CreateBindingRow(rowsRoot, action, i, sourceText);
                row = FindDirectChild(rowsRoot, $"Controls_Row_{action}");
            }

            if (!row)
                continue;

            row.SetSiblingIndex(i);
            SyncBindingRow(row, action, i, sourceText);
        }
    }

    static void SyncBindingRow(Transform row, TetrabeastsControlAction action, int rowIndex, TMP_Text sourceText)
    {
        var rowImage = row.GetComponent<Image>();
        if (rowImage)
        {
            rowImage.color = rowIndex % 2 == 0
                ? new Color(1f, 1f, 1f, 0.055f)
                : new Color(0f, 0f, 0f, 0.14f);
            rowImage.raycastTarget = false;
        }

        var actionText = FindChildComponent<TMP_Text>(row, "Action_Text");
        if (!actionText)
        {
            actionText = CreateText("Action_Text", row, string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, sourceText);
            actionText.transform.SetSiblingIndex(0);
            AddLayout(actionText.gameObject, 300f, 38f);
        }

        actionText.text = $" {TetrabeastsControls.GetActionLabel(action)}";

        var bindingButton = FindChildComponent<Button>(row, "Binding_Button");
        var bindingText = bindingButton
            ? FindChildComponent<TMP_Text>(bindingButton.transform, "Binding_Text")
            : FindChildComponent<TMP_Text>(row, "Binding_Text");

        if (!bindingButton)
            bindingButton = WrapBindingTextInButton(row, bindingText);

        if (!bindingText)
            bindingText = CreateText("Binding_Text", bindingButton.transform, string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Right, Color.white, sourceText);

        bindingText.transform.SetParent(bindingButton.transform, false);
        bindingText.raycastTarget = false;
        bindingText.richText = true;
        bindingText.text = TetrabeastsControls.GetBindingLabel(action, TetrabeastsControlProfile.PlatformDefault);
        StretchToFill(bindingText.rectTransform, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        EnsureBindingButtonGraphic(bindingButton);
    }

    static Button WrapBindingTextInButton(Transform row, TMP_Text bindingText)
    {
        int siblingIndex = bindingText ? bindingText.transform.GetSiblingIndex() : row.childCount;
        var oldLayout = bindingText ? bindingText.GetComponent<LayoutElement>() : null;
        float preferredWidth = oldLayout ? oldLayout.preferredWidth : 360f;
        float preferredHeight = oldLayout ? oldLayout.preferredHeight : 38f;
        float flexibleWidth = oldLayout ? oldLayout.flexibleWidth : 1f;
        float flexibleHeight = oldLayout ? oldLayout.flexibleHeight : 0f;

        var bindingButton = CreateBindingButton("Binding_Button", row);
        bindingButton.transform.SetSiblingIndex(siblingIndex);
        AddLayout(bindingButton.gameObject, preferredWidth, preferredHeight, flexibleWidth, flexibleHeight);

        if (bindingText)
        {
            bindingText.transform.SetParent(bindingButton.transform, false);
            if (oldLayout)
                Object.DestroyImmediate(oldLayout);
        }

        return bindingButton;
    }

    static void CreateBindingRow(Transform parent, TetrabeastsControlAction action, int rowIndex, TMP_Text sourceText)
    {
        var row = CreateHorizontalLayout($"Controls_Row_{action}", parent, 10f, 38f);
        var rowImage = row.gameObject.AddComponent<Image>();
        rowImage.color = rowIndex % 2 == 0
            ? new Color(1f, 1f, 1f, 0.055f)
            : new Color(0f, 0f, 0f, 0.14f);
        rowImage.raycastTarget = false;

        var actionText = CreateText("Action_Text", row.transform, $" {TetrabeastsControls.GetActionLabel(action)}", 20f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, sourceText);
        var bindingButton = CreateBindingButton("Binding_Button", row.transform);
        var bindingText = CreateText("Binding_Text", bindingButton.transform, TetrabeastsControls.GetBindingLabel(action, TetrabeastsControlProfile.PlatformDefault), 20f, FontStyles.Normal, TextAlignmentOptions.Right, Color.white, sourceText);
        StretchToFill(bindingText.rectTransform, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        AddLayout(actionText.gameObject, 300f, 38f);
        AddLayout(bindingButton.gameObject, 360f, 38f, 1f);
    }

    static Button CreateButton(string name, Transform parent, string label, TMP_Text sourceText)
    {
        var buttonImage = CreateObject<Image>(name, parent);
        buttonImage.color = new Color(0.98f, 0.68f, 0.17f, 1f);

        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var labelText = CreateText("Text (TMP)", button.transform, label, 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.12f, 0.1f, 0.08f, 1f), sourceText);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        return button;
    }

    static Button CreateBindingButton(string name, Transform parent)
    {
        var image = CreateObject<Image>(name, parent);
        image.color = new Color(1f, 1f, 1f, 0.02f);

        var button = image.gameObject.AddComponent<Button>();
        StyleBindingButton(button);

        return button;
    }

    static void StyleBindingButton(Button button)
    {
        if (!button)
            return;

        var image = EnsureBindingButtonGraphic(button);
        image.color = new Color(1f, 1f, 1f, 0.02f);

        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.02f);
        colors.highlightedColor = new Color(1f, 0.62f, 0.08f, 0.28f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(1f, 0.82f, 0.22f, 0.45f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.05f);
        button.colors = colors;
    }

    static Image EnsureBindingButtonGraphic(Button button)
    {
        if (!button)
            return null;

        var image = button.GetComponent<Image>();
        if (!image)
            image = button.gameObject.AddComponent<Image>();

        image.raycastTarget = true;

        if (!button.targetGraphic)
            button.targetGraphic = image;

        return image;
    }

    static TMP_Dropdown CreateFallbackDropdown(Transform parent)
    {
        var image = CreateObject<Image>("Controls_Profile_Dropdown", parent);
        image.color = new Color(0.98f, 0.68f, 0.17f, 1f);
        return image.gameObject.AddComponent<TMP_Dropdown>();
    }

    static GameObject CreateHorizontalLayout(string name, Transform parent, float spacing, float preferredHeight)
    {
        var layout = CreateObject<HorizontalLayoutGroup>(name, parent);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        AddLayout(layout.gameObject, -1f, preferredHeight, 1f);
        return layout.gameObject;
    }

    static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color, TMP_Text sourceText)
    {
        var tmp = CreateObject<TextMeshProUGUI>(name, parent);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = fontSize;
        tmp.color = color;
        tmp.richText = true;
        tmp.raycastTarget = false;

        if (sourceText)
        {
            tmp.font = sourceText.font;
            tmp.fontSharedMaterial = sourceText.fontSharedMaterial;
        }

        return tmp;
    }

    static T CreateObject<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(T));
        go.layer = parent ? parent.gameObject.layer : 5;
        go.transform.SetParent(parent, false);
        return go.GetComponent<T>();
    }

    static void AddLayout(GameObject go, float preferredWidth, float preferredHeight, float flexibleWidth = 0f, float flexibleHeight = 0f)
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

    static void StretchToFill(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (!rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
}
