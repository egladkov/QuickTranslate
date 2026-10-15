using QuickTranslate.Models;

namespace QuickTranslate.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    event Action<AppSettings>? SettingsChanged;
    Task LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
