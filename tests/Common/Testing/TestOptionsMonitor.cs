using System;
using Microsoft.Extensions.Options;

namespace Hartonomous.Testing.Common;

public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    private T _current;

    public TestOptionsMonitor(T value)
    {
        _current = value ?? throw new ArgumentNullException(nameof(value));
    }

    public T CurrentValue => _current;

    public T Get(string? name) => _current;

    public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

    public void Update(T value)
    {
        _current = value ?? throw new ArgumentNullException(nameof(value));
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose()
        {
        }
    }
}
