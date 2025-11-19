using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Interface for space-filling curve implementations.
    /// Linearizes multi-dimensional data for cache-optimal access.
    /// </summary>
    public interface ISpaceFillingCurve
    {
        /// <summary>
        /// Gets the strategy type.
        /// </summary>
        SpatialIndexStrategy Strategy { get; }

        /// <summary>
        /// Encodes 3D coordinates to 1D linearized index.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <param name="maxCoord">Maximum coordinate value (defines grid size)</param>
        /// <returns>Linearized 1D index</returns>
        ulong Encode(int x, int y, int z, int maxCoord);

        /// <summary>
        /// Decodes 1D linearized index to 3D coordinates.
        /// </summary>
        /// <param name="index">Linearized 1D index</param>
        /// <param name="maxCoord">Maximum coordinate value</param>
        /// <returns>Tuple of (x, y, z) coordinates</returns>
        (int x, int y, int z) Decode(ulong index, int maxCoord);
    }
}
