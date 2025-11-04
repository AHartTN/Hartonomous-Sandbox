using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hartonomous.Core.Performance;

/// <summary>
/// High-performance memory pooling for vectors and buffers.
/// Thread-safe, GC-friendly, with automatic cleanup.
/// </summary>
public static class MemoryPool
{
    private static readonly ArrayPool<float> FloatPool = ArrayPool<float>.Shared;
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    private static readonly ArrayPool<int> IntPool = ArrayPool<int>.Shared;
    private static readonly ArrayPool<double> DoublePool = ArrayPool<double>.Shared;

    #region Float Vector Pooling

    /// <summary>
    /// Rent a float array from the pool.
    /// MUST call Return() when done to avoid leaks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] RentFloatArray(int minimumLength)
    {
        return FloatPool.Rent(minimumLength);
    }

    /// <summary>
    /// Return a float array to the pool.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnFloatArray(float[] array, bool clearArray = false)
    {
        FloatPool.Return(array, clearArray);
    }

    /// <summary>
    /// Rent array and copy data. Caller must return.
    /// </summary>
    public static float[] RentAndCopy(ReadOnlySpan<float> source)
    {
        var array = FloatPool.Rent(source.Length);
        source.CopyTo(array);
        return array;
    }

    /// <summary>
    /// Disposable wrapper for automatic return to pool.
    /// Usage: using var buffer = MemoryPool.RentDisposable(dimension);
    /// </summary>
    public static RentedFloatArray RentDisposable(int minimumLength)
    {
        return new RentedFloatArray(minimumLength);
    }

    #endregion

    #region Byte Pooling

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] RentByteArray(int minimumLength)
    {
        return BytePool.Rent(minimumLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnByteArray(byte[] array, bool clearArray = false)
    {
        BytePool.Return(array, clearArray);
    }

    public static RentedByteArray RentDisposableBytes(int minimumLength)
    {
        return new RentedByteArray(minimumLength);
    }

    #endregion

    #region Int Pooling

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int[] RentIntArray(int minimumLength)
    {
        return IntPool.Rent(minimumLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnIntArray(int[] array, bool clearArray = false)
    {
        IntPool.Return(array, clearArray);
    }

    #endregion

    #region Double Pooling

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double[] RentDoubleArray(int minimumLength)
    {
        return DoublePool.Rent(minimumLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnDoubleArray(double[] array, bool clearArray = false)
    {
        DoublePool.Return(array, clearArray);
    }

    #endregion
}

/// <summary>
/// RAII wrapper for pooled float arrays.
/// Automatically returns to pool on disposal.
/// </summary>
public ref struct RentedFloatArray
{
    private float[] _array;
    private readonly int _length;
    private bool _disposed;

    internal RentedFloatArray(int minimumLength)
    {
        _array = MemoryPool.RentFloatArray(minimumLength);
        _length = minimumLength;
        _disposed = false;
    }

    public Span<float> Span => _array.AsSpan(0, _length);
    public Memory<float> Memory => _array.AsMemory(0, _length);
    public int Length => _length;

    public void Dispose()
    {
        if (!_disposed && _array != null)
        {
            MemoryPool.ReturnFloatArray(_array);
            _array = null!;
            _disposed = true;
        }
    }
}

/// <summary>
/// RAII wrapper for pooled byte arrays.
/// </summary>
public ref struct RentedByteArray
{
    private byte[] _array;
    private readonly int _length;
    private bool _disposed;

    internal RentedByteArray(int minimumLength)
    {
        _array = MemoryPool.RentByteArray(minimumLength);
        _length = minimumLength;
        _disposed = false;
    }

    public Span<byte> Span => _array.AsSpan(0, _length);
    public Memory<byte> Memory => _array.AsMemory(0, _length);
    public int Length => _length;

    public void Dispose()
    {
        if (!_disposed && _array != null)
        {
            MemoryPool.ReturnByteArray(_array);
            _array = null!;
            _disposed = true;
        }
    }
}

/// <summary>
/// String builder using pooled char arrays.
/// Much faster than StringBuilder for known-size strings.
/// </summary>
public ref struct PooledStringBuilder
{
    private char[] _buffer;
    private int _position;
    private bool _disposed;

    public PooledStringBuilder(int initialCapacity = 256)
    {
        _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
        _position = 0;
        _disposed = false;
    }

    public void Append(char c)
    {
        EnsureCapacity(_position + 1);
        _buffer[_position++] = c;
    }

    public void Append(ReadOnlySpan<char> text)
    {
        EnsureCapacity(_position + text.Length);
        text.CopyTo(_buffer.AsSpan(_position));
        _position += text.Length;
    }

    public void Append(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Append(text.AsSpan());
    }

    public void AppendLine()
    {
        Append(Environment.NewLine);
    }

    public void Clear()
    {
        _position = 0;
    }

    public override string ToString()
    {
        return new string(_buffer, 0, _position);
    }

    public ReadOnlySpan<char> AsSpan() => _buffer.AsSpan(0, _position);

    private void EnsureCapacity(int required)
    {
        if (_buffer.Length >= required) return;

        int newSize = Math.Max(_buffer.Length * 2, required);
        var newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        if (!_disposed && _buffer != null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null!;
            _disposed = true;
        }
    }
}
