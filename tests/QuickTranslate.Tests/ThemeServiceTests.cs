using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class ThemeServiceTests
{
    private readonly ISettingsService _settingsService;
    private readonly ThemeService _themeService;

    public ThemeServiceTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _settingsService.CurrentSettings.Returns(new AppSettings { Theme = AppTheme.System });

        _themeService = new ThemeService(_settingsService, NullLogger<ThemeService>.Instance);
    }

    [Fact]
    public void ApplyTheme_Light_SetsIsDarkEffectiveFalse()
    {
        bool? eventFiredWith = null;
        _themeService.ThemeChanged += isDark => eventFiredWith = isDark;

        _themeService.ApplyTheme(AppTheme.Light);

        Assert.Equal(AppTheme.Light, _themeService.CurrentTheme);
        Assert.False(_themeService.IsDarkEffective);
        Assert.False(eventFiredWith);
    }

    [Fact]
    public void ApplyTheme_Dark_SetsIsDarkEffectiveTrue()
    {
        bool? eventFiredWith = null;
        _themeService.ThemeChanged += isDark => eventFiredWith = isDark;

        _themeService.ApplyTheme(AppTheme.Dark);

        Assert.Equal(AppTheme.Dark, _themeService.CurrentTheme);
        Assert.True(_themeService.IsDarkEffective);
        Assert.True(eventFiredWith);
    }

    [Fact]
    public void ApplyTheme_System_ResolvesToSystemSetting()
    {
        var expectedSystemIsDark = ThemeService.IsSystemInDarkMode();

        _themeService.ApplyTheme(AppTheme.System);

        Assert.Equal(AppTheme.System, _themeService.CurrentTheme);
        Assert.Equal(expectedSystemIsDark, _themeService.IsDarkEffective);
    }

    [Fact]
    public void Initialize_SubscribesToSettingsChangedAndAppliesTheme()
    {
        _themeService.Initialize();

        // Simulate settings changed event from settings service
        _settingsService.SettingsChanged += Raise.Event<Action<AppSettings>>(new AppSettings
        {
            Theme = AppTheme.Dark
        });

        Assert.Equal(AppTheme.Dark, _themeService.CurrentTheme);
        Assert.True(_themeService.IsDarkEffective);
    }

    [Fact]
    public void Dispose_UnsubscribesGracefully()
    {
        _themeService.Initialize();
        _themeService.Dispose();

        // Changing settings after dispose should not update theme
        _settingsService.SettingsChanged += Raise.Event<Action<AppSettings>>(new AppSettings
        {
            Theme = AppTheme.Dark
        });

        // Theme was System on initialize, shouldn't change
        Assert.Equal(AppTheme.System, _themeService.CurrentTheme);
    }

    [Fact]
    public void UpdateTitleBar_WhenWindowAllowsTransparencyOrNoneStyle_DoesNotThrowAndSkips()
    {
        var thread = new System.Threading.Thread(() =>
        {
            var transparentWindow = new System.Windows.Window
            {
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent
            };

            // Should safely do nothing and not throw
            _themeService.UpdateTitleBar(transparentWindow);
            _themeService.UpdateTitleBar(null!);
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
