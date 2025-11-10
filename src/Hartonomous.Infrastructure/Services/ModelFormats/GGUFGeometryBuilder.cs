using NetTopologySuite.Geometries;
using Hartonomous.Infrastructure.Services.ModelFormats;

namespace Hartonomous.Infrastructure.Services.ModelFormats;

/// <summary>
/// Handles creation of NetTopologySuite geometries from GGUF tensor data.
/// Creates LINESTRING geometries for tensor weight previews with spatial indexing.
/// </summary>
public class GGUFGeometryBuilder
{
    private readonly NetTopologySuite.Geometries.GeometryFactory _geometryFactory;

    public GGUFGeometryBuilder()
    {
        _geometryFactory = new NetTopologySuite.Geometries.GeometryFactory();
    }

    /// <summary>
    /// Creates a LINESTRING geometry from tensor weight preview data.
    /// X = index, Y = weight value, Z = importance (gradient magnitude), M = temporal metadata.
    /// </summary>
    public LineString? CreateTensorGeometry(GGUFTensorInfo tensorInfo)
    {
        // For now, create a simple geometry footprint without actual weight data
        // In a full implementation, this would include dequantized weight previews
        if (tensorInfo.ElementCount == 0)
            return null;

        // Create a bounding box geometry representing the tensor
        // X: [0, elementCount], Y: [-1, 1] (normalized range), Z: [0, 1] (importance), M: [0, 1] (temporal)
        var coordinates = new CoordinateZM[]
        {
            new CoordinateZM(0, -1, 0, 0), // Start point
            new CoordinateZM(tensorInfo.ElementCount - 1, 1, 1, 1) // End point
        };

        return _geometryFactory.CreateLineString(coordinates);
    }

    /// <summary>
    /// Creates a MULTILINESTRING geometry for multiple tensor segments.
    /// </summary>
    public MultiLineString? CreateMultiTensorGeometry(IEnumerable<GGUFTensorInfo> tensorInfos)
    {
        var lineStrings = tensorInfos
            .Select(CreateTensorGeometry)
            .Where(ls => ls != null)
            .Cast<LineString>()
            .ToArray();

        if (lineStrings.Length == 0)
            return null;

        return _geometryFactory.CreateMultiLineString(lineStrings);
    }

    /// <summary>
    /// Creates a geometry representing the spatial bounds of a tensor.
    /// </summary>
    public Polygon? CreateTensorBounds(GGUFTensorInfo tensorInfo)
    {
        if (tensorInfo.ElementCount == 0)
            return null;

        // Create a simple bounding box polygon
        var coordinates = new Coordinate[]
        {
            new Coordinate(0, -1),
            new Coordinate(tensorInfo.ElementCount - 1, -1),
            new Coordinate(tensorInfo.ElementCount - 1, 1),
            new Coordinate(0, 1),
            new Coordinate(0, -1) // Close the polygon
        };

        var shell = _geometryFactory.CreateLinearRing(coordinates);
        return _geometryFactory.CreatePolygon(shell);
    }
}
