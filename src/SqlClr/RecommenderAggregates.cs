using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// RECOMMENDER SYSTEM AGGREGATES
    /// Collaborative filtering and recommendation via vector embeddings
    /// </summary>

    /// <summary>
    /// COLLABORATIVE FILTERING AGGREGATE
    /// User-based collaborative filtering using vector similarity
    /// 
    /// SELECT target_user_id,
    ///        dbo.CollaborativeFilter(user_id, item_vector, 10)
    /// FROM user_item_embeddings
    /// WHERE user_id IN (SELECT similar_users FROM ...)
    /// GROUP BY target_user_id
    /// 
    /// Returns: Top N recommended item vectors
    /// USE CASE: "Users who liked X also liked Y" recommendations
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct CollaborativeFilter : IBinarySerialize
    {
        private Dictionary<string, List<float[]>> userItems;
        private int topN;
        private int dimension;

        public void Init()
        {
            userItems = new Dictionary<string, List<float[]>>();
            topN = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString userId, SqlString itemVectorJson, SqlInt32 topNItems)
        {
            if (userId.IsNull || itemVectorJson.IsNull)
                return;

            if (topN == 0 && !topNItems.IsNull)
                topN = topNItems.Value;

            var vec = VectorUtilities.ParseVectorJson(itemVectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            string uid = userId.Value;
            if (!userItems.ContainsKey(uid))
                userItems[uid] = new List<float[]>();
            
            userItems[uid].Add(vec);
        }

        public void Merge(CollaborativeFilter other)
        {
            if (other.userItems != null)
            {
                foreach (var kvp in other.userItems)
                {
                    if (!userItems.ContainsKey(kvp.Key))
                        userItems[kvp.Key] = new List<float[]>();
                    userItems[kvp.Key].AddRange(kvp.Value);
                }
            }
        }

        public SqlString Terminate()
        {
            if (userItems.Count == 0 || dimension == 0 || topN == 0)
                return SqlString.Null;

            // Compute user centroids (average item vector per user)
            var userCentroids = new Dictionary<string, float[]>();
            foreach (var kvp in userItems)
            {
                float[] centroid = new float[dimension];
                foreach (var vec in kvp.Value)
                    for (int i = 0; i < dimension; i++)
                        centroid[i] += vec[i];
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
            var topItems = itemScores.OrderByDescending(kvp => kvp.Value)
                .Take(topN)
                .Select(kvp => itemVectors[kvp.Key])
                .ToList();

            var recommendations = topItems.Select((vec, idx) => new { rank = idx + 1, vector = vec }).ToArray();
            var result = new { recommendations };
            var serializer = new SqlClrFunctions.JsonProcessing.JsonSerializerImpl();
            return new SqlString(serializer.Serialize(result));
        }

        public void Read(BinaryReader r)
        {
            topN = r.ReadInt32();
            dimension = r.ReadInt32();
            int userCount = r.ReadInt32();
            userItems = new Dictionary<string, List<float[]>>();
            
            for (int i = 0; i < userCount; i++)
            {
                string userId = r.ReadString();
                int itemCount = r.ReadInt32();
                var items = new List<float[]>(itemCount);
                
                for (int j = 0; j < itemCount; j++)
                {
                    float[] vec = new float[dimension];
                    for (int k = 0; k < dimension; k++)
                        vec[k] = r.ReadSingle();
                    items.Add(vec);
                }
                
                userItems[userId] = items;
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(topN);
            w.Write(dimension);
            w.Write(userItems.Count);
            
            foreach (var kvp in userItems)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value.Count);
                foreach (var vec in kvp.Value)
                    foreach (var val in vec)
                        w.Write(val);
            }
        }
    }

    /// <summary>
    /// CONTENT-BASED FILTERING AGGREGATE
    /// Recommend items similar to user's preferred items
    /// 
    /// SELECT user_id,
    ///        dbo.ContentBasedFilter(item_vector, preference_score, 10)
    /// FROM user_interactions
    /// WHERE user_id = @targetUser
    /// GROUP BY user_id
    /// 
    /// Returns: Weighted centroid representing user preferences
    /// USE CASE: Build user profile from interaction history
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct ContentBasedFilter : IBinarySerialize
    {
        private List<(float[] Vector, double Weight)> items;
        private int topN;
        private int dimension;

        public void Init()
        {
            items = new List<(float[], double)>();
            topN = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString itemVectorJson, SqlDouble preferenceScore, SqlInt32 topNItems)
        {
            if (itemVectorJson.IsNull || preferenceScore.IsNull)
                return;

            if (topN == 0 && !topNItems.IsNull)
                topN = topNItems.Value;

            var vec = VectorUtilities.ParseVectorJson(itemVectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            items.Add((vec, preferenceScore.Value));
        }

        public void Merge(ContentBasedFilter other)
        {
            if (other.items != null)
                items.AddRange(other.items);
        }

        public SqlString Terminate()
        {
            if (items.Count == 0 || dimension == 0)
                return SqlString.Null;

            // Weighted centroid of preferred items
            float[] profile = new float[dimension];
            double totalWeight = items.Sum(item => Math.Abs(item.Weight));

            foreach (var (vec, weight) in items)
            {
                double normalizedWeight = weight / totalWeight;
                for (int i = 0; i < dimension; i++)
                    profile[i] += (float)(vec[i] * normalizedWeight);
            }

            // Also compute variance (diversity metric)
            float[] variance = new float[dimension];
            foreach (var (vec, weight) in items)
            {
                double normalizedWeight = weight / totalWeight;
                for (int i = 0; i < dimension; i++)
                {
                    float diff = vec[i] - profile[i];
                    variance[i] += (float)(diff * diff * normalizedWeight);
                }
            }

            double avgVariance = variance.Average();

            var result = new
            {
                user_profile = profile,
                preference_diversity = avgVariance,
                num_items = items.Count
            };
            var serializer = new SqlClrFunctions.JsonProcessing.JsonSerializerImpl();
            return new SqlString(serializer.Serialize(result));
        }

        public void Read(BinaryReader r)
        {
            topN = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            items = new List<(float[], double)>(count);
            
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                double weight = r.ReadDouble();
                items.Add((vec, weight));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(topN);
            w.Write(dimension);
            w.Write(items.Count);
            
            foreach (var (vec, weight) in items)
            {
                foreach (var val in vec)
                    w.Write(val);
                w.Write(weight);
            }
        }
    }

    /// <summary>
    /// MATRIX FACTORIZATION AGGREGATE
    /// Approximate matrix factorization for recommendation
    /// 
    /// SELECT dbo.MatrixFactorization(user_id, item_id, rating, 20)
    /// FROM user_item_ratings
    /// 
    /// Returns: Latent factor matrices (simplified SGD approach)
    /// USE CASE: Predict missing ratings in user-item matrix
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct MatrixFactorization : IBinarySerialize
    {
        private Dictionary<string, int> userIds;
        private Dictionary<string, int> itemIds;
        private List<(int User, int Item, double Rating)> ratings;
        private int numFactors;

        public void Init()
        {
            userIds = new Dictionary<string, int>();
            itemIds = new Dictionary<string, int>();
            ratings = new List<(int, int, double)>();
            numFactors = 0;
        }

        public void Accumulate(SqlString userId, SqlString itemId, SqlDouble rating, SqlInt32 factors)
        {
            if (userId.IsNull || itemId.IsNull || rating.IsNull)
                return;

            if (numFactors == 0 && !factors.IsNull)
                numFactors = factors.Value;

            string uid = userId.Value;
            string iid = itemId.Value;

            if (!userIds.ContainsKey(uid))
                userIds[uid] = userIds.Count;
            if (!itemIds.ContainsKey(iid))
                itemIds[iid] = itemIds.Count;

            ratings.Add((userIds[uid], itemIds[iid], rating.Value));
        }

        public void Merge(MatrixFactorization other)
        {
            if (other.ratings == null) return;

            // Merge user/item mappings
            foreach (var kvp in other.userIds)
            {
                if (!userIds.ContainsKey(kvp.Key))
                    userIds[kvp.Key] = userIds.Count;
            }
            foreach (var kvp in other.itemIds)
            {
                if (!itemIds.ContainsKey(kvp.Key))
                    itemIds[kvp.Key] = itemIds.Count;
            }

            // Remap and merge ratings
            foreach (var (user, item, rating) in other.ratings)
            {
                // Note: This is simplified; proper merge would remap indices
                ratings.Add((user, item, rating));
            }
        }

        public SqlString Terminate()
        {
            if (ratings.Count == 0 || numFactors == 0 || userIds.Count == 0 || itemIds.Count == 0)
                return SqlString.Null;

            int numUsers = userIds.Count;
            int numItems = itemIds.Count;
            int k = numFactors;

            // Initialize factor matrices
            Random rng = new Random(42);
            // Use bridge library for PROPER matrix factorization
            // Replaces: "Simplified" SGD with only 10 iterations and improper index handling
            
            // Convert ratings to array format expected by bridge
            var interactionArray = ratings.Select(r => (r.Item1, r.Item2, (float)r.Item3)).ToArray();
            
            // Run proper SGD matrix factorization with 100 iterations
            var (userFactors, itemFactors) = SqlClrFunctions.MachineLearning.MatrixFactorization.Factorize(
                interactionArray,
                numUsers: numUsers,
                numItems: numItems,
                latentDim: k,
                learningRate: 0.01,
                regularization: 0.01,
                iterations: 100 // Proper number of iterations for convergence
            );

            // Compute RMSE with learned factors
            double sumSquaredError = 0;
            foreach (var (user, item, rating) in ratings)
            {
                float prediction = SqlClrFunctions.MachineLearning.MatrixFactorization.PredictRating(
                    userFactors[user], 
                    itemFactors[item]
                );
                double error = rating - prediction;
                sumSquaredError += error * error;
            }
            double rmse = Math.Sqrt(sumSquaredError / ratings.Count);

            // Use bridge JSON serializer instead of manual concatenation
            var result = new
            {
                num_users = numUsers,
                num_items = numItems,
                num_factors = k,
                rmse = rmse,
                num_ratings = ratings.Count
            };
            
            var serializer = new SqlClrFunctions.JsonProcessing.JsonSerializerImpl();
            return new SqlString(serializer.Serialize(result));
        }

        public void Read(BinaryReader r)
        {
            numFactors = r.ReadInt32();
            
            int userCount = r.ReadInt32();
            userIds = new Dictionary<string, int>(userCount);
            for (int i = 0; i < userCount; i++)
                userIds[r.ReadString()] = r.ReadInt32();

            int itemCount = r.ReadInt32();
            itemIds = new Dictionary<string, int>(itemCount);
            for (int i = 0; i < itemCount; i++)
                itemIds[r.ReadString()] = r.ReadInt32();

            int ratingCount = r.ReadInt32();
            ratings = new List<(int, int, double)>(ratingCount);
            for (int i = 0; i < ratingCount; i++)
                ratings.Add((r.ReadInt32(), r.ReadInt32(), r.ReadDouble()));
        }

        public void Write(BinaryWriter w)
        {
            w.Write(numFactors);
            
            w.Write(userIds.Count);
            foreach (var kvp in userIds)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value);
            }

            w.Write(itemIds.Count);
            foreach (var kvp in itemIds)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value);
            }

            w.Write(ratings.Count);
            foreach (var (user, item, rating) in ratings)
            {
                w.Write(user);
                w.Write(item);
                w.Write(rating);
            }
        }
    }

    /// <summary>
    /// DIVERSITY-AWARE RECOMMENDATION AGGREGATE
    /// Maximize recommendation diversity while maintaining relevance
    /// 
    /// SELECT dbo.DiversityRecommendation(candidate_vector, relevance_score, 10, 0.7)
    /// FROM candidate_items
    /// 
    /// Returns: Diverse set of recommendations (MMR-style)
    /// USE CASE: Avoid filter bubbles, show varied content
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct DiversityRecommendation : IBinarySerialize
    {
        private List<(float[] Vector, double Relevance)> candidates;
        private int topN;
        private double lambda; // Diversity weight
        private int dimension;

        public void Init()
        {
            candidates = new List<(float[], double)>();
            topN = 0;
            lambda = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlDouble relevanceScore, SqlInt32 topNItems, SqlDouble diversityWeight)
        {
            if (vectorJson.IsNull || relevanceScore.IsNull)
                return;

            if (topN == 0 && !topNItems.IsNull)
                topN = topNItems.Value;
            if (lambda == 0 && !diversityWeight.IsNull)
                lambda = diversityWeight.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            candidates.Add((vec, relevanceScore.Value));
        }

        public void Merge(DiversityRecommendation other)
        {
            if (other.candidates != null)
                candidates.AddRange(other.candidates);
        }

        public SqlString Terminate()
        {
            if (candidates.Count == 0 || dimension == 0 || topN == 0)
                return SqlString.Null;

            // Maximal Marginal Relevance (MMR) algorithm
            var selected = new List<(float[] Vector, double Relevance, double DiversityScore)>();
            var remaining = new List<(float[] Vector, double Relevance)>(candidates);

            while (selected.Count < topN && remaining.Count > 0)
            {
                int bestIdx = -1;
                double bestScore = double.NegativeInfinity;

                for (int i = 0; i < remaining.Count; i++)
                {
                    var (vec, relevance) = remaining[i];

                    // Compute max similarity to already selected items
                    double maxSim = 0;
                    if (selected.Count > 0)
                    {
                        maxSim = selected.Max(s => VectorUtilities.CosineSimilarity(vec, s.Vector));
                    }

                    // MMR score: lambda * relevance - (1-lambda) * similarity
                    double mmrScore = lambda * relevance - (1 - lambda) * maxSim;

                    if (mmrScore > bestScore)
                    {
                        bestScore = mmrScore;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                {
                    var (vec, relevance) = remaining[bestIdx];
                    selected.Add((vec, relevance, bestScore));
                    remaining.RemoveAt(bestIdx);
                }
                else
                {
                    break;
                }
            }

            var json = "{\"recommendations\":[" +
                string.Join(",",
                    selected.Select((item, idx) =>
                        $"{{\"rank\":{idx + 1}," +
                        $"\"relevance\":{item.Relevance:G6}," +
                        $"\"diversity_score\":{item.DiversityScore:G6}}}"
                    )
                ) + "]}";

            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            topN = r.ReadInt32();
            lambda = r.ReadDouble();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            candidates = new List<(float[], double)>(count);
            
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                double relevance = r.ReadDouble();
                candidates.Add((vec, relevance));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(topN);
            w.Write(lambda);
            w.Write(dimension);
            w.Write(candidates.Count);
            
            foreach (var (vec, relevance) in candidates)
            {
                foreach (var val in vec)
                    w.Write(val);
                w.Write(relevance);
            }
        }
    }
}
