using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QuickTranslate.Infrastructure;

public static class MemoryOptimizer
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    /// <summary>
    /// Safely trims the working set of the current process, returning unreferenced pages to the OS.
    /// This mimics standard Windows behavior when a window is minimized to the taskbar.
    /// </summary>
    public static void TrimWorkingSet()
    {
        try
        {
            var handle = Process.GetCurrentProcess().Handle;
            SetProcessWorkingSetSize(handle, (IntPtr)(-1), (IntPtr)(-1));
        }
        catch
        {
            // Ignore trimming errors in restricted environments
        }
    }
}
