using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Size = System.Windows.Size;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;
using System.Windows.Interop;
using System.Windows.Threading;
using QuickTranslate.Models;
using QuickTranslate.Services;
using QuickTranslate.ViewModels;

namespace QuickTranslate.Views;

public partial class TranslationWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly TranslationViewModel _viewModel;
    private readonly DispatcherTimer _autoCloseTimer;
    private int _remainingSeconds;
    private bool _isPausedByHover;
    private bool _isShowing;

    public int RemainingAutoCloseSeconds => _remainingSeconds;
    public bool IsAutoCloseTimerRunning => _autoCloseTimer.IsEnabled;
    public bool IsAutoClosePausedByHover => _isPausedByHover;

    #region Win32 Monitor & Cursor
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
    #endregion

    public TranslationWindow(
        TranslationViewModel viewModel,
        ISettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsService = settingsService;
        DataContext = _viewModel;

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _autoCloseTimer.Tick += AutoCloseTimer_Tick;

        _viewModel.RequestClose += () =>
        {
            if (Dispatcher.CheckAccess())
            {
                HideWindow();
            }
            else
            {
                Dispatcher.BeginInvoke(HideWindow);
            }
        };

        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(TranslationViewModel.IsLoading))
            {
                void HandleLoadingChange()
                {
                    if (_viewModel.IsLoading)
                    {
                        StopAutoCloseTimerWhileLoading();
                    }
                    else if (IsVisible && _viewModel.IsSourceReadOnly)
                    {
                        ResetAndStartAutoCloseTimer();
                    }
                }

                if (Dispatcher.CheckAccess())
                {
                    HandleLoadingChange();
                }
                else
                {
                    Dispatcher.BeginInvoke(HandleLoadingChange);
                }
            }
            else if (e.PropertyName == nameof(TranslationViewModel.IsSourceReadOnly))
            {
                void HandleReadOnlyChange()
                {
                    if (!_viewModel.IsSourceReadOnly)
                    {
                        StopAutoCloseTimer();
                    }
                    else if (IsVisible && !_viewModel.IsLoading)
                    {
                        ResetAndStartAutoCloseTimer();
                    }
                }

                if (Dispatcher.CheckAccess())
                {
                    HandleReadOnlyChange();
                }
                else
                {
                    Dispatcher.BeginInvoke(HandleReadOnlyChange);
                }
            }
        };

        _settingsService.SettingsChanged += _ =>
        {
            void HandleSettingsChanged()
            {
                if (IsVisible && _viewModel.IsSourceReadOnly)
                {
                    ResetAndStartAutoCloseTimer();
                }
                else
                {
                    _viewModel.UpdateCloseHint();
                }
            }

            if (Dispatcher.CheckAccess())
            {
                HandleSettingsChanged();
            }
            else
            {
                Dispatcher.BeginInvoke(HandleSettingsChanged);
            }
        };

        IsVisibleChanged += TranslationWindow_IsVisibleChanged;
    }

    public bool AllowActualClose { get; set; }

    public void HideWindow()
    {
        _isShowing = false;
        StopAutoCloseTimer();
        _viewModel.ResetCopiedState();
        Hide();
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, QuickTranslate.Infrastructure.MemoryOptimizer.TrimWorkingSet);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        HandleDeactivated();
    }

    public void HandleDeactivated()
    {
        if (_isShowing || Visibility != Visibility.Visible || AllowActualClose)
        {
            return;
        }

        var closeOnClickOutside = _settingsService.CurrentSettings?.CloseOnClickOutside ?? true;
        if (closeOnClickOutside)
        {
            HideWindow();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        StopAutoCloseTimer();
        if (!AllowActualClose && System.Windows.Application.Current?.Dispatcher.HasShutdownStarted != true)
        {
            // Cancel actual close and hide instead, enabling clean window reuse
            e.Cancel = true;
            HideWindow();
        }
    }

    private void TranslationWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            if (_viewModel.IsSourceReadOnly)
            {
                ResetAndStartAutoCloseTimer();
            }
            else
            {
                StopAutoCloseTimer();
            }
        }
        else
        {
            StopAutoCloseTimer();
        }
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        PauseAutoCloseTimer();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ResumeAutoCloseTimer();
    }

    public void ShowAtOptimalPosition()
    {
        _isShowing = true;
        try
        {
            var positionMode = _settingsService.CurrentSettings?.PositionMode ?? WindowPositionMode.NearCursor;

            var targetWidth = double.IsNaN(Width) ? 440 : Width;

            // Ensure window is measured with fixed width to calculate height properly
            UpdateLayout();
            Measure(new Size(targetWidth, double.PositiveInfinity));

            var desiredWidth = targetWidth;
            var desiredHeight = DesiredSize.Height > 0 ? DesiredSize.Height : (ActualHeight > 0 ? ActualHeight : 260);

            GetCursorPos(out var cursorPos);
            var hMonitor = MonitorFromPoint(cursorPos, MONITOR_DEFAULTTONEAREST);

            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            double workLeft = 0;
            double workTop = 0;
            double workRight = SystemParameters.PrimaryScreenWidth;
            double workBottom = SystemParameters.PrimaryScreenHeight;

            // Retrieve DPI scale factor for accurate DIP conversion
            var dpiScale = VisualTreeHelper.GetDpi(this);
            var dpiX = dpiScale.DpiScaleX;
            var dpiY = dpiScale.DpiScaleY;

            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                workLeft = monitorInfo.rcWork.Left / dpiX;
                workTop = monitorInfo.rcWork.Top / dpiY;
                workRight = monitorInfo.rcWork.Right / dpiX;
                workBottom = monitorInfo.rcWork.Bottom / dpiY;
            }

            double targetLeft;
            double targetTop;

            if (positionMode == WindowPositionMode.CenterScreen)
            {
                targetLeft = workLeft + ((workRight - workLeft - desiredWidth) / 2);
                targetTop = workTop + ((workBottom - workTop - desiredHeight) / 2);
            }
            else
            {
                var cursorDipX = cursorPos.X / dpiX;
                var cursorDipY = cursorPos.Y / dpiY;

                // Place near cursor with small offset
                targetLeft = cursorDipX + 15;
                targetTop = cursorDipY + 15;

                // Clamp inside work area horizontally
                if (targetLeft + desiredWidth > workRight - 10)
                {
                    targetLeft = cursorDipX - desiredWidth - 15;
                }
                if (targetLeft < workLeft + 10)
                {
                    targetLeft = workLeft + 10;
                }

                // Clamp inside work area vertically
                if (targetTop + desiredHeight > workBottom - 10)
                {
                    targetTop = cursorDipY - desiredHeight - 15;
                }
                if (targetTop < workTop + 10)
                {
                    targetTop = workTop + 10;
                }
            }

            Left = targetLeft;
            Top = targetTop;

            if (!IsVisible)
            {
                Show();
            }
            Activate();

            if (!_viewModel.IsSourceReadOnly)
            {
                InputTextBox.Focus();
                InputTextBox.CaretIndex = InputTextBox.Text.Length;
            }
            else
            {
                Focus();
            }
        }
        finally
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                _isShowing = false;
            });
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void TranslationResult_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.CopyTranslationCommand.CanExecute(null))
        {
            _viewModel.CopyTranslationCommand.Execute(null);
            if (_viewModel.IsSourceReadOnly)
            {
                ResetAndStartAutoCloseTimer();
            }
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var closeOnEsc = _settingsService.CurrentSettings?.CloseOnEsc ?? true;
            if (closeOnEsc)
            {
                HideWindow();
                e.Handled = true;
            }
        }
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel.IsSourceReadOnly)
        {
            return;
        }

        if (e.Key == Key.Enter && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Control))
        {
            _viewModel.ManualTranslateCommand.Execute(null);
            e.Handled = true;
        }
    }

    public void ResetAndStartAutoCloseTimer()
    {
        _autoCloseTimer.Stop();

        if (!_viewModel.IsSourceReadOnly)
        {
            _remainingSeconds = 0;
            _isPausedByHover = false;
            _viewModel.UpdateCloseHint();
            return;
        }

        var autoCloseSeconds = _settingsService.CurrentSettings?.AutoCloseSeconds ?? 0;
        if (autoCloseSeconds <= 0)
        {
            _remainingSeconds = 0;
            _viewModel.UpdateCloseHint(0, isPaused: false);
            return;
        }

        if (_viewModel.IsLoading)
        {
            StopAutoCloseTimerWhileLoading();
            return;
        }

        _remainingSeconds = autoCloseSeconds;
        _isPausedByHover = IsMouseOver;
        _viewModel.UpdateCloseHint(_remainingSeconds, _isPausedByHover);

        if (!_isPausedByHover)
        {
            _autoCloseTimer.Start();
        }
    }

    public void PauseAutoCloseTimer()
    {
        if (!_viewModel.IsSourceReadOnly) return;
        var autoCloseSeconds = _settingsService.CurrentSettings?.AutoCloseSeconds ?? 0;
        if (autoCloseSeconds <= 0) return;

        _isPausedByHover = true;
        _autoCloseTimer.Stop();
        _viewModel.UpdateCloseHint(_remainingSeconds, isPaused: true);
    }

    public void ResumeAutoCloseTimer()
    {
        if (!_viewModel.IsSourceReadOnly) return;
        var autoCloseSeconds = _settingsService.CurrentSettings?.AutoCloseSeconds ?? 0;
        if (autoCloseSeconds <= 0) return;

        _isPausedByHover = false;
        if (_remainingSeconds <= 0)
        {
            _remainingSeconds = autoCloseSeconds;
        }
        _viewModel.UpdateCloseHint(_remainingSeconds, isPaused: false);
        _autoCloseTimer.Start();
    }

    public void StopAutoCloseTimer()
    {
        _autoCloseTimer.Stop();
        _isPausedByHover = false;
        _remainingSeconds = 0;
        _viewModel.UpdateCloseHint();
    }

    private void StopAutoCloseTimerWhileLoading()
    {
        _autoCloseTimer.Stop();
        _remainingSeconds = _settingsService.CurrentSettings?.AutoCloseSeconds ?? 0;
        _viewModel.UpdateCloseHint(_remainingSeconds, isPaused: false, isLoading: true);
    }

    private void AutoCloseTimer_Tick(object? sender, EventArgs e)
    {
        if (_isPausedByHover || !IsVisible)
        {
            return;
        }

        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _autoCloseTimer.Stop();
            HideWindow();
        }
        else
        {
            _viewModel.UpdateCloseHint(_remainingSeconds, isPaused: false);
        }
    }
}
