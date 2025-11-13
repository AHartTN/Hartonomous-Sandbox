using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// Vector operations for AI inference
    /// These functions work with vectors stored as VARBINARY until VECTOR type is fully enabled
    /// SQL CLR does not support SIMD, so all operations use simple float[] loops
    /// </summary>
    public class VectorOperations
    {
        /// <summary>
        /// Compute dot product of two vectors.
        /// </summary>
        [SqlFunction(Name = "clr_VectorDotProduct", IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorDotProduct(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            // Use GPU accelerator with automatic CPU fallback
            return new SqlDouble(GpuAccelerator.DotProduct(values1, values2));
        }

        /// <summary>
        /// Compute cosine similarity between two vectors.
        /// </summary>
        [SqlFunction(Name = "clr_VectorCosineSimilarity", IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorCosineSimilarity(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            // Use GPU accelerator with automatic CPU fallback
            return new SqlDouble(GpuAccelerator.CosineSimilarity(values1, values2));
        }

        /// <summary>
        /// Compute Euclidean distance between two vectors.
        /// </summary>
        [SqlFunction(Name = "clr_VectorEuclideanDistance", IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorEuclideanDistance(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlDouble.Null;

            var values1 = SqlBytesInterop.GetFloatArray(vector1, out var length1);
            var values2 = SqlBytesInterop.GetFloatArray(vector2, out var length2);

            if (length1 != length2)
                throw new ArgumentException("Vectors must have same dimension");

            // Use GPU accelerator with automatic CPU fallback
            return new SqlDouble(GpuAccelerator.EuclideanDistance(values1, values2));
        }

        /// <summary>
        /// Add two vectors element-wise.
        /// </summary>
        [SqlFunction(Name = "clr_VectorAdd", IsDeterministic = true, IsPrecise = false)]
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
        /// Subtract two vectors element-wise.
        /// </summary>
        [SqlFunction(Name = "clr_VectorSubtract", IsDeterministic = true, IsPrecise = false)]
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
        /// Multiply vector by scalar.
        /// </summary>
        [SqlFunction(Name = "clr_VectorScale", IsDeterministic = true, IsPrecise = false)]
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
        /// Compute L2 norm (magnitude) of vector.
        /// </summary>
        [SqlFunction(Name = "clr_VectorNorm", IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorNorm(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlDouble.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);

            // Delegate to VectorMath
            return new SqlDouble(VectorMath.Norm(values));
        }

        /// <summary>
        /// Normalize vector to unit length.
        /// </summary>
        [SqlFunction(Name = "clr_VectorNormalize", IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorNormalize(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlBytes.Null;

            var values = SqlBytesInterop.GetFloatArray(vector, out var length);

            float norm = VectorMath.Norm(values);

            if (norm == 0)
            {
                return vector; // Cannot normalize a zero-length vector
            }

            var scale = 1.0f / norm;
            var result = new float[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = values[i] * scale;
            }

            return SqlBytesInterop.CreateFromFloats(result);
        }

        /// <summary>
        /// Linear interpolation between two vectors.
        /// </summary>
        [SqlFunction(Name = "clr_VectorLerp", IsDeterministic = true, IsPrecise = false)]
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
            float complement = 1 - tVal;

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
        [SqlFunction(Name = "clr_VectorSoftmax", IsDeterministic = true, IsPrecise = false)]
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
        [SqlFunction(Name = "clr_VectorArgMax", IsDeterministic = true, IsPrecise = true)]
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
