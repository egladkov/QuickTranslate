using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickTranslate.Infrastructure;
using QuickTranslate.Services;
using QuickTranslate.ViewModels;
using QuickTranslate.Views;
using Application = System.Windows.Application;

namespace QuickTranslate;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;
    private SingleInstanceManager? _singleInstanceManager;
    private TrayIconManager? _trayIconManager;
    private IHotkeyService? _hotkeyService;
    private IClipboardService? _clipboardService;
    private TranslationViewModel? _translationViewModel;
    private TranslationWindow? _translationWindow;
    private SettingsWindow? _settingsWindow;
    private bool _isTranslating;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Use software rendering to eliminate ~100-150MB of native GPU driver allocations (D3D9/AMDXN64.DLL)
        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        // Setup global exception handling
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        SessionEnding += OnSessionEnding;

        // Configure Services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("QuickTranslate is starting up...");

        // Check Single Instance
        _singleInstanceManager = _serviceProvider.GetRequiredService<SingleInstanceManager>();
        if (!_singleInstanceManager.TryAcquireOwnership())
        {
            _logger.LogInformation("Another instance is already running. Exiting.");
            Shutdown(0);
            return;
        }

        // Listen for wake request from subsequent instances
        _singleInstanceManager.WakeRequested += () =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                GetSettingsWindow().ShowSettings();
            });
        };

        // Load Settings
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        await settingsService.LoadSettingsAsync();

        // Initialize Localization
        var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
        LocalizationService.Instance = localizationService;
        localizationService.Initialize();

        // Initialize Theme
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        themeService.Initialize();

        // Resolve core background services (windows and ViewModels are lazily loaded on demand)
        _clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();

        // Initialize Tray Icon
        _trayIconManager = _serviceProvider.GetRequiredService<TrayIconManager>();
        _trayIconManager.OpenSettingsRequested += () => GetSettingsWindow().ShowSettings();
        _trayIconManager.OpenTranslationWindowRequested += () =>
        {
            GetTranslationViewModel().PrepareForManualInput();
            GetTranslationWindow().ShowAtOptimalPosition();
        };
        _trayIconManager.ExitRequested += () => Shutdown(0);
        _trayIconManager.Initialize();

        // Register Global Hotkey
        _hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        var registered = _hotkeyService.Register(settingsService.CurrentSettings.Hotkey, out var hotkeyError);
        if (!registered)
        {
            _logger.LogWarning("Failed to register startup hotkey: {Error}", hotkeyError);
            _trayIconManager.ShowNotification(
                localizationService.GetString("Tray_HotkeyConflictTitle"),
                localizationService.GetString("Tray_HotkeyConflictMessage", hotkeyError ?? string.Empty),
                System.Windows.Forms.ToolTipIcon.Warning);
        }

        _logger.LogInformation("Application initialized and running in system tray.");
        MemoryOptimizer.TrimWorkingSet();
    }

    private TranslationViewModel GetTranslationViewModel() =>
        _translationViewModel ??= _serviceProvider!.GetRequiredService<TranslationViewModel>();

    private TranslationWindow GetTranslationWindow() =>
        _translationWindow ??= _serviceProvider!.GetRequiredService<TranslationWindow>();

    private SettingsWindow GetSettingsWindow() =>
        _settingsWindow ??= _serviceProvider!.GetRequiredService<SettingsWindow>();

    private void ConfigureServices(IServiceCollection services)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logFile = Path.Combine(appData, "QuickTranslate", "logs", "app.log");

        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            logging.AddFile(logFile);
        });

        services.AddHttpClient<GoogleTranslateService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(35);
        });
        services.AddHttpClient<YandexTranslateService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(35);
        });
        services.AddHttpClient<MyMemoryTranslateService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(35);
        });

        services.AddSingleton<ITranslationService, TranslationServiceRouter>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IStartupService, RegistryStartupService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<SingleInstanceManager>();
        services.AddSingleton<TrayIconManager>();

        services.AddSingleton<TranslationViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<TranslationWindow>();
        services.AddSingleton<SettingsWindow>();
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Prevent concurrent translation triggers from hammering the service
        if (_isTranslating)
        {
            _logger?.LogInformation("Hotkey pressed while previous translation is in progress. Ignoring duplicate trigger.");
            return;
        }

        _isTranslating = true;
        try
        {
            if (_clipboardService == null || _serviceProvider == null) return;

            var selectedText = await _clipboardService.GetSelectedTextAsync();
            var translationViewModel = GetTranslationViewModel();
            var translationWindow = GetTranslationWindow();

            if (string.IsNullOrWhiteSpace(selectedText))
            {
                _logger?.LogInformation("No text was selected or retrieved from foreground application. Opening window for manual entry.");
                translationViewModel.PrepareForManualInput();
                translationWindow.ShowAtOptimalPosition();
                return;
            }

            translationViewModel.IsSourceReadOnly = true;
            translationViewModel.IsTranslateButtonVisible = false;

            // Show window and begin translation
            translationWindow.ShowAtOptimalPosition();

            await translationViewModel.StartTranslationAsync(selectedText, isFromSelection: true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling hotkey translation cycle");
        }
        finally
        {
            _isTranslating = false;
        }
    }

    private void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        _logger?.LogInformation("Windows session is ending.");
        if (_translationWindow != null) _translationWindow.AllowActualClose = true;
        if (_settingsWindow != null) _settingsWindow.AllowActualClose = true;
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Dispatcher unhandled exception caught: {Message}", e.Exception.Message);
        // Prevent application crash
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.LogCritical(ex, "Domain unhandled exception: {Message}", ex.Message);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("QuickTranslate is shutting down.");

        try
        {
            if (_hotkeyService != null)
            {
                _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
                _hotkeyService.Dispose();
            }

            if (_translationWindow != null)
            {
                _translationWindow.AllowActualClose = true;
                _translationWindow.Close();
            }

            if (_settingsWindow != null)
            {
                _settingsWindow.AllowActualClose = true;
                _settingsWindow.Close();
            }

            _trayIconManager?.Dispose();
            _singleInstanceManager?.Dispose();
            _serviceProvider?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during cleanup on exit");
        }

        base.OnExit(e);
    }
}
