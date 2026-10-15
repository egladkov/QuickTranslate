using System.Globalization;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickTranslate.Models;
using Application = System.Windows.Application;

namespace QuickTranslate.Services;

public sealed class LocalizationService : ILocalizationService, IDisposable
{
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<LocalizationService> _logger;
    private bool _disposed;

    public static ILocalizationService Instance { get; set; } = new LocalizationService();

    public string CurrentLanguage { get; private set; } = "system";
    public string EffectiveLanguage { get; private set; } = LanguageResources.DefaultLanguage;

    public event Action<string>? LanguageChanged;

    public LocalizationService(
        ISettingsService? settingsService = null,
        ILogger<LocalizationService>? logger = null)
    {
        _settingsService = settingsService;
        _logger = logger ?? NullLogger<LocalizationService>.Instance;
    }

    public void Initialize()
    {
        if (_settingsService != null)
        {
            _settingsService.SettingsChanged += OnSettingsChanged;
            ApplyLanguage(_settingsService.CurrentSettings.AppLanguage);
        }
        else
        {
            ApplyLanguage("system");
        }
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        ApplyLanguage(settings.AppLanguage);
    }

    public static bool IsSupportedLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        if (code.Equals("system", StringComparison.OrdinalIgnoreCase)) return true;
        return LanguageResources.Translations.ContainsKey(code);
    }

    public static string ResolveSystemLanguageCode(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var name = culture.Name;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return "zh-CN";

        var twoLetter = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return twoLetter switch
        {
            "ru" => "ru",
            "es" => "es",
            "de" => "de",
            "fr" => "fr",
            "en" => "en",
            _ => "en"
        };
    }

    public void ApplyLanguage(string languageCode)
    {
        CurrentLanguage = string.IsNullOrWhiteSpace(languageCode) ? "system" : languageCode;

        string effective;
        if (CurrentLanguage.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            effective = ResolveSystemLanguageCode();
        }
        else if (LanguageResources.Translations.ContainsKey(CurrentLanguage))
        {
            effective = CurrentLanguage;
        }
        else
        {
            effective = LanguageResources.DefaultLanguage;
        }

        EffectiveLanguage = effective;
        _logger.LogInformation("Applying application language: {Language} (Resolved Effective: {Effective})", CurrentLanguage, EffectiveLanguage);

        _supportedAppLanguages[0].DisplayName = GetString("Language_System");

        UpdateWpfResources();

        LanguageChanged?.Invoke(EffectiveLanguage);
    }

    private void UpdateWpfResources()
    {
        if (Application.Current == null) return;

        if (Application.Current.Dispatcher.CheckAccess())
        {
            ApplyResourcesToCurrentApp();
        }
        else
        {
            Application.Current.Dispatcher.BeginInvoke(ApplyResourcesToCurrentApp);
        }
    }

    private void ApplyResourcesToCurrentApp()
    {
        try
        {
            if (Application.Current == null) return;

            var dict = new ResourceDictionary();
            dict["__AppStringDictionaryMarker"] = true;

            var strings = GetStringsForLanguage(EffectiveLanguage);
            foreach (var kvp in strings)
            {
                dict[kvp.Key] = kvp.Value;
            }

            var merged = Application.Current.Resources.MergedDictionaries;
            var existing = merged.FirstOrDefault(d => d.Contains("__AppStringDictionaryMarker"));

            if (existing != null)
            {
                var idx = merged.IndexOf(existing);
                merged[idx] = dict;
            }
            else
            {
                merged.Add(dict);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply localization dictionary to WPF resources");
        }
    }

    private IReadOnlyDictionary<string, string> GetStringsForLanguage(string langCode)
    {
        if (LanguageResources.Translations.TryGetValue(langCode, out var dict))
        {
            return dict;
        }
        return LanguageResources.Translations[LanguageResources.DefaultLanguage];
    }

    public string GetString(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        var currentStrings = GetStringsForLanguage(EffectiveLanguage);
        if (currentStrings.TryGetValue(key, out var val))
        {
            return val;
        }

        var fallbackStrings = LanguageResources.Translations[LanguageResources.DefaultLanguage];
        if (fallbackStrings.TryGetValue(key, out var fallbackVal))
        {
            return fallbackVal;
        }

        return key;
    }

    public string GetString(string key, params object[] args)
    {
        var raw = GetString(key);
        if (args == null || args.Length == 0) return raw;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, raw, args);
        }
        catch
        {
            return raw;
        }
    }

    public string this[string key] => GetString(key);

    private readonly List<AppLanguageInfo> _supportedAppLanguages =
    [
        new("system", "Системный (по умолчанию)"),
        new("ru", "Русский"),
        new("en", "English"),
        new("es", "Español"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("zh-CN", "中文 (简体)")
    ];

    public IReadOnlyList<AppLanguageInfo> SupportedAppLanguages => _supportedAppLanguages;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_settingsService != null)
        {
            _settingsService.SettingsChanged -= OnSettingsChanged;
        }
    }
}
