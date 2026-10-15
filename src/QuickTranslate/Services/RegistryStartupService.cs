using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace QuickTranslate.Services;

public class RegistryStartupService : IStartupService
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "QuickTranslate";
    private readonly ILogger<RegistryStartupService> _logger;

    public RegistryStartupService(ILogger<RegistryStartupService> logger)
    {
        _logger = logger;
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read autostart registry key");
            return false;
        }
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key == null)
            {
                _logger.LogWarning("Unable to open Run registry key for writing");
                return;
            }

            if (enable)
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath))
                {
                    var quotedPath = $"\"{processPath}\"";
                    key.SetValue(AppName, quotedPath);
                    _logger.LogInformation("Autostart enabled for path {Path}", quotedPath);
                }
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                    _logger.LogInformation("Autostart disabled");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update autostart setting to {Enable}", enable);
            throw;
        }
    }
}
