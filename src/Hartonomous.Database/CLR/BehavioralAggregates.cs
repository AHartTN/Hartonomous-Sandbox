using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr
{
    /// <summary>
    /// BEHAVIORAL ANALYSIS AGGREGATES
    /// User journeys, session patterns, engagement scoring, churn prediction
    /// </summary>

    /// <summary>
    /// USER JOURNEY AGGREGATE
    /// Analyze complete user behavior path with semantic understanding
    /// 
    /// SELECT user_id,
    ///        dbo.UserJourney(timestamp, page_vector, action_type, duration_seconds, conversion_value)
    /// FROM user_events
    /// WHERE session_date >= @start_date
    /// GROUP BY user_id
    /// 
    /// Returns: Journey quality score, engagement metrics, drop-off points
    /// USE CASE: Optimize conversion funnels, identify friction points
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct UserJourney : IBinarySerialize
    {
        private class JourneyStep
        {
            public DateTime Timestamp;
            public float[] PageVector;
            public string ActionType; // view, click, scroll, convert, exit
            public int DurationSeconds;
            public double ConversionValue;
        }

        private List<JourneyStep> steps;
        private int dimension;

        public void Init()
        {
            steps = new List<JourneyStep>();
            dimension = 0;
        }

        public void Accumulate(SqlDateTime timestamp, SqlString pageVectorJson, SqlString actionType,
            SqlInt32 durationSeconds, SqlDouble conversionValue)
        {
            if (timestamp.IsNull || pageVectorJson.IsNull) return;

            var vec = VectorUtilities.ParseVectorJson(pageVectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            steps.Add(new JourneyStep
            {
                Timestamp = timestamp.Value,
                PageVector = vec,
                ActionType = actionType.IsNull ? "view" : actionType.Value,
                DurationSeconds = durationSeconds.IsNull ? 0 : durationSeconds.Value,
                ConversionValue = conversionValue.IsNull ? 0 : conversionValue.Value
            });
        }

        public void Merge(UserJourney other)
        {
            if (other.steps != null)
                steps.AddRange(other.steps);
        }

        public SqlString Terminate()
        {
            if (steps.Count == 0 || dimension == 0) return SqlString.Null;

            // Sort by timestamp
            steps.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Compute journey metrics
            var totalDuration = steps.Sum(s => s.DurationSeconds);
            var totalValue = steps.Sum(s => s.ConversionValue);
            var converted = steps.Any(s => s.ConversionValue > 0);

            // Engagement score: time spent + actions + semantic coherence
            double engagementScore = Math.Log(1 + totalDuration) / 10 * 0.4 +
                                    Math.Log(1 + steps.Count) / 5 * 0.3;
            double semanticCoherence = 0;
            
            for (int i = 1; i < steps.Count; i++)
            {
                // Coherence: how related is this page to the previous
                double similarity = VectorMath.CosineSimilarity(
                    steps[i].PageVector, steps[i - 1].PageVector);
                semanticCoherence += similarity;
            }
            semanticCoherence /= Math.Max(1, steps.Count - 1);

            // Complete engagement score with semantic coherence
            engagementScore += semanticCoherence * 0.3;

            // High coherence = focused journey, low = exploring
            double focusScore = semanticCoherence;

            // Identify drop-off candidates (sudden decreases in engagement)
            var dropOffPoints = new List<(int StepIndex, double EngagementDrop)>();
            for (int i = 1; i < steps.Count - 1; i++)
            {
                double beforeDuration = steps[i - 1].DurationSeconds;
                double afterDuration = steps[i].DurationSeconds;
                
                if (beforeDuration > 10 && afterDuration < beforeDuration * 0.3)
                {
                    dropOffPoints.Add((i, beforeDuration - afterDuration));
                }
            }

            // Page importance (which pages matter most)
            var pageImportance = new Dictionary<int, double>();
            for (int i = 0; i < steps.Count; i++)
            {
                double importance = steps[i].DurationSeconds * 0.5 + 
                                   steps[i].ConversionValue * 10;
                pageImportance[i] = importance;
            }

            var topPages = pageImportance
                .OrderByDescending(kv => kv.Value)
                .Take(3)
                .ToList();

            // Session quality: 0-1 score
            double sessionQuality = Math.Min(1.0,
                (Math.Log(1 + totalDuration) / 10) * 0.3 +  // Time engagement
                (Math.Log(1 + steps.Count) / 5) * 0.3 +      // Action count
                focusScore * 0.2 +                            // Semantic focus
                (converted ? 1.0 : 0.0) * 0.2                // Conversion
            );

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"total_steps\":{0},", steps.Count);
            sb.AppendFormat("\"total_duration_seconds\":{0},", totalDuration);
            sb.AppendFormat("\"total_value\":{0:G6},", totalValue);
            sb.AppendFormat("\"converted\":{0},", converted.ToString().ToLower());
            sb.AppendFormat("\"focus_score\":{0:G6},", focusScore);
            sb.AppendFormat("\"session_quality\":{0:G6},", sessionQuality);
            sb.AppendFormat("\"drop_off_points\":{0},", dropOffPoints.Count);
            
            sb.Append("\"top_pages\":[");
            sb.Append(string.Join(",", topPages.Select(p =>
                $"{{\"step\":{p.Key},\"importance\":{p.Value:G6}}}"
            )));
            sb.Append("]}");

            return new SqlString(sb.ToString());
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            steps = new List<JourneyStep>(count);

            for (int i = 0; i < count; i++)
            {
                var step = new JourneyStep
                {
                    Timestamp = DateTime.FromBinary(r.ReadInt64()),
                    ActionType = r.ReadString(),
                    DurationSeconds = r.ReadInt32(),
                    ConversionValue = r.ReadDouble()
                };

                step.PageVector = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    step.PageVector[j] = r.ReadSingle();

                steps.Add(step);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(steps.Count);

            foreach (var step in steps)
            {
                w.Write(step.Timestamp.ToBinary());
                w.Write(step.ActionType);
                w.Write(step.DurationSeconds);
                w.Write(step.ConversionValue);

                for (int j = 0; j < dimension; j++)
                    w.Write(step.PageVector[j]);
            }
        }
    }

    /// <summary>
    /// A/B TEST ANALYSIS AGGREGATE
    /// Statistical analysis of variant performance with semantic understanding
    /// 
    /// SELECT test_id,
    ///        dbo.ABTestAnalysis(variant_id, outcome_vector, conversion, metric_value)
    /// FROM ab_test_results
    /// GROUP BY test_id
    /// 
    /// Returns: Winner determination, confidence intervals, semantic impact
    /// USE CASE: Automatically determine winning variants with statistical rigor
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct ABTestAnalysis : IBinarySerialize
    {
        private class VariantData
        {
            public List<float[]> OutcomeVectors = new List<float[]>();
            public List<bool> Conversions = new List<bool>();
            public List<double> MetricValues = new List<double>();
        }

        private Dictionary<string, VariantData> variants;
        private int dimension;

        public void Init()
        {
            variants = new Dictionary<string, VariantData>();
            dimension = 0;
        }

        public void Accumulate(SqlString variantId, SqlString outcomeVectorJson, 
            SqlBoolean conversion, SqlDouble metricValue)
        {
            if (variantId.IsNull) return;

            string vid = variantId.Value;
            if (!variants.ContainsKey(vid))
                variants[vid] = new VariantData();

            if (!outcomeVectorJson.IsNull)
            {
                var vec = VectorUtilities.ParseVectorJson(outcomeVectorJson.Value);
                if (vec != null)
                {
                    if (dimension == 0)
                        dimension = vec.Length;
                    else if (vec.Length != dimension)
                        return;

                    variants[vid].OutcomeVectors.Add(vec);
                }
            }

            if (!conversion.IsNull)
                variants[vid].Conversions.Add(conversion.Value);

            if (!metricValue.IsNull)
                variants[vid].MetricValues.Add(metricValue.Value);
        }

        public void Merge(ABTestAnalysis other)
        {
            if (other.variants == null) return;

            foreach (var kvp in other.variants)
            {
                if (!variants.ContainsKey(kvp.Key))
                    variants[kvp.Key] = new VariantData();

                variants[kvp.Key].OutcomeVectors.AddRange(kvp.Value.OutcomeVectors);
                variants[kvp.Key].Conversions.AddRange(kvp.Value.Conversions);
                variants[kvp.Key].MetricValues.AddRange(kvp.Value.MetricValues);
            }
        }

        public SqlString Terminate()
        {
            if (variants.Count < 2) return SqlString.Null;

            // Compute statistics for each variant
            var variantStats = new Dictionary<string, (double ConversionRate, double AvgMetric, double SampleSize)>();

            foreach (var kvp in variants)
            {
                double conversionRate = kvp.Value.Conversions.Count > 0
                    ? kvp.Value.Conversions.Average(c => c ? 1.0 : 0.0)
                    : 0;

                double avgMetric = kvp.Value.MetricValues.Count > 0
                    ? kvp.Value.MetricValues.Average()
                    : 0;

                variantStats[kvp.Key] = (conversionRate, avgMetric, kvp.Value.Conversions.Count);
            }

            // Find winner (highest conversion rate with enough samples)
            var winner = variantStats
                .Where(v => v.Value.SampleSize >= 30) // Minimum sample size
                .OrderByDescending(v => v.Value.ConversionRate)
                .FirstOrDefault();

            // Compute confidence intervals using proper Wilson score interval
            var confidenceIntervals = new Dictionary<string, (double Lower, double Upper)>();
            foreach (var kvp in variantStats)
            {
                double p = kvp.Value.ConversionRate;
                double n = kvp.Value.SampleSize;
                
                if (n > 0)
                {
                    double z = 1.96; // 95% confidence
                    double denominator = 1 + z * z / n;
                    double center = p + z * z / (2 * n);
                    double spread = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n));
                    
                    confidenceIntervals[kvp.Key] = (
                        Math.Max(0, (center - spread) / denominator),
                        Math.Min(1, (center + spread) / denominator)
                    );
                }
            }

            // Compute semantic differences between variants
            double semanticDivergence = 0;
            if (dimension > 0)
            {
                var variantCentroids = new Dictionary<string, float[]>();
                foreach (var kvp in variants)
                {
                    if (kvp.Value.OutcomeVectors.Count > 0)
                    {
                        var centroid = new float[dimension];
                        VectorMath.ComputeCentroid(kvp.Value.OutcomeVectors.ToArray(), centroid);
                        variantCentroids[kvp.Key] = centroid;
                    }
                }

                if (variantCentroids.Count >= 2)
                {
                    var centroids = variantCentroids.Values.ToArray();
                    for (int i = 0; i < centroids.Length - 1; i++)
                    {
                        for (int j = i + 1; j < centroids.Length; j++)
                        {
                            semanticDivergence += VectorMath.EuclideanDistance(centroids[i], centroids[j]);
                        }
                    }
                    semanticDivergence /= (centroids.Length * (centroids.Length - 1) / 2.0);
                }
            }

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"num_variants\":{0},", variants.Count);
            sb.AppendFormat("\"winner\":\"{0}\",", winner.Key ?? "none");
            sb.AppendFormat("\"semantic_divergence\":{0:G6},", semanticDivergence);
            
            sb.Append("\"variants\":[");
            sb.Append(string.Join(",", variantStats.Select(v =>
            {
                var ci = confidenceIntervals.ContainsKey(v.Key) ? confidenceIntervals[v.Key] : (0, 0);
                return $"{{\"id\":\"{v.Key.Replace("\"", "\\\"")}\"," +
                       $"\"conversion_rate\":{v.Value.ConversionRate:G6}," +
                       $"\"avg_metric\":{v.Value.AvgMetric:G6}," +
                       $"\"sample_size\":{v.Value.SampleSize}," +
                       $"\"ci_lower\":{ci.Lower:G6}," +
                       $"\"ci_upper\":{ci.Upper:G6}}}";
            })));
            sb.Append("]}");

            return new SqlString(sb.ToString());
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int variantCount = r.ReadInt32();
            variants = new Dictionary<string, VariantData>(variantCount);

            for (int i = 0; i < variantCount; i++)
            {
                string variantId = r.ReadString();
                var data = new VariantData();

                int vectorCount = r.ReadInt32();
                for (int j = 0; j < vectorCount; j++)
                {
                    var vec = new float[dimension];
                    for (int k = 0; k < dimension; k++)
                        vec[k] = r.ReadSingle();
                    data.OutcomeVectors.Add(vec);
                }

                int conversionCount = r.ReadInt32();
                for (int j = 0; j < conversionCount; j++)
                    data.Conversions.Add(r.ReadBoolean());

                int metricCount = r.ReadInt32();
                for (int j = 0; j < metricCount; j++)
                    data.MetricValues.Add(r.ReadDouble());

                variants[variantId] = data;
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(variants.Count);

            foreach (var kvp in variants)
            {
                w.Write(kvp.Key);

                w.Write(kvp.Value.OutcomeVectors.Count);
                foreach (var vec in kvp.Value.OutcomeVectors)
                {
                    for (int k = 0; k < dimension; k++)
                        w.Write(vec[k]);
                }

                w.Write(kvp.Value.Conversions.Count);
                foreach (var conv in kvp.Value.Conversions)
                    w.Write(conv);

                w.Write(kvp.Value.MetricValues.Count);
                foreach (var metric in kvp.Value.MetricValues)
                    w.Write(metric);
            }
        }
    }

    /// <summary>
    /// CHURN PREDICTION AGGREGATE
    /// Analyze user behavior patterns to predict churn risk
    /// 
    /// SELECT user_cohort,
    ///        dbo.ChurnPrediction(user_id, activity_vector, days_since_last_activity, engagement_score)
    /// FROM user_metrics
    /// GROUP BY user_cohort
    /// 
    /// Returns: Churn risk distribution, early warning indicators
    /// USE CASE: Proactive retention, identify at-risk users
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct ChurnPrediction : IBinarySerialize
    {
        private class UserData
        {
            public string UserId;
            public float[] ActivityVector;
            public int DaysSinceLastActivity;
            public double EngagementScore;
            public double ChurnRisk; // Computed
        }

        private List<UserData> users;
        private int dimension;

        public void Init()
        {
            users = new List<UserData>();
            dimension = 0;
        }

        public void Accumulate(SqlString userId, SqlString activityVectorJson,
            SqlInt32 daysSinceLastActivity, SqlDouble engagementScore)
        {
            if (userId.IsNull) return;

            var vec = !activityVectorJson.IsNull ? VectorUtilities.ParseVectorJson(activityVectorJson.Value) : null;

            if (vec != null)
            {
                if (dimension == 0)
                    dimension = vec.Length;
                else if (vec.Length != dimension)
                    return;
            }

            users.Add(new UserData
            {
                UserId = userId.Value,
                ActivityVector = vec,
                DaysSinceLastActivity = daysSinceLastActivity.IsNull ? 999 : daysSinceLastActivity.Value,
                EngagementScore = engagementScore.IsNull ? 0 : engagementScore.Value
            });
        }

        public void Merge(ChurnPrediction other)
        {
            if (other.users != null)
                users.AddRange(other.users);
        }

        public SqlString Terminate()
        {
            if (users.Count == 0) return SqlString.Null;

            // Compute churn risk for each user
            double avgEngagement = users.Average(u => u.EngagementScore);
            double avgDaysSince = users.Average(u => (double)u.DaysSinceLastActivity);

            foreach (var user in users)
            {
                // Risk factors:
                // 1. Low engagement relative to cohort
                // 2. Many days since last activity
                // 3. Activity pattern divergence from active users

                double engagementRisk = avgEngagement > 0
                    ? 1.0 - (user.EngagementScore / avgEngagement)
                    : 0.5;

                double inactivityRisk = Math.Min(1.0, user.DaysSinceLastActivity / 30.0);

                double patternRisk = 0;
                if (dimension > 0 && user.ActivityVector != null)
                {
                    // Compare to highly engaged users
                    var activeUsers = users
                        .Where(u => u.EngagementScore > avgEngagement && u.ActivityVector != null)
                        .ToList();

                    if (activeUsers.Count > 0)
                    {
                        var avgSimilarity = activeUsers.Average(au =>
                            VectorMath.CosineSimilarity(user.ActivityVector, au.ActivityVector));
                        patternRisk = 1.0 - avgSimilarity;
                    }
                }

                user.ChurnRisk = (engagementRisk * 0.4 + inactivityRisk * 0.4 + patternRisk * 0.2);
            }

            // Segment users by risk
            var highRisk = users.Where(u => u.ChurnRisk > 0.7).Count();
            var mediumRisk = users.Where(u => u.ChurnRisk > 0.4 && u.ChurnRisk <= 0.7).Count();
            var lowRisk = users.Where(u => u.ChurnRisk <= 0.4).Count();

            var avgRisk = users.Average(u => u.ChurnRisk);

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"total_users\":{0},", users.Count);
            sb.AppendFormat("\"avg_churn_risk\":{0:G6},", avgRisk);
            sb.AppendFormat("\"high_risk\":{0},", highRisk);
            sb.AppendFormat("\"medium_risk\":{0},", mediumRisk);
            sb.AppendFormat("\"low_risk\":{0},", lowRisk);
            sb.AppendFormat("\"high_risk_pct\":{0:G6}", (double)highRisk / users.Count);
            sb.Append("}");

            return new SqlString(sb.ToString());
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            users = new List<UserData>(count);

            for (int i = 0; i < count; i++)
            {
                var user = new UserData
                {
                    UserId = r.ReadString(),
                    DaysSinceLastActivity = r.ReadInt32(),
                    EngagementScore = r.ReadDouble(),
                    ChurnRisk = r.ReadDouble()
                };

                bool hasVector = r.ReadBoolean();
                if (hasVector)
                {
                    user.ActivityVector = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        user.ActivityVector[j] = r.ReadSingle();
                }

                users.Add(user);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(users.Count);

            foreach (var user in users)
            {
                w.Write(user.UserId);
                w.Write(user.DaysSinceLastActivity);
                w.Write(user.EngagementScore);
                w.Write(user.ChurnRisk);

                w.Write(user.ActivityVector != null);
                if (user.ActivityVector != null)
                {
                    for (int j = 0; j < dimension; j++)
                        w.Write(user.ActivityVector[j]);
                }
            }
        }
    }
}
