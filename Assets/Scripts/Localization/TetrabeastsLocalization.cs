using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
using Steamworks;
#endif

public static class TetrabeastsLocalization
{
    public const string EnglishCode = "en";
    public const string SpanishCode = "es";
    public const string PortugueseBrazilCode = "pt-BR";
    public const string RussianCode = "ru";
    public const string ChineseSimplifiedCode = "zh-CN";
    public const string UiTableName = "Tetrabeasts UI";

    const string KeySelectedLanguage = "Settings_LanguageCode";

    static readonly LanguageOption[] Supported =
    {
        new LanguageOption(EnglishCode, "English", "English", SystemLanguage.English, new[] { "english", "en" }),
        new LanguageOption(SpanishCode, "Spanish", "Espa\u00f1ol", SystemLanguage.Spanish, new[] { "spanish", "latam", "es", "es-es", "es-mx" }),
        new LanguageOption(PortugueseBrazilCode, "Brazilian Portuguese", "Portugu\u00eas (Brasil)", SystemLanguage.Portuguese, new[] { "brazilian", "brazilian portuguese", "portuguese brazil", "portuguese-brazil", "pt-br", "pt_br" }),
        new LanguageOption(RussianCode, "Russian", "\u0420\u0443\u0441\u0441\u043a\u0438\u0439", SystemLanguage.Russian, new[] { "russian", "ru" }),
        new LanguageOption(ChineseSimplifiedCode, "Simplified Chinese", "\u7b80\u4f53\u4e2d\u6587", SystemLanguage.ChineseSimplified, new[] { "schinese", "simplified chinese", "chinese", "zh", "zh-cn", "zh-hans" })
    };

    static readonly Dictionary<string, Dictionary<string, string>> FallbackText = new()
    {
        {
            EnglishCode,
            new Dictionary<string, string>
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
                ["title_new_game"] = "New Game",
                ["title_shop"] = "Shop",
                ["title_codex"] = "Codex",
                ["title_help"] = "Help",
                ["title_highscore"] = "HighScore",
                ["title_select_commander"] = "Select Commander",
                ["title_select_monsters"] = "Select Monsters",
                ["title_monster_units"] = "Monster Units",
                ["title_active_squad"] = "Active Squad",
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
                ["panel_leaderboards"] = "LEADERBOARDS",
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
                ["gameplay_combo"] = "Combo",
                ["gameplay_gravity"] = "Gravity",
                ["gameplay_level"] = "Level",
                ["gameplay_reroll"] = "Reroll",
                ["gameplay_exp_gained"] = "EXP Gained",
                ["gameplay_permanent_exp_gained"] = "Permanent EXP Gained",
                ["gameplay_exp_preserved"] = "EXP Preserved",
                ["gameplay_unit_reserve"] = "Unit Reserve",
                ["gameplay_new_difficulty"] = "New Difficulty Unlocked!",
                ["gameplay_do_not_show_again"] = "Do not show this message again",
                ["leaderboard_global"] = "Global",
                ["leaderboard_friends"] = "Friends",
                ["leaderboard_current"] = "Current",
                ["leaderboard_rank"] = "Rank",
                ["leaderboard_player"] = "Player",
                ["leaderboard_refresh"] = "Refresh",
                ["leaderboard_loading"] = "Loading...",
                ["leaderboard_refreshing"] = "Refreshing leaderboards...",
                ["leaderboard_ready"] = "Steam leaderboard ready.",
                ["leaderboard_failed"] = "Leaderboard refresh failed.",
                ["leaderboard_no_data"] = "No leaderboard data.",
                ["leaderboard_no_global"] = "No global scores yet.",
                ["leaderboard_no_friends"] = "No friend scores yet.",
                ["leaderboard_not_ranked"] = "You are not ranked yet."
            }
        },
        {
            SpanishCode,
            new Dictionary<string, string>
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
                ["title_new_game"] = "Nueva partida",
                ["title_shop"] = "Tienda",
                ["title_codex"] = "C\u00f3dice",
                ["title_help"] = "Ayuda",
                ["title_highscore"] = "Puntuaci\u00f3n m\u00e1xima",
                ["title_select_commander"] = "Seleccionar comandante",
                ["title_select_monsters"] = "Elegir monstruos",
                ["title_monster_units"] = "Unidades monstruo",
                ["title_active_squad"] = "Escuadron activo",
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
                ["panel_leaderboards"] = "CLASIFICACIONES",
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
                ["gameplay_combo"] = "Combo",
                ["gameplay_gravity"] = "Gravedad",
                ["gameplay_level"] = "Nivel",
                ["gameplay_reroll"] = "Repetir tirada",
                ["gameplay_exp_gained"] = "EXP ganada",
                ["gameplay_permanent_exp_gained"] = "EXP permanente ganada",
                ["gameplay_exp_preserved"] = "EXP conservada",
                ["gameplay_unit_reserve"] = "Reserva de unidades",
                ["gameplay_new_difficulty"] = "Nueva dificultad desbloqueada",
                ["gameplay_do_not_show_again"] = "No volver a mostrar este mensaje",
                ["leaderboard_global"] = "Global",
                ["leaderboard_friends"] = "Amigos",
                ["leaderboard_current"] = "Actual",
                ["leaderboard_rank"] = "Rango",
                ["leaderboard_player"] = "Jugador",
                ["leaderboard_refresh"] = "Actualizar",
                ["leaderboard_loading"] = "Cargando...",
                ["leaderboard_refreshing"] = "Actualizando clasificaciones...",
                ["leaderboard_ready"] = "Clasificaci\u00f3n de Steam lista.",
                ["leaderboard_failed"] = "No se pudo actualizar la clasificaci\u00f3n.",
                ["leaderboard_no_data"] = "No hay datos de clasificaci\u00f3n.",
                ["leaderboard_no_global"] = "A\u00fan no hay puntuaciones globales.",
                ["leaderboard_no_friends"] = "A\u00fan no hay puntuaciones de amigos.",
                ["leaderboard_not_ranked"] = "A\u00fan no tienes rango."
            }
        }
    };

    static readonly Dictionary<string, int> DropdownIndexByCode = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, string> TextKeyByLocalizedValue = new(StringComparer.Ordinal);
    static bool initialized;
    static bool relayingLocaleChange;
    static bool textLookupBuilt;

    public enum SelectionSource
    {
        Default,
        User,
        Steam,
        System
    }

    [Serializable]
    public readonly struct LanguageOption
    {
        public readonly string Code;
        public readonly string EnglishName;
        public readonly string NativeName;
        public readonly SystemLanguage SystemLanguage;
        readonly string[] steamLanguageNames;

        public LanguageOption(string code, string englishName, string nativeName, SystemLanguage systemLanguage, string[] steamLanguageNames)
        {
            Code = code;
            EnglishName = englishName;
            NativeName = nativeName;
            SystemLanguage = systemLanguage;
            this.steamLanguageNames = steamLanguageNames;
        }

        public bool MatchesSteamLanguage(string steamLanguage)
        {
            if (string.IsNullOrWhiteSpace(steamLanguage) || steamLanguageNames == null)
                return false;

            string normalized = NormalizeCode(steamLanguage);
            for (int i = 0; i < steamLanguageNames.Length; i++)
            {
                if (normalized == NormalizeCode(steamLanguageNames[i]))
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    sealed class RuntimeLocalesProvider : ILocalesProvider
    {
        [SerializeField] List<Locale> locales = new();

        public List<Locale> Locales => locales;

        public Locale GetLocale(LocaleIdentifier id)
        {
            for (int i = 0; i < locales.Count; i++)
            {
                var locale = locales[i];
                if (locale != null && locale.Identifier == id)
                    return locale;
            }

            string parentCode = GetParentLanguageCode(id.Code);
            if (!string.IsNullOrEmpty(parentCode) && !string.Equals(parentCode, id.Code, StringComparison.OrdinalIgnoreCase))
                return GetLocale(new LocaleIdentifier(parentCode));

            return null;
        }

        public void AddLocale(Locale locale)
        {
            if (locale == null || GetLocale(locale.Identifier) != null)
                return;

            locales.Add(locale);
            locales.Sort((a, b) => string.CompareOrdinal(a?.Identifier.CultureInfo?.EnglishName, b?.Identifier.CultureInfo?.EnglishName));
        }

        public bool RemoveLocale(Locale locale) => locales.Remove(locale);
    }

    public static event Action LanguageChanged;

    public static SelectionSource LastSelectionSource { get; private set; } = SelectionSource.Default;
    public static IReadOnlyList<LanguageOption> SupportedLanguages => Supported;

    public static bool HasUserLanguage => PlayerPrefs.HasKey(KeySelectedLanguage);

    public static bool ShouldShowFirstLaunchPrompt
    {
        get
        {
            EnsureInitialized();
            return !HasUserLanguage && LastSelectionSource == SelectionSource.Default;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeBeforeSceneLoad()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        EnsureUnityLocalizationSettings();
        SelectInitialLocale();
        LocalizationSettings.SelectedLocaleChanged += RelayLocaleChanged;
    }

    public static string CurrentLanguageCode
    {
        get
        {
            EnsureInitialized();
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : EnglishCode;
            return FindSupportedCode(code, out var supportedCode) ? supportedCode : EnglishCode;
        }
    }

    public static string GetLanguageDisplayName(string code, bool nativeName = true)
    {
        return FindLanguage(code, out var language)
            ? (nativeName ? language.NativeName : language.EnglishName)
            : code;
    }

    public static void PopulateLanguageDropdown(TMP_Dropdown dropdown)
    {
        if (!dropdown)
            return;

        EnsureInitialized();

        DropdownIndexByCode.Clear();
        dropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>(Supported.Length);
        for (int i = 0; i < Supported.Length; i++)
        {
            var language = Supported[i];
            DropdownIndexByCode[language.Code] = i;
            options.Add(new TMP_Dropdown.OptionData(language.NativeName));
        }

        dropdown.AddOptions(options);
        dropdown.interactable = options.Count > 1;

        int selectedIndex = DropdownIndexByCode.TryGetValue(CurrentLanguageCode, out int index) ? index : 0;
        dropdown.SetValueWithoutNotify(selectedIndex);
        dropdown.RefreshShownValue();
    }

    public static bool SetLanguageByDropdownIndex(int index, bool persist)
    {
        EnsureInitialized();

        if (index < 0 || index >= Supported.Length)
            return false;

        return SetLanguageCode(Supported[index].Code, persist);
    }

    public static bool SetLanguageCode(string code, bool persist)
    {
        EnsureInitialized();

        if (!FindSupportedCode(code, out string supportedCode))
            supportedCode = EnglishCode;

        var locale = GetLocale(supportedCode);
        if (locale == null)
            return false;

        LocalizationSettings.SelectedLocale = locale;

        if (persist)
        {
            PlayerPrefs.SetString(KeySelectedLanguage, supportedCode);
            PlayerPrefs.Save();
            LastSelectionSource = SelectionSource.User;
        }

        LanguageChanged?.Invoke();
        return true;
    }

    public static string GetText(string key, string englishFallback = null)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        string code = CurrentLanguageCode;
        if (FallbackText.TryGetValue(code, out var languageTable) && languageTable.TryGetValue(key, out string value))
            return value;

        if (FallbackText[EnglishCode].TryGetValue(key, out string englishValue))
            return LocalizeText(englishValue);

        return string.IsNullOrEmpty(englishFallback) ? key : englishFallback;
    }

    public static string LocalizeText(string englishText)
    {
        if (string.IsNullOrEmpty(englishText))
            return string.Empty;

        EnsureInitialized();

        string languageCode = CurrentLanguageCode;
        if (languageCode == SpanishCode && TetrabeastsSpanishTranslations.TryGetText(englishText, out string spanishText))
            return spanishText;

        if (languageCode == PortugueseBrazilCode && TetrabeastsPortugueseBrazilTranslations.TryGetText(englishText, out string portugueseText))
            return portugueseText;

        if (languageCode == RussianCode && TetrabeastsRussianTranslations.TryGetText(englishText, out string russianText))
            return russianText;

        if (languageCode == ChineseSimplifiedCode && TetrabeastsChineseTranslations.TryGetText(englishText, out string chineseText))
            return chineseText;

        return englishText;
    }

    public static bool HasRuntimeTranslation(string englishText)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            return false;

        return TetrabeastsSpanishTranslations.TryGetText(englishText, out _)
            || TetrabeastsPortugueseBrazilTranslations.TryGetText(englishText, out _)
            || TetrabeastsRussianTranslations.TryGetText(englishText, out _)
            || TetrabeastsChineseTranslations.TryGetText(englishText, out _);
    }

    public static string LocalizeFormat(string englishFormat, params object[] args)
    {
        string format = LocalizeText(englishFormat);
        if (args == null || args.Length == 0)
            return format;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
        catch (FormatException)
        {
            return string.Format(CultureInfo.CurrentCulture, englishFormat, args);
        }
    }

    public static bool TryGetStaticTextKey(string visibleText, out string key, out string englishFallback)
    {
        EnsureTextLookup();

        string normalized = NormalizeVisibleText(visibleText);
        if (!string.IsNullOrEmpty(normalized) && TextKeyByLocalizedValue.TryGetValue(normalized, out key))
        {
            englishFallback = FallbackText[EnglishCode].TryGetValue(key, out string englishValue) ? englishValue : normalized;
            return true;
        }

        key = null;
        englishFallback = null;
        return false;
    }

    static void EnsureUnityLocalizationSettings()
    {
        var settings = LocalizationSettings.GetInstanceDontCreateDefault();
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Tetrabeasts Runtime Localization Settings";
            LocalizationSettings.Instance = settings;
        }

        var provider = settings.GetAvailableLocales();
        if (provider == null)
        {
            provider = new RuntimeLocalesProvider();
            settings.SetAvailableLocales(provider);
        }

        LocalizationSettings.PreloadBehavior = PreloadBehavior.NoPreloading;

        bool hasAnyLocale = false;
        try
        {
            hasAnyLocale = provider.Locales.Count > 0;
        }
        catch
        {
            hasAnyLocale = false;
        }

        if (!hasAnyLocale)
        {
            provider = new RuntimeLocalesProvider();
            settings.SetAvailableLocales(provider);
        }

        for (int i = 0; i < Supported.Length; i++)
        {
            var language = Supported[i];
            if (provider.GetLocale(new LocaleIdentifier(language.Code)) == null)
                provider.AddLocale(Locale.CreateLocale(new LocaleIdentifier(language.Code)));
        }

        var english = provider.GetLocale(new LocaleIdentifier(EnglishCode));
        if (english != null)
            LocalizationSettings.ProjectLocale = english;
    }

    static void SelectInitialLocale()
    {
        if (TryGetSavedUserLanguage(out string savedCode))
        {
            LastSelectionSource = SelectionSource.User;
            SetSelectedLocaleWithoutSaving(savedCode);
            return;
        }

        if (TryGetSteamLanguageCode(out string steamCode))
        {
            LastSelectionSource = SelectionSource.Steam;
            SetSelectedLocaleWithoutSaving(steamCode);
            return;
        }

        if (TryGetSystemLanguageCode(out string systemCode))
        {
            LastSelectionSource = SelectionSource.System;
            SetSelectedLocaleWithoutSaving(systemCode);
            return;
        }

        LastSelectionSource = SelectionSource.Default;
        SetSelectedLocaleWithoutSaving(EnglishCode);
    }

    static void SetSelectedLocaleWithoutSaving(string code)
    {
        if (!FindSupportedCode(code, out string supportedCode))
            supportedCode = EnglishCode;

        var locale = GetLocale(supportedCode);
        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;
    }

    static bool TryGetSavedUserLanguage(out string code)
    {
        code = PlayerPrefs.GetString(KeySelectedLanguage, string.Empty);
        return FindSupportedCode(code, out code);
    }

    static bool TryGetSteamLanguageCode(out string code)
    {
        code = null;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        try
        {
            var steam = SteamPlatformService.Ensure();
            if (steam && steam.IsAvailable)
            {
                string gameLanguage = SteamApps.GetCurrentGameLanguage();
                if (FindSupportedSteamLanguage(gameLanguage, out code))
                    return true;

                string uiLanguage = SteamUtils.GetSteamUILanguage();
                if (FindSupportedSteamLanguage(uiLanguage, out code))
                    return true;
            }
        }
        catch
        {
            code = null;
        }
#endif

        return false;
    }

    static bool TryGetSystemLanguageCode(out string code)
    {
        code = null;

        try
        {
            var culture = CultureInfo.CurrentUICulture;
            if (culture != null && FindSupportedCode(culture.Name, out code))
                return true;

            if (culture != null && FindSupportedCode(culture.TwoLetterISOLanguageName, out code))
                return true;
        }
        catch
        {
            code = null;
        }

        for (int i = 0; i < Supported.Length; i++)
        {
            if (Application.systemLanguage == Supported[i].SystemLanguage)
            {
                code = Supported[i].Code;
                return true;
            }
        }

        return false;
    }

    static Locale GetLocale(string code)
    {
        var provider = LocalizationSettings.AvailableLocales;
        return provider?.GetLocale(new LocaleIdentifier(code));
    }

    static bool FindSupportedSteamLanguage(string steamLanguage, out string code)
    {
        for (int i = 0; i < Supported.Length; i++)
        {
            if (Supported[i].MatchesSteamLanguage(steamLanguage))
            {
                code = Supported[i].Code;
                return true;
            }
        }

        code = null;
        return false;
    }

    static bool FindSupportedCode(string candidateCode, out string code)
    {
        string normalized = NormalizeCode(candidateCode);
        if (string.IsNullOrEmpty(normalized))
        {
            code = null;
            return false;
        }

        for (int i = 0; i < Supported.Length; i++)
        {
            string supportedCode = Supported[i].Code;
            if (NormalizeCode(supportedCode) == normalized || NormalizeCode(GetParentLanguageCode(normalized)) == NormalizeCode(supportedCode))
            {
                code = supportedCode;
                return true;
            }
        }

        code = null;
        return false;
    }

    static bool FindLanguage(string code, out LanguageOption language)
    {
        if (FindSupportedCode(code, out string supportedCode))
        {
            for (int i = 0; i < Supported.Length; i++)
            {
                if (Supported[i].Code == supportedCode)
                {
                    language = Supported[i];
                    return true;
                }
            }
        }

        language = default;
        return false;
    }

    static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return code.Trim().Replace('_', '-').ToLowerInvariant();
    }

    static string GetParentLanguageCode(string code)
    {
        string normalized = NormalizeCode(code);
        int dashIndex = normalized.IndexOf('-');
        return dashIndex > 0 ? normalized.Substring(0, dashIndex) : normalized;
    }

    static void EnsureTextLookup()
    {
        if (textLookupBuilt)
            return;

        textLookupBuilt = true;
        TextKeyByLocalizedValue.Clear();

        foreach (var languageTable in FallbackText.Values)
        {
            foreach (var entry in languageTable)
            {
                string normalized = NormalizeVisibleText(entry.Value);
                if (!string.IsNullOrEmpty(normalized) && !TextKeyByLocalizedValue.ContainsKey(normalized))
                    TextKeyByLocalizedValue.Add(normalized, entry.Key);
            }
        }
    }

    static string NormalizeVisibleText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    static void RelayLocaleChanged(Locale locale)
    {
        if (relayingLocaleChange)
            return;

        relayingLocaleChange = true;
        LanguageChanged?.Invoke();
        relayingLocaleChange = false;
    }
}
