using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace QuickTranslate.Infrastructure;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly BlockingCollection<string> _logQueue = new(new ConcurrentQueue<string>(), 10000);
    private readonly Thread _writeThread;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public FileLoggerProvider(string logFilePath)
    {
        _logFilePath = logFilePath;

        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writeThread = new Thread(ProcessLogQueue)
        {
            IsBackground = true,
            Name = "FileLoggerThread"
        };
        _writeThread.Start();

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        Dispose();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    internal void Enqueue(string message)
    {
        if (!_cts.IsCancellationRequested)
        {
            try
            {
                _logQueue.TryAdd(message);
            }
            catch (InvalidOperationException)
            {
                // Queue completed
            }
        }
    }

    private void ProcessLogQueue()
    {
        var batch = new List<string>(64);
        var messageCountSinceRotationCheck = 0;

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                if (_logQueue.TryTake(out var message, 1000, _cts.Token))
                {
                    batch.Clear();
                    batch.Add(message);

                    while (batch.Count < 50 && _logQueue.TryTake(out var nextMsg))
                    {
                        batch.Add(nextMsg);
                    }

                    messageCountSinceRotationCheck += batch.Count;
                    if (messageCountSinceRotationCheck >= 50)
                    {
                        messageCountSinceRotationCheck = 0;
                        CheckFileRotation();
                    }

                    File.AppendAllLines(_logFilePath, batch, Encoding.UTF8);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Never crash the application because of logging failures
            }
        }

        // Flush remaining messages
        try
        {
            batch.Clear();
            while (_logQueue.TryTake(out var message))
            {
                batch.Add(message);
            }
            if (batch.Count > 0)
            {
                File.AppendAllLines(_logFilePath, batch, Encoding.UTF8);
            }
        }
        catch
        {
            // Ignore on shutdown
        }
    }

    private void CheckFileRotation()
    {
        try
        {
            var fileInfo = new FileInfo(_logFilePath);
            if (fileInfo.Exists && fileInfo.Length > 5 * 1024 * 1024)
            {
                var backupPath = _logFilePath + ".old";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(_logFilePath, backupPath);
            }
        }
        catch
        {
            // Ignore rotation errors
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        _cts.Cancel();
        _logQueue.CompleteAdding();
        try
        {
            _writeThread.Join(2000);
        }
        catch
        {
            // ignored
        }
        _cts.Dispose();
        _logQueue.Dispose();
    }
}

public class FileLogger : ILogger
{
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = logLevel.ToString().ToUpperInvariant();
        var logLine = $"[{timestamp}] [{level}] [{_categoryName}] {message}";

        if (exception != null)
        {
            logLine += Environment.NewLine + exception;
        }

        _provider.Enqueue(logLine);
    }
}

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath)
    {
        builder.AddProvider(new FileLoggerProvider(filePath));
        return builder;
    }
}
