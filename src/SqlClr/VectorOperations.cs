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

            float[] v1 = BytesToFloatArray(vector1.Value);
            float[] v2 = BytesToFloatArray(vector2.Value);

            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            double result = 0;
            for (int i = 0; i < v1.Length; i++)
                result += v1[i] * v2[i];

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

            float[] v1 = BytesToFloatArray(vector1.Value);
            float[] v2 = BytesToFloatArray(vector2.Value);

            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            double dotProduct = 0;
            double norm1 = 0;
            double norm2 = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                dotProduct += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
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

            float[] v1 = BytesToFloatArray(vector1.Value);
            float[] v2 = BytesToFloatArray(vector2.Value);

            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            double sumSquares = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                double diff = v1[i] - v2[i];
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

            float[] v1 = BytesToFloatArray(vector1.Value);
            float[] v2 = BytesToFloatArray(vector2.Value);

            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            float[] result = new float[v1.Length];
            for (int i = 0; i < v1.Length; i++)
                result[i] = v1[i] + v2[i];

            return new SqlBytes(FloatArrayToBytes(result));
        }

        /// <summary>
        /// Subtract two vectors element-wise
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorSubtract(SqlBytes vector1, SqlBytes vector2)
        {
            if (vector1.IsNull || vector2.IsNull)
                return SqlBytes.Null;

            float[] v1 = BytesToFloatArray(vector1.Value);
            float[] v2 = BytesToFloatArray(vector2.Value);

            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            float[] result = new float[v1.Length];
            for (int i = 0; i < v1.Length; i++)
                result[i] = v1[i] - v2[i];

            return new SqlBytes(FloatArrayToBytes(result));
        }

        /// <summary>
        /// Multiply vector by scalar
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorScale(SqlBytes vector, SqlDouble scalar)
        {
            if (vector.IsNull || scalar.IsNull)
                return SqlBytes.Null;

            float[] v = BytesToFloatArray(vector.Value);
            float s = (float)scalar.Value;

            float[] result = new float[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = v[i] * s;

            return new SqlBytes(FloatArrayToBytes(result));
        }

        /// <summary>
        /// Compute L2 norm (magnitude) of vector
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble VectorNorm(SqlBytes vector)
        {
            if (vector.IsNull)
                return SqlDouble.Null;

            float[] v = BytesToFloatArray(vector.Value);

            double sumSquares = 0;
            for (int i = 0; i < v.Length; i++)
                sumSquares += v[i] * v[i];

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

            float[] v = BytesToFloatArray(vector.Value);

            double norm = 0;
            for (int i = 0; i < v.Length; i++)
                norm += v[i] * v[i];

            norm = Math.Sqrt(norm);

            if (norm == 0)
                return vector; // Return unchanged if zero vector

            float[] result = new float[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = (float)(v[i] / norm);

            return new SqlBytes(FloatArrayToBytes(result));
        }

        /// <summary>
        /// Linear interpolation between two vectors
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes VectorLerp(SqlBytes vector1, SqlBytes vector2, SqlDouble t)
        {
            if (vector1.IsNull || vector2.IsNull || t.IsNull)
                return SqlBytes.Null;

            float[] v1 = BytesToFloatArray(vector1.Value);
            float[] v2 = BytesToFloatArray(vector2.Value);
            float tVal = (float)t.Value;

            if (v1.Length != v2.Length)
                throw new ArgumentException("Vectors must have same dimension");

            float[] result = new float[v1.Length];
            for (int i = 0; i < v1.Length; i++)
                result[i] = v1[i] * (1 - tVal) + v2[i] * tVal;

            return new SqlBytes(FloatArrayToBytes(result));
        }

        // Helper methods to convert between byte arrays and float arrays
        private static float[] BytesToFloatArray(byte[] bytes)
        {
            int floatCount = bytes.Length / sizeof(float);
            float[] floats = new float[floatCount];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        private static byte[] FloatArrayToBytes(float[] floats)
        {
            byte[] bytes = new byte[floats.Length * sizeof(float)];
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
