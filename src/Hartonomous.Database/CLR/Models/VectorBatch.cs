namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Batch of vectors for efficient processing.
    /// Used in aggregates and batch inference operations.
    /// </summary>
    public struct VectorBatch
    {
        /// <summary>
        /// Array of embedding vectors (each vector is float[])
        /// </summary>
        public float[][] Vectors { get; set; }

        /// <summary>
        /// Optional labels/identifiers for each vector
        /// </summary>
        public string[] Labels { get; set; }

        /// <summary>
        /// Number of vectors in batch
        /// </summary>
        public int Count => Vectors?.Length ?? 0;

        /// <summary>
        /// Dimensionality of vectors (assumes all same dimension)
        /// </summary>
        public int Dimension => (Vectors != null && Vectors.Length > 0) ? Vectors[0].Length : 0;

        public VectorBatch(float[][] vectors, string[]? labels = null)
        {
            Vectors = vectors;
            Labels = labels ?? System.Array.Empty<string>();
        }
    }
}
