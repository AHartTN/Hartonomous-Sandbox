using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Collaborative filtering algorithms for recommendation systems.
    /// 
    /// UNIVERSAL DISTANCE SUPPORT: Configurable distance metrics enable cross-modal
    /// recommendation systems - same algorithms work for text, image, audio, code, and
    /// model weight recommendations via metric selection.
    /// </summary>
    internal static class CollaborativeFiltering
    {
        /// <summary>
        /// Compute user-based collaborative filtering recommendations.
        /// </summary>
        /// <param name="userItems">Dictionary mapping user IDs to their item vectors</param>
        /// <param name="topN">Number of recommendations to return</param>
        /// <returns>Top N recommended item vectors with scores</returns>
        public static List<(float[] Vector, double Score)> RecommendItems(
            Dictionary<string, List<float[]>> userItems,
            int topN)
        {
            if (userItems == null || userItems.Count == 0 || topN <= 0)
                return new List<(float[], double)>();

            int dimension = userItems.First().Value.First().Length;

            // Compute user centroids (average item vector per user)
            var userCentroids = new Dictionary<string, float[]>();
            foreach (var kvp in userItems)
            {
                float[] centroid = new float[dimension];
                foreach (var vec in kvp.Value)
                {
                    for (int i = 0; i < dimension; i++)
                        centroid[i] += vec[i];
                }
                for (int i = 0; i < dimension; i++)
                    centroid[i] /= kvp.Value.Count;
                
                userCentroids[kvp.Key] = centroid;
            }

            // Aggregate all items with weighted voting
            var itemScores = new Dictionary<string, double>();
            var itemVectors = new Dictionary<string, float[]>();

            foreach (var kvp in userItems)
            {
                string user = kvp.Key;
                foreach (var itemVec in kvp.Value)
                {
                    // Create item key from first 10 dimensions
                    string itemKey = string.Join(",", itemVec.Take(10).Select(v => v.ToString("G4")));
                    
                    if (!itemVectors.ContainsKey(itemKey))
                    {
                        itemVectors[itemKey] = itemVec;
                        itemScores[itemKey] = 0;
                    }

                    // Weight by user importance (number of items)
                    itemScores[itemKey] += kvp.Value.Count;
                }
            }

            // Get top N items by score
            return itemScores
                .OrderByDescending(kvp => kvp.Value)
                .Take(topN)
                .Select(kvp => (itemVectors[kvp.Key], kvp.Value))
                .ToList();
        }

        /// <summary>
        /// Compute content-based filtering user profile.
        /// Creates weighted centroid of user's preferred items.
        /// </summary>
        /// <param name="items">List of items with their preference weights</param>
        /// <returns>User profile vector and diversity metrics</returns>
        public static (float[] Profile, double Diversity) ComputeUserProfile(
            List<(float[] Vector, double Weight)> items)
        {
            if (items == null || items.Count == 0)
                return (Array.Empty<float>(), 0.0);

            int dimension = items[0].Vector.Length;
            
            // Weighted centroid of preferred items
            float[] profile = new float[dimension];
            double totalWeight = items.Sum(item => Math.Abs(item.Weight));

            foreach (var (vec, weight) in items)
            {
                for (int i = 0; i < dimension; i++)
                    profile[i] += vec[i] * (float)(weight / totalWeight);
            }

            // Compute variance as diversity metric
            double avgVariance = 0;
            for (int d = 0; d < dimension; d++)
            {
                double mean = items.Average(item => item.Vector[d]);
                double variance = items.Average(item =>
                {
                    double diff = item.Vector[d] - mean;
                    return diff * diff;
                });
                avgVariance += variance;
            }
            avgVariance /= dimension;

            return (profile, avgVariance);
        }

        /// <summary>
        /// Maximal Marginal Relevance (MMR) for diversity-aware recommendations.
        /// Balances relevance and diversity in recommendation selection.
        /// </summary>
        /// <param name="candidates">Candidate items with relevance scores</param>
        /// <param name="topN">Number of items to select</param>
        /// <param name="lambda">Diversity weight (0 = only relevance, 1 = only diversity)</param>
        /// <param name="metric">Distance metric for similarity (null = Cosine via 1-distance)</param>
        /// <returns>Selected items with diversity scores</returns>
        public static List<(float[] Vector, double Relevance, double DiversityScore)>
            SelectDiverseRecommendations(
                List<(float[] Vector, double Relevance)> candidates,
                int topN,
                double lambda)
        {
            return SelectDiverseRecommendations(candidates, topN, lambda, new CosineDistance());
        }

        public static List<(float[] Vector, double Relevance, double DiversityScore)> 
            SelectDiverseRecommendations(
                List<(float[] Vector, double Relevance)> candidates,
                int topN,
                double lambda,
                IDistanceMetric metric)
        {
            if (metric == null)
                metric = new CosineDistance();

            if (candidates == null || candidates.Count == 0 || topN <= 0)
                return new List<(float[], double, double)>();

            var selected = new List<(float[] Vector, double Relevance, double DiversityScore)>();
            var remaining = new List<(float[] Vector, double Relevance)>(candidates);

            // Sort by relevance initially
            remaining = remaining.OrderByDescending(c => c.Relevance).ToList();

            // Select first item (highest relevance)
            if (remaining.Count > 0)
            {
                var first = remaining[0];
                selected.Add((first.Vector, first.Relevance, 1.0));
                remaining.RemoveAt(0);
            }

            // Iteratively select items maximizing MMR
            while (selected.Count < topN && remaining.Count > 0)
            {
                double maxMMR = double.MinValue;
                int maxIdx = -1;

                for (int i = 0; i < remaining.Count; i++)
                {
                    var candidate = remaining[i];
                    
                    // Find maximum similarity to already selected items
                    // For CosineDistance: similarity = 1 - distance
                    double maxSimilarity = 0;
                    foreach (var selectedItem in selected)
                    {
                        double distance = metric.Distance(candidate.Vector, selectedItem.Vector);
                        double similarity = 1.0 - distance; // Convert distance to similarity
                        maxSimilarity = Math.Max(maxSimilarity, similarity);
                    }

                    // MMR = λ * Relevance - (1-λ) * MaxSimilarity
                    double mmr = lambda * candidate.Relevance - (1 - lambda) * maxSimilarity;

                    if (mmr > maxMMR)
                    {
                        maxMMR = mmr;
                        maxIdx = i;
                    }
                }

                if (maxIdx >= 0)
                {
                    var selectedCandidate = remaining[maxIdx];
                    double diversityScore = 1.0 - (selected.Count / (double)topN);
                    selected.Add((selectedCandidate.Vector, selectedCandidate.Relevance, diversityScore));
                    remaining.RemoveAt(maxIdx);
                }
            }

            return selected;
        }
    }
}
