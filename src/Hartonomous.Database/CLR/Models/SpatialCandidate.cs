namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Spatial candidate from R-Tree query (Stage 1 of O(log N) + O(K) pattern).
    /// Represents a point that passed spatial filter, needs exact vector refinement.
    /// </summary>
    public struct SpatialCandidate
    {
        /// <summary>
        /// Database identifier (AtomEmbeddingId, TensorAtomId, etc.)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Spatial distance from query geometry (Stage 1 score)
        /// </summary>
        public double SpatialDistance { get; set; }

        /// <summary>
        /// Optional: Exact vector distance (Stage 2 score, computed after retrieval)
        /// </summary>
        public double? VectorDistance { get; set; }

        /// <summary>
        /// 3D spatial coordinates (from LandmarkProjection)
        /// </summary>
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        /// <summary>
        /// Optional: Full embedding vector (if retrieved for refinement)
        /// </summary>
        public float[]? Embedding { get; set; }

        public SpatialCandidate(long id, double spatialDistance, float x, float y, float z)
        {
            Id = id;
            SpatialDistance = spatialDistance;
            VectorDistance = null;
            X = x;
            Y = y;
            Z = z;
            Embedding = null;
        }
    }
}
