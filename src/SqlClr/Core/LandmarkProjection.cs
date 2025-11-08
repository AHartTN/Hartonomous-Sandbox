using System;
using System.Numerics;

namespace SqlClrFunctions.Core
{
    /// <summary>
    /// Performs a landmark-based projection to reduce high-dimensional vectors to 3D space.
    /// This is a fast, stateless dimensionality reduction technique conceptually similar to
    /// the user's intent of "Trilateration Projection".
    /// </summary>
    public static class LandmarkProjection
    {
        private const int Dimensions = 1998;
        private static readonly float[] BasisVectorX;
        private static readonly float[] BasisVectorY;
        private static readonly float[] BasisVectorZ;

        /// <summary>
        /// Static constructor to initialize the basis vectors for the projection.
        /// In a real system, these landmarks would be carefully selected from the dataset
        /// (e.g., via clustering) and persisted. For now, we use fixed random vectors
        /// for a deterministic and stateless projection.
        /// </summary>
        static LandmarkProjection()
        {
            // Step 1: Define 3 landmark vectors. Using a fixed seed for reproducibility.
            var rand = new Random(42);
            var landmark1 = CreateRandomUnitVector(rand, Dimensions);
            var landmark2 = CreateRandomUnitVector(rand, Dimensions);
            var landmark3 = CreateRandomUnitVector(rand, Dimensions);

            // Step 2: Create an orthonormal basis using the Gram-Schmidt process.
            BasisVectorX = landmark1;

            BasisVectorY = Orthogonalize(landmark2, BasisVectorX);
            Normalize(BasisVectorY);

            BasisVectorZ = Orthogonalize(landmark3, BasisVectorX);
            BasisVectorZ = Orthogonalize(BasisVectorZ, BasisVectorY);
            Normalize(BasisVectorZ);
        }

        /// <summary>
        /// Projects a high-dimensional vector into the 3D space defined by the landmarks.
        /// </summary>
        /// <param name="vector">The high-dimensional vector to project.</param>
        /// <returns>A tuple containing the X, Y, and Z coordinates.</returns>
        public static (double X, double Y, double Z) ProjectTo3D(float[] vector)
        {
            if (vector == null || vector.Length != Dimensions)
            {
                throw new ArgumentException($"Input vector must not be null and have {Dimensions} dimensions.", nameof(vector));
            }

            double x = VectorMath.DotProduct(vector, BasisVectorX);
            double y = VectorMath.DotProduct(vector, BasisVectorY);
            double z = VectorMath.DotProduct(vector, BasisVectorZ);

            return (x, y, z);
        }

        private static float[] CreateRandomUnitVector(Random rand, int dimensions)
        {
            var vector = new float[dimensions];
            for (int i = 0; i < dimensions; i++)
            {
                vector[i] = (float)(rand.NextDouble() * 2.0 - 1.0);
            }

            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;

            // Calculate sum of squares using SIMD
            for (; i <= dimensions - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(vector, i);
                sumSquares += Vector.Dot(v, v);
            }

            // Process remaining elements
            for (; i < dimensions; i++)
            {
                float value = vector[i];
                sumSquares += value * value;
            }

            var mag = (float)Math.Sqrt(sumSquares);
            if (mag > 0)
            {
                var scale = 1.0f / mag;
                i = 0;

                // Scale the vector using SIMD
                var scaleVector = new Vector<float>(scale);
                for (; i <= dimensions - vectorSize; i += vectorSize)
                {
                    var v = new Vector<float>(vector, i);
                    (v * scaleVector).CopyTo(vector, i);
                }

                // Process remaining elements
                for (; i < dimensions; i++)
                {
                    vector[i] *= scale;
                }
            }
            return vector;
        }

        private static float[] Orthogonalize(float[] vectorToClean, float[] basisVector)
        {
            var projection = new float[vectorToClean.Length];
            var dot = VectorMath.DotProduct(vectorToClean, basisVector);

            for (int i = 0; i < vectorToClean.Length; i++)
            {
                projection[i] = (float)(vectorToClean[i] - dot * basisVector[i]);
            }
            return projection;
        }

        private static void Normalize(float[] vector)
        {
            float sumSquares = 0;
            int i = 0;
            int vectorSize = Vector<float>.Count;
            int length = vector.Length;

            // Calculate sum of squares using SIMD
            for (; i <= length - vectorSize; i += vectorSize)
            {
                var v = new Vector<float>(vector, i);
                sumSquares += Vector.Dot(v, v);
            }

            // Process remaining elements
            for (; i < length; i++)
            {
                float value = vector[i];
                sumSquares += value * value;
            }

            var mag = (float)Math.Sqrt(sumSquares);
            if (mag > 0)
            {
                var scale = 1.0f / mag;
                i = 0;

                // Scale the vector using SIMD
                var scaleVector = new Vector<float>(scale);
                for (; i <= length - vectorSize; i += vectorSize)
                {
                    var v = new Vector<float>(vector, i);
                    (v * scaleVector).CopyTo(vector, i);
                }

                // Process remaining elements
                for (; i < length; i++)
                {
                    vector[i] *= scale;
                }
            }
        }
    }
}
