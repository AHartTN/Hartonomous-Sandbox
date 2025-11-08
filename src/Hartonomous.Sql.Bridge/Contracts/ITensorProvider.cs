using System;
using System.Collections.Generic;

namespace Hartonomous.Sql.Bridge.Contracts
{
    /// <summary>
    /// Interface for accessing TensorAtoms data from SQL Server.
    /// Implemented by SQL CLR layer using context connection.
    /// </summary>
    public interface ITensorProvider
    {
        /// <summary>
        /// Load tensor weights from TensorAtoms.WeightsGeometry.
        /// Pattern: Query GEOMETRY column, extract via STPointN(i).STY.Value
        /// </summary>
        /// <param name="tensorNamePattern">LIKE pattern for TensorAtoms.TensorName</param>
        /// <param name="maxElements">Maximum number of elements to load</param>
        /// <returns>Float array of weight values</returns>
        float[] LoadWeights(string tensorNamePattern, int maxElements);

        /// <summary>
        /// Load multiple tensors in a single batch query.
        /// More efficient than multiple LoadWeights calls.
        /// </summary>
        /// <param name="tensorPatterns">Dictionary of name → pattern mappings</param>
        /// <returns>Dictionary of name → weights mappings</returns>
        Dictionary<string, float[]> LoadWeightsBatch(Dictionary<string, string> tensorPatterns);

        /// <summary>
        /// Get tensor metadata without loading weights.
        /// </summary>
        /// <param name="tensorNamePattern">LIKE pattern for TensorAtoms.TensorName</param>
        /// <returns>Tensor shape, data type, element count</returns>
        TensorMetadata GetMetadata(string tensorNamePattern);
    }

    /// <summary>
    /// Metadata about a tensor in TensorAtoms table.
    /// </summary>
    public class TensorMetadata
    {
        public string TensorName { get; set; } = string.Empty;
        public string TensorShape { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public long ElementCount { get; set; }
        public long ByteSize { get; set; }
    }
}
