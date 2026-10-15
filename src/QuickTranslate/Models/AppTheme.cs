using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickTranslate.Models;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public class ThemeInfo : ObservableObject
{
    public AppTheme Theme { get; init; }

    private string _displayName;
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public ThemeInfo(AppTheme theme, string displayName)
    {
        Theme = theme;
        _displayName = displayName;
    }
}
