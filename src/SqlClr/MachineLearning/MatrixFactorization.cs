using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SqlClrFunctions.Core;

namespace SqlClrFunctions.MachineLearning
{
    /// <summary>
    /// Proper SGD-based matrix factorization for collaborative filtering.
    /// REPLACES: RecommenderAggregates.cs:416 ("simplified" SGD with improper index handling)
    /// 
    /// Factorizes user-item interaction matrix into latent factor matrices.
    /// R ≈ U × I^T where U is users×k and I is items×k
    /// </summary>
    public class MatrixFactorization
    {
        /// <summary>
        /// Perform SGD matrix factorization on interaction matrix.
        /// </summary>
        public static (float[][] UserFactors, float[][] ItemFactors) Factorize(
            (int UserId, int ItemId, float Rating)[] interactions,
            int numUsers,
            int numItems,
            int latentDim = 20,
            double learningRate = 0.01,
            double regularization = 0.01,
            int iterations = 100)
        {
            if (interactions == null || interactions.Length == 0)
                throw new ArgumentException("Interactions cannot be empty", nameof(interactions));

            var random = new Random(42);

            var userFactors = new float[numUsers][];
            for (int u = 0; u < numUsers; u++)
            {
                userFactors[u] = new float[latentDim];
                for (int k = 0; k < latentDim; k++)
                {
                    userFactors[u][k] = (float)(random.NextDouble() * 0.01);
                }
            }

            var itemFactors = new float[numItems][];
            for (int i = 0; i < numItems; i++)
            {
                itemFactors[i] = new float[latentDim];
                for (int k = 0; k < latentDim; k++)
                {
                    itemFactors[i][k] = (float)(random.NextDouble() * 0.01);
                }
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                var shuffled = interactions.OrderBy(x => random.Next()).ToArray();
                double totalError = 0;

                foreach (var (userId, itemId, rating) in shuffled)
                {
                    if (userId < 0 || userId >= numUsers || itemId < 0 || itemId >= numItems)
                        continue;

                    float prediction = VectorMath.DotProduct(userFactors[userId], itemFactors[itemId]);
                    float error = rating - prediction;
                    totalError += error * error;

                    for (int k = 0; k < latentDim; k++)
                    {
                        float userGrad = (float)(-2.0 * error * itemFactors[itemId][k] + regularization * userFactors[userId][k]);
                        float itemGrad = (float)(-2.0 * error * userFactors[userId][k] + regularization * itemFactors[itemId][k]);

                        userFactors[userId][k] -= (float)(learningRate * userGrad);
                        itemFactors[itemId][k] -= (float)(learningRate * itemGrad);
                    }
                }

                if (iter > 0 && iter % 20 == 0)
                {
                    learningRate *= 0.9;
                }

                double rmse = Math.Sqrt(totalError / interactions.Length);
                if (rmse < 0.01) break;
            }

            return (userFactors, itemFactors);
        }

        /// <summary>
        /// Predict rating for user-item pair using learned factors.
        /// </summary>
        public static float PredictRating(float[] userFactor, float[] itemFactor)
        {
            if (userFactor == null || itemFactor == null)
                return 0f;

            if (userFactor.Length != itemFactor.Length)
                throw new ArgumentException("Factor dimensions must match");

            return VectorMath.DotProduct(userFactor, itemFactor);
        }

        /// <summary>
        /// Get top-N item recommendations for a user.
        /// </summary>
        public static (int ItemId, float PredictedRating)[] GetTopRecommendations(
            int userId,
            float[][] userFactors,
            float[][] itemFactors,
            int topN,
            int[] excludeItems = null)
        {
            if (userId < 0 || userId >= userFactors.Length)
                throw new ArgumentException($"Invalid user ID: {userId}");

            var exclude = excludeItems != null ? new HashSet<int>(excludeItems) : new HashSet<int>();
            var predictions = new List<(int ItemId, float Rating)>();

            for (int itemId = 0; itemId < itemFactors.Length; itemId++)
            {
                if (exclude.Contains(itemId))
                    continue;

                float prediction = PredictRating(userFactors[userId], itemFactors[itemId]);
                predictions.Add((itemId, prediction));
            }

            return predictions
                .OrderByDescending(p => p.Rating)
                .Take(topN)
                .ToArray();
        }

        /// <summary>
        /// Compute similarity between two items using their learned factors.
        /// </summary>
        public static float ComputeItemSimilarity(float[] itemFactor1, float[] itemFactor2)
        {
            if (itemFactor1 == null || itemFactor2 == null)
                return 0f;

            float dot = VectorMath.DotProduct(itemFactor1, itemFactor2);
            float norm1 = VectorMath.Norm(itemFactor1);
            float norm2 = VectorMath.Norm(itemFactor2);

            if (norm1 == 0f || norm2 == 0f)
                return 0f;

            return dot / (norm1 * norm2);
        }
    }
}

