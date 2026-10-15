using System.Runtime.InteropServices;
using System.Windows;
using IDataObject = System.Windows.IDataObject;
using DataObject = System.Windows.DataObject;
using Clipboard = System.Windows.Clipboard;
using TextDataFormat = System.Windows.TextDataFormat;
using DataFormats = System.Windows.DataFormats;
using Microsoft.Extensions.Logging;

namespace QuickTranslate.Services;

public class ClipboardService : IClipboardService
{
    private readonly ILogger<ClipboardService> _logger;

    public ClipboardService(ILogger<ClipboardService> logger)
    {
        _logger = logger;
    }

    #region Win32 API
    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult);

    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint SMTO_NORMAL = 0x0000;

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    // Explicit 40-byte layout matching sizeof(tagINPUT) on 64-bit Windows
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public uint type;

        [FieldOffset(8)]
        public KEYBDINPUT ki;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint WM_COPY = 0x0301;

    private const byte VK_SHIFT = 0x10;
    private const byte VK_LSHIFT = 0xA0;
    private const byte VK_RSHIFT = 0xA1;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_MENU = 0x12; // Alt
    private const byte VK_LWIN = 0x5B;
    private const byte VK_RWIN = 0x5C;
    private const byte VK_C = 0x43;
    #endregion

    public async Task<string?> GetSelectedTextAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteOnStaThreadAsync(async () =>
        {
            var initialSeq = GetClipboardSequenceNumber();
            string? initialText = null;

            // 1. Capture current clipboard state snapshot
            IDataObject? originalClipboardData = null;
            try
            {
                (originalClipboardData, initialText) = SafeBackupClipboard();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to backup clipboard data: {Message}", ex.Message);
            }

            // 2. Try fast direct WM_COPY to focused control first if available
            TrySendWmCopy();

            var currentSeq = GetClipboardSequenceNumber();
            var seqChanged = (currentSeq != initialSeq);

            // 3. If WM_COPY didn't trigger clipboard update, use keyboard simulation
            if (!seqChanged)
            {
                await SendCopyKeystrokesAsync();

                // Wait for clipboard sequence number to change
                const int watchTimeoutMs = 180;
                const int pollIntervalMs = 10;
                var elapsed = 0;

                while (elapsed < watchTimeoutMs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await Task.Delay(pollIntervalMs, cancellationToken);
                    elapsed += pollIntervalMs;

                    currentSeq = GetClipboardSequenceNumber();
                    if (currentSeq != initialSeq)
                    {
                        seqChanged = true;
                        break;
                    }
                }
            }

            // 4. Retrieve copied text
            string? copiedText = null;
            try
            {
                copiedText = SafeGetClipboardText();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to retrieve text from clipboard: {Message}", ex.Message);
            }

            // Fallback: if sequence number didn't change but text is new
            if (!seqChanged && !string.IsNullOrEmpty(copiedText) && copiedText != initialText)
            {
                seqChanged = true;
            }

            if (!seqChanged || string.IsNullOrWhiteSpace(copiedText))
            {
                _logger.LogInformation("No text was copied to clipboard from foreground application");
                return null;
            }

            var postCopySeq = GetClipboardSequenceNumber();

            // 5. Restore previous clipboard content if no new external change occurred in between
            try
            {
                currentSeq = GetClipboardSequenceNumber();
                if (currentSeq == postCopySeq && originalClipboardData != null)
                {
                    SafeSetClipboardDataObject(originalClipboardData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to restore original clipboard: {Message}", ex.Message);
            }

            return copiedText;
        });
    }

    public async Task SetTextAsync(string text)
    {
        if (text == null) return;

        await ExecuteOnStaThreadAsync(async () =>
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetText(text, TextDataFormat.UnicodeText);
                    return;
                }
                catch (COMException)
                {
                    if (i < 4)
                    {
                        await Task.Delay(20);
                    }
                }
            }
        });
    }

    private static void TrySendWmCopy()
    {
        try
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return;

            var fgThread = GetWindowThreadProcessId(fg, out _);
            var currentThread = GetCurrentThreadId();

            if (fgThread != 0 && fgThread != currentThread)
            {
                if (AttachThreadInput(currentThread, fgThread, true))
                {
                    try
                    {
                        var focused = GetFocus();
                        if (focused != IntPtr.Zero)
                        {
                            SendMessageTimeout(focused, WM_COPY, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG | SMTO_NORMAL, 20, out _);
                        }
                    }
                    finally
                    {
                        AttachThreadInput(currentThread, fgThread, false);
                    }
                }
            }
        }
        catch
        {
            // Ignore WM_COPY errors and fall back to keyboard simulation
        }
    }

    private static async Task SendCopyKeystrokesAsync()
    {
        // 1. Brief pause for hotkey release / input queue to settle
        await Task.Delay(15);

        // 2. Release interfering modifier keys (Shift, Alt, Win) and press Ctrl+C down
        var downInputs = new List<INPUT>(8);

        // Explicitly release interfering modifier keys
        AddKeyInput(downInputs, VK_SHIFT, isDown: false);
        AddKeyInput(downInputs, VK_LSHIFT, isDown: false);
        AddKeyInput(downInputs, VK_RSHIFT, isDown: false);
        AddKeyInput(downInputs, VK_MENU, isDown: false);
        AddKeyInput(downInputs, VK_LWIN, isDown: false);
        AddKeyInput(downInputs, VK_RWIN, isDown: false);

        // Press Ctrl down, then C down
        AddKeyInput(downInputs, VK_CONTROL, isDown: true);
        AddKeyInput(downInputs, VK_C, isDown: true);

        SendInput((uint)downInputs.Count, downInputs.ToArray(), Marshal.SizeOf<INPUT>());

        // 3. Small hold time between keydown and keyup for message loop processing
        await Task.Delay(10);

        // 4. Release C and Ctrl
        var upInputs = new INPUT[]
        {
            CreateKeyInput(VK_C, isDown: false),
            CreateKeyInput(VK_CONTROL, isDown: false)
        };

        SendInput((uint)upInputs.Length, upInputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT CreateKeyInput(byte vk, bool isDown)
    {
        var scan = (ushort)MapVirtualKey(vk, 0);
        var flags = isDown ? 0 : KEYEVENTF_KEYUP;

        return new INPUT
        {
            type = INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = scan,
                dwFlags = (uint)flags,
                time = 0,
                dwExtraInfo = UIntPtr.Zero
            }
        };
    }

    private static void AddKeyInput(List<INPUT> list, byte vk, bool isDown)
    {
        list.Add(CreateKeyInput(vk, isDown));
    }

    private static (IDataObject? Data, string? Text) SafeBackupClipboard()
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                var original = Clipboard.GetDataObject();
                if (original == null) return (null, null);

                string? initialText = null;
                if (original.GetDataPresent(DataFormats.UnicodeText, autoConvert: false))
                {
                    initialText = original.GetData(DataFormats.UnicodeText, autoConvert: false) as string;
                }
                else if (original.GetDataPresent(DataFormats.Text, autoConvert: false))
                {
                    initialText = original.GetData(DataFormats.Text, autoConvert: false) as string;
                }

                var clone = new DataObject();
                var addedAny = false;
                foreach (var format in original.GetFormats(autoConvert: false))
                {
                    // Skip heavy graphic/multimedia formats to avoid copying massive bitmaps into managed memory
                    if (IsHeavyFormat(format))
                    {
                        continue;
                    }

                    try
                    {
                        var data = original.GetData(format, autoConvert: false);
                        if (data != null)
                        {
                            clone.SetData(format, data);
                            addedAny = true;
                        }
                    }
                    catch
                    {
                        // Ignore individual formats that fail to read
                    }
                }
                return (addedAny ? clone : null, initialText);
            }
            catch (COMException)
            {
                Thread.Sleep(10);
            }
        }
        return (null, null);
    }

    private static string? SafeGetClipboardText()
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    return Clipboard.GetText(TextDataFormat.UnicodeText);
                }
                if (Clipboard.ContainsText(TextDataFormat.Text))
                {
                    return Clipboard.GetText(TextDataFormat.Text);
                }
                return null;
            }
            catch (COMException)
            {
                Thread.Sleep(10);
            }
        }
        return null;
    }

    private static void SafeSetClipboardDataObject(IDataObject data)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                Clipboard.SetDataObject(data, copy: true);
                return;
            }
            catch (COMException)
            {
                Thread.Sleep(10);
            }
        }
    }

    private static bool IsHeavyFormat(string format)
    {
        return format.Equals(DataFormats.Bitmap, StringComparison.OrdinalIgnoreCase)
            || format.Equals(DataFormats.Dib, StringComparison.OrdinalIgnoreCase)
            || format.Equals(DataFormats.EnhancedMetafile, StringComparison.OrdinalIgnoreCase)
            || format.Equals(DataFormats.WaveAudio, StringComparison.OrdinalIgnoreCase)
            || format.Equals("DeviceIndependentBitmap", StringComparison.OrdinalIgnoreCase)
            || format.Equals("PNG", StringComparison.OrdinalIgnoreCase)
            || format.Equals("Format17", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<T> ExecuteOnStaThreadAsync<T>(Func<Task<T>> func)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.HasShutdownStarted && dispatcher.Thread.IsAlive)
        {
            if (dispatcher.CheckAccess())
            {
                return await func();
            }
            return await dispatcher.InvokeAsync(func).Task.Unwrap();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var task = func();
                tcs.SetResult(task.GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return await tcs.Task;
    }

    private static async Task ExecuteOnStaThreadAsync(Func<Task> func)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.HasShutdownStarted && dispatcher.Thread.IsAlive)
        {
            if (dispatcher.CheckAccess())
            {
                await func();
                return;
            }
            await dispatcher.InvokeAsync(func).Task.Unwrap();
            return;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var task = func();
                task.GetAwaiter().GetResult();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await tcs.Task;
    }
}
