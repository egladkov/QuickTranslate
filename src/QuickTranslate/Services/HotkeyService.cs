using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public class HotkeyService : IHotkeyService
{
    private const int HotkeyId = 9001;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNorepeat = 0x4000;
    private const int ErrorHotkeyAlreadyRegistered = 1409;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly ILogger<HotkeyService> _logger;
    private HwndSource? _hwndSource;
    private bool _isDisposed;

    public event EventHandler? HotkeyPressed;
    public bool IsRegistered { get; private set; }
    public HotkeyConfig? CurrentConfig { get; private set; }

    public HotkeyService(ILogger<HotkeyService> logger)
    {
        _logger = logger;
        InitializeMessageWindow();
    }

    private void InitializeMessageWindow()
    {
        var parameters = new HwndSourceParameters("QuickTranslate_HotkeySink")
        {
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(HwndHook);
    }

    public bool Register(HotkeyConfig config, out string? errorMessage)
    {
        errorMessage = null;
        Unregister();

        if (config == null || config.Key == Key.None)
        {
            errorMessage = "Клавиша для сочетания не выбрана.";
            return false;
        }

        if (_hwndSource == null)
        {
            errorMessage = "Не удалось инициализировать окно обработки сообщений.";
            return false;
        }

        uint modifiers = 0;
        if (config.IsAlt) modifiers |= ModAlt;
        if (config.IsControl) modifiers |= ModControl;
        if (config.IsShift) modifiers |= ModShift;
        if (config.IsWin) modifiers |= ModWin;
        modifiers |= ModNorepeat;

        var vk = (uint)KeyInterop.VirtualKeyFromKey(config.Key);
        if (vk == 0)
        {
            errorMessage = $"Недопустимая клавиша: {config.Key}.";
            return false;
        }

        var success = RegisterHotKey(_hwndSource.Handle, HotkeyId, modifiers, vk);
        if (!success)
        {
            var win32Error = Marshal.GetLastWin32Error();
            if (win32Error == ErrorHotkeyAlreadyRegistered)
            {
                errorMessage = $"Сочетание клавиш '{config}' уже используется другим приложением.";
            }
            else
            {
                errorMessage = $"Не удалось зарегистрировать горячую клавишу '{config}' (Win32 Error: {win32Error}).";
            }

            _logger.LogWarning("Failed to register hotkey {Hotkey}: {Error}", config, errorMessage);
            return false;
        }

        IsRegistered = true;
        CurrentConfig = config.Clone();
        _logger.LogInformation("Successfully registered global hotkey: {Hotkey}", config);
        return true;
    }

    public void Unregister()
    {
        if (IsRegistered && _hwndSource != null && !_hwndSource.IsDisposed)
        {
            UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            IsRegistered = false;
            _logger.LogInformation("Unregistered global hotkey");
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            try
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling HotkeyPressed event");
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Unregister();

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource.Dispose();
            _hwndSource = null;
        }

        GC.SuppressFinalize(this);
    }
}
