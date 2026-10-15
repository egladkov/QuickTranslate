using System.Threading;
using Microsoft.Extensions.Logging;

namespace QuickTranslate.Infrastructure;

public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = @"Global\QuickTranslate_SingleInstance_Mutex";
    private const string EventName = @"Global\QuickTranslate_WakeEvent";

    private readonly ILogger<SingleInstanceManager> _logger;
    private Mutex? _mutex;
    private EventWaitHandle? _wakeEvent;
    private RegisteredWaitHandle? _registeredWaitHandle;
    private bool _hasOwnership;

    public event Action? WakeRequested;

    public SingleInstanceManager(ILogger<SingleInstanceManager> logger)
    {
        _logger = logger;
    }

    public bool TryAcquireOwnership()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out _hasOwnership);
            if (!_hasOwnership)
            {
                // Another instance is already running
                SignalExistingInstance();
                return false;
            }

            // Setup wake event to listen for secondary instances
            _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                _wakeEvent,
                (state, timedOut) =>
                {
                    if (!timedOut)
                    {
                        WakeRequested?.Invoke();
                    }
                },
                null,
                -1,
                false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check or acquire single instance mutex");
            // If mutex creation fails, allow running as fallback
            return true;
        }
    }

    private void SignalExistingInstance()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(EventName, out var existingEvent))
            {
                existingEvent.Set();
                existingEvent.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to signal existing instance: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        _registeredWaitHandle?.Unregister(null);
        _wakeEvent?.Dispose();

        if (_hasOwnership && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // ignored
            }
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
