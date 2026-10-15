using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickTranslate.Models;

public enum TranslationProvider
{
    Google,
    Yandex,
    MyMemory
}

public class TranslationProviderInfo : ObservableObject
{
    public TranslationProvider Provider { get; init; }

    private string _displayName;
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    private string _description;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public TranslationProviderInfo(TranslationProvider provider, string displayName, string description)
    {
        Provider = provider;
        _displayName = displayName;
        _description = description;
    }
}
