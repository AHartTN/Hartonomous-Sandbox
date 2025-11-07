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
    /// ADVANCED REASONING FRAMEWORK AGGREGATES
    /// Implement Tree-of-Thought, Reflexion, Chain-of-Thought, Self-Consistency patterns
    /// </summary>

    /// <summary>
    /// TREE OF THOUGHT AGGREGATE
    /// Explore multiple reasoning paths and select best branch
    /// 
    /// SELECT problem_id,
    ///        dbo.TreeOfThought(step_number, reasoning_vector, confidence_score, parent_step)
    /// FROM reasoning_steps
    /// GROUP BY problem_id
    /// 
    /// Returns: Best reasoning path with cumulative scores
    /// USE CASE: Multi-path reasoning, explore different solution approaches
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct TreeOfThought : IBinarySerialize
    {
        private class ReasoningNode
        {
            public int StepNumber;
            public float[] Vector;
            public double Confidence;
            public int ParentStep;
            public List<int> Children = new List<int>();
            public double CumulativeScore;
            public double PathDiversity;
        }

        private Dictionary<int, ReasoningNode> nodes;
        private int dimension;

        public void Init()
        {
            nodes = new Dictionary<int, ReasoningNode>();
            dimension = 0;
        }

        public void Accumulate(SqlInt32 stepNumber, SqlString vectorJson, SqlDouble confidence, SqlInt32 parentStep)
        {
            if (stepNumber.IsNull || vectorJson.IsNull || confidence.IsNull)
                return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            int step = stepNumber.Value;
            var node = new ReasoningNode
            {
                StepNumber = step,
                Vector = vec,
                Confidence = confidence.Value,
                ParentStep = parentStep.IsNull ? -1 : parentStep.Value
            };

            nodes[step] = node;

            // Link to parent
            if (node.ParentStep >= 0 && nodes.ContainsKey(node.ParentStep))
            {
                nodes[node.ParentStep].Children.Add(step);
            }
        }

        public void Merge(TreeOfThought other)
        {
            if (other.nodes != null)
            {
                foreach (var kvp in other.nodes)
                {
                    if (!nodes.ContainsKey(kvp.Key))
                        nodes[kvp.Key] = kvp.Value;
                }
            }
        }

        public SqlString Terminate()
        {
            if (nodes.Count == 0 || dimension == 0)
                return SqlString.Null;

            // Build tree structure
            var roots = nodes.Values.Where(n => n.ParentStep < 0).ToList();
            if (roots.Count == 0)
                roots = nodes.Values.Take(1).ToList(); // Fallback

            // Compute cumulative scores via DFS
            void ComputeScores(ReasoningNode node, double parentScore, float[] parentVector)
            {
                // Score = confidence + semantic coherence with parent
                double coherence = 1.0;
                if (parentVector != null)
                {
                    coherence = VectorUtilities.CosineSimilarity(node.Vector, parentVector);
                }

                node.CumulativeScore = parentScore + (node.Confidence * coherence);
                node.PathDiversity = parentVector != null 
                    ? Math.Sqrt(VectorUtilities.EuclideanDistance(node.Vector, parentVector))
                    : 0;

                foreach (int childStep in node.Children)
                {
                    if (nodes.ContainsKey(childStep))
                        ComputeScores(nodes[childStep], node.CumulativeScore, node.Vector);
                }
            }

            foreach (var root in roots)
            {
                ComputeScores(root, 0, null);
            }

            // Find best leaf path (highest cumulative score)
            var leaves = nodes.Values.Where(n => n.Children.Count == 0).ToList();
            var bestLeaf = leaves.OrderByDescending(n => n.CumulativeScore).FirstOrDefault();

            if (bestLeaf == null)
                return SqlString.Null;

            // Trace back best path
            var bestPath = new List<ReasoningNode>();
            var current = bestLeaf;
            while (current != null)
            {
                bestPath.Insert(0, current);
                current = current.ParentStep >= 0 && nodes.ContainsKey(current.ParentStep)
                    ? nodes[current.ParentStep]
                    : null;
            }

            // Build JSON
            var json = "{" +
                $"\"best_path_score\":{bestLeaf.CumulativeScore:G6}," +
                $"\"path_length\":{bestPath.Count}," +
                $"\"total_nodes_explored\":{nodes.Count}," +
                $"\"branching_factor\":{(double)nodes.Count / Math.Max(1, bestPath.Count):G3}," +
                "\"path\":[" +
                string.Join(",",
                    bestPath.Select((n, idx) =>
                        $"{{\"step\":{n.StepNumber}," +
                        $"\"confidence\":{n.Confidence:G6}," +
                        $"\"cumulative_score\":{n.CumulativeScore:G6}," +
                        $"\"diversity\":{n.PathDiversity:G6}}}"
                    )
                ) + "]}";

            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            nodes = new Dictionary<int, ReasoningNode>(count);

            for (int i = 0; i < count; i++)
            {
                var node = new ReasoningNode
                {
                    StepNumber = r.ReadInt32(),
                    Confidence = r.ReadDouble(),
                    ParentStep = r.ReadInt32(),
                    CumulativeScore = r.ReadDouble(),
                    PathDiversity = r.ReadDouble()
                };

                node.Vector = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    node.Vector[j] = r.ReadSingle();

                int childCount = r.ReadInt32();
                for (int j = 0; j < childCount; j++)
                    node.Children.Add(r.ReadInt32());

                nodes[node.StepNumber] = node;
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(nodes.Count);

            foreach (var node in nodes.Values)
            {
                w.Write(node.StepNumber);
                w.Write(node.Confidence);
                w.Write(node.ParentStep);
                w.Write(node.CumulativeScore);
                w.Write(node.PathDiversity);

                foreach (var val in node.Vector)
                    w.Write(val);

                w.Write(node.Children.Count);
                foreach (var child in node.Children)
                    w.Write(child);
            }
        }
    }

    /// <summary>
    /// REFLEXION AGGREGATE
    /// Self-reflection on reasoning quality with iterative improvement
    /// 
    /// SELECT iteration,
    ///        dbo.ReflexionAggregate(attempt_number, reasoning_vector, outcome_score, reflection_text_vector)
    /// FROM reasoning_attempts
    /// GROUP BY iteration
    /// 
    /// Returns: Learning trajectory showing improvement over iterations
    /// USE CASE: Track how model learns from mistakes, self-corrects
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct ReflexionAggregate : IBinarySerialize
    {
        private class ReflexionAttempt
        {
            public int AttemptNumber;
            public float[] ReasoningVector;
            public double OutcomeScore;
            public float[] ReflectionVector;
        }

        private List<ReflexionAttempt> attempts;
        private int dimension;

        public void Init()
        {
            attempts = new List<ReflexionAttempt>();
            dimension = 0;
        }

        public void Accumulate(SqlInt32 attemptNumber, SqlString reasoningJson, SqlDouble outcomeScore, SqlString reflectionJson)
        {
            if (attemptNumber.IsNull || reasoningJson.IsNull || outcomeScore.IsNull)
                return;

            var reasoningVec = VectorUtilities.ParseVectorJson(reasoningJson.Value);
            if (reasoningVec == null) return;

            if (dimension == 0)
                dimension = reasoningVec.Length;
            else if (reasoningVec.Length != dimension)
                return;

            float[] reflectionVec = null;
            if (!reflectionJson.IsNull)
            {
                reflectionVec = VectorUtilities.ParseVectorJson(reflectionJson.Value);
                if (reflectionVec != null && reflectionVec.Length != dimension)
                    reflectionVec = null;
            }

            attempts.Add(new ReflexionAttempt
            {
                AttemptNumber = attemptNumber.Value,
                ReasoningVector = reasoningVec,
                OutcomeScore = outcomeScore.Value,
                ReflectionVector = reflectionVec
            });
        }

        public void Merge(ReflexionAggregate other)
        {
            if (other.attempts != null)
                attempts.AddRange(other.attempts);
        }

        public SqlString Terminate()
        {
            if (attempts.Count == 0 || dimension == 0)
                return SqlString.Null;

            // Sort by attempt number
            attempts.Sort((a, b) => a.AttemptNumber.CompareTo(b.AttemptNumber));

            // Compute learning metrics
            var metrics = new List<object>();
            
            for (int i = 0; i < attempts.Count; i++)
            {
                var attempt = attempts[i];
                
                // Compute divergence from previous attempt
                double divergence = 0;
                if (i > 0)
                {
                    divergence = Math.Sqrt(VectorUtilities.EuclideanDistance(
                        attempt.ReasoningVector,
                        attempts[i - 1].ReasoningVector
                    ));
                }

                // Compute reflection alignment (how well reflection predicted next move)
                double reflectionAlignment = 0;
                if (i > 0 && attempts[i - 1].ReflectionVector != null)
                {
                    reflectionAlignment = VectorUtilities.CosineSimilarity(
                        attempt.ReasoningVector,
                        attempts[i - 1].ReflectionVector
                    );
                }

                // Improvement rate
                double improvement = 0;
                if (i > 0)
                {
                    improvement = attempt.OutcomeScore - attempts[i - 1].OutcomeScore;
                }

                metrics.Add(new
                {
                    attempt = attempt.AttemptNumber,
                    score = attempt.OutcomeScore,
                    improvement = improvement,
                    divergence = divergence,
                    reflection_alignment = reflectionAlignment,
                    has_reflection = attempt.ReflectionVector != null
                });
            }

            // Overall learning statistics
            double avgImprovement = attempts.Count > 1
                ? (attempts.Last().OutcomeScore - attempts.First().OutcomeScore) / (attempts.Count - 1)
                : 0;

            double totalProgress = attempts.Count > 1
                ? attempts.Last().OutcomeScore - attempts.First().OutcomeScore
                : 0;

            var json = "{" +
                $"\"num_attempts\":{attempts.Count}," +
                $"\"initial_score\":{attempts.First().OutcomeScore:G6}," +
                $"\"final_score\":{attempts.Last().OutcomeScore:G6}," +
                $"\"total_improvement\":{totalProgress:G6}," +
                $"\"avg_improvement_per_attempt\":{avgImprovement:G6}," +
                $"\"learning_rate\":{(totalProgress / Math.Max(1, attempts.Count)):G6}," +
                "\"attempts\":[" +
                string.Join(",",
                    metrics.Select(m =>
                    {
                        var dict = m.GetType().GetProperties()
                            .ToDictionary(p => p.Name, p => p.GetValue(m));
                        return "{" + string.Join(",",
                            dict.Select(kvp => $"\"{kvp.Key}\":{FormatValue(kvp.Value)}")
                        ) + "}";
                    })
                ) + "]}";

            return new SqlString(json);
        }

        private static string FormatValue(object val)
        {
            if (val is double d) return d.ToString("G6");
            if (val is bool b) return b.ToString().ToLower();
            return val.ToString();
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            attempts = new List<ReflexionAttempt>(count);

            for (int i = 0; i < count; i++)
            {
                var attempt = new ReflexionAttempt
                {
                    AttemptNumber = r.ReadInt32(),
                    OutcomeScore = r.ReadDouble()
                };

                attempt.ReasoningVector = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    attempt.ReasoningVector[j] = r.ReadSingle();

                bool hasReflection = r.ReadBoolean();
                if (hasReflection)
                {
                    attempt.ReflectionVector = new float[dimension];
                    for (int j = 0; j < dimension; j++)
                        attempt.ReflectionVector[j] = r.ReadSingle();
                }

                attempts.Add(attempt);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(attempts.Count);

            foreach (var attempt in attempts)
            {
                w.Write(attempt.AttemptNumber);
                w.Write(attempt.OutcomeScore);

                foreach (var val in attempt.ReasoningVector)
                    w.Write(val);

                w.Write(attempt.ReflectionVector != null);
                if (attempt.ReflectionVector != null)
                {
                    foreach (var val in attempt.ReflectionVector)
                        w.Write(val);
                }
            }
        }
    }

    /// <summary>
    /// SELF-CONSISTENCY AGGREGATE
    /// Sample multiple reasoning paths and find consensus answer
    /// 
    /// SELECT problem_id,
    ///        dbo.SelfConsistency(reasoning_path_vector, final_answer_vector, confidence)
    /// FROM reasoning_samples
    /// GROUP BY problem_id
    /// 
    /// Returns: Consensus answer based on clustering similar outputs
    /// USE CASE: Majority voting over multiple reasoning attempts
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct SelfConsistency : IBinarySerialize
    {
        private class ReasoningSample
        {
            public float[] PathVector;
            public float[] AnswerVector;
            public double Confidence;
        }

        private List<ReasoningSample> samples;
        private int dimension;

        public void Init()
        {
            samples = new List<ReasoningSample>();
            dimension = 0;
        }

        public void Accumulate(SqlString pathVectorJson, SqlString answerVectorJson, SqlDouble confidence)
        {
            if (pathVectorJson.IsNull || answerVectorJson.IsNull || confidence.IsNull)
                return;

            var pathVec = VectorUtilities.ParseVectorJson(pathVectorJson.Value);
            var answerVec = VectorUtilities.ParseVectorJson(answerVectorJson.Value);
            
            if (pathVec == null || answerVec == null) return;

            if (dimension == 0)
                dimension = pathVec.Length;
            else if (pathVec.Length != dimension || answerVec.Length != dimension)
                return;

            samples.Add(new ReasoningSample
            {
                PathVector = pathVec,
                AnswerVector = answerVec,
                Confidence = confidence.Value
            });
        }

        public void Merge(SelfConsistency other)
        {
            if (other.samples != null)
                samples.AddRange(other.samples);
        }

        public SqlString Terminate()
        {
            if (samples.Count == 0 || dimension == 0)
                return SqlString.Null;

            // Cluster answers to find consensus
            var clusters = new List<(float[] Centroid, List<ReasoningSample> Members, double TotalConfidence)>();

            foreach (var sample in samples)
            {
                // Find closest cluster
                int bestCluster = -1;
                double bestSim = 0.7; // Threshold for same cluster

                for (int i = 0; i < clusters.Count; i++)
                {
                    double sim = VectorUtilities.CosineSimilarity(sample.AnswerVector, clusters[i].Centroid);
                    if (sim > bestSim)
                    {
                        bestSim = sim;
                        bestCluster = i;
                    }
                }

                if (bestCluster >= 0)
                {
                    // Add to existing cluster
                    clusters[bestCluster].Members.Add(sample);
                    clusters[bestCluster] = (
                        clusters[bestCluster].Centroid,
                        clusters[bestCluster].Members,
                        clusters[bestCluster].TotalConfidence + sample.Confidence
                    );
                }
                else
                {
                    // Create new cluster
                    clusters.Add((sample.AnswerVector, new List<ReasoningSample> { sample }, sample.Confidence));
                }
            }

            // Find consensus cluster (highest total confidence)
            var consensusCluster = clusters.OrderByDescending(c => c.TotalConfidence).First();

            // Compute consensus answer (weighted average)
            float[] consensusAnswer = new float[dimension];
            double totalWeight = 0;

            foreach (var sample in consensusCluster.Members)
            {
                double weight = sample.Confidence;
                totalWeight += weight;
                for (int i = 0; i < dimension; i++)
                    consensusAnswer[i] += (float)(sample.AnswerVector[i] * weight);
            }
            for (int i = 0; i < dimension; i++)
                consensusAnswer[i] /= (float)totalWeight;

            // Compute agreement metrics
            double agreementRatio = (double)consensusCluster.Members.Count / samples.Count;
            double avgConfidence = consensusCluster.TotalConfidence / consensusCluster.Members.Count;

            // Measure answer diversity (variance within consensus cluster)
            double diversity = 0;
            foreach (var sample in consensusCluster.Members)
            {
                for (int i = 0; i < dimension; i++)
                {
                    double diff = sample.AnswerVector[i] - consensusAnswer[i];
                    diversity += diff * diff;
                }
            }
            diversity = Math.Sqrt(diversity / (consensusCluster.Members.Count * dimension));

            var json = "{" +
                $"\"consensus_answer\":[{string.Join(",", consensusAnswer.Select(v => v.ToString("G9")))}]," +
                $"\"agreement_ratio\":{agreementRatio:G6}," +
                $"\"num_supporting_samples\":{consensusCluster.Members.Count}," +
                $"\"total_samples\":{samples.Count}," +
                $"\"avg_confidence\":{avgConfidence:G6}," +
                $"\"answer_diversity\":{diversity:G6}," +
                $"\"num_clusters\":{clusters.Count}" +
                "}";

            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            samples = new List<ReasoningSample>(count);

            for (int i = 0; i < count; i++)
            {
                var sample = new ReasoningSample
                {
                    Confidence = r.ReadDouble()
                };

                sample.PathVector = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    sample.PathVector[j] = r.ReadSingle();

                sample.AnswerVector = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    sample.AnswerVector[j] = r.ReadSingle();

                samples.Add(sample);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(samples.Count);

            foreach (var sample in samples)
            {
                w.Write(sample.Confidence);

                foreach (var val in sample.PathVector)
                    w.Write(val);

                foreach (var val in sample.AnswerVector)
                    w.Write(val);
            }
        }
    }

    /// <summary>
    /// CHAIN-OF-THOUGHT COHERENCE AGGREGATE
    /// Measure semantic coherence across reasoning chain
    /// 
    /// SELECT reasoning_chain_id,
    ///        dbo.ChainOfThoughtCoherence(step_order, step_vector)
    /// FROM reasoning_steps
    /// GROUP BY reasoning_chain_id
    /// 
    /// Returns: Coherence score and weak links in reasoning
    /// USE CASE: Validate reasoning quality, find logical gaps
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct ChainOfThoughtCoherence : IBinarySerialize
    {
        private class ReasoningStep
        {
            public int Order;
            public float[] Vector;
        }

        private List<ReasoningStep> steps;
        private int dimension;

        public void Init()
        {
            steps = new List<ReasoningStep>();
            dimension = 0;
        }

        public void Accumulate(SqlInt32 stepOrder, SqlString vectorJson)
        {
            if (stepOrder.IsNull || vectorJson.IsNull)
                return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            steps.Add(new ReasoningStep
            {
                Order = stepOrder.Value,
                Vector = vec
            });
        }

        public void Merge(ChainOfThoughtCoherence other)
        {
            if (other.steps != null)
                steps.AddRange(other.steps);
        }

        public SqlString Terminate()
        {
            if (steps.Count < 2 || dimension == 0)
                return SqlString.Null;

            // Sort by order
            steps.Sort((a, b) => a.Order.CompareTo(b.Order));

            // Compute coherence between consecutive steps
            var coherenceScores = new List<(int FromStep, int ToStep, double Similarity)>();
            double totalCoherence = 0;

            for (int i = 0; i < steps.Count - 1; i++)
            {
                double sim = VectorUtilities.CosineSimilarity(steps[i].Vector, steps[i + 1].Vector);
                coherenceScores.Add((steps[i].Order, steps[i + 1].Order, sim));
                totalCoherence += sim;
            }

            double avgCoherence = totalCoherence / coherenceScores.Count;

            // Find weak links (below average coherence)
            var weakLinks = coherenceScores.Where(c => c.Similarity < avgCoherence * 0.8).ToList();

            // Measure overall chain progression (semantic drift)
            double totalDrift = 0;
            if (steps.Count > 1)
            {
                totalDrift = Math.Sqrt(VectorUtilities.EuclideanDistance(steps.First().Vector, steps.Last().Vector));
            }

            var json = "{" +
                $"\"avg_coherence\":{avgCoherence:G6}," +
                $"\"min_coherence\":{coherenceScores.Min(c => c.Similarity):G6}," +
                $"\"max_coherence\":{coherenceScores.Max(c => c.Similarity):G6}," +
                $"\"total_semantic_drift\":{totalDrift:G6}," +
                $"\"num_steps\":{steps.Count}," +
                $"\"num_weak_links\":{weakLinks.Count}," +
                "\"weak_links\":[" +
                string.Join(",",
                    weakLinks.Select(wl =>
                        $"{{\"from_step\":{wl.FromStep}," +
                        $"\"to_step\":{wl.ToStep}," +
                        $"\"coherence\":{wl.Similarity:G6}}}"
                    )
                ) + "]}";

            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            steps = new List<ReasoningStep>(count);

            for (int i = 0; i < count; i++)
            {
                var step = new ReasoningStep
                {
                    Order = r.ReadInt32()
                };

                step.Vector = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    step.Vector[j] = r.ReadSingle();

                steps.Add(step);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(steps.Count);

            foreach (var step in steps)
            {
                w.Write(step.Order);
                foreach (var val in step.Vector)
                    w.Write(val);
            }
        }
    }
}
