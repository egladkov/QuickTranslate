using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using QuickTranslate.Models;
using Application = System.Windows.Application;

namespace QuickTranslate.Services;

public sealed class ThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ThemeService> _logger;
    private bool _disposed;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;
    public bool IsDarkEffective { get; private set; }

    public event Action<bool>? ThemeChanged;

    public ThemeService(ISettingsService settingsService, ILogger<ThemeService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public void Initialize()
    {
        _settingsService.SettingsChanged += OnSettingsChanged;
        try
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register SystemEvents.UserPreferenceChanged handler");
        }

        ApplyTheme(_settingsService.CurrentSettings.Theme);
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        ApplyTheme(settings.Theme);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (CurrentTheme == AppTheme.System)
        {
            var systemIsDark = IsSystemInDarkMode();
            if (systemIsDark != IsDarkEffective)
            {
                _logger.LogInformation("System theme preference changed, updating application theme");
                ApplyTheme(AppTheme.System);
            }
        }
    }

    public void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        bool isDark = theme switch
        {
            AppTheme.Light => false,
            AppTheme.Dark => true,
            _ => IsSystemInDarkMode()
        };

        IsDarkEffective = isDark;
        _logger.LogInformation("Applying theme {Theme} (Resolved Dark: {IsDark})", theme, isDark);

        if (Application.Current != null)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                ApplyThemeResources(isDark);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(() => ApplyThemeResources(isDark));
            }
        }

        ThemeChanged?.Invoke(isDark);
    }

    private void ApplyThemeResources(bool isDark)
    {
        var themeName = isDark ? "DarkTheme.xaml" : "LightTheme.xaml";
        var uri = new Uri($"pack://application:,,,/QuickTranslate;component/Themes/{themeName}", UriKind.Absolute);

        try
        {
            var newDict = new ResourceDictionary { Source = uri };
            var merged = Application.Current.Resources.MergedDictionaries;

            var existingThemeDict = merged.FirstOrDefault(d =>
                d.Source != null && (d.Source.OriginalString.Contains("LightTheme.xaml") || d.Source.OriginalString.Contains("DarkTheme.xaml")));

            if (existingThemeDict != null)
            {
                var index = merged.IndexOf(existingThemeDict);
                merged[index] = newDict;
            }
            else
            {
                merged.Add(newDict);
            }

            foreach (Window window in Application.Current.Windows)
            {
                UpdateTitleBar(window);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load theme resource dictionary: {ThemeUri}", uri);
        }
    }

    public void UpdateTitleBar(Window window)
    {
        if (window == null) return;

        // Skip windows that don't have standard Windows title bars.
        // Calling DwmSetWindowAttribute on transparent/borderless windows causes DWM
        // to render an opaque background rectangular frame behind the window in Windows 11.
        if (window.AllowsTransparency || window.WindowStyle == WindowStyle.None)
        {
            return;
        }

        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ApplyImmersiveDarkMode(hwnd, IsDarkEffective);
            }
            else
            {
                window.SourceInitialized += OnWindowSourceInitialized;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not update window title bar dark mode");
        }
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.SourceInitialized -= OnWindowSourceInitialized;
            if (window.AllowsTransparency || window.WindowStyle == WindowStyle.None)
            {
                return;
            }

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ApplyImmersiveDarkMode(hwnd, IsDarkEffective);
            }
        }
    }

    public static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int intVal)
            {
                return intVal == 0;
            }
        }
        catch
        {
            // Fallback to light theme if registry cannot be read
        }
        return false;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    private static void ApplyImmersiveDarkMode(IntPtr hwnd, bool isDark)
    {
        if (hwnd == IntPtr.Zero) return;
        int darkMode = isDark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _settingsService.SettingsChanged -= OnSettingsChanged;
        try
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
        catch
        {
            // Ignore unregistration errors
        }
    }
}
