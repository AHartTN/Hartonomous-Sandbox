using NetTopologySuite.Geometries;
using System.IO.Compression;

namespace Hartonomous.Core.Utilities;

public static class GeometryConverter
{
    private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 0);
    
    // Maximum points per LINESTRING segment to avoid serialization issues
    // Based on empirical testing: ~100K points = ~1.6MB WKB (safe for MemoryStream)
    private const int MAX_POINTS_PER_SEGMENT = 100_000;

    /// <summary>
    /// Convert weight array to 2D LINESTRING where X=index, Y=weight.
    /// Simple and efficient - no wasted Z/M coordinates until needed for training.
    /// </summary>
    public static LineString ToLineString(float[] weights, int srid = 0)
    {
        if (weights == null || weights.Length == 0)
            throw new ArgumentException("Weights array cannot be null or empty", nameof(weights));

        var coordinates = new Coordinate[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            coordinates[i] = new Coordinate(i, weights[i]);
        }

        var factory = srid != 0 ? new GeometryFactory(new PrecisionModel(), srid) : _geometryFactory;
        return factory.CreateLineString(coordinates);
    }

    private static LineString CreateSingleLineString(float[] weights, int srid)
    {
        var coordinates = new Coordinate[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            coordinates[i] = new Coordinate(i, weights[i]);
        }

        var factory = srid != 0 ? new GeometryFactory(new PrecisionModel(), srid) : _geometryFactory;
        return factory.CreateLineString(coordinates);
    }

    private static MultiLineString CreateChunkedMultiLineString(float[] weights, int srid)
    {
        var factory = srid != 0 ? new GeometryFactory(new PrecisionModel(), srid) : _geometryFactory;
        var numChunks = (int)Math.Ceiling((double)weights.Length / MAX_POINTS_PER_SEGMENT);
        var lineStrings = new LineString[numChunks];

        for (int chunk = 0; chunk < numChunks; chunk++)
        {
            int startIdx = chunk * MAX_POINTS_PER_SEGMENT;
            int endIdx = Math.Min(startIdx + MAX_POINTS_PER_SEGMENT, weights.Length);
            int chunkSize = endIdx - startIdx;

            var coordinates = new Coordinate[chunkSize];
            for (int i = 0; i < chunkSize; i++)
            {
                coordinates[i] = new Coordinate(startIdx + i, weights[startIdx + i]);
            }

            lineStrings[chunk] = factory.CreateLineString(coordinates);
        }

        return factory.CreateMultiLineString(lineStrings);
    }

    /// <summary>
    /// Convert weight array to LINESTRING with Z/M coordinates for training data.
    /// Use only when you have actual importance/iteration values to store.
    /// </summary>
    public static LineString ToLineStringZM(
        float[] weights,
        float[] importance,
        int[] iterations,
        int srid = 0)
    {
        if (weights == null || weights.Length == 0)
            throw new ArgumentException("Weights array cannot be null or empty", nameof(weights));

        if (importance.Length != weights.Length)
            throw new ArgumentException("Importance array must match weights length", nameof(importance));
        if (iterations.Length != weights.Length)
            throw new ArgumentException("Iterations array must match weights length", nameof(iterations));

        var coordinates = new CoordinateZM[weights.Length];

        for (int i = 0; i < weights.Length; i++)
        {
            coordinates[i] = new CoordinateZM(i, weights[i], importance[i], iterations[i]);
        }

        var factory = srid != 0 ? new GeometryFactory(new PrecisionModel(), srid) : _geometryFactory;
        return factory.CreateLineString(coordinates);
    }

    public static float[] FromLineString(LineString lineString)
    {
        if (lineString == null)
            throw new ArgumentNullException(nameof(lineString));

        var coordinates = lineString.Coordinates;
        var weights = new float[coordinates.Length];

        for (int i = 0; i < coordinates.Length; i++)
        {
            weights[i] = (float)coordinates[i].Y;
        }

        return weights;
    }

    /// <summary>
    /// Extract weights from Geometry (handles both LineString and MultiLineString).
    /// </summary>
    public static float[] FromGeometry(Geometry geometry)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        if (geometry is LineString lineString)
        {
            return FromLineString(lineString);
        }
        else if (geometry is MultiLineString multiLineString)
        {
            return FromMultiLineString(multiLineString);
        }
        else
        {
            throw new ArgumentException($"Unsupported geometry type: {geometry.GeometryType}", nameof(geometry));
        }
    }

    /// <summary>
    /// Extract weights from MultiLineString (reconstructs from chunks).
    /// </summary>
    public static float[] FromMultiLineString(MultiLineString multiLineString)
    {
        if (multiLineString == null)
            throw new ArgumentNullException(nameof(multiLineString));

        // Calculate total points across all segments
        int totalPoints = 0;
        for (int i = 0; i < multiLineString.NumGeometries; i++)
        {
            totalPoints += multiLineString.GetGeometryN(i).NumPoints;
        }

        var weights = new float[totalPoints];
        int offset = 0;

        // Reconstruct array from chunks
        for (int i = 0; i < multiLineString.NumGeometries; i++)
        {
            var lineString = (LineString)multiLineString.GetGeometryN(i);
            var coordinates = lineString.Coordinates;

            for (int j = 0; j < coordinates.Length; j++)
            {
                weights[offset + j] = (float)coordinates[j].Y;
            }

            offset += coordinates.Length;
        }

        return weights;
    }

    public static int GetDimension(Geometry? geometry)
    {
        return geometry?.NumPoints ?? 0;
    }

    /// <summary>
    /// Compress float array to binary format for storage as VARBINARY(MAX).
    /// Use for very large tensors where spatial indexing isn't needed.
    /// </summary>
    public static byte[] CompressToBinary(float[] weights)
    {
        if (weights == null || weights.Length == 0)
            throw new ArgumentException("Weights array cannot be null or empty", nameof(weights));

        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Fastest))
        using (var writer = new BinaryWriter(gzipStream))
        {
            writer.Write(weights.Length);
            foreach (var weight in weights)
            {
                writer.Write(weight);
            }
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decompress binary format back to float array.
    /// </summary>
    public static float[] DecompressFromBinary(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            throw new ArgumentException("Compressed data cannot be null or empty", nameof(compressedData));

        using var memoryStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzipStream);

        int length = reader.ReadInt32();
        var weights = new float[length];

        for (int i = 0; i < length; i++)
        {
            weights[i] = reader.ReadSingle();
        }

        return weights;
    }
}
