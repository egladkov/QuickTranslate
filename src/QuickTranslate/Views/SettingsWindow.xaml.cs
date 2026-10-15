using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using QuickTranslate.Infrastructure;
using QuickTranslate.Services;
using QuickTranslate.ViewModels;

namespace QuickTranslate.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly IThemeService? _themeService;

    public SettingsWindow(SettingsViewModel viewModel, IThemeService? themeService = null)
    {
        InitializeComponent();
        Icon ??= AppIcon.ImageSource;
        _viewModel = viewModel;
        _themeService = themeService;
        DataContext = _viewModel;

        _viewModel.RequestClose += () =>
        {
            Dispatcher.Invoke(HideSettings);
        };

        if (_themeService != null)
        {
            _themeService.UpdateTitleBar(this);
            _themeChangedHandler = _ => Dispatcher.Invoke(() => _themeService.UpdateTitleBar(this));
            _themeService.ThemeChanged += _themeChangedHandler;
        }
    }

    private readonly Action<bool>? _themeChangedHandler;

    public void ShowSettings()
    {
        _viewModel.LoadCurrentSettings();
        _themeService?.UpdateTitleBar(this);
        if (!IsVisible)
        {
            Show();
        }
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
    }

    public void HideSettings()
    {
        Hide();
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, QuickTranslate.Infrastructure.MemoryOptimizer.TrimWorkingSet);
    }

    public bool AllowActualClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (AllowActualClose || System.Windows.Application.Current?.Dispatcher.HasShutdownStarted == true)
        {
            if (_themeService != null && _themeChangedHandler != null)
            {
                _themeService.ThemeChanged -= _themeChangedHandler;
            }
            return;
        }

        // Hide instead of destroy to enable reuse
        e.Cancel = true;
        HideSettings();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HideSettings();
            e.Handled = true;
        }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        _viewModel.OpenGitHub();
        e.Handled = true;
    }
}
