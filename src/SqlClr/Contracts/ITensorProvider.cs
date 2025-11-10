namespace SqlClrFunctions.Contracts
{
    /// <summary>
    /// Defines a contract for a service that can provide tensor data (weights)
    /// for a machine learning model. This allows the inference engine to be
    /// decoupled from the underlying storage mechanism.
    /// </summary>
    public interface ITensorProvider
    {
        /// <summary>
        /// Loads the raw binary data for a specific tensor by name.
        /// </summary>
        /// <param name="tensorName">The fully qualified name of the tensor (e.g., "transformer.layer.0.attention.qkv_proj.weight").</param>
        /// <param name="expectedSizeInBytes">The expected size of the tensor in bytes, for validation.</param>
        /// <returns>A byte array containing the tensor data, or null if not found.</returns>
        byte[] LoadWeights(string tensorName, int expectedSizeInBytes);
    }
}