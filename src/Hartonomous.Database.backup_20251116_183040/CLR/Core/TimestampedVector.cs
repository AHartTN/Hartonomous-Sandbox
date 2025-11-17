using System;

namespace Hartonomous.Clr.Core;

/// <summary>
/// Lightweight struct representing a timestamped embedding vector for SQL CLR aggregates.
/// </summary>
internal struct TimestampedVector
{
    internal TimestampedVector(DateTime timestamp, float[] vector)
    {
        Timestamp = timestamp;
        Vector = vector;
    }

    internal DateTime Timestamp;
    internal float[] Vector;
}
