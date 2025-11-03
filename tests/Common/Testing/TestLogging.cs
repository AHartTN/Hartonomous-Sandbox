using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Testing.Common;

public sealed class TestLogger<T> : ILogger<T>
{
    private readonly bool _enabled;

    public TestLogger(bool enabled = true)
    {
        _enabled = enabled;
    }

    public List<LogEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => _enabled;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!_enabled)
        {
            return;
        }

        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception, eventId));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose()
        {
        }
    }
}

public readonly record struct LogEntry(LogLevel Level, string Message, Exception? Exception, EventId EventId);

public static class TestLogger
{
    public static TestLogger<T> Create<T>() => new();

    public static ILogger<T> Silent<T>() => new TestLogger<T>(enabled: false);
}
