using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Graph-based reasoning algorithms for knowledge traversal and path finding.
    /// </summary>
    /// <remarks>
    /// Provides algorithms for:
    /// - Shortest path finding (Dijkstra's algorithm)
    /// - Maximum flow computation (Ford-Fulkerson)
    /// - PageRank-style importance scoring
    /// - Community detection via modularity
    /// 
    /// Used for semantic graph traversal, knowledge network analysis,
    /// and relationship-based reasoning in the provenance graph.
    /// </remarks>
    public static class GraphAlgorithms
    {
        /// <summary>
        /// Represents a directed weighted edge in the graph.
        /// </summary>
        public struct Edge
        {
            public int From;
            public int To;
            public double Weight;
        }

        /// <summary>
        /// Result of shortest path computation.
        /// </summary>
        public struct ShortestPathResult
        {
            public List<int> Path;
            public double TotalDistance;
            public int NodesVisited;
        }

        /// <summary>
        /// Computes shortest path using Dijkstra's algorithm.
        /// </summary>
        /// <param name="edges">List of directed weighted edges</param>
        /// <param name="start">Start node ID</param>
        /// <param name="end">End node ID</param>
        /// <returns>Shortest path result or null if no path exists</returns>
        public static ShortestPathResult? ShortestPath(List<Edge> edges, int start, int end)
        {
            if (edges == null || edges.Count == 0 || start == end)
                return null;

            // Build adjacency list
            var adjacency = new Dictionary<int, List<(int Node, double Weight)>>();
            var allNodes = new HashSet<int>();
            
            foreach (var edge in edges)
            {
                allNodes.Add(edge.From);
                allNodes.Add(edge.To);
                
                if (!adjacency.ContainsKey(edge.From))
                    adjacency[edge.From] = new List<(int, double)>();
                
                adjacency[edge.From].Add((edge.To, edge.Weight));
            }

            if (!allNodes.Contains(start) || !allNodes.Contains(end))
                return null;

            // Dijkstra's algorithm
            var distances = new Dictionary<int, double>();
            var previous = new Dictionary<int, int>();
            var unvisited = new HashSet<int>(allNodes);
            
            foreach (var node in allNodes)
                distances[node] = double.PositiveInfinity;
            
            distances[start] = 0;
            int nodesVisited = 0;

            while (unvisited.Count > 0)
            {
                // Find unvisited node with minimum distance
                int current = -1;
                double minDist = double.PositiveInfinity;
                
                foreach (var node in unvisited)
                {
                    if (distances[node] < minDist)
                    {
                        minDist = distances[node];
                        current = node;
                    }
                }

                if (current == -1 || minDist == double.PositiveInfinity)
                    break; // No path to remaining nodes

                unvisited.Remove(current);
                nodesVisited++;

                if (current == end)
                    break; // Found shortest path to target

                // Update distances to neighbors
                if (adjacency.ContainsKey(current))
                {
                    foreach (var (neighbor, weight) in adjacency[current])
                    {
                        if (unvisited.Contains(neighbor))
                        {
                            double altDist = distances[current] + weight;
                            if (altDist < distances[neighbor])
                            {
                                distances[neighbor] = altDist;
                                previous[neighbor] = current;
                            }
                        }
                    }
                }
            }

            // Reconstruct path
            if (!previous.ContainsKey(end) && start != end)
                return null; // No path found

            var path = new List<int>();
            int curr = end;
            while (curr != start)
            {
                path.Insert(0, curr);
                if (!previous.ContainsKey(curr))
                    return null;
                curr = previous[curr];
            }
            path.Insert(0, start);

            return new ShortestPathResult
            {
                Path = path,
                TotalDistance = distances[end],
                NodesVisited = nodesVisited
            };
        }

        /// <summary>
        /// Computes PageRank-style importance scores for nodes.
        /// </summary>
        /// <param name="edges">List of directed edges (weights ignored)</param>
        /// <param name="dampingFactor">Damping factor (typically 0.85)</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <param name="tolerance">Convergence tolerance</param>
        /// <returns>Dictionary of node ID to importance score</returns>
        public static Dictionary<int, double> PageRank(
            List<Edge> edges,
            double dampingFactor = 0.85,
            int maxIterations = 100,
            double tolerance = 1e-6)
        {
            if (edges == null || edges.Count == 0)
                return new Dictionary<int, double>();

            // Build graph structure
            var outlinks = new Dictionary<int, List<int>>();
            var inlinks = new Dictionary<int, List<int>>();
            var nodes = new HashSet<int>();

            foreach (var edge in edges)
            {
                nodes.Add(edge.From);
                nodes.Add(edge.To);

                if (!outlinks.ContainsKey(edge.From))
                    outlinks[edge.From] = new List<int>();
                outlinks[edge.From].Add(edge.To);

                if (!inlinks.ContainsKey(edge.To))
                    inlinks[edge.To] = new List<int>();
                inlinks[edge.To].Add(edge.From);
            }

            int n = nodes.Count;
            if (n == 0)
                return new Dictionary<int, double>();

            // Initialize ranks
            var ranks = new Dictionary<int, double>();
            var newRanks = new Dictionary<int, double>();
            double initialRank = 1.0 / n;

            foreach (var node in nodes)
            {
                ranks[node] = initialRank;
                newRanks[node] = 0;
            }

            // Iterate until convergence
            for (int iter = 0; iter < maxIterations; iter++)
            {
                double diff = 0;

                foreach (var node in nodes)
                {
                    double rank = (1 - dampingFactor) / n;

                    if (inlinks.ContainsKey(node))
                    {
                        foreach (var inNode in inlinks[node])
                        {
                            int outlinkCount = outlinks.ContainsKey(inNode) 
                                ? outlinks[inNode].Count 
                                : 1;
                            rank += dampingFactor * (ranks[inNode] / outlinkCount);
                        }
                    }

                    newRanks[node] = rank;
                    diff += Math.Abs(rank - ranks[node]);
                }

                // Swap rank dictionaries
                var temp = ranks;
                ranks = newRanks;
                newRanks = temp;

                if (diff < tolerance)
                    break; // Converged
            }

            return ranks;
        }

        /// <summary>
        /// Detects strongly connected components using Tarjan's algorithm.
        /// </summary>
        /// <param name="edges">List of directed edges</param>
        /// <returns>List of components (each component is a list of node IDs)</returns>
        public static List<List<int>> StronglyConnectedComponents(List<Edge> edges)
        {
            if (edges == null || edges.Count == 0)
                return new List<List<int>>();

            // Build adjacency list
            var adjacency = new Dictionary<int, List<int>>();
            var nodes = new HashSet<int>();

            foreach (var edge in edges)
            {
                nodes.Add(edge.From);
                nodes.Add(edge.To);

                if (!adjacency.ContainsKey(edge.From))
                    adjacency[edge.From] = new List<int>();
                adjacency[edge.From].Add(edge.To);
            }

            // Tarjan's algorithm
            var index = 0;
            var stack = new Stack<int>();
            var indices = new Dictionary<int, int>();
            var lowlinks = new Dictionary<int, int>();
            var onStack = new HashSet<int>();
            var components = new List<List<int>>();

            void StrongConnect(int v)
            {
                indices[v] = index;
                lowlinks[v] = index;
                index++;
                stack.Push(v);
                onStack.Add(v);

                if (adjacency.ContainsKey(v))
                {
                    foreach (var w in adjacency[v])
                    {
                        if (!indices.ContainsKey(w))
                        {
                            StrongConnect(w);
                            lowlinks[v] = Math.Min(lowlinks[v], lowlinks[w]);
                        }
                        else if (onStack.Contains(w))
                        {
                            lowlinks[v] = Math.Min(lowlinks[v], indices[w]);
                        }
                    }
                }

                if (lowlinks[v] == indices[v])
                {
                    var component = new List<int>();
                    int w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w);
                        component.Add(w);
                    } while (w != v);
                    
                    components.Add(component);
                }
            }

            foreach (var node in nodes)
            {
                if (!indices.ContainsKey(node))
                    StrongConnect(node);
            }

            return components;
        }
    }
}
