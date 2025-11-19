using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Tree-of-Thought reasoning algorithm.
    /// Explores multiple reasoning paths, evaluates branches, and selects optimal solution path.
    /// </summary>
    /// <remarks>
    /// Implements the Tree-of-Thought (ToT) prompting strategy where the model explores
    /// multiple reasoning branches in parallel, evaluates each path's quality, and selects
    /// the most promising branch. Each node represents a reasoning step with semantic embeddings.
    /// 
    /// Path evaluation combines:
    /// - Confidence scores at each step
    /// - Semantic coherence with parent (cosine similarity)
    /// - Path diversity (Euclidean distance for exploration breadth)
    /// 
    /// Returns the highest-scoring complete path from root to leaf.
    /// </remarks>
    public static class TreeOfThought
    {
        /// <summary>
        /// Represents a single node in the reasoning tree.
        /// </summary>
        public class ReasoningNode
        {
            public int StepNumber { get; set; }
            public float[] Vector { get; set; } = Array.Empty<float>();
            public double Confidence { get; set; }
            public int ParentStep { get; set; }
            public List<int> Children { get; set; } = new List<int>();
            public double CumulativeScore { get; set; }
            public double PathDiversity { get; set; }
        }

        /// <summary>
        /// Result of tree-of-thought exploration.
        /// </summary>
        public struct ToTResult
        {
            public double BestPathScore;
            public int PathLength;
            public int TotalNodesExplored;
            public double BranchingFactor;
            public List<(int Step, double Confidence, double CumulativeScore, double Diversity)> Path;
        }

        /// <summary>
        /// Explores reasoning tree and returns best path.
        /// </summary>
        /// <param name="nodes">Dictionary of step numbers to reasoning nodes</param>
        /// <param name="cosineSimilarity">Function to compute cosine similarity between vectors</param>
        /// <param name="euclideanDistance">Function to compute Euclidean distance between vectors</param>
        /// <returns>ToT result with best path and metrics</returns>
        public static ToTResult? FindBestPath(
            Dictionary<int, ReasoningNode> nodes,
            Func<float[], float[], double> cosineSimilarity,
            Func<float[], float[], double> euclideanDistance)
        {
            if (nodes == null || nodes.Count == 0)
                return null;

            // Find root nodes (no parent)
            var roots = nodes.Values.Where(n => n.ParentStep < 0).ToList();
            if (roots.Count == 0)
                roots = nodes.Values.Take(1).ToList(); // Fallback to first node

            // Compute cumulative scores via depth-first traversal
            void ComputeScores(ReasoningNode node, double parentScore, float[]? parentVector)
            {
                // Score = confidence + semantic coherence with parent
                double coherence = 1.0;
                if (parentVector != null && node.Vector.Length == parentVector.Length)
                {
                    coherence = cosineSimilarity(node.Vector, parentVector);
                }

                node.CumulativeScore = parentScore + (node.Confidence * coherence);
                
                // Measure path diversity (exploration breadth)
                node.PathDiversity = parentVector != null && node.Vector.Length == parentVector.Length
                    ? Math.Sqrt(euclideanDistance(node.Vector, parentVector))
                    : 0;

                // Recursively compute scores for children
                foreach (int childStep in node.Children)
                {
                    if (nodes.ContainsKey(childStep))
                        ComputeScores(nodes[childStep], node.CumulativeScore, node.Vector);
                }
            }

            // Start DFS from each root
            foreach (var root in roots)
            {
                ComputeScores(root, 0, null);
            }

            // Find best leaf path (highest cumulative score)
            var leaves = nodes.Values.Where(n => n.Children.Count == 0).ToList();
            var bestLeaf = leaves.OrderByDescending(n => n.CumulativeScore).FirstOrDefault();

            if (bestLeaf == null)
                return null;

            // Trace back best path from leaf to root
            var bestPath = new List<ReasoningNode>();
            var current = bestLeaf;
            while (current != null)
            {
                bestPath.Insert(0, current);
                current = current.ParentStep >= 0 && nodes.ContainsKey(current.ParentStep)
                    ? nodes[current.ParentStep]
                    : null;
            }

            return new ToTResult
            {
                BestPathScore = bestLeaf.CumulativeScore,
                PathLength = bestPath.Count,
                TotalNodesExplored = nodes.Count,
                BranchingFactor = (double)nodes.Count / Math.Max(1, bestPath.Count),
                Path = bestPath.Select(n => (
                    n.StepNumber,
                    n.Confidence,
                    n.CumulativeScore,
                    n.PathDiversity
                )).ToList()
            };
        }

        /// <summary>
        /// Prunes unpromising branches based on score threshold.
        /// </summary>
        /// <param name="nodes">Dictionary of reasoning nodes</param>
        /// <param name="scoreThreshold">Minimum cumulative score to keep branch</param>
        /// <returns>Pruned node dictionary</returns>
        public static Dictionary<int, ReasoningNode> PruneBranches(
            Dictionary<int, ReasoningNode> nodes,
            double scoreThreshold)
        {
            if (nodes == null || nodes.Count == 0)
                return new Dictionary<int, ReasoningNode>();

            var keepers = new HashSet<int>();
            
            // Find all nodes above threshold
            foreach (var node in nodes.Values)
            {
                if (node.CumulativeScore >= scoreThreshold)
                {
                    keepers.Add(node.StepNumber);
                    
                    // Keep path to root
                    var current = node;
                    while (current != null && current.ParentStep >= 0)
                    {
                        keepers.Add(current.ParentStep);
                        current = nodes.ContainsKey(current.ParentStep) 
                            ? nodes[current.ParentStep] 
                            : null;
                    }
                }
            }

            return nodes.Where(kvp => keepers.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
