using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class TetrabeastsLocalizationProjectSetup
{
    const string RootFolder = "Assets/Localization";
    const string LocaleFolder = RootFolder + "/Locales";
    const string StringTableFolder = RootFolder + "/String Tables";
    const string SettingsPath = RootFolder + "/Localization Settings.asset";
    const string VolumePanelPrefabPath = "Assets/Prefabs/Panels/Volume_Panel.prefab";
    const string AutoSetupSessionKey = "TetrabeastsLocalizationProjectSetup.AutoSetupAttempted.v2";

    static readonly Dictionary<string, string> EnglishUi = new()
    {
        ["settings_language_label"] = "Language",
        ["settings_title"] = "Settings",
        ["settings_music_genre"] = "BGM Genre",
        ["settings_master_volume"] = "Master Volume",
        ["settings_music_volume"] = "Music Volume",
        ["settings_sfx_volume"] = "SFX Volume",
        ["settings_cursor_size"] = "Cursor Size",
        ["settings_combat_log"] = "Combat Log",
        ["settings_skip_trailer"] = "Skip Trailer",
        ["language_prompt_title"] = "Choose Language",
        ["language_prompt_body"] = "Pick the language Tetrabeasts should use. You can change this later in Settings.",
        ["language_prompt_confirm"] = "Confirm",
        ["intro_press_any_key"] = "Press Any Key",
        ["title_credits"] = "Credits",
        ["title_select_commander"] = "Select Commander",
        ["title_monster_units"] = "Monster Units",
        ["title_active_squad"] = "Active Squad",
        ["credits_body"] =
            "Solo Development Project By:\n" +
            "Roxythe (Rocky) Harding\n\n" +
            "Big Thank You To:\n" +
            "Andrue Harless\n" +
            "Joyce Shen\n" +
            "Nick Blackwell\n" +
            "Trent Mcconnaughhay\n" +
            "Zach Warren\n\n" +
            "For Beta Testing And Providing Feedback To Improve The Game!",
        ["generic_confirm"] = "Confirm",
        ["generic_continue"] = "Continue",
        ["generic_close"] = "Close",
        ["generic_back"] = "Back",
        ["generic_start"] = "Start",
        ["pause_paused"] = "PAUSED",
        ["pause_resume"] = "Resume",
        ["pause_main_menu"] = "Main Menu",
        ["pause_restart"] = "Restart",
        ["pause_save_quit"] = "Save & Quit",
        ["pause_quit"] = "Quit",
        ["panel_achievements"] = "Achievements",
        ["panel_leaderboard"] = "Leaderboard",
        ["panel_submit_name"] = "Submit Name",
        ["panel_help_menu"] = "Help Menu",
        ["panel_codex_buffs"] = "Buffs",
        ["panel_codex_debuffs"] = "Debuffs",
        ["panel_codex_level_mod"] = "Level Mod",
        ["panel_stats"] = "Stats",
        ["panel_units"] = "Units",
        ["gameplay_controls"] = "Controls",
        ["gameplay_choose_buff"] = "Choose A Buff",
        ["gameplay_choose_debuff"] = "Choose A Debuff",
        ["gameplay_level_modifier"] = "Level Modifier",
        ["gameplay_next_piece"] = "Next Piece",
        ["gameplay_score"] = "Score",
        ["gameplay_reroll"] = "Reroll",
        ["gameplay_exp_gained"] = "EXP Gained",
        ["gameplay_permanent_exp_gained"] = "Permanent EXP Gained",
        ["gameplay_exp_preserved"] = "EXP Preserved",
        ["gameplay_unit_reserve"] = "Unit Reserve",
        ["gameplay_new_difficulty"] = "New Difficulty Unlocked!",
        ["gameplay_do_not_show_again"] = "Do not show this message again"
    };

    static readonly Dictionary<string, string> SpanishUi = new()
    {
        ["settings_language_label"] = "Idioma",
        ["settings_title"] = "Ajustes",
        ["settings_music_genre"] = "Genero BGM",
        ["settings_master_volume"] = "Volumen general",
        ["settings_music_volume"] = "Volumen de musica",
        ["settings_sfx_volume"] = "Volumen de efectos",
        ["settings_cursor_size"] = "Tamano del cursor",
        ["settings_combat_log"] = "Registro de combate",
        ["settings_skip_trailer"] = "Saltar trailer",
        ["language_prompt_title"] = "Elegir idioma",
        ["language_prompt_body"] = "Elige el idioma que debe usar Tetrabeasts. Puedes cambiarlo luego en Ajustes.",
        ["language_prompt_confirm"] = "Confirmar",
        ["intro_press_any_key"] = "Pulsa cualquier tecla",
        ["title_credits"] = "Cr\u00e9ditos",
        ["title_select_commander"] = "Seleccionar comandante",
        ["title_monster_units"] = "Unidades monstruo",
        ["title_active_squad"] = "Escuadron activo",
        ["credits_body"] =
            "Proyecto desarrollado en solitario por:\n" +
            "Roxythe (Rocky) Harding\n\n" +
            "Muchas gracias a:\n" +
            "Andrue Harless\n" +
            "Joyce Shen\n" +
            "Nick Blackwell\n" +
            "Trent Mcconnaughhay\n" +
            "Zach Warren\n\n" +
            "Por probar la beta y aportar comentarios para mejorar el juego!",
        ["generic_confirm"] = "Confirmar",
        ["generic_continue"] = "Continuar",
        ["generic_close"] = "Cerrar",
        ["generic_back"] = "Atras",
        ["generic_start"] = "Comenzar",
        ["pause_paused"] = "PAUSA",
        ["pause_resume"] = "Reanudar",
        ["pause_main_menu"] = "Menu principal",
        ["pause_restart"] = "Reiniciar",
        ["pause_save_quit"] = "Guardar y salir",
        ["pause_quit"] = "Salir",
        ["panel_achievements"] = "Logros",
        ["panel_leaderboard"] = "Clasificacion",
        ["panel_submit_name"] = "Enviar nombre",
        ["panel_help_menu"] = "Menu de ayuda",
        ["panel_codex_buffs"] = "Mejoras",
        ["panel_codex_debuffs"] = "Desventajas",
        ["panel_codex_level_mod"] = "Mod. de nivel",
        ["panel_stats"] = "Estadisticas",
        ["panel_units"] = "Unidades",
        ["gameplay_controls"] = "Controles",
        ["gameplay_choose_buff"] = "Elige una mejora",
        ["gameplay_choose_debuff"] = "Elige una desventaja",
        ["gameplay_level_modifier"] = "Modificador de nivel",
        ["gameplay_next_piece"] = "Siguiente pieza",
        ["gameplay_score"] = "Puntuacion",
        ["gameplay_reroll"] = "Repetir tirada",
        ["gameplay_exp_gained"] = "EXP ganada",
        ["gameplay_permanent_exp_gained"] = "EXP permanente ganada",
        ["gameplay_exp_preserved"] = "EXP conservada",
        ["gameplay_unit_reserve"] = "Reserva de unidades",
        ["gameplay_new_difficulty"] = "Nueva dificultad desbloqueada",
        ["gameplay_do_not_show_again"] = "No volver a mostrar este mensaje"
    };

    [MenuItem("Tetrabeasts/Localization/Setup English + Spanish")]
    public static void SetupEnglishAndSpanish()
    {
        EnsureFolders();

        var settings = EnsureLocalizationSettings();
        var english = EnsureLocale(TetrabeastsLocalization.EnglishCode, "English", "English.asset", 0);
        var spanish = EnsureLocale(TetrabeastsLocalization.SpanishCode, "Spanish", "Spanish.asset", 1);

        LocalizationSettings.Instance = settings;
        LocalizationSettings.ProjectLocale = english;
        LocalizationSettings.PreloadBehavior = PreloadBehavior.NoPreloading;
        EditorUtility.SetDirty(settings);

        EnsureUiStringTable(new List<Locale> { english, spanish });
        EnsureVolumePanelLanguageDropdown();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Tetrabeasts localization setup complete: English and Spanish locales, UI string table, and Volume_Panel language dropdown are ready.");
    }

    [InitializeOnLoadMethod]
    static void AutoSetupWhenNeeded()
    {
        if (Application.isBatchMode || SessionState.GetBool(AutoSetupSessionKey, false))
            return;

        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (!NeedsSetup())
                return;

            SessionState.SetBool(AutoSetupSessionKey, true);
            SetupEnglishAndSpanish();
        };
    }

    public static void RunBatchSetup()
    {
        SetupEnglishAndSpanish();
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    static bool NeedsSetup()
    {
        if (!LocalizationEditorSettings.ActiveLocalizationSettings)
            return true;

        if (!LocalizationEditorSettings.GetLocale(TetrabeastsLocalization.EnglishCode) ||
            !LocalizationEditorSettings.GetLocale(TetrabeastsLocalization.SpanishCode))
            return true;

        if (LocalizationEditorSettings.GetStringTableCollection(TetrabeastsLocalization.UiTableName) == null)
            return true;

        if (!UiStringTablesHaveExpectedEntries())
            return true;

        return !VolumePanelPrefabHasLanguageDropdown();
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Localization");
        EnsureFolder(RootFolder, "Locales");
        EnsureFolder(RootFolder, "String Tables");
    }

    static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }

    static LocalizationSettings EnsureLocalizationSettings()
    {
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (!settings)
            settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);

        if (!settings)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Tetrabeasts Localization Settings";
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        return settings;
    }

    static Locale EnsureLocale(string code, string localeName, string assetName, ushort sortOrder)
    {
        var locale = LocalizationEditorSettings.GetLocale(code);
        string assetPath = $"{LocaleFolder}/{assetName}";

        if (!locale)
            locale = AssetDatabase.LoadAssetAtPath<Locale>(assetPath);

        if (!locale)
        {
            locale = Locale.CreateLocale(new LocaleIdentifier(code));
            AssetDatabase.CreateAsset(locale, assetPath);
        }

        locale.LocaleName = localeName;
        locale.SortOrder = sortOrder;
        EditorUtility.SetDirty(locale);
        LocalizationEditorSettings.AddLocale(locale);
        return locale;
    }

    static void EnsureUiStringTable(IList<Locale> locales)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(TetrabeastsLocalization.UiTableName);
        if (collection == null)
            collection = LocalizationEditorSettings.CreateStringTableCollection(TetrabeastsLocalization.UiTableName, StringTableFolder, locales);

        for (int i = 0; i < locales.Count; i++)
        {
            var locale = locales[i];
            if (collection.GetTable(locale.Identifier) == null)
                collection.AddNewTable(locale.Identifier);
        }

        var englishTable = collection.GetTable(TetrabeastsLocalization.EnglishCode) as StringTable;
        var spanishTable = collection.GetTable(TetrabeastsLocalization.SpanishCode) as StringTable;

        ApplyEntries(englishTable, EnglishUi);
        ApplyEntries(spanishTable, SpanishUi);

        foreach (var table in collection.StringTables)
        {
            LocalizationEditorSettings.SetPreloadTableFlag(table, false);
            EditorUtility.SetDirty(table);
        }

        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);
    }

    static void ApplyEntries(StringTable table, Dictionary<string, string> entries)
    {
        if (table == null)
            return;

        foreach (var entry in entries)
            table.AddEntry(entry.Key, entry.Value);

        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(table.SharedData);
    }

    static void EnsureVolumePanelLanguageDropdown()
    {
        var prefab = PrefabUtility.LoadPrefabContents(VolumePanelPrefabPath);
        try
        {
            var ui = prefab.GetComponent<VolumePanelUI>();
            if (!ui || !ui.musicModeDropdown)
                return;

            if (!ui.languageDropdown)
                ui.languageDropdown = FindChildComponent<TMP_Dropdown>(prefab.transform, "Language_Dropdown");

            if (!ui.languageLabel)
                ui.languageLabel = FindChildComponent<TMP_Text>(prefab.transform, "Language_Text");

            if (!ui.languageDropdown)
            {
                ui.languageDropdown = Object.Instantiate(ui.musicModeDropdown, ui.musicModeDropdown.transform.parent);
                ui.languageDropdown.name = "Language_Dropdown";
                ui.languageDropdown.onValueChanged.RemoveAllListeners();

                var sourceRect = ui.musicModeDropdown.GetComponent<RectTransform>();
                var targetRect = ui.languageDropdown.GetComponent<RectTransform>();
                if (sourceRect && targetRect)
                    targetRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -40f);
            }

            if (!ui.languageLabel)
            {
                var musicLabel = FindChildComponent<TMP_Text>(prefab.transform, "MusicGenre_Text");
                if (musicLabel)
                {
                    ui.languageLabel = Object.Instantiate(musicLabel, musicLabel.transform.parent);
                    ui.languageLabel.name = "Language_Text";

                    var sourceRect = musicLabel.GetComponent<RectTransform>();
                    var targetRect = ui.languageLabel.GetComponent<RectTransform>();
                    if (sourceRect && targetRect)
                        targetRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -40f);
                }
            }

            if (ui.languageLabel)
                ui.languageLabel.text = "Language";

            if (ui.languageDropdown)
            {
                ui.languageDropdown.ClearOptions();
                ui.languageDropdown.AddOptions(new List<TMP_Dropdown.OptionData>
                {
                    new("English"),
                    new("Espa\u00f1ol")
                });
                ui.languageDropdown.SetValueWithoutNotify(0);
                ui.languageDropdown.RefreshShownValue();
            }

            EditorUtility.SetDirty(ui);
            PrefabUtility.SaveAsPrefabAsset(prefab, VolumePanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    static bool VolumePanelPrefabHasLanguageDropdown()
    {
        var prefab = PrefabUtility.LoadPrefabContents(VolumePanelPrefabPath);
        try
        {
            var ui = prefab.GetComponent<VolumePanelUI>();
            if (!ui)
                return false;

            return ui.languageDropdown || FindChildComponent<TMP_Dropdown>(prefab.transform, "Language_Dropdown");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    static bool UiStringTablesHaveExpectedEntries()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(TetrabeastsLocalization.UiTableName);
        var englishTable = collection?.GetTable(TetrabeastsLocalization.EnglishCode) as StringTable;
        var spanishTable = collection?.GetTable(TetrabeastsLocalization.SpanishCode) as StringTable;

        return TableHasEntries(englishTable, EnglishUi) && TableHasEntries(spanishTable, SpanishUi);
    }

    static bool TableHasEntries(StringTable table, Dictionary<string, string> entries)
    {
        if (table == null)
            return false;

        foreach (var entry in entries)
        {
            var tableEntry = table.GetEntry(entry.Key);
            if (tableEntry == null || tableEntry.LocalizedValue != entry.Value)
                return false;
        }

        return true;
    }

    static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        var components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].name == childName)
                return components[i];
        }

        return null;
    }
}
