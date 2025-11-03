using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions;

[Serializable]
[SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1, IsByteOrdered = false)]
public struct ComponentStream : INullable, IBinarySerialize
{
    private const int SerializerVersion = 1;
    private const int MaxHashLength = 64;

    private bool _isNull;
    private List<ComponentRun> _runs;
    private long _totalCount;

    public bool IsNull => _isNull;

    public static ComponentStream Null => new() { _isNull = true };

    public SqlInt32 RunCount => !_isNull && _runs is { Count: > 0 } ? new SqlInt32(_runs.Count) : SqlInt32.Zero;

    public SqlInt64 TotalComponents => !_isNull ? new SqlInt64(_totalCount) : SqlInt64.Zero;

    [SqlMethod(IsMutator = true, IsDeterministic = false, IsPrecise = true)]
    public ComponentStream Initialize()
    {
        _runs ??= new List<ComponentRun>(16);
        _runs.Clear();
        _totalCount = 0;
        _isNull = false;
        return this;
    }

    [SqlMethod(IsDeterministic = false, IsPrecise = true, IsMutator = true)]
    public ComponentStream Append(SqlBytes componentHash, SqlInt32 repetitions)
    {
        EnsureWritable();

        var hash = ExtractHash(componentHash);
        var count = ValidateRepetitions(repetitions);

        AppendRun(hash, count);
        return this;
    }

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public SqlBytes GetComponentHash(SqlInt32 ordinal)
    {
        var run = RequireRun(ordinal);
        return new SqlBytes((byte[])run.Hash.Clone());
    }

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public SqlInt32 GetRepetitionCount(SqlInt32 ordinal)
    {
        var run = RequireRun(ordinal);
        return new SqlInt32(run.Count);
    }

    public override string ToString()
    {
        if (_isNull)
        {
            return "NULL";
        }

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            Write(writer);
        }

        return Convert.ToBase64String(stream.ToArray());
    }

    public static ComponentStream Parse(SqlString input)
    {
        if (input.IsNull)
        {
            return Null;
        }

        var trimmed = input.Value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || string.Equals(trimmed, "NULL", StringComparison.OrdinalIgnoreCase))
        {
            return Null;
        }

        var buffer = Convert.FromBase64String(trimmed);
        using var stream = new MemoryStream(buffer, writable: false);
        using var reader = new BinaryReader(stream);

        var result = new ComponentStream();
        result.Read(reader);
        return result;
    }

    public void Read(BinaryReader reader)
    {
        Reset();

        _isNull = reader.ReadBoolean();
        if (_isNull)
        {
            return;
        }

        var version = reader.ReadInt32();
        if (version != SerializerVersion)
        {
            throw new InvalidOperationException($"Unsupported ComponentStream version {version}.");
        }

        var count = reader.ReadInt32();
        if (count < 0)
        {
            throw new InvalidDataException("Run count cannot be negative.");
        }

        _runs = new List<ComponentRun>(count);
        _totalCount = 0;

        for (var index = 0; index < count; index++)
        {
            var hashLength = reader.ReadInt32();
            if (hashLength <= 0 || hashLength > MaxHashLength)
            {
                throw new InvalidDataException("Component hash length is out of range.");
            }

            var hash = reader.ReadBytes(hashLength);
            if (hash.Length != hashLength)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading component hash.");
            }

            var repetitions = reader.ReadInt32();
            if (repetitions <= 0)
            {
                throw new InvalidDataException("Run length must be greater than zero.");
            }

            _runs.Add(new ComponentRun(hash, repetitions));
            _totalCount += repetitions;
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(_isNull);
        if (_isNull)
        {
            return;
        }

        writer.Write(SerializerVersion);
        var count = _runs?.Count ?? 0;
        writer.Write(count);

        if (count == 0)
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            var run = _runs[index];
            writer.Write(run.Hash.Length);
            writer.Write(run.Hash);
            writer.Write(run.Count);
        }
    }

    internal IEnumerable<ComponentRunSnapshot> EnumerateRuns()
    {
        if (_runs is null)
        {
            yield break;
        }

        foreach (var run in _runs)
        {
            yield return new ComponentRunSnapshot(run.Hash, run.Count);
        }
    }

    internal static ComponentStream FromRuns(IEnumerable<ComponentRunSnapshot> runs)
    {
        var stream = new ComponentStream();
        stream.Initialize();

        foreach (var run in runs)
        {
            if (run.Count <= 0)
            {
                continue;
            }

            stream.AppendRun(CloneHash(run.Hash), run.Count);
        }

        return stream;
    }

    private void Reset()
    {
        _isNull = false;
        _totalCount = 0;
        _runs = null;
    }

    private void EnsureWritable()
    {
        if (_isNull)
        {
            Initialize();
            return;
        }

        _runs ??= new List<ComponentRun>(16);
    }

    private ComponentRun RequireRun(SqlInt32 ordinal)
    {
        if (ordinal.IsNull)
        {
            throw new ArgumentNullException(nameof(ordinal));
        }

        if (_runs is null || ordinal.Value < 0 || ordinal.Value >= _runs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Run ordinal is out of range.");
        }

        return _runs[ordinal.Value];
    }

    private void AppendRun(byte[] hash, int count)
    {
        if (_runs.Count > 0)
        {
            var last = _runs[_runs.Count - 1];
            if (HashesEqual(last.Hash, hash))
            {
                _runs[_runs.Count - 1] = new ComponentRun(last.Hash, checked(last.Count + count));
                _totalCount += count;
                return;
            }
        }

        _runs.Add(new ComponentRun(hash, count));
        _totalCount += count;
    }

    private static byte[] ExtractHash(SqlBytes componentHash)
    {
        if (componentHash is null || componentHash.IsNull)
        {
            throw new ArgumentNullException(nameof(componentHash), "Component hash cannot be null.");
        }

        var buffer = componentHash.Value;
        if (buffer.Length == 0 || buffer.Length > MaxHashLength)
        {
            throw new ArgumentOutOfRangeException(nameof(componentHash), $"Component hash length must be between 1 and {MaxHashLength} bytes.");
        }

        return CloneHash(buffer);
    }

    private static int ValidateRepetitions(SqlInt32 repetitions)
    {
        if (repetitions.IsNull)
        {
            throw new ArgumentNullException(nameof(repetitions));
        }

        if (repetitions.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions), "Repetition count must be positive.");
        }

        return repetitions.Value;
    }

    private static bool HashesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] CloneHash(byte[] hash)
    {
        var clone = new byte[hash.Length];
        Buffer.BlockCopy(hash, 0, clone, 0, hash.Length);
        return clone;
    }

    internal readonly struct ComponentRunSnapshot
    {
        internal ComponentRunSnapshot(byte[] hash, int count)
        {
            Hash = hash ?? Array.Empty<byte>();
            Count = count;
        }

        internal byte[] Hash { get; }

        internal int Count { get; }
    }

    private readonly struct ComponentRun
    {
        internal ComponentRun(byte[] hash, int count)
        {
            Hash = hash ?? Array.Empty<byte>();
            Count = count;
        }

        internal byte[] Hash { get; }

        internal int Count { get; }
    }
}
