using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    /// <summary>
    /// Vector operations for AI inference
    /// These functions work with vectors stored as VARBINARY until VECTOR type is fully enabled
    /// </summary>
    public class VectorOperations
    {
        /// <summary>
        /// Compute dot product of two vectors
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorDotProduct(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            double result = 0;
            for (int i = 0; i < length1; i++)
            {
                result += values1[i] * values2[i];
            }

            return new SqlDouble(result);
        }

        /// <summary>
        /// Compute cosine similarity between two vectors
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorCosineSimilarity(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            double dotProduct = 0;
            double norm1 = 0;
            double norm2 = 0;
            for (int i = 0; i < length1; i++)
            {
                var left = values1[i];
                var right = values2[i];
                dotProduct += left * right;
                norm1 += left * left;
                norm2 += right * right;
            }

            if (norm1 == 0 || norm2 == 0)
                return new SqlDouble(0);

            return new SqlDouble(dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2)));
        }

        /// <summary>
        /// Compute Euclidean distance between two vectors
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorEuclideanDistance(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            double sumSquares = 0;
            for (int i = 0; i < length1; i++)
            {
                double diff = values1[i] - values2[i];
                sumSquares += diff * diff;
            }

            return new SqlDouble(Math.Sqrt(sumSquares));
        }

        /// <summary>
        /// Add two vectors element-wise
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorAdd(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlBytes.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            var result = new float[length1];
            for (int i = 0; i < length1; i++)
            {
                result[i] = values1[i] + values2[i];
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Subtract two vectors element-wise
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorSubtract(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlBytes.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            var result = new float[length1];
            for (int i = 0; i < length1; i++)
            {
                result[i] = values1[i] - values2[i];
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Multiply vector by scalar
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorScale(SqlBytes vector, SqlDouble scalar)
        {
            if (vector.IsNull || scalar.IsNull)
                return SqlBytes.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);
            float s = (float)scalar.Value;

            var result = new float[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = values[i] * s;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Compute L2 norm (magnitude) of vector
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorNorm(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlDouble.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);

            double sumSquares = 0;
            for (int i = 0; i < length; i++)
            {
                double value = values[i];
                sumSquares += value * value;
            }

            return new SqlDouble(Math.Sqrt(sumSquares));
        }

        /// <summary>
        /// Normalize vector to unit length
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorNormalize(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlBytes.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);

            double sumSquares = 0;
            for (int i = 0; i < length; i++)
            {
                double value = values[i];
                sumSquares += value * value;
            }

            var norm = Math.Sqrt(sumSquares);

            if (norm == 0)
            {
                return vector;
            }

            var scale = (float)(1.0 / norm);
            var result = new float[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = values[i] * scale;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Linear interpolation between two vectors
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorLerp(SqlBytes vector1, SqlBytes vector2, SqlDouble t)
        {
            if (vector1.IsNull || vector2.IsNull || t.IsNull)
                return SqlBytes.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);
            float tVal = (float)t.Value;

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            float complement = 1 - tVal;
            var result = new float[length1];
            for (int i = 0; i < length1; i++)
            {
                result[i] = values1[i] * complement + values2[i] * tVal;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        // Helper methods to convert between byte arrays and float arrays
        /// <summary>
        /// Apply numerically stable softmax to an input vector and return the probability vector.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorSoftmax(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlBytes.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);
            if (length == 0)
                return new SqlBytes(Array.Empty<byte>());

            double max = double.NegativeInfinity;
            for (int i = 0; i < length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            double sum = 0;
            var result = new float[length];
            for (int i = 0; i < length; i++)
            {
                double exp = Math.Exp(values[i] - max);
                result[i] = (float)exp;
                sum += exp;
            }

            if (sum == 0)
            {
                return SqlBytesInterop.CreateFromFloats(result);
            }

            float invSum = (float)(1.0 / sum);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] *= invSum;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Return the index of the largest value in the vector (0-based). Returns NULL when the vector is empty.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlInt32 VectorArgMax(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlInt32.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);
            if (length == 0)
                return SqlInt32.Null;

            int index = 0;
            float max = values[0];
            for (int i = 1; i < length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                    index = i;
                }
            }

            return new SqlInt32(index);
        }
    }
}
