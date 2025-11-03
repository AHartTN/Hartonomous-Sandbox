using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    public static class AtomicStreamFunctions
    {
        [SqlFunction(
            DataAccess = DataAccessKind.None,
            FillRowMethodName = nameof(FillSegmentRow),
            IsDeterministic = true,
            IsPrecise = false,
            TableDefinition = "segment_ordinal INT, segment_kind NVARCHAR(32), timestamp_utc DATETIME2(3), content_type NVARCHAR(128), metadata NVARCHAR(MAX), payload VARBINARY(MAX)")]
        public static IEnumerable EnumerateSegments(AtomicStream stream)
        {
            if (stream.IsNull)
            {
                yield break;
            }

            foreach (var snapshot in stream.EnumerateSegments())
            {
                yield return new SegmentRow(snapshot);
            }
        }

        public static void FillSegmentRow(object rowObject, out int ordinal, out string kind, out DateTime timestampUtc, out string contentType, out string metadata, out SqlBytes payload)
        {
            var row = (SegmentRow)rowObject;
            ordinal = row.Ordinal;
            kind = row.Kind;
            timestampUtc = row.TimestampUtc;
            contentType = row.ContentType;
            metadata = row.Metadata;
            payload = SqlBytesInterop.CreateFromBytes(row.Payload);
        }

        private sealed class SegmentRow
        {
            internal SegmentRow(AtomicStream.AtomicStreamSegmentSnapshot snapshot)
            {
                Ordinal = snapshot.Ordinal;
                Kind = snapshot.Kind.ToString();
                TimestampUtc = new DateTime(snapshot.TimestampTicks, DateTimeKind.Utc);
                ContentType = snapshot.ContentType;
                Metadata = snapshot.Metadata;
                Payload = snapshot.Payload ?? Array.Empty<byte>();
            }

            internal int Ordinal { get; }

            internal string Kind { get; }

            internal DateTime TimestampUtc { get; }

            internal string ContentType { get; }

            internal string Metadata { get; }

            internal byte[] Payload { get; }
        }
    }
}
