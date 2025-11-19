namespace Hartonomous.Clr.Enums
{
    /// <summary>
    /// Spatial indexing strategies with int backing for SQL Server CLR performance.
    /// Determines how high-dimensional data is linearized for spatial queries.
    /// </summary>
    public enum SpatialIndexStrategy : int
    {
        None = 0,
        RTree = 1,          // R-Tree spatial index (user's primary strategy)
        Hilbert3D = 2,      // Hilbert curve linearization
        Morton2D = 3,       // Z-order (Morton) curve 2D
        Morton3D = 4,       // Z-order (Morton) curve 3D
        KDTree = 5,         // k-d tree partitioning
        BallTree = 6        // Ball tree partitioning
    }
}
