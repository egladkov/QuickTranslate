namespace QuickTranslate.Models;

public enum WindowPositionMode
{
    NearCursor,
    CenterScreen
}

public class AppSettings
{
    public string AppLanguage { get; set; } = "system";
    public TranslationProvider Provider { get; set; } = TranslationProvider.Google;
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;
    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "ru";
    public bool StartWithWindows { get; set; } = false;
    public bool CloseOnEsc { get; set; } = true;
    public bool CloseOnClickOutside { get; set; } = true;
    public int AutoCloseSeconds { get; set; } = 0;
    public int RequestTimeoutSeconds { get; set; } = 5;
    public WindowPositionMode PositionMode { get; set; } = WindowPositionMode.NearCursor;
    public AppTheme Theme { get; set; } = AppTheme.System;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppLanguage))
            AppLanguage = "system";

        if (!Enum.IsDefined(typeof(TranslationProvider), Provider))
            Provider = TranslationProvider.Google;

        if (!Enum.IsDefined(typeof(AppTheme), Theme))
            Theme = AppTheme.System;

        if (!Enum.IsDefined(typeof(WindowPositionMode), PositionMode))
            PositionMode = WindowPositionMode.NearCursor;

        if (AutoCloseSeconds < 0)
            AutoCloseSeconds = 0;
        if (AutoCloseSeconds > 300)
            AutoCloseSeconds = 300;

        if (RequestTimeoutSeconds < 2)
            RequestTimeoutSeconds = 2;
        if (RequestTimeoutSeconds > 30)
            RequestTimeoutSeconds = 30;

        if (string.IsNullOrWhiteSpace(SourceLanguage))
            SourceLanguage = "auto";

        if (string.IsNullOrWhiteSpace(TargetLanguage))
            TargetLanguage = "ru";

        Hotkey ??= HotkeyConfig.Default;
        if (Hotkey.Key == System.Windows.Input.Key.None)
            Hotkey.Key = System.Windows.Input.Key.T;
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            AppLanguage = AppLanguage,
            Provider = Provider,
            Hotkey = Hotkey.Clone(),
            SourceLanguage = SourceLanguage,
            TargetLanguage = TargetLanguage,
            StartWithWindows = StartWithWindows,
            CloseOnEsc = CloseOnEsc,
            CloseOnClickOutside = CloseOnClickOutside,
            AutoCloseSeconds = AutoCloseSeconds,
            RequestTimeoutSeconds = RequestTimeoutSeconds,
            PositionMode = PositionMode,
            Theme = Theme
        };
    }
}
