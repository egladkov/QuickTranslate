using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;
using QuickTranslate.Services;

namespace QuickTranslate.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IStartupService _startupService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly Action<string> _urlOpener;

    public const string AuthorGitHubUrl = "https://github.com/egladkov/";
    public string AuthorName => _localizationService.GetString("Settings_AuthorName");
    public string GitHubUrl => AuthorGitHubUrl;

    public IReadOnlyList<TranslationProviderInfo> AvailableProviders { get; } =
    [
        new(TranslationProvider.Google, string.Empty, string.Empty),
        new(TranslationProvider.Yandex, string.Empty, string.Empty),
        new(TranslationProvider.MyMemory, string.Empty, string.Empty)
    ];

    public IReadOnlyList<ThemeInfo> AvailableThemes { get; } =
    [
        new(AppTheme.System, string.Empty),
        new(AppTheme.Light, string.Empty),
        new(AppTheme.Dark, string.Empty)
    ];

    public IReadOnlyList<Language> AvailableSourceLanguages { get; } =
        Language.LanguageCodes.Select(code => new Language(code, string.Empty)).ToList();

    public IReadOnlyList<Language> AvailableTargetLanguages { get; } =
        Language.LanguageCodes.Where(code => code != "auto").Select(code => new Language(code, string.Empty)).ToList();

    public IReadOnlyList<AutoCloseOption> AvailableAutoCloseOptions { get; } =
    [
        new(0, string.Empty),
        new(5, string.Empty),
        new(10, string.Empty),
        new(15, string.Empty),
        new(30, string.Empty)
    ];

    public IReadOnlyList<AppLanguageInfo> AvailableAppLanguages => _localizationService.SupportedAppLanguages;

    public IReadOnlyList<Key> AvailableKeys { get; } =
    [
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J,
        Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T,
        Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
        Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6,
        Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
        Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
        Key.OemTilde, Key.Space
    ];

    [ObservableProperty]
    private string _selectedAppLanguage = "system";

    [ObservableProperty]
    private bool _isControl;

    [ObservableProperty]
    private bool _isShift;

    [ObservableProperty]
    private bool _isAlt;

    [ObservableProperty]
    private bool _isWin;

    [ObservableProperty]
    private Key _selectedKey = Key.T;

    [ObservableProperty]
    private TranslationProvider _selectedProvider = TranslationProvider.Google;

    private string _selectedSourceLanguage = "auto";
    public string SelectedSourceLanguage
    {
        get => _selectedSourceLanguage;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetProperty(ref _selectedSourceLanguage, value);
            }
        }
    }

    private string _selectedTargetLanguage = "ru";
    public string SelectedTargetLanguage
    {
        get => _selectedTargetLanguage;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetProperty(ref _selectedTargetLanguage, value);
            }
        }
    }

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _closeOnEsc = true;

    [ObservableProperty]
    private bool _closeOnClickOutside = true;

    [ObservableProperty]
    private int _autoCloseSeconds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedRequestTimeoutText))]
    private int _requestTimeoutSeconds = 5;

    [ObservableProperty]
    private WindowPositionMode _positionMode = WindowPositionMode.NearCursor;

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.System;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusError;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public string FormattedRequestTimeoutText =>
        $"{RequestTimeoutSeconds} {_localizationService.GetString("Common_SecondsShort")}";

    public string HotkeyPreviewText
    {
        get
        {
            var parts = new List<string>();
            if (IsControl) parts.Add("Ctrl");
            if (IsShift) parts.Add("Shift");
            if (IsAlt) parts.Add("Alt");
            if (IsWin) parts.Add("Win");
            parts.Add(SelectedKey.ToString());
            return string.Join(" + ", parts);
        }
    }

    public event Action? RequestClose;

    public SettingsViewModel(
        ISettingsService settingsService,
        IHotkeyService hotkeyService,
        IStartupService startupService,
        ILogger<SettingsViewModel> logger,
        IThemeService? themeService = null,
        Action<string>? urlOpener = null,
        ILocalizationService? localizationService = null)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _startupService = startupService;
        _logger = logger;
        _localizationService = localizationService ?? LocalizationService.Instance;
        _urlOpener = urlOpener ?? (url =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        });

        _localizationService.LanguageChanged += OnLanguageChanged;

        LoadCurrentSettings();
    }

    private void OnLanguageChanged(string _)
    {
        ReloadLocalizedLists();
    }

    private void ReloadLocalizedLists()
    {
        var eff = _localizationService.EffectiveLanguage;

        foreach (var p in AvailableProviders)
        {
            switch (p.Provider)
            {
                case TranslationProvider.Google:
                    p.DisplayName = _localizationService.GetString("Provider_Google_Title");
                    p.Description = _localizationService.GetString("Provider_Google_Desc");
                    break;
                case TranslationProvider.Yandex:
                    p.DisplayName = _localizationService.GetString("Provider_Yandex_Title");
                    p.Description = _localizationService.GetString("Provider_Yandex_Desc");
                    break;
                case TranslationProvider.MyMemory:
                    p.DisplayName = _localizationService.GetString("Provider_MyMemory_Title");
                    p.Description = _localizationService.GetString("Provider_MyMemory_Desc");
                    break;
            }
        }

        foreach (var t in AvailableThemes)
        {
            switch (t.Theme)
            {
                case AppTheme.System:
                    t.DisplayName = _localizationService.GetString("Theme_System");
                    break;
                case AppTheme.Light:
                    t.DisplayName = _localizationService.GetString("Theme_Light");
                    break;
                case AppTheme.Dark:
                    t.DisplayName = _localizationService.GetString("Theme_Dark");
                    break;
            }
        }

        foreach (var opt in AvailableAutoCloseOptions)
        {
            opt.DisplayName = opt.Seconds == 0
                ? _localizationService.GetString("AutoClose_Off")
                : _localizationService.GetString("AutoClose_SecondsFormat", opt.Seconds);
        }

        foreach (var lang in AvailableSourceLanguages)
        {
            lang.Name = Language.GetDisplayName(lang.Code, eff);
        }

        foreach (var lang in AvailableTargetLanguages)
        {
            lang.Name = Language.GetDisplayName(lang.Code, eff);
        }

        OnPropertyChanged(nameof(AvailableAppLanguages));
        OnPropertyChanged(nameof(FormattedRequestTimeoutText));
        OnPropertyChanged(nameof(AuthorName));
    }

    public void LoadCurrentSettings()
    {
        var settings = _settingsService.CurrentSettings;

        SelectedAppLanguage = settings.AppLanguage;
        if (_localizationService.CurrentLanguage != settings.AppLanguage)
        {
            _localizationService.ApplyLanguage(settings.AppLanguage);
        }

        ReloadLocalizedLists();

        IsControl = settings.Hotkey.IsControl;
        IsShift = settings.Hotkey.IsShift;
        IsAlt = settings.Hotkey.IsAlt;
        IsWin = settings.Hotkey.IsWin;
        SelectedKey = settings.Hotkey.Key;

        SelectedProvider = settings.Provider;
        SelectedSourceLanguage = settings.SourceLanguage;
        SelectedTargetLanguage = settings.TargetLanguage;
        StartWithWindows = _startupService.IsAutoStartEnabled();
        CloseOnEsc = settings.CloseOnEsc;
        CloseOnClickOutside = settings.CloseOnClickOutside;
        AutoCloseSeconds = settings.AutoCloseSeconds;
        RequestTimeoutSeconds = settings.RequestTimeoutSeconds;
        PositionMode = settings.PositionMode;
        SelectedTheme = settings.Theme;

        StatusMessage = string.Empty;
        HasStatusError = false;
        OnPropertyChanged(nameof(HotkeyPreviewText));
    }

    partial void OnSelectedAppLanguageChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && _localizationService.CurrentLanguage != value)
        {
            _localizationService.ApplyLanguage(value);
        }
    }

    partial void OnIsControlChanged(bool value) => OnPropertyChanged(nameof(HotkeyPreviewText));
    partial void OnIsShiftChanged(bool value) => OnPropertyChanged(nameof(HotkeyPreviewText));
    partial void OnIsAltChanged(bool value) => OnPropertyChanged(nameof(HotkeyPreviewText));
    partial void OnIsWinChanged(bool value) => OnPropertyChanged(nameof(HotkeyPreviewText));
    partial void OnSelectedKeyChanged(Key value) => OnPropertyChanged(nameof(HotkeyPreviewText));

    [RelayCommand]
    public async Task SaveAsync()
    {
        StatusMessage = string.Empty;
        HasStatusError = false;

        if (!IsControl && !IsShift && !IsAlt && !IsWin)
        {
            HasStatusError = true;
            StatusMessage = _localizationService.GetString("Settings_StatusModifierRequired");
            return;
        }

        var newHotkey = new HotkeyConfig
        {
            IsControl = IsControl,
            IsShift = IsShift,
            IsAlt = IsAlt,
            IsWin = IsWin,
            Key = SelectedKey
        };

        // Try registering the hotkey to check for conflicts
        if (!_hotkeyService.Register(newHotkey, out var hotkeyError))
        {
            HasStatusError = true;
            StatusMessage = hotkeyError ?? _localizationService.GetString("Settings_StatusHotkeyRegisterFailed");
            return;
        }

        // Apply startup setting
        try
        {
            _startupService.SetAutoStart(StartWithWindows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update autostart setting");
            HasStatusError = true;
            StatusMessage = _localizationService.GetString("Settings_StatusAutostartFailed");
            return;
        }

        var newSettings = new AppSettings
        {
            AppLanguage = SelectedAppLanguage,
            Provider = SelectedProvider,
            Hotkey = newHotkey,
            SourceLanguage = SelectedSourceLanguage,
            TargetLanguage = SelectedTargetLanguage,
            StartWithWindows = StartWithWindows,
            CloseOnEsc = CloseOnEsc,
            CloseOnClickOutside = CloseOnClickOutside,
            AutoCloseSeconds = AutoCloseSeconds,
            RequestTimeoutSeconds = RequestTimeoutSeconds,
            PositionMode = PositionMode,
            Theme = SelectedTheme
        };

        try
        {
            await _settingsService.SaveSettingsAsync(newSettings);
            StatusMessage = _localizationService.GetString("Settings_StatusSavedSuccess");
            HasStatusError = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            HasStatusError = true;
            StatusMessage = _localizationService.GetString("Settings_StatusSaveFailed");
        }
    }

    [RelayCommand]
    public void ResetDefaults()
    {
        var defaults = new AppSettings();
        SelectedAppLanguage = defaults.AppLanguage;
        if (_localizationService.CurrentLanguage != defaults.AppLanguage)
        {
            _localizationService.ApplyLanguage(defaults.AppLanguage);
        }

        IsControl = defaults.Hotkey.IsControl;
        IsShift = defaults.Hotkey.IsShift;
        IsAlt = defaults.Hotkey.IsAlt;
        IsWin = defaults.Hotkey.IsWin;
        SelectedKey = defaults.Hotkey.Key;

        SelectedProvider = defaults.Provider;
        SelectedSourceLanguage = defaults.SourceLanguage;
        SelectedTargetLanguage = defaults.TargetLanguage;
        StartWithWindows = false;
        CloseOnEsc = defaults.CloseOnEsc;
        CloseOnClickOutside = defaults.CloseOnClickOutside;
        AutoCloseSeconds = defaults.AutoCloseSeconds;
        RequestTimeoutSeconds = defaults.RequestTimeoutSeconds;
        PositionMode = defaults.PositionMode;
        SelectedTheme = defaults.Theme;

        StatusMessage = _localizationService.GetString("Settings_StatusResetDefaults");
        HasStatusError = false;
        OnPropertyChanged(nameof(HotkeyPreviewText));
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    public void OpenGitHub()
    {
        try
        {
            _urlOpener(GitHubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open author GitHub URL: {Url}", GitHubUrl);
        }
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }
}

public class AutoCloseOption : ObservableObject
{
    public int Seconds { get; }

    private string _displayName;
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public AutoCloseOption(int seconds, string displayName)
    {
        Seconds = seconds;
        _displayName = displayName;
    }
}
