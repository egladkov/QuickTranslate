using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using QuickTranslate.Services;

namespace QuickTranslate.Infrastructure;

public sealed class TrayIconManager : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly ILogger<TrayIconManager> _logger;
    private readonly ILocalizationService _localizationService;

    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _settingsItem;
    private ToolStripMenuItem? _showTranslationItem;
    private ToolStripMenuItem? _autostartMenuItem;
    private ToolStripMenuItem? _exitItem;

    public event Action? OpenSettingsRequested;
    public event Action? OpenTranslationWindowRequested;
    public event Action? ExitRequested;

    public TrayIconManager(
        ISettingsService settingsService,
        IStartupService startupService,
        ILogger<TrayIconManager> logger,
        ILocalizationService? localizationService = null)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _logger = logger;
        _localizationService = localizationService ?? LocalizationService.Instance;
    }

    public void Initialize()
    {
        var icon = AppIcon.CreateTrayIcon();

        var contextMenu = new ContextMenuStrip();

        _settingsItem = new ToolStripMenuItem(_localizationService.GetString("Tray_Settings"), null, (s, e) => OpenSettingsRequested?.Invoke());
        _settingsItem.Font = new Font(_settingsItem.Font, System.Drawing.FontStyle.Bold);

        _showTranslationItem = new ToolStripMenuItem(_localizationService.GetString("Tray_ShowTranslationWindow"), null, (s, e) => OpenTranslationWindowRequested?.Invoke());

        _autostartMenuItem = new ToolStripMenuItem(_localizationService.GetString("Tray_StartWithWindows"))
        {
            CheckOnClick = true,
            Checked = _startupService.IsAutoStartEnabled()
        };
        _autostartMenuItem.Click += OnAutostartMenuItemClick;

        _exitItem = new ToolStripMenuItem(_localizationService.GetString("Tray_Exit"), null, (s, e) => ExitRequested?.Invoke());

        contextMenu.Items.Add(_settingsItem);
        contextMenu.Items.Add(_showTranslationItem);
        contextMenu.Items.Add(_autostartMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "QuickTranslate (Ctrl+Shift+T)",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => OpenSettingsRequested?.Invoke();

        _settingsService.SettingsChanged += OnSettingsChanged;
        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    private void UpdateMenuTexts()
    {
        if (_settingsItem != null)
            _settingsItem.Text = _localizationService.GetString("Tray_Settings");
        if (_showTranslationItem != null)
            _showTranslationItem.Text = _localizationService.GetString("Tray_ShowTranslationWindow");
        if (_autostartMenuItem != null)
            _autostartMenuItem.Text = _localizationService.GetString("Tray_StartWithWindows");
        if (_exitItem != null)
            _exitItem.Text = _localizationService.GetString("Tray_Exit");
    }

    private void OnLanguageChanged(string _)
    {
        UpdateMenuTexts();
    }

    private void OnAutostartMenuItemClick(object? sender, EventArgs e)
    {
        if (_autostartMenuItem == null) return;

        var newState = _autostartMenuItem.Checked;
        try
        {
            _startupService.SetAutoStart(newState);
            var settings = _settingsService.CurrentSettings;
            settings.StartWithWindows = newState;
            _ = _settingsService.SaveSettingsAsync(settings);
            _logger.LogInformation("Autostart toggled from tray: {Enabled}", newState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle autostart from tray");
            _autostartMenuItem.Checked = !newState;
        }
    }

    private void OnSettingsChanged(Models.AppSettings settings)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = $"QuickTranslate ({settings.Hotkey})";
        }
        if (_autostartMenuItem != null)
        {
            _autostartMenuItem.Checked = _startupService.IsAutoStartEnabled();
        }
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _settingsService.SettingsChanged -= OnSettingsChanged;
        _localizationService.LanguageChanged -= OnLanguageChanged;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
