using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings CurrentSettings { get; private set; } = new();

    public event Action<AppSettings>? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger, string? customSettingsPath = null)
    {
        _logger = logger;
        if (!string.IsNullOrEmpty(customSettingsPath))
        {
            _settingsFilePath = customSettingsPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDir = Path.Combine(appData, "QuickTranslate");
            _settingsFilePath = Path.Combine(appDir, "settings.json");
        }
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    loaded.Validate();
                    lock (_lock)
                    {
                        CurrentSettings = loaded;
                    }
                    _logger.LogInformation("Settings loaded successfully from {Path}", _settingsFilePath);
                    SettingsChanged?.Invoke(CurrentSettings);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}, using defaults", _settingsFilePath);
        }

        var defaults = new AppSettings();
        defaults.Validate();
        lock (_lock)
        {
            CurrentSettings = defaults;
        }
        await SaveSettingsAsync(defaults);
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Validate();

        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = _settingsFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _settingsFilePath, overwrite: true);

            lock (_lock)
            {
                CurrentSettings = settings.Clone();
            }

            _logger.LogInformation("Settings saved successfully to {Path}", _settingsFilePath);
            SettingsChanged?.Invoke(CurrentSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsFilePath);
            throw;
        }
    }
}
