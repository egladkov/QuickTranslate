using System.IO;
using System.Windows.Input;
using Microsoft.Extensions.Logging.Abstractions;
using QuickTranslate.Models;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;

    public SettingsServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "QuickTranslateTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "test_settings.json");
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultsAndSavesFile()
    {
        var service = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);

        await service.LoadSettingsAsync();

        Assert.NotNull(service.CurrentSettings);
        Assert.Equal("system", service.CurrentSettings.AppLanguage);
        Assert.Equal("auto", service.CurrentSettings.SourceLanguage);
        Assert.Equal("ru", service.CurrentSettings.TargetLanguage);
        Assert.Equal(Key.T, service.CurrentSettings.Hotkey.Key);
        Assert.Equal(AppTheme.System, service.CurrentSettings.Theme);
        Assert.True(File.Exists(_settingsFilePath));
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsToDiskAndTriggersEvent()
    {
        var service = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await service.LoadSettingsAsync();

        var eventFired = false;
        service.SettingsChanged += s =>
        {
            eventFired = true;
            Assert.Equal("en", s.TargetLanguage);
        };

        var newSettings = new AppSettings
        {
            TargetLanguage = "en",
            RequestTimeoutSeconds = 12
        };

        await service.SaveSettingsAsync(newSettings);

        Assert.True(eventFired);
        Assert.Equal("en", service.CurrentSettings.TargetLanguage);
        Assert.Equal(12, service.CurrentSettings.RequestTimeoutSeconds);

        // Reload to ensure persistence
        var reloadedService = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await reloadedService.LoadSettingsAsync();
        Assert.Equal("en", reloadedService.CurrentSettings.TargetLanguage);
        Assert.Equal(12, reloadedService.CurrentSettings.RequestTimeoutSeconds);
    }

    [Fact]
    public void AppSettings_Validate_ClampsTimeoutAndEnsuresDefaults()
    {
        var settings = new AppSettings
        {
            RequestTimeoutSeconds = 1,
            SourceLanguage = "",
            TargetLanguage = ""
        };

        settings.Validate();

        Assert.Equal(2, settings.RequestTimeoutSeconds);
        Assert.Equal("auto", settings.SourceLanguage);
        Assert.Equal("ru", settings.TargetLanguage);

        settings.RequestTimeoutSeconds = 100;
        settings.Provider = (TranslationProvider)999;
        settings.Theme = (AppTheme)999;
        settings.AutoCloseSeconds = -5;
        settings.Validate();
        Assert.Equal(30, settings.RequestTimeoutSeconds);
        Assert.Equal(TranslationProvider.Google, settings.Provider);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(0, settings.AutoCloseSeconds);

        settings.AutoCloseSeconds = 500;
        settings.AppLanguage = "";
        settings.PositionMode = (WindowPositionMode)999;
        settings.Hotkey = new HotkeyConfig { Key = Key.None };
        settings.Validate();
        Assert.Equal(300, settings.AutoCloseSeconds);
        Assert.Equal("system", settings.AppLanguage);
        Assert.Equal(WindowPositionMode.NearCursor, settings.PositionMode);
        Assert.Equal(Key.T, settings.Hotkey.Key);
    }

    [Fact]
    public async Task SaveSettingsAsync_PreservesAutoCloseSeconds()
    {
        var service = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await service.LoadSettingsAsync();

        var newSettings = new AppSettings
        {
            AutoCloseSeconds = 10
        };

        await service.SaveSettingsAsync(newSettings);
        Assert.Equal(10, service.CurrentSettings.AutoCloseSeconds);

        var reloadedService = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await reloadedService.LoadSettingsAsync();
        Assert.Equal(10, reloadedService.CurrentSettings.AutoCloseSeconds);
    }

    [Fact]
    public async Task SaveSettingsAsync_PreservesTheme()
    {
        var service = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await service.LoadSettingsAsync();

        var newSettings = new AppSettings
        {
            Theme = AppTheme.Dark
        };

        await service.SaveSettingsAsync(newSettings);
        Assert.Equal(AppTheme.Dark, service.CurrentSettings.Theme);

        var reloadedService = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await reloadedService.LoadSettingsAsync();
        Assert.Equal(AppTheme.Dark, reloadedService.CurrentSettings.Theme);
    }

    [Fact]
    public async Task SaveSettingsAsync_PreservesCloseOnClickOutside()
    {
        var service = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await service.LoadSettingsAsync();
        Assert.True(service.CurrentSettings.CloseOnClickOutside);

        var newSettings = new AppSettings
        {
            CloseOnClickOutside = false
        };

        await service.SaveSettingsAsync(newSettings);
        Assert.False(service.CurrentSettings.CloseOnClickOutside);

        var reloadedService = new SettingsService(NullLogger<SettingsService>.Instance, _settingsFilePath);
        await reloadedService.LoadSettingsAsync();
        Assert.False(reloadedService.CurrentSettings.CloseOnClickOutside);
    }

    [Fact]
    public void AppSettings_Clone_PreservesCloseOnClickOutside()
    {
        var original = new AppSettings
        {
            CloseOnClickOutside = false
        };

        var clone = original.Clone();
        Assert.False(clone.CloseOnClickOutside);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
