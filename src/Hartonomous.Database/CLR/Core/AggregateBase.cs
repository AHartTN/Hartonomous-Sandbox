using System;
using System.IO;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Base infrastructure for vector aggregates.
    /// Provides common patterns for dimension validation and null handling.
    /// </summary>
    internal static class AggregateHelpers
    {
        /// <summary>
        /// Validate and set dimension from a vector.
        /// Returns true if vector is valid and dimension matches or is unset.
        /// </summary>
        public static bool ValidateAndSetDimension(float[] vector, ref int dimension)
        {
            if (vector == null || vector.Length == 0)
                return false;

            if (dimension == 0)
            {
                dimension = vector.Length;
                return true;
            }

            return vector.Length == dimension;
        }

        /// <summary>
        /// Check if dimension is compatible with existing dimension.
        /// </summary>
        public static bool IsCompatibleDimension(int vectorLength, int existingDimension)
        {
            if (existingDimension == 0)
                return vectorLength > 0;
            
            return vectorLength == existingDimension;
        }

        /// <summary>
        /// Serialize a nullable float array with dimension check.
        /// </summary>
        public static void WriteFloatArrayWithDimension(BinaryWriter writer, float[]? array, int expectedDimension)
        {
            if (array == null || array.Length != expectedDimension)
            {
                writer.WriteFloatArray(null);
                return;
            }

            writer.WriteFloatArray(array);
        }

        /// <summary>
        /// Deserialize a float array and validate dimension.
        /// </summary>
        public static float[]? ReadFloatArrayWithDimension(BinaryReader reader, int expectedDimension)
        {
            var array = reader.ReadFloatArray();
            
            if (array != null && array.Length != expectedDimension)
                return null; // Dimension mismatch
            
            return array;
        }

        /// <summary>
        /// Merge two aggregates by checking if the other aggregate has data.
        /// </summary>
        public static bool ShouldMerge(int otherCount, int otherDimension, ref int thisDimension)
        {
            if (otherCount == 0)
                return false;

            if (thisDimension == 0)
            {
                thisDimension = otherDimension;
                return true;
            }

            return thisDimension == otherDimension;
        }

        /// <summary>
        /// Common pattern: accumulate vector with dimension validation.
        /// </summary>
        public static float[]? AccumulateVector(string vectorJson, ref int dimension)
        {
            var vec = VectorUtilities.ParseVectorJson(vectorJson);
            if (vec == null)
                return null;

            if (!ValidateAndSetDimension(vec, ref dimension))
                return null;

            return vec;
        }
    }
}
