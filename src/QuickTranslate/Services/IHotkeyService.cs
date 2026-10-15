using QuickTranslate.Models;

namespace QuickTranslate.Services;

public interface IHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Register(HotkeyConfig config, out string? errorMessage);
    void Unregister();
    bool IsRegistered { get; }
    HotkeyConfig? CurrentConfig { get; }
}
