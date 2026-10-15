using System.Windows;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public interface IThemeService : IDisposable
{
    AppTheme CurrentTheme { get; }
    bool IsDarkEffective { get; }
    event Action<bool>? ThemeChanged;
    void Initialize();
    void ApplyTheme(AppTheme theme);
    void UpdateTitleBar(Window window);
}
