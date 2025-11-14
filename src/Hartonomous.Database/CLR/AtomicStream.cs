using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr
{
    [Serializable]
    [SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1, IsByteOrdered = false)]
    public struct AtomicStream : INullable, IBinarySerialize
    {
        private const int SerializerVersion = 1;

        private bool _isNull;
        private Guid _streamId;
        private long _createdUtcTicks;
        private SqlString _scope;
        private SqlString _model;
        private SqlString _metadata;
        private List<Segment> _segments;

        internal IEnumerable<AtomicStreamSegmentSnapshot> EnumerateSegments()
        {
            if (_segments is null)
            {
                yield break;
            }

            for (int index = 0; index < _segments.Count; index++)
            {
                var segment = _segments[index];
                yield return new AtomicStreamSegmentSnapshot(
                    index,
                    segment.Kind,
                    segment.TimestampTicks,
                    segment.ContentType,
                    segment.Metadata,
                    segment.Payload);
            }
        }

        public bool IsNull => _isNull;

        public static AtomicStream Null => new AtomicStream { _isNull = true };

        public SqlGuid StreamId => _isNull || _streamId == Guid.Empty ? SqlGuid.Null : new SqlGuid(_streamId);

        public DateTime CreatedUtc => _isNull || _createdUtcTicks == 0 ? DateTime.MinValue : new DateTime(_createdUtcTicks, DateTimeKind.Utc);

        public SqlString Scope => _scope;

        public SqlString Model => _model;

        public SqlString Metadata => _metadata;

        public SqlInt32 SegmentCount => !_isNull && _segments != null ? new SqlInt32(_segments.Count) : SqlInt32.Zero;

        [SqlMethod(IsMutator = true, IsDeterministic = false, IsPrecise = false)]
        public void Initialize(SqlGuid streamId, DateTime createdUtc, SqlString scope, SqlString model, SqlString metadata)
        {
            if (streamId.IsNull || streamId.Value == Guid.Empty)
            {
                throw new ArgumentException("StreamId cannot be null or empty.", nameof(streamId));
            }

            _streamId = streamId.Value;
            _createdUtcTicks = EnsureUtc(createdUtc).Ticks;
            _scope = Normalize(scope);
            _model = Normalize(model);
            _metadata = metadata.IsNull ? SqlString.Null : metadata;
            _segments ??= new List<Segment>(8);
            _segments.Clear();
            _isNull = false;
        }

        [SqlMethod(IsDeterministic = false, IsPrecise = false)]
        public static AtomicStream Create(SqlGuid streamId, DateTime createdUtc, SqlString scope, SqlString model, SqlString metadata)
        {
            var stream = new AtomicStream();
            stream.Initialize(streamId, createdUtc, scope, model, metadata);
            return stream;
        }

        [SqlMethod(IsMutator = true, IsDeterministic = false, IsPrecise = false)]
        public void AddSegment(SqlString kind, DateTime timestampUtc, SqlString contentType, SqlString metadata, SqlBytes payload)
        {
            EnsureInitialized();

            var (buffer, length) = ExtractPayload(payload);
            var clone = length == 0 ? Array.Empty<byte>() : CloneBytes(buffer, length);

            var segment = new Segment(
                ParseSegmentKind(kind),
                EnsureUtc(timestampUtc).Ticks,
                contentType.IsNull ? null : contentType.Value,
                metadata.IsNull ? null : metadata.Value,
                clone);

            _segments.Add(segment);
        }

        [SqlMethod(IsDeterministic = false, IsPrecise = false)]
        public static AtomicStream AppendSegment(AtomicStream stream, SqlString kind, DateTime timestampUtc, SqlString contentType, SqlString metadata, SqlBytes payload)
        {
            stream.AddSegment(kind, timestampUtc, contentType, metadata, payload);
            return stream;
        }

        [SqlMethod(IsDeterministic = true, IsPrecise = true)]
        public SqlString GetSegmentKind(SqlInt32 ordinal)
        {
            var segment = RequireSegment(ordinal);
            return new SqlString(segment.Kind.ToString());
        }

        [SqlMethod(IsDeterministic = true, IsPrecise = true)]
        public DateTime GetSegmentTimestamp(SqlInt32 ordinal)
        {
            var segment = RequireSegment(ordinal);
            return new DateTime(segment.TimestampTicks, DateTimeKind.Utc);
        }

        [SqlMethod(IsDeterministic = true, IsPrecise = true)]
        public SqlString GetSegmentContentType(SqlInt32 ordinal)
        {
            var segment = RequireSegment(ordinal);
            return segment.ContentType is null ? SqlString.Null : new SqlString(segment.ContentType);
        }

        [SqlMethod(IsDeterministic = true, IsPrecise = true)]
        public SqlString GetSegmentMetadata(SqlInt32 ordinal)
        {
            var segment = RequireSegment(ordinal);
            return segment.Metadata is null ? SqlString.Null : new SqlString(segment.Metadata);
        }

        [SqlMethod(IsDeterministic = true, IsPrecise = false)]
        public SqlBytes GetSegmentPayload(SqlInt32 ordinal)
        {
            var segment = RequireSegment(ordinal);
            return SqlBytesInterop.CreateFromBytes(segment.Payload ?? Array.Empty<byte>());
        }

        public static AtomicStream Parse(SqlString input)
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

            var result = new AtomicStream();
            result.Read(reader);
            return result;
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
                throw new InvalidOperationException($"Unsupported AtomicStream version {version}.");
            }

            var streamIdBytes = reader.ReadBytes(16);
            if (streamIdBytes.Length != 16)
            {
                throw new EndOfStreamException("AtomicStream header truncated.");
            }

            _streamId = new Guid(streamIdBytes);
            _createdUtcTicks = reader.ReadInt64();
            _scope = ReadSqlString(reader);
            _model = ReadSqlString(reader);
            _metadata = ReadSqlString(reader);

            var count = reader.ReadInt32();
            if (count < 0)
            {
                throw new InvalidDataException("Segment count cannot be negative.");
            }

            _segments = new List<Segment>(count);
            for (int index = 0; index < count; index++)
            {
                var kind = (AtomicStreamSegmentKind)reader.ReadByte();
                var timestampTicks = reader.ReadInt64();
                var contentType = ReadOptionalString(reader);
                var metadata = ReadOptionalString(reader);
                var payloadLength = reader.ReadInt32();
                if (payloadLength < 0)
                {
                    payloadLength = 0;
                }

                var payload = payloadLength == 0 ? Array.Empty<byte>() : reader.ReadBytes(payloadLength);
                if (payload.Length != payloadLength)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading segment payload.");
                }

                _segments.Add(new Segment(kind, timestampTicks, contentType, metadata, payload));
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
            writer.Write(_streamId.ToByteArray());
            writer.Write(_createdUtcTicks);
            WriteSqlString(writer, _scope);
            WriteSqlString(writer, _model);
            WriteSqlString(writer, _metadata);

            var count = _segments?.Count ?? 0;
            writer.Write(count);

            if (count == 0)
            {
                return;
            }

            for (int index = 0; index < count; index++)
            {
                var segment = _segments[index];
                writer.Write((byte)segment.Kind);
                writer.Write(segment.TimestampTicks);
                WriteOptionalString(writer, segment.ContentType);
                WriteOptionalString(writer, segment.Metadata);

                var payload = segment.Payload ?? Array.Empty<byte>();
                writer.Write(payload.Length);
                writer.Write(payload);
            }
        }

        private void Reset()
        {
            _isNull = false;
            _streamId = Guid.Empty;
            _createdUtcTicks = 0;
            _scope = SqlString.Null;
            _model = SqlString.Null;
            _metadata = SqlString.Null;
            _segments = null;
        }

        private void EnsureInitialized()
        {
            if (_isNull || _streamId == Guid.Empty)
            {
                throw new InvalidOperationException("AtomicStream must be initialized before use.");
            }

            _segments ??= new List<Segment>(8);
        }

        private Segment RequireSegment(SqlInt32 ordinal)
        {
            if (ordinal.IsNull)
            {
                throw new ArgumentNullException(nameof(ordinal));
            }

            var index = ordinal.Value;
            if (_segments is null || index < 0 || index >= _segments.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal), "Segment ordinal is out of range.");
            }

            return _segments[index];
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => value
            };
        }

        private static SqlString Normalize(SqlString input)
        {
            if (input.IsNull)
            {
                return SqlString.Null;
            }

            var trimmed = input.Value.Trim();
            return trimmed.Length == 0 ? SqlString.Null : new SqlString(trimmed);
        }

        private static AtomicStreamSegmentKind ParseSegmentKind(SqlString value)
        {
            if (value.IsNull)
            {
                return AtomicStreamSegmentKind.Unknown;
            }

            var text = value.Value.Trim();
            if (text.Length == 0)
            {
                return AtomicStreamSegmentKind.Unknown;
            }

            if (Enum.TryParse(text, ignoreCase: true, out AtomicStreamSegmentKind parsed))
            {
                return parsed;
            }

            var normalized = text.ToLowerInvariant();
            return normalized switch
            {
                "prompt" => AtomicStreamSegmentKind.Input,
                "input" => AtomicStreamSegmentKind.Input,
                "completion" => AtomicStreamSegmentKind.Output,
                "output" => AtomicStreamSegmentKind.Output,
                "embedding" => AtomicStreamSegmentKind.Embedding,
                "vector" => AtomicStreamSegmentKind.Embedding,
                "moderation" => AtomicStreamSegmentKind.Moderation,
                "guard" => AtomicStreamSegmentKind.Moderation,
                "artifact" => AtomicStreamSegmentKind.Artifact,
                "attachment" => AtomicStreamSegmentKind.Artifact,
                "telemetry" => AtomicStreamSegmentKind.Telemetry,
                "metrics" => AtomicStreamSegmentKind.Telemetry,
                "control" => AtomicStreamSegmentKind.Control,
                _ => AtomicStreamSegmentKind.Unknown
            };
        }

        private static void WriteSqlString(BinaryWriter writer, SqlString value)
        {
            if (value.IsNull)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.Write(value.Value);
        }

        private static SqlString ReadSqlString(BinaryReader reader)
        {
            var hasValue = reader.ReadBoolean();
            return hasValue ? new SqlString(reader.ReadString()) : SqlString.Null;
        }

        private static void WriteOptionalString(BinaryWriter writer, string value)
        {
            var hasValue = !string.IsNullOrEmpty(value);
            writer.Write(hasValue);
            if (hasValue)
            {
                writer.Write(value);
            }
        }

        private static string ReadOptionalString(BinaryReader reader)
        {
            var hasValue = reader.ReadBoolean();
            return hasValue ? reader.ReadString() : null;
        }

        private static (byte[] Buffer, int Length) ExtractPayload(SqlBytes payload)
        {
            if (payload is null || payload.IsNull)
            {
                return (Array.Empty<byte>(), 0);
            }

            var buffer = SqlBytesInterop.GetBuffer(payload, out var length);
            return (buffer, length);
        }

        private static byte[] CloneBytes(byte[] buffer, int length)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            var clone = new byte[length];
            Buffer.BlockCopy(buffer, 0, clone, 0, length);
            return clone;
        }

        internal readonly struct AtomicStreamSegmentSnapshot
        {
            internal AtomicStreamSegmentSnapshot(
                int ordinal,
                AtomicStreamSegmentKind kind,
                long timestampTicks,
                string contentType,
                string metadata,
                byte[] payload)
            {
                Ordinal = ordinal;
                Kind = kind;
                TimestampTicks = timestampTicks;
                ContentType = contentType;
                Metadata = metadata;
                Payload = payload ?? Array.Empty<byte>();
            }

            internal int Ordinal { get; }

            internal AtomicStreamSegmentKind Kind { get; }

            internal long TimestampTicks { get; }

            internal string ContentType { get; }

            internal string Metadata { get; }

            internal byte[] Payload { get; }
        }

        private sealed class Segment
        {
            internal Segment(AtomicStreamSegmentKind kind, long timestampTicks, string contentType, string metadata, byte[] payload)
            {
                Kind = kind;
                TimestampTicks = timestampTicks;
                ContentType = contentType;
                Metadata = metadata;
                Payload = payload ?? Array.Empty<byte>();
            }

            internal AtomicStreamSegmentKind Kind { get; }

            internal long TimestampTicks { get; }

            internal string ContentType { get; }

            internal string Metadata { get; }

            internal byte[] Payload { get; }
        }
    }

    public enum AtomicStreamSegmentKind : byte
    {
        Unknown = 0,
        Input = 1,
        Output = 2,
        Embedding = 3,
        Moderation = 4,
        Artifact = 5,
        Telemetry = 6,
        Control = 7
    }
}
