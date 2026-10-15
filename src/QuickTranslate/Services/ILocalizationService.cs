using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickTranslate.Services;

public class AppLanguageInfo : ObservableObject
{
    public string Code { get; init; }

    private string _displayName;
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public AppLanguageInfo(string code, string displayName)
    {
        Code = code;
        _displayName = displayName;
    }
}

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    string EffectiveLanguage { get; }
    IReadOnlyList<AppLanguageInfo> SupportedAppLanguages { get; }

    void Initialize();
    void ApplyLanguage(string languageCode);

    string GetString(string key);
    string GetString(string key, params object[] args);
    string this[string key] { get; }

    event Action<string>? LanguageChanged;
}
