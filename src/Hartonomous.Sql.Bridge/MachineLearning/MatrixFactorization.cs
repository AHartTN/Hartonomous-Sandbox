using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Sql.Bridge.MachineLearning
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
        /// <param name="interactions">Array of (userId, itemId, rating) tuples</param>
        /// <param name="numUsers">Total number of users</param>
        /// <param name="numItems">Total number of items</param>
        /// <param name="latentDim">Number of latent factors (k)</param>
        /// <param name="learningRate">SGD learning rate</param>
        /// <param name="regularization">L2 regularization parameter</param>
        /// <param name="iterations">Number of SGD iterations</param>
        /// <returns>Tuple of (userFactors, itemFactors)</returns>
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

            // Initialize factor matrices with small random values
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

            // Compute global mean rating for initialization
            double globalMean = interactions.Average(x => x.Rating);

            // SGD iterations
            for (int iter = 0; iter < iterations; iter++)
            {
                // Shuffle interactions for better convergence
                var shuffled = interactions.OrderBy(x => random.Next()).ToArray();

                double totalError = 0;

                foreach (var (userId, itemId, rating) in shuffled)
                {
                    if (userId < 0 || userId >= numUsers || itemId < 0 || itemId >= numItems)
                        continue; // Skip invalid indices

                    // Predict rating: r_ui_hat = u_u · i_i
                    float prediction = DotProduct(userFactors[userId], itemFactors[itemId]);

                    // Compute error: e_ui = r_ui - r_ui_hat
                    float error = rating - prediction;
                    totalError += error * error;

                    // Update factors using gradient descent
                    // ∇u = -2 * e_ui * i_i + λ * u_u
                    // ∇i = -2 * e_ui * u_u + λ * i_i
                    for (int k = 0; k < latentDim; k++)
                    {
                        float userGrad = (float)(-2.0 * error * itemFactors[itemId][k] + regularization * userFactors[userId][k]);
                        float itemGrad = (float)(-2.0 * error * userFactors[userId][k] + regularization * itemFactors[itemId][k]);

                        userFactors[userId][k] -= (float)(learningRate * userGrad);
                        itemFactors[itemId][k] -= (float)(learningRate * itemGrad);
                    }
                }

                // Optional: Decrease learning rate over time
                if (iter > 0 && iter % 20 == 0)
                {
                    learningRate *= 0.9;
                }

                // Optional: Early stopping if error plateaus
                double rmse = Math.Sqrt(totalError / interactions.Length);
                if (rmse < 0.01)
                {
                    break; // Converged
                }
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

            return DotProduct(userFactor, itemFactor);
        }

        /// <summary>
        /// Get top-N item recommendations for a user.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userFactors">Learned user factors</param>
        /// <param name="itemFactors">Learned item factors</param>
        /// <param name="topN">Number of recommendations</param>
        /// <param name="excludeItems">Item IDs to exclude (already interacted)</param>
        /// <returns>Array of (itemId, predictedRating) sorted by rating descending</returns>
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
                .Select(p => (p.ItemId, p.Rating))
                .ToArray();
        }

        /// <summary>
        /// Compute similarity between two items using their learned factors.
        /// Returns cosine similarity.
        /// </summary>
        public static float ComputeItemSimilarity(float[] itemFactor1, float[] itemFactor2)
        {
            if (itemFactor1 == null || itemFactor2 == null)
                return 0f;

            float dot = DotProduct(itemFactor1, itemFactor2);
            float norm1 = Norm(itemFactor1);
            float norm2 = Norm(itemFactor2);

            if (norm1 == 0f || norm2 == 0f)
                return 0f;

            return dot / (norm1 * norm2);
        }

        private static float DotProduct(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }
            return sum;
        }

        private static float Norm(float[] a)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * a[i];
            }
            return (float)Math.Sqrt(sum);
        }
    }
}
