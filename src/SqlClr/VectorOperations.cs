using System;
using System.Data.SqlTypes;
using System.Numerics;
using Microsoft.SqlServer.Server;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// Vector operations for AI inference
    /// These functions work with vectors stored as VARBINARY until VECTOR type is fully enabled
    /// </summary>
    public class VectorOperations
    {
        /// <summary>
        /// Compute dot product of two vectors, accelerated with SIMD.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorDotProduct(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            // Delegate to the centralized, optimized math function
            return new SqlDouble(VectorMath.DotProduct(values1, values2));
        }

        /// <summary>
        /// Compute cosine similarity between two vectors, accelerated with SIMD.
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

            float dotProduct = 0;
            float norm1 = 0;
            float norm2 = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;
            int length = values1.Length;

            // Process vectors in SIMD chunks
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(values1, i);
                var v2 = new Vector<float>(values2, i);
                dotProduct += Vector.Dot(v1, v2);
                norm1 += Vector.Dot(v1, v1);
                norm2 += Vector.Dot(v2, v2);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                var val1 = values1[i];
                var val2 = values2[i];
                dotProduct += val1 * val2;
                norm1 += val1 * val1;
                norm2 += val2 * val2;
            }

            if (norm1 == 0 || norm2 == 0)
                return new SqlDouble(0);

            return new SqlDouble(dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2)));
        }

        /// <summary>
        /// Compute Euclidean distance between two vectors, accelerated with SIMD.
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

            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;
            int length = values1.Length;

            // Process vectors in SIMD chunks
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(values1, i);
                var v2 = new Vector<float>(values2, i);
                var diff = v1 - v2;
                sumSquares += Vector.Dot(diff, diff);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                float diff = values1[i] - values2[i];
                sumSquares += diff * diff;
            }

            return new SqlDouble(Math.Sqrt(sumSquares));
        }

        /// <summary>
        /// Add two vectors element-wise, accelerated with SIMD.
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
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Process vectors in SIMD chunks
            for (; i <= length1 - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(values1, i);
                var v2 = new Vector<float>(values2, i);
                (v1 + v2).CopyTo(result, i);
            }

            // Process remaining elements
            for (; i < length1; i++)
            {
                result[i] = values1[i] + values2[i];
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Subtract two vectors element-wise, accelerated with SIMD.
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
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Process vectors in SIMD chunks
            for (; i <= length1 - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(values1, i);
                var v2 = new Vector<float>(values2, i);
                (v1 - v2).CopyTo(result, i);
            }

            // Process remaining elements
            for (; i < length1; i++)
            {
                result[i] = values1[i] - values2[i];
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Multiply vector by scalar, accelerated with SIMD.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorScale(SqlBytes vector, SqlDouble scalar)
        {
            if (vector.IsNull || scalar.IsNull)
                return SqlBytes.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);
            float s = (float)scalar.Value;

            var result = new float[length];
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Process vectors in SIMD chunks
            var scalarVector = new Vector<float>(s);
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(values, i);
                (v * scalarVector).CopyTo(result, i);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                result[i] = values[i] * s;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Compute L2 norm (magnitude) of vector, accelerated with SIMD.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorNorm(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlDouble.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);

            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Process vectors in SIMD chunks
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(values, i);
                sumSquares += Vector.Dot(v, v);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                float value = values[i];
                sumSquares += value * value;
            }

            return new SqlDouble(Math.Sqrt(sumSquares));
        }

        /// <summary>
        /// Normalize vector to unit length, accelerated with SIMD.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorNormalize(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlBytes.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);

            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Process vectors in SIMD chunks to get sum of squares
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(values, i);
                sumSquares += Vector.Dot(v, v);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                float value = values[i];
                sumSquares += value * value;
            }

            var norm = Math.Sqrt(sumSquares);

            if (norm == 0)
            {
                return vector; // Cannot normalize a zero-length vector
            }

            var scale = (float)(1.0 / norm);
            var result = new float[length];
            i = 0;

            // Process scaling in SIMD chunks
            var scaleVector = new Vector<float>(scale);
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(values, i);
                (v * scaleVector).CopyTo(result, i);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                result[i] = values[i] * scale;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Linear interpolation between two vectors, accelerated with SIMD.
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

            var result = new float[length1];
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Process vectors in SIMD chunks
            float complement = 1 - tVal;
            var tVector = new Vector<float>(tVal);
            var complementVector = new Vector<float>(complement);

            for (; i <= length1 - vectorSize; i += vectorSize)
            {
                var v1 = new Vector<float>(values1, i);
                var v2 = new Vector<float>(values2, i);
                (v1 * complementVector + v2 * tVector).CopyTo(result, i);
            }

            // Process remaining elements
            for (; i < length1; i++)
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
