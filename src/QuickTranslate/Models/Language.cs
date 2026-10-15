using CommunityToolkit.Mvvm.ComponentModel;
using QuickTranslate.Services;

namespace QuickTranslate.Models;

public class Language : ObservableObject
{
    public string Code { get; init; }

    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Language(string code, string name)
    {
        Code = code;
        _name = name;
    }

    public static readonly string[] LanguageCodes =
    [
        "auto", "ru", "en", "de", "fr", "es", "it", "zh-CN",
        "ja", "ko", "pt", "tr", "pl", "uk", "ar"
    ];

    public static IReadOnlyList<Language> GetSupportedLanguages(string? uiLanguage = null)
    {
        return LanguageCodes.Select(code => new Language(code, GetDisplayName(code, uiLanguage))).ToList();
    }

    public static IReadOnlyList<Language> GetTargetLanguages(string? uiLanguage = null)
    {
        return GetSupportedLanguages(uiLanguage).Where(l => l.Code != "auto").ToList();
    }

    public static string GetDisplayName(string code, string? uiLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;

        var effective = string.IsNullOrWhiteSpace(uiLanguage)
            ? (LocalizationService.Instance?.EffectiveLanguage ?? LanguageResources.DefaultLanguage)
            : uiLanguage;

        if (effective.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            effective = LocalizationService.ResolveSystemLanguageCode();
        }

        if (!LanguageResources.Translations.TryGetValue(effective, out var langDict))
        {
            langDict = LanguageResources.Translations[LanguageResources.DefaultLanguage];
        }

        var fallbackDict = LanguageResources.Translations[LanguageResources.DefaultLanguage];

        // Direct key lookup
        var key = $"Lang_{code}";
        if (langDict.TryGetValue(key, out var name) || fallbackDict.TryGetValue(key, out name))
        {
            return name;
        }

        // Special case: zh -> zh-CN
        if (code.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            if (langDict.TryGetValue("Lang_zh-CN", out var zhName) || fallbackDict.TryGetValue("Lang_zh-CN", out zhName))
            {
                return zhName;
            }
        }

        // Prefix lookup: e.g. en-US -> en
        var dashIndex = code.IndexOf('-');
        if (dashIndex > 0)
        {
            var prefixKey = $"Lang_{code.Substring(0, dashIndex)}";
            if (langDict.TryGetValue(prefixKey, out var prefixName) || fallbackDict.TryGetValue(prefixKey, out prefixName))
            {
                return prefixName;
            }
        }

        return code;
    }
}
