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
    public ComponentStream Append(SqlInt64 atomId, SqlInt32 repetitions)
    {
        EnsureWritable();

        var id = ValidateAtomId(atomId);
        var count = ValidateRepetitions(repetitions);

        AppendRun(id, count);
        return this;
    }

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public SqlInt64 GetComponentAtomId(SqlInt32 ordinal)
    {
        var run = RequireRun(ordinal);
        return new SqlInt64(run.AtomId);
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

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public SqlBytes Write()
    {
        if (_isNull)
        {
            return SqlBytes.Null;
        }

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            Write(writer);
        }

        return new SqlBytes(buffer.ToArray());
    }

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public byte[] ToByteArray()
    {
        var sqlBytes = Write();
        return sqlBytes.IsNull ? Array.Empty<byte>() : sqlBytes.Value;
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
            var atomId = reader.ReadInt64();
            if (atomId <= 0)
            {
                throw new InvalidDataException("Atom identifier must be a positive value.");
            }

            var repetitions = reader.ReadInt32();
            if (repetitions <= 0)
            {
                throw new InvalidDataException("Run length must be greater than zero.");
            }

            _runs.Add(new ComponentRun(atomId, repetitions));
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
            writer.Write(run.AtomId);
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
            yield return new ComponentRunSnapshot(run.AtomId, run.Count);
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

            stream.AppendRun(run.AtomId, run.Count);
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

    private void AppendRun(long atomId, int count)
    {
        if (_runs.Count > 0)
        {
            var last = _runs[_runs.Count - 1];
            if (last.AtomId == atomId)
            {
                _runs[_runs.Count - 1] = new ComponentRun(last.AtomId, checked(last.Count + count));
                _totalCount += count;
                return;
            }
        }

        _runs.Add(new ComponentRun(atomId, count));
        _totalCount += count;
    }

    private static long ValidateAtomId(SqlInt64 atomId)
    {
        if (atomId.IsNull)
        {
            throw new ArgumentNullException(nameof(atomId), "Atom identifier cannot be null.");
        }

        if (atomId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(atomId), "Atom identifier must be positive.");
        }

        return atomId.Value;
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

    internal readonly struct ComponentRunSnapshot
    {
        internal ComponentRunSnapshot(long atomId, int count)
        {
            AtomId = atomId;
            Count = count;
        }

        internal long AtomId { get; }

        internal int Count { get; }
    }

    private readonly struct ComponentRun
    {
        internal ComponentRun(long atomId, int count)
        {
            AtomId = atomId;
            Count = count;
        }

        internal long AtomId { get; }

        internal int Count { get; }
    }
}
