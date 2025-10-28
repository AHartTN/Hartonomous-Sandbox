using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Utilities;

public static class GeometryConverter
{
    private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 0);

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
    
    public static int GetDimension(LineString? lineString)
    {
        return lineString?.NumPoints ?? 0;
    }
}
