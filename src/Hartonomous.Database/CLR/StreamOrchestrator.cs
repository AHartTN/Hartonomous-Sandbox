using System;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;

namespace Hartonomous.SqlClr
{
    /// <summary>
    /// Real-Time Stream Orchestrator: Time-windowed sensor fusion aggregate
    /// Accumulates atoms into ComponentStream UDT with run-length encoding
    /// Used for Event atom generation from time-bucketed sensor data
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        MaxByteSize = -1, // Use max size for large streams
        IsInvariantToDuplicates = false,
        IsInvariantToNulls = true,
        IsInvariantToOrder = false, // Order matters for time-based bucketing
        IsNullIfEmpty = true
    )]
    public struct clr_StreamOrchestrator : IBinarySerialize
    {
        // Internal state
        private MemoryStream _componentBuffer;
        private BinaryWriter _writer;
        private int _componentCount;
        private long _lastAtomId;
        private int _runLength;
        private DateTime _windowStart;
        private DateTime _windowEnd;
        
        // Constants
        private const int MaxComponentsPerStream = 100000;
        private const int RunLengthThreshold = 3; // Compress runs of 3+ identical atoms

        /// <summary>
        /// Initialize the aggregate state
        /// </summary>
        public void Init()
        {
            _componentBuffer = new MemoryStream();
            _writer = new BinaryWriter(_componentBuffer, Encoding.UTF8);
            _componentCount = 0;
            _lastAtomId = -1;
            _runLength = 0;
            _windowStart = DateTime.MaxValue;
            _windowEnd = DateTime.MinValue;
        }

        /// <summary>
        /// Accumulate a single atom into the stream
        /// </summary>
        /// <param name="atomId">Atom identifier</param>
        /// <param name="timestamp">Event timestamp for time-bucketing</param>
        /// <param name="weight">Component weight (0.0-1.0)</param>
        public void Accumulate(SqlInt64 atomId, SqlDateTime timestamp, SqlDouble weight)
        {
            if (atomId.IsNull || timestamp.IsNull)
                return;

            long currentAtomId = atomId.Value;
            DateTime currentTimestamp = timestamp.Value;
            double currentWeight = weight.IsNull ? 1.0 : weight.Value;

            // Update time window bounds
            if (currentTimestamp < _windowStart)
                _windowStart = currentTimestamp;
            if (currentTimestamp > _windowEnd)
                _windowEnd = currentTimestamp;

            // Run-length encoding: compress consecutive identical atoms
            if (currentAtomId == _lastAtomId)
            {
                _runLength++;
                return; // Don't write yet, accumulate run
            }

            // Flush previous run if it exists
            if (_lastAtomId != -1)
            {
                WriteComponent(_lastAtomId, _runLength, currentWeight);
            }

            // Start new run
            _lastAtomId = currentAtomId;
            _runLength = 1;

            // Safety limit: prevent unbounded memory growth
            if (_componentCount >= MaxComponentsPerStream)
            {
                throw new InvalidOperationException(
                    $"ComponentStream exceeded maximum size ({MaxComponentsPerStream} components). " +
                    "Consider narrower time windows or higher aggregation level."
                );
            }
        }

        /// <summary>
        /// Merge two partial aggregates (for parallel execution)
        /// </summary>
        public void Merge(clr_StreamOrchestrator other)
        {
            if (other._componentBuffer == null || other._componentBuffer.Length == 0)
                return;

            // Flush current run before merging
            if (_lastAtomId != -1)
            {
                WriteComponent(_lastAtomId, _runLength, 1.0);
                _lastAtomId = -1;
                _runLength = 0;
            }

            // Merge time windows
            if (other._windowStart < _windowStart)
                _windowStart = other._windowStart;
            if (other._windowEnd > _windowEnd)
                _windowEnd = other._windowEnd;

            // Append other's buffer
            byte[] otherData = other._componentBuffer.ToArray();
            _componentBuffer.Write(otherData, 0, otherData.Length);
            _componentCount += other._componentCount;
        }

        /// <summary>
        /// Finalize and return the ComponentStream UDT as binary
        /// </summary>
        public SqlBytes Terminate()
        {
            // Flush final run
            if (_lastAtomId != -1)
            {
                WriteComponent(_lastAtomId, _runLength, 1.0);
            }

            if (_componentBuffer == null || _componentCount == 0)
                return SqlBytes.Null;

            // Build ComponentStream header
            using (var finalStream = new MemoryStream())
            using (var finalWriter = new BinaryWriter(finalStream, Encoding.UTF8))
            {
                // Header: version, component count, time window
                finalWriter.Write((byte)1); // ComponentStream version
                finalWriter.Write(_componentCount);
                finalWriter.Write(_windowStart.ToBinary());
                finalWriter.Write(_windowEnd.ToBinary());

                // Copy accumulated components
                byte[] components = _componentBuffer.ToArray();
                finalWriter.Write(components.Length);
                finalWriter.Write(components);

                return new SqlBytes(finalStream.ToArray());
            }
        }

        /// <summary>
        /// Write a component to the buffer with run-length encoding
        /// </summary>
        private void WriteComponent(long atomId, int runLength, double weight)
        {
            if (runLength >= RunLengthThreshold)
            {
                // Compressed format: [flag=1][atomId][runLength][weight]
                _writer.Write((byte)1); // Compression flag
                _writer.Write(atomId);
                _writer.Write(runLength);
                _writer.Write(weight);
                _componentCount++; // One compressed component
            }
            else
            {
                // Uncompressed format: [flag=0][atomId][weight] repeated runLength times
                for (int i = 0; i < runLength; i++)
                {
                    _writer.Write((byte)0); // No compression
                    _writer.Write(atomId);
                    _writer.Write(weight);
                    _componentCount++;
                }
            }
        }

        #region IBinarySerialize Implementation

        public void Read(BinaryReader r)
        {
            int bufferLength = r.ReadInt32();
            if (bufferLength > 0)
            {
                _componentBuffer = new MemoryStream(r.ReadBytes(bufferLength));
                _writer = new BinaryWriter(_componentBuffer, Encoding.UTF8);
                _componentBuffer.Seek(0, SeekOrigin.End); // Position at end for appending
            }
            else
            {
                _componentBuffer = new MemoryStream();
                _writer = new BinaryWriter(_componentBuffer, Encoding.UTF8);
            }

            _componentCount = r.ReadInt32();
            _lastAtomId = r.ReadInt64();
            _runLength = r.ReadInt32();
            _windowStart = DateTime.FromBinary(r.ReadInt64());
            _windowEnd = DateTime.FromBinary(r.ReadInt64());
        }

        public void Write(BinaryWriter w)
        {
            byte[] buffer = _componentBuffer?.ToArray() ?? Array.Empty<byte>();
            w.Write(buffer.Length);
            if (buffer.Length > 0)
                w.Write(buffer);

            w.Write(_componentCount);
            w.Write(_lastAtomId);
            w.Write(_runLength);
            w.Write(_windowStart.ToBinary());
            w.Write(_windowEnd.ToBinary());
        }

        #endregion
    }

    /// <summary>
    /// Helper functions for ComponentStream manipulation
    /// </summary>
    public static class ComponentStreamHelpers
    {
        /// <summary>
        /// Extract component count from ComponentStream binary
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlInt32 fn_GetComponentCount(SqlBytes componentStream)
        {
            if (componentStream.IsNull || componentStream.Length == 0)
                return SqlInt32.Null;

            using (var stream = new MemoryStream(componentStream.Value))
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadByte(); // Skip version
                return new SqlInt32(reader.ReadInt32());
            }
        }

        /// <summary>
        /// Extract time window from ComponentStream binary
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlString fn_GetTimeWindow(SqlBytes componentStream)
        {
            if (componentStream.IsNull || componentStream.Length == 0)
                return SqlString.Null;

            using (var stream = new MemoryStream(componentStream.Value))
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadByte(); // Skip version
                reader.ReadInt32(); // Skip component count
                DateTime start = DateTime.FromBinary(reader.ReadInt64());
                DateTime end = DateTime.FromBinary(reader.ReadInt64());

                return new SqlString($"{start:O} - {end:O}");
            }
        }

        /// <summary>
        /// Decompress ComponentStream into individual atom IDs
        /// Table-valued function for downstream queries
        /// </summary>
        [SqlFunction(
            FillRowMethodName = "FillComponentRow",
            TableDefinition = "AtomId BIGINT, Weight FLOAT",
            IsDeterministic = true,
            IsPrecise = true
        )]
        public static System.Collections.IEnumerable fn_DecompressComponents(SqlBytes componentStream)
        {
            if (componentStream.IsNull || componentStream.Length == 0)
                yield break;

            using (var stream = new MemoryStream(componentStream.Value))
            using (var reader = new BinaryReader(stream))
            {
                // Read header
                reader.ReadByte(); // version
                int componentCount = reader.ReadInt32();
                reader.ReadInt64(); // windowStart
                reader.ReadInt64(); // windowEnd
                int componentsLength = reader.ReadInt32();

                // Read components
                long bytesRead = 0;
                while (bytesRead < componentsLength)
                {
                    byte flag = reader.ReadByte();
                    bytesRead++;

                    if (flag == 1) // Compressed
                    {
                        long atomId = reader.ReadInt64();
                        int runLength = reader.ReadInt32();
                        double weight = reader.ReadDouble();
                        bytesRead += 8 + 4 + 8;

                        for (int i = 0; i < runLength; i++)
                        {
                            yield return new ComponentRow { AtomId = atomId, Weight = weight };
                        }
                    }
                    else // Uncompressed
                    {
                        long atomId = reader.ReadInt64();
                        double weight = reader.ReadDouble();
                        bytesRead += 8 + 8;

                        yield return new ComponentRow { AtomId = atomId, Weight = weight };
                    }
                }
            }
        }

        public static void FillComponentRow(object obj, out SqlInt64 atomId, out SqlDouble weight)
        {
            var row = (ComponentRow)obj;
            atomId = new SqlInt64(row.AtomId);
            weight = new SqlDouble(row.Weight);
        }

        private class ComponentRow
        {
            public long AtomId { get; set; }
            public double Weight { get; set; }
        }
    }
}
