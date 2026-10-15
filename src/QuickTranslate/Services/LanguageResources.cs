namespace QuickTranslate.Services;

public static class LanguageResources
{
    public const string DefaultLanguage = "ru";

    private static readonly Dictionary<string, string> Russian = new()
    {
        ["App_Title"] = "QuickTranslate",
        ["Common_Save"] = "Сохранить",
        ["Common_Close"] = "Закрыть",
        ["Common_ResetDefaults"] = "Сбросить по умолчанию",
        ["Common_SecondsShort"] = "сек",
        ["Common_ErrorPrefix"] = "Ошибка: {0}",
        ["Language_System"] = "Системный (по умолчанию)",
        ["Language_Detected"] = "определен",

        // Settings Window
        ["Settings_WindowTitle"] = "Настройки — QuickTranslate",
        ["Settings_HeaderTitle"] = "Настройки переводчика",
        ["Settings_HeaderSubtitle"] = "Настройте язык приложения, горячие клавиши, сервис, тему и поведение окна перевода",

        ["Settings_LanguageCardTitle"] = "🌐 Язык интерфейса",
        ["Settings_LanguageDescription"] = "Выберите язык для меню, кнопок и сообщений программы:",
        ["Settings_AppLanguageLabel"] = "Язык приложения:",

        ["Settings_HotkeyCardTitle"] = "⌨️ Горячая клавиша для перевода",
        ["Settings_HotkeyDescription"] = "Выделите текст в любой программе и нажмите это сочетание:",
        ["Settings_KeyLabel"] = "Клавиша:",

        ["Settings_ServiceCardTitle"] = "🌐 Сервис и языки перевода",
        ["Settings_ProviderLabel"] = "Сервис перевода:",
        ["Settings_SourceLanguageLabel"] = "Исходный язык:",
        ["Settings_TargetLanguageLabel"] = "Целевой язык:",

        ["Settings_ThemeCardTitle"] = "🎨 Оформление",
        ["Settings_ThemeLabel"] = "Тема оформления:",

        ["Settings_WindowBehaviorCardTitle"] = "🪟 Поведение окна перевода",
        ["Settings_PositionLabel"] = "Позиция окна:",
        ["Settings_PositionNearCursor"] = "Рядом с курсором",
        ["Settings_PositionCenter"] = "По центру экрана",
        ["Settings_AutoCloseLabel"] = "Автозакрытие окна:",
        ["Settings_CloseOnEsc"] = "Закрывать окно перевода при нажатии клавиши Esc",
        ["Settings_CloseOnClickOutside"] = "Закрывать окно перевода при клике вне его",

        ["Settings_SystemCardTitle"] = "⚙️ Система и сеть",
        ["Settings_StartWithWindows"] = "Запускать приложение вместе с Windows",
        ["Settings_RequestTimeoutLabel"] = "Таймаут запроса к сервису перевода:",

        ["Settings_AboutCardTitle"] = "ℹ️ О программе",
        ["Settings_AuthorLabel"] = "Автор: ",
        ["Settings_AuthorName"] = "Евгений Гладков",
        ["Settings_OpenGitHubTooltip"] = "Открыть профиль автора на GitHub",

        ["Settings_StatusModifierRequired"] = "Выберите хотя бы одну клавишу-модификатор (Ctrl, Shift, Alt или Win).",
        ["Settings_StatusHotkeyRegisterFailed"] = "Не удалось зарегистрировать выбранное сочетание клавиш.",
        ["Settings_StatusAutostartFailed"] = "Ошибка при настройке автозапуска Windows.",
        ["Settings_StatusSavedSuccess"] = "Настройки успешно сохранены!",
        ["Settings_StatusSaveFailed"] = "Не удалось сохранить настройки на диск.",
        ["Settings_StatusResetDefaults"] = "Параметры сброшены к значениям по умолчанию. Нажмите «Сохранить».",

        // Themes
        ["Theme_System"] = "Системная",
        ["Theme_Light"] = "Светлая",
        ["Theme_Dark"] = "Тёмная",

        // Auto Close
        ["AutoClose_Off"] = "Выключено",
        ["AutoClose_SecondsFormat"] = "{0} секунд",

        // Providers
        ["Provider_Google_Title"] = "Google Translate",
        ["Provider_Google_Desc"] = "Быстрый универсальный перевод от Google",
        ["Provider_Yandex_Title"] = "Yandex Translate",
        ["Provider_Yandex_Desc"] = "Качественный перевод с поддержкой русского языка (Яндекс)",
        ["Provider_MyMemory_Title"] = "MyMemory Translated",
        ["Provider_MyMemory_Desc"] = "Крупнейшая бесплатная база переводов (Translated.net)",

        // Translation Window
        ["Translation_CloseTooltip"] = "Закрыть (Esc)",
        ["Translation_InputPlaceholder"] = "Выделите текст или введите здесь...",
        ["Translation_TranslateButton"] = "Перевести ↵",
        ["Translation_Translating"] = "Переводим...",
        ["Translation_ErrorTitle"] = "Не удалось выполнить перевод",
        ["Translation_ClickToCopy"] = "Нажмите, чтобы скопировать",
        ["Translation_Copied"] = "Скопировано!",
        ["Translation_CopyButton"] = "Копировать",
        ["Translation_EscToClose"] = "Esc для закрытия",
        ["Translation_ClosingInFormat"] = "Закроется через {0} сек",
        ["Translation_TimerPausedFormat"] = "Таймер на паузе ({0} сек)",
        ["Translation_UnknownError"] = "Произошла ошибка при получении перевода.",

        // Tray
        ["Tray_Settings"] = "Настройки",
        ["Tray_ShowTranslationWindow"] = "Показать окно перевода",
        ["Tray_StartWithWindows"] = "Запускать вместе с Windows",
        ["Tray_Exit"] = "Выход",
        ["Tray_HotkeyConflictTitle"] = "Конфликт горячей клавиши",
        ["Tray_HotkeyConflictMessage"] = "{0} Откройте настройки, чтобы изменить сочетание.",

        // Translation Target Languages
        ["Lang_auto"] = "Автоопределение",
        ["Lang_ru"] = "Русский",
        ["Lang_en"] = "Английский",
        ["Lang_de"] = "Немецкий",
        ["Lang_fr"] = "Французский",
        ["Lang_es"] = "Испанский",
        ["Lang_it"] = "Итальянский",
        ["Lang_zh-CN"] = "Китайский (упрощенный)",
        ["Lang_ja"] = "Японский",
        ["Lang_ko"] = "Корейский",
        ["Lang_pt"] = "Португальский",
        ["Lang_tr"] = "Турецкий",
        ["Lang_pl"] = "Польский",
        ["Lang_uk"] = "Украинский",
        ["Lang_ar"] = "Арабский"
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["App_Title"] = "QuickTranslate",
        ["Common_Save"] = "Save",
        ["Common_Close"] = "Close",
        ["Common_ResetDefaults"] = "Reset to defaults",
        ["Common_SecondsShort"] = "sec",
        ["Common_ErrorPrefix"] = "Error: {0}",
        ["Language_System"] = "System (Default)",
        ["Language_Detected"] = "detected",

        // Settings Window
        ["Settings_WindowTitle"] = "Settings — QuickTranslate",
        ["Settings_HeaderTitle"] = "Translator Settings",
        ["Settings_HeaderSubtitle"] = "Configure application language, hotkeys, translation service, theme, and window behavior",

        ["Settings_LanguageCardTitle"] = "🌐 Interface Language",
        ["Settings_LanguageDescription"] = "Choose the language for menus, buttons, and notifications:",
        ["Settings_AppLanguageLabel"] = "App language:",

        ["Settings_HotkeyCardTitle"] = "⌨️ Translation Hotkey",
        ["Settings_HotkeyDescription"] = "Select text in any application and press this hotkey:",
        ["Settings_KeyLabel"] = "Key:",

        ["Settings_ServiceCardTitle"] = "🌐 Translation Service & Languages",
        ["Settings_ProviderLabel"] = "Translation service:",
        ["Settings_SourceLanguageLabel"] = "Source language:",
        ["Settings_TargetLanguageLabel"] = "Target language:",

        ["Settings_ThemeCardTitle"] = "🎨 Appearance",
        ["Settings_ThemeLabel"] = "Color theme:",

        ["Settings_WindowBehaviorCardTitle"] = "🪟 Translation Window Behavior",
        ["Settings_PositionLabel"] = "Window position:",
        ["Settings_PositionNearCursor"] = "Near cursor",
        ["Settings_PositionCenter"] = "Center of screen",
        ["Settings_AutoCloseLabel"] = "Auto-close window:",
        ["Settings_CloseOnEsc"] = "Close translation window when Esc is pressed",
        ["Settings_CloseOnClickOutside"] = "Close translation window when clicking outside",

        ["Settings_SystemCardTitle"] = "⚙️ System & Network",
        ["Settings_StartWithWindows"] = "Start application with Windows",
        ["Settings_RequestTimeoutLabel"] = "Translation request timeout:",

        ["Settings_AboutCardTitle"] = "ℹ️ About",
        ["Settings_AuthorLabel"] = "Author: ",
        ["Settings_AuthorName"] = "Evgeny Gladkov",
        ["Settings_OpenGitHubTooltip"] = "Open author profile on GitHub",

        ["Settings_StatusModifierRequired"] = "Select at least one modifier key (Ctrl, Shift, Alt, or Win).",
        ["Settings_StatusHotkeyRegisterFailed"] = "Failed to register the selected hotkey combination.",
        ["Settings_StatusAutostartFailed"] = "Failed to configure Windows autostart.",
        ["Settings_StatusSavedSuccess"] = "Settings saved successfully!",
        ["Settings_StatusSaveFailed"] = "Failed to save settings to disk.",
        ["Settings_StatusResetDefaults"] = "Settings reset to defaults. Click \"Save\".",

        // Themes
        ["Theme_System"] = "System",
        ["Theme_Light"] = "Light",
        ["Theme_Dark"] = "Dark",

        // Auto Close
        ["AutoClose_Off"] = "Disabled",
        ["AutoClose_SecondsFormat"] = "{0} seconds",

        // Providers
        ["Provider_Google_Title"] = "Google Translate",
        ["Provider_Google_Desc"] = "Fast universal translation powered by Google",
        ["Provider_Yandex_Title"] = "Yandex Translate",
        ["Provider_Yandex_Desc"] = "High quality translation powered by Yandex",
        ["Provider_MyMemory_Title"] = "MyMemory Translated",
        ["Provider_MyMemory_Desc"] = "World's largest free translation database (Translated.net)",

        // Translation Window
        ["Translation_CloseTooltip"] = "Close (Esc)",
        ["Translation_InputPlaceholder"] = "Select text or type here...",
        ["Translation_TranslateButton"] = "Translate ↵",
        ["Translation_Translating"] = "Translating...",
        ["Translation_ErrorTitle"] = "Translation failed",
        ["Translation_ClickToCopy"] = "Click to copy",
        ["Translation_Copied"] = "Copied!",
        ["Translation_CopyButton"] = "Copy",
        ["Translation_EscToClose"] = "Esc to close",
        ["Translation_ClosingInFormat"] = "Closes in {0} sec",
        ["Translation_TimerPausedFormat"] = "Timer paused ({0} sec)",
        ["Translation_UnknownError"] = "An error occurred while retrieving the translation.",

        // Tray
        ["Tray_Settings"] = "Settings",
        ["Tray_ShowTranslationWindow"] = "Show translation window",
        ["Tray_StartWithWindows"] = "Start with Windows",
        ["Tray_Exit"] = "Exit",
        ["Tray_HotkeyConflictTitle"] = "Hotkey Conflict",
        ["Tray_HotkeyConflictMessage"] = "{0} Open settings to change the combination.",

        // Translation Target Languages
        ["Lang_auto"] = "Auto-detect",
        ["Lang_ru"] = "Russian",
        ["Lang_en"] = "English",
        ["Lang_de"] = "German",
        ["Lang_fr"] = "French",
        ["Lang_es"] = "Spanish",
        ["Lang_it"] = "Italian",
        ["Lang_zh-CN"] = "Chinese (Simplified)",
        ["Lang_ja"] = "Japanese",
        ["Lang_ko"] = "Korean",
        ["Lang_pt"] = "Portuguese",
        ["Lang_tr"] = "Turkish",
        ["Lang_pl"] = "Polish",
        ["Lang_uk"] = "Ukrainian",
        ["Lang_ar"] = "Arabic"
    };

    private static readonly Dictionary<string, string> Spanish = new()
    {
        ["App_Title"] = "QuickTranslate",
        ["Common_Save"] = "Guardar",
        ["Common_Close"] = "Cerrar",
        ["Common_ResetDefaults"] = "Restablecer valores predeterminados",
        ["Common_SecondsShort"] = "seg",
        ["Common_ErrorPrefix"] = "Error: {0}",
        ["Language_System"] = "Del sistema (predeterminado)",
        ["Language_Detected"] = "detectado",

        // Settings Window
        ["Settings_WindowTitle"] = "Configuración — QuickTranslate",
        ["Settings_HeaderTitle"] = "Configuración del traductor",
        ["Settings_HeaderSubtitle"] = "Configure el idioma de la aplicación, teclas de acceso rápido, servicio, tema y comportamiento",

        ["Settings_LanguageCardTitle"] = "🌐 Idioma de la interfaz",
        ["Settings_LanguageDescription"] = "Seleccione el idioma para menús, botones y notificaciones:",
        ["Settings_AppLanguageLabel"] = "Idioma de la aplicación:",

        ["Settings_HotkeyCardTitle"] = "⌨️ Tecla de acceso rápido para traducir",
        ["Settings_HotkeyDescription"] = "Seleccione texto en cualquier programa y pulse esta combinación:",
        ["Settings_KeyLabel"] = "Tecla:",

        ["Settings_ServiceCardTitle"] = "🌐 Servicio e idiomas de traducción",
        ["Settings_ProviderLabel"] = "Servicio de traducción:",
        ["Settings_SourceLanguageLabel"] = "Idioma de origen:",
        ["Settings_TargetLanguageLabel"] = "Idioma de destino:",

        ["Settings_ThemeCardTitle"] = "🎨 Apariencia",
        ["Settings_ThemeLabel"] = "Tema:",

        ["Settings_WindowBehaviorCardTitle"] = "🪟 Comportamiento de la ventana",
        ["Settings_PositionLabel"] = "Posición de la ventana:",
        ["Settings_PositionNearCursor"] = "Cerca del cursor",
        ["Settings_PositionCenter"] = "En el centro de la pantalla",
        ["Settings_AutoCloseLabel"] = "Cierre automático de la ventana:",
        ["Settings_CloseOnEsc"] = "Cerrar la ventana de traducción al pulsar la tecla Esc",
        ["Settings_CloseOnClickOutside"] = "Cerrar la ventana de traducción al hacer clic fuera",

        ["Settings_SystemCardTitle"] = "⚙️ Sistema y red",
        ["Settings_StartWithWindows"] = "Iniciar la aplicación junto con Windows",
        ["Settings_RequestTimeoutLabel"] = "Tiempo de espera de la solicitud de traducción:",

        ["Settings_AboutCardTitle"] = "ℹ️ Acerca de",
        ["Settings_AuthorLabel"] = "Autor: ",
        ["Settings_AuthorName"] = "Evgeny Gladkov",
        ["Settings_OpenGitHubTooltip"] = "Abrir perfil del autor en GitHub",

        ["Settings_StatusModifierRequired"] = "Seleccione al menos una tecla modificadora (Ctrl, Shift, Alt o Win).",
        ["Settings_StatusHotkeyRegisterFailed"] = "No se pudo registrar la combinación de teclas seleccionada.",
        ["Settings_StatusAutostartFailed"] = "Error al configurar el inicio automático de Windows.",
        ["Settings_StatusSavedSuccess"] = "¡Configuración guardada correctamente!",
        ["Settings_StatusSaveFailed"] = "No se pudo guardar la configuración en el disco.",
        ["Settings_StatusResetDefaults"] = "Valores restablecidos por defecto. Haga clic en «Guardar».",

        // Themes
        ["Theme_System"] = "Del sistema",
        ["Theme_Light"] = "Clara",
        ["Theme_Dark"] = "Oscura",

        // Auto Close
        ["AutoClose_Off"] = "Desactivado",
        ["AutoClose_SecondsFormat"] = "{0} segundos",

        // Providers
        ["Provider_Google_Title"] = "Google Translate",
        ["Provider_Google_Desc"] = "Traducción rápida y universal de Google",
        ["Provider_Yandex_Title"] = "Yandex Translate",
        ["Provider_Yandex_Desc"] = "Traducción de alta calidad con tecnología de Yandex",
        ["Provider_MyMemory_Title"] = "MyMemory Translated",
        ["Provider_MyMemory_Desc"] = "La base de datos de traducción gratuita más grande del mundo (Translated.net)",

        // Translation Window
        ["Translation_CloseTooltip"] = "Cerrar (Esc)",
        ["Translation_InputPlaceholder"] = "Seleccione texto o escriba aquí...",
        ["Translation_TranslateButton"] = "Traducir ↵",
        ["Translation_Translating"] = "Traduciendo...",
        ["Translation_ErrorTitle"] = "Error al traducir",
        ["Translation_ClickToCopy"] = "Haga clic para copiar",
        ["Translation_Copied"] = "¡Copiado!",
        ["Translation_CopyButton"] = "Copiar",
        ["Translation_EscToClose"] = "Esc para cerrar",
        ["Translation_ClosingInFormat"] = "Se cierra en {0} seg",
        ["Translation_TimerPausedFormat"] = "Temporizador en pausa ({0} seg)",
        ["Translation_UnknownError"] = "Se produjo un error al obtener la traducción.",

        // Tray
        ["Tray_Settings"] = "Configuración",
        ["Tray_ShowTranslationWindow"] = "Mostrar ventana de traducción",
        ["Tray_StartWithWindows"] = "Iniciar con Windows",
        ["Tray_Exit"] = "Salir",
        ["Tray_HotkeyConflictTitle"] = "Conflicto de teclas de acceso rápido",
        ["Tray_HotkeyConflictMessage"] = "{0} Abra la configuración para cambiar la combinación.",

        // Translation Target Languages
        ["Lang_auto"] = "Detección automática",
        ["Lang_ru"] = "Ruso",
        ["Lang_en"] = "Inglés",
        ["Lang_de"] = "Alemán",
        ["Lang_fr"] = "Francés",
        ["Lang_es"] = "Español",
        ["Lang_it"] = "Italiano",
        ["Lang_zh-CN"] = "Chino (simplificado)",
        ["Lang_ja"] = "Japonés",
        ["Lang_ko"] = "Coreano",
        ["Lang_pt"] = "Portugués",
        ["Lang_tr"] = "Turco",
        ["Lang_pl"] = "Polaco",
        ["Lang_uk"] = "Ucraniano",
        ["Lang_ar"] = "Árabe"
    };

    private static readonly Dictionary<string, string> German = new()
    {
        ["App_Title"] = "QuickTranslate",
        ["Common_Save"] = "Speichern",
        ["Common_Close"] = "Schließen",
        ["Common_ResetDefaults"] = "Auf Standard zurücksetzen",
        ["Common_SecondsShort"] = "Sek.",
        ["Common_ErrorPrefix"] = "Fehler: {0}",
        ["Language_System"] = "Systemstandard",
        ["Language_Detected"] = "erkannt",

        // Settings Window
        ["Settings_WindowTitle"] = "Einstellungen — QuickTranslate",
        ["Settings_HeaderTitle"] = "Übersetzer-Einstellungen",
        ["Settings_HeaderSubtitle"] = "Konfigurieren Sie App-Sprache, Hotkeys, Dienst, Design und Fensterverhalten",

        ["Settings_LanguageCardTitle"] = "🌐 Benutzeroberflächensprache",
        ["Settings_LanguageDescription"] = "Wählen Sie die Sprache für Menüs, Schaltflächen und Benachrichtigungen:",
        ["Settings_AppLanguageLabel"] = "App-Sprache:",

        ["Settings_HotkeyCardTitle"] = "⌨️ Übersetzungs-Hotkey",
        ["Settings_HotkeyDescription"] = "Markieren Sie Text in einem beliebigen Programm und drücken Sie diesen Hotkey:",
        ["Settings_KeyLabel"] = "Taste:",

        ["Settings_ServiceCardTitle"] = "🌐 Übersetzungsdienst und Sprachen",
        ["Settings_ProviderLabel"] = "Übersetzungsdienst:",
        ["Settings_SourceLanguageLabel"] = "Ausgangssprache:",
        ["Settings_TargetLanguageLabel"] = "Zielsprache:",

        ["Settings_ThemeCardTitle"] = "🎨 Erscheinungsbild",
        ["Settings_ThemeLabel"] = "Design:",

        ["Settings_WindowBehaviorCardTitle"] = "🪟 Übersetzungsfenster-Verhalten",
        ["Settings_PositionLabel"] = "Fensterposition:",
        ["Settings_PositionNearCursor"] = "In der Nähe des Cursors",
        ["Settings_PositionCenter"] = "In der Bildschirmmitte",
        ["Settings_AutoCloseLabel"] = "Fenster automatisch schließen:",
        ["Settings_CloseOnEsc"] = "Übersetzungsfenster mit Esc-Taste schließen",
        ["Settings_CloseOnClickOutside"] = "Übersetzungsfenster beim Klick außerhalb schließen",

        ["Settings_SystemCardTitle"] = "⚙️ System & Netzwerk",
        ["Settings_StartWithWindows"] = "Anwendung mit Windows starten",
        ["Settings_RequestTimeoutLabel"] = "Timeout für Übersetzungsanfragen:",

        ["Settings_AboutCardTitle"] = "ℹ️ Über das Programm",
        ["Settings_AuthorLabel"] = "Autor: ",
        ["Settings_AuthorName"] = "Evgeny Gladkov",
        ["Settings_OpenGitHubTooltip"] = "Autorenprofil auf GitHub öffnen",

        ["Settings_StatusModifierRequired"] = "Wählen Sie mindestens eine Modifikatortaste (Strg, Umschalt, Alt oder Win).",
        ["Settings_StatusHotkeyRegisterFailed"] = "Die ausgewählte Tastenkombination konnte nicht registriert werden.",
        ["Settings_StatusAutostartFailed"] = "Fehler beim Konfigurieren des Windows-Autostarts.",
        ["Settings_StatusSavedSuccess"] = "Einstellungen erfolgreich gespeichert!",
        ["Settings_StatusSaveFailed"] = "Einstellungen konnten nicht auf der Festplatte gespeichert werden.",
        ["Settings_StatusResetDefaults"] = "Einstellungen auf Standardwerte zurückgesetzt. Klicken Sie auf „Speichern“.",

        // Themes
        ["Theme_System"] = "System",
        ["Theme_Light"] = "Hell",
        ["Theme_Dark"] = "Dunkel",

        // Auto Close
        ["AutoClose_Off"] = "Aus",
        ["AutoClose_SecondsFormat"] = "{0} Sekunden",

        // Providers
        ["Provider_Google_Title"] = "Google Translate",
        ["Provider_Google_Desc"] = "Schnelle universelle Übersetzung von Google",
        ["Provider_Yandex_Title"] = "Yandex Translate",
        ["Provider_Yandex_Desc"] = "Hochwertige Übersetzung bereitgestellt von Yandex",
        ["Provider_MyMemory_Title"] = "MyMemory Translated",
        ["Provider_MyMemory_Desc"] = "Größte kostenlose Übersetzungsdatenbank der Welt (Translated.net)",

        // Translation Window
        ["Translation_CloseTooltip"] = "Schließen (Esc)",
        ["Translation_InputPlaceholder"] = "Text markieren oder hier eingeben...",
        ["Translation_TranslateButton"] = "Übersetzen ↵",
        ["Translation_Translating"] = "Wird übersetzt...",
        ["Translation_ErrorTitle"] = "Übersetzung fehlgeschlagen",
        ["Translation_ClickToCopy"] = "Klicken zum Kopieren",
        ["Translation_Copied"] = "Kopiert!",
        ["Translation_CopyButton"] = "Kopieren",
        ["Translation_EscToClose"] = "Esc zum Schließen",
        ["Translation_ClosingInFormat"] = "Schließt in {0} Sek.",
        ["Translation_TimerPausedFormat"] = "Timer pausiert ({0} Sek.)",
        ["Translation_UnknownError"] = "Beim Abrufen der Übersetzung ist ein Fehler aufgetreten.",

        // Tray
        ["Tray_Settings"] = "Einstellungen",
        ["Tray_ShowTranslationWindow"] = "Übersetzungsfenster anzeigen",
        ["Tray_StartWithWindows"] = "Mit Windows starten",
        ["Tray_Exit"] = "Beenden",
        ["Tray_HotkeyConflictTitle"] = "Hotkey-Konflikt",
        ["Tray_HotkeyConflictMessage"] = "{0} Öffnen Sie die Einstellungen, um die Kombination zu ändern.",

        // Translation Target Languages
        ["Lang_auto"] = "Automatische Erkennung",
        ["Lang_ru"] = "Russisch",
        ["Lang_en"] = "Englisch",
        ["Lang_de"] = "Deutsch",
        ["Lang_fr"] = "Französisch",
        ["Lang_es"] = "Spanisch",
        ["Lang_it"] = "Italienisch",
        ["Lang_zh-CN"] = "Chinesisch (Vereinfacht)",
        ["Lang_ja"] = "Japanisch",
        ["Lang_ko"] = "Koreanisch",
        ["Lang_pt"] = "Portugiesisch",
        ["Lang_tr"] = "Türkisch",
        ["Lang_pl"] = "Polnisch",
        ["Lang_uk"] = "Ukrainisch",
        ["Lang_ar"] = "Arabisch"
    };

    private static readonly Dictionary<string, string> French = new()
    {
        ["App_Title"] = "QuickTranslate",
        ["Common_Save"] = "Enregistrer",
        ["Common_Close"] = "Fermer",
        ["Common_ResetDefaults"] = "Rétablir les valeurs par défaut",
        ["Common_SecondsShort"] = "s",
        ["Common_ErrorPrefix"] = "Erreur : {0}",
        ["Language_System"] = "Système (par défaut)",
        ["Language_Detected"] = "détecté",

        // Settings Window
        ["Settings_WindowTitle"] = "Paramètres — QuickTranslate",
        ["Settings_HeaderTitle"] = "Paramètres du traducteur",
        ["Settings_HeaderSubtitle"] = "Configurez la langue de l'application, les raccourcis, le service, le thème et le comportement",

        ["Settings_LanguageCardTitle"] = "🌐 Langue de l'interface",
        ["Settings_LanguageDescription"] = "Sélectionnez la langue pour les menus, les boutons et les notifications :",
        ["Settings_AppLanguageLabel"] = "Langue de l'application :",

        ["Settings_HotkeyCardTitle"] = "⌨️ Raccourci clavier de traduction",
        ["Settings_HotkeyDescription"] = "Sélectionnez du texte dans n'importe quel programme et appuyez sur cette combinaison :",
        ["Settings_KeyLabel"] = "Touche :",

        ["Settings_ServiceCardTitle"] = "🌐 Service et langues de traduction",
        ["Settings_ProviderLabel"] = "Service de traduction :",
        ["Settings_SourceLanguageLabel"] = "Langue source :",
        ["Settings_TargetLanguageLabel"] = "Langue cible :",

        ["Settings_ThemeCardTitle"] = "🎨 Apparence",
        ["Settings_ThemeLabel"] = "Thème :",

        ["Settings_WindowBehaviorCardTitle"] = "🪟 Comportement de la fenêtre",
        ["Settings_PositionLabel"] = "Position de la fenêtre :",
        ["Settings_PositionNearCursor"] = "Près du curseur",
        ["Settings_PositionCenter"] = "Au centre de l'écran",
        ["Settings_AutoCloseLabel"] = "Fermeture automatique de la fenêtre :",
        ["Settings_CloseOnEsc"] = "Fermer la fenêtre de traduction en appuyant sur Échap",
        ["Settings_CloseOnClickOutside"] = "Fermer la fenêtre de traduction lors d'un clic à l'extérieur",

        ["Settings_SystemCardTitle"] = "⚙️ Système et réseau",
        ["Settings_StartWithWindows"] = "Lancer l'application avec Windows",
        ["Settings_RequestTimeoutLabel"] = "Délai d'expiration de la requête de traduction :",

        ["Settings_AboutCardTitle"] = "ℹ️ À propos",
        ["Settings_AuthorLabel"] = "Auteur : ",
        ["Settings_AuthorName"] = "Evgeny Gladkov",
        ["Settings_OpenGitHubTooltip"] = "Ouvrir le profil de l'auteur sur GitHub",

        ["Settings_StatusModifierRequired"] = "Sélectionnez au moins une touche de modification (Ctrl, Maj, Alt ou Win).",
        ["Settings_StatusHotkeyRegisterFailed"] = "Impossible d'enregistrer la combinaison de touches sélectionnée.",
        ["Settings_StatusAutostartFailed"] = "Erreur lors de la configuration du démarrage automatique de Windows.",
        ["Settings_StatusSavedSuccess"] = "Paramètres enregistrés avec succès !",
        ["Settings_StatusSaveFailed"] = "Impossible d'enregistrer les paramètres sur le disque.",
        ["Settings_StatusResetDefaults"] = "Paramètres réinitialisés par défaut. Cliquez sur « Enregistrer ».",

        // Themes
        ["Theme_System"] = "Système",
        ["Theme_Light"] = "Clair",
        ["Theme_Dark"] = "Sombre",

        // Auto Close
        ["AutoClose_Off"] = "Désactivé",
        ["AutoClose_SecondsFormat"] = "{0} secondes",

        // Providers
        ["Provider_Google_Title"] = "Google Translate",
        ["Provider_Google_Desc"] = "Traduction universelle rapide par Google",
        ["Provider_Yandex_Title"] = "Yandex Translate",
        ["Provider_Yandex_Desc"] = "Traduction de haute qualité optimisée par Yandex",
        ["Provider_MyMemory_Title"] = "MyMemory Translated",
        ["Provider_MyMemory_Desc"] = "La plus grande base de données de traduction gratuite au monde (Translated.net)",

        // Translation Window
        ["Translation_CloseTooltip"] = "Fermer (Échap)",
        ["Translation_InputPlaceholder"] = "Sélectionnez du texte ou saisissez ici...",
        ["Translation_TranslateButton"] = "Traduire ↵",
        ["Translation_Translating"] = "Traduction en cours...",
        ["Translation_ErrorTitle"] = "Échec de la traduction",
        ["Translation_ClickToCopy"] = "Cliquer pour copier",
        ["Translation_Copied"] = "Copié !",
        ["Translation_CopyButton"] = "Copier",
        ["Translation_EscToClose"] = "Échap pour fermer",
        ["Translation_ClosingInFormat"] = "Fermeture dans {0} s",
        ["Translation_TimerPausedFormat"] = "Minuteur en pause ({0} s)",
        ["Translation_UnknownError"] = "Une erreur est survenue lors de la récupération de la traduction.",

        // Tray
        ["Tray_Settings"] = "Paramètres",
        ["Tray_ShowTranslationWindow"] = "Afficher la fenêtre de traduction",
        ["Tray_StartWithWindows"] = "Démarrer avec Windows",
        ["Tray_Exit"] = "Quitter",
        ["Tray_HotkeyConflictTitle"] = "Conflit de raccourci",
        ["Tray_HotkeyConflictMessage"] = "{0} Ouvrez les paramètres pour modifier la combinaison.",

        // Translation Target Languages
        ["Lang_auto"] = "Détection automatique",
        ["Lang_ru"] = "Russe",
        ["Lang_en"] = "Anglais",
        ["Lang_de"] = "Allemand",
        ["Lang_fr"] = "Français",
        ["Lang_es"] = "Espagnol",
        ["Lang_it"] = "Italien",
        ["Lang_zh-CN"] = "Chinois (simplifié)",
        ["Lang_ja"] = "Japonais",
        ["Lang_ko"] = "Coréen",
        ["Lang_pt"] = "Portugais",
        ["Lang_tr"] = "Turc",
        ["Lang_pl"] = "Polonais",
        ["Lang_uk"] = "Ukrainien",
        ["Lang_ar"] = "Arabe"
    };

    private static readonly Dictionary<string, string> ChineseSimplified = new()
    {
        ["App_Title"] = "QuickTranslate",
        ["Common_Save"] = "保存",
        ["Common_Close"] = "关闭",
        ["Common_ResetDefaults"] = "恢复默认值",
        ["Common_SecondsShort"] = "秒",
        ["Common_ErrorPrefix"] = "错误：{0}",
        ["Language_System"] = "系统默认",
        ["Language_Detected"] = "已识别",

        // Settings Window
        ["Settings_WindowTitle"] = "设置 — QuickTranslate",
        ["Settings_HeaderTitle"] = "翻译器设置",
        ["Settings_HeaderSubtitle"] = "配置应用程序语言、快捷键、翻译服务、主题以及窗口行为",

        ["Settings_LanguageCardTitle"] = "🌐 界面语言",
        ["Settings_LanguageDescription"] = "选择菜单、按钮和通知的显示语言：",
        ["Settings_AppLanguageLabel"] = "应用语言：",

        ["Settings_HotkeyCardTitle"] = "⌨️ 快捷翻译热键",
        ["Settings_HotkeyDescription"] = "在任意程序中选中文本并按下此快捷键：",
        ["Settings_KeyLabel"] = "按键：",

        ["Settings_ServiceCardTitle"] = "🌐 翻译服务与语言",
        ["Settings_ProviderLabel"] = "翻译服务：",
        ["Settings_SourceLanguageLabel"] = "源语言：",
        ["Settings_TargetLanguageLabel"] = "目标语言：",

        ["Settings_ThemeCardTitle"] = "🎨 外观",
        ["Settings_ThemeLabel"] = "主题：",

        ["Settings_WindowBehaviorCardTitle"] = "🪟 翻译窗口行为",
        ["Settings_PositionLabel"] = "窗口位置：",
        ["Settings_PositionNearCursor"] = "光标附近",
        ["Settings_PositionCenter"] = "屏幕中央",
        ["Settings_AutoCloseLabel"] = "自动关闭窗口：",
        ["Settings_CloseOnEsc"] = "按 Esc 键关闭翻译窗口",
        ["Settings_CloseOnClickOutside"] = "点击翻译窗口外部时自动关闭",

        ["Settings_SystemCardTitle"] = "⚙️ 系统与网络",
        ["Settings_StartWithWindows"] = "随 Windows 启动",
        ["Settings_RequestTimeoutLabel"] = "翻译请求超时时间：",

        ["Settings_AboutCardTitle"] = "ℹ️ 关于软件",
        ["Settings_AuthorLabel"] = "作者：",
        ["Settings_AuthorName"] = "Evgeny Gladkov",
        ["Settings_OpenGitHubTooltip"] = "在 GitHub 上打开作者主页",

        ["Settings_StatusModifierRequired"] = "请至少选择一个修饰键（Ctrl、Shift、Alt 或 Win）。",
        ["Settings_StatusHotkeyRegisterFailed"] = "无法注册选定的快捷键组合。",
        ["Settings_StatusAutostartFailed"] = "配置 Windows 开机自启失败。",
        ["Settings_StatusSavedSuccess"] = "设置保存成功！",
        ["Settings_StatusSaveFailed"] = "无法将设置保存到磁盘。",
        ["Settings_StatusResetDefaults"] = "参数已重置为默认值。请点击“保存”。",

        // Themes
        ["Theme_System"] = "跟随系统",
        ["Theme_Light"] = "浅色",
        ["Theme_Dark"] = "深色",

        // Auto Close
        ["AutoClose_Off"] = "关闭",
        ["AutoClose_SecondsFormat"] = "{0} 秒",

        // Providers
        ["Provider_Google_Title"] = "Google Translate",
        ["Provider_Google_Desc"] = "Google 提供的快速通用翻译",
        ["Provider_Yandex_Title"] = "Yandex Translate",
        ["Provider_Yandex_Desc"] = "Yandex 优质翻译服务",
        ["Provider_MyMemory_Title"] = "MyMemory Translated",
        ["Provider_MyMemory_Desc"] = "全球最大的免费翻译记忆库 (Translated.net)",

        // Translation Window
        ["Translation_CloseTooltip"] = "关闭 (Esc)",
        ["Translation_InputPlaceholder"] = "选中文本或在此输入...",
        ["Translation_TranslateButton"] = "翻译 ↵",
        ["Translation_Translating"] = "翻译中...",
        ["Translation_ErrorTitle"] = "翻译失败",
        ["Translation_ClickToCopy"] = "点击复制",
        ["Translation_Copied"] = "已复制！",
        ["Translation_CopyButton"] = "复制",
        ["Translation_EscToClose"] = "按 Esc 关闭",
        ["Translation_ClosingInFormat"] = "{0} 秒后关闭",
        ["Translation_TimerPausedFormat"] = "计时器已暂停 ({0} 秒)",
        ["Translation_UnknownError"] = "获取翻译时发生错误。",

        // Tray
        ["Tray_Settings"] = "设置",
        ["Tray_ShowTranslationWindow"] = "显示翻译窗口",
        ["Tray_StartWithWindows"] = "随 Windows 启动",
        ["Tray_Exit"] = "退出",
        ["Tray_HotkeyConflictTitle"] = "快捷键冲突",
        ["Tray_HotkeyConflictMessage"] = "{0} 请在设置中修改快捷键。",

        // Translation Target Languages
        ["Lang_auto"] = "自动检测",
        ["Lang_ru"] = "俄语",
        ["Lang_en"] = "英语",
        ["Lang_de"] = "德语",
        ["Lang_fr"] = "法语",
        ["Lang_es"] = "西班牙语",
        ["Lang_it"] = "意大利语",
        ["Lang_zh-CN"] = "中文 (简体)",
        ["Lang_ja"] = "日语",
        ["Lang_ko"] = "韩语",
        ["Lang_pt"] = "葡萄牙语",
        ["Lang_tr"] = "土耳其语",
        ["Lang_pl"] = "波兰语",
        ["Lang_uk"] = "乌克兰语",
        ["Lang_ar"] = "阿拉伯语"
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ru"] = Russian,
            ["en"] = English,
            ["es"] = Spanish,
            ["de"] = German,
            ["fr"] = French,
            ["zh-CN"] = ChineseSimplified
        };
}
