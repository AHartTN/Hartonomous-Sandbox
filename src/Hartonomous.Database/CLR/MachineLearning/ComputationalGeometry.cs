using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Computational geometry algorithms with configurable distance metrics.
    /// 
    /// UNIVERSAL MATHEMATICAL SUBSTRATE:
    /// These algorithms power spatial reasoning, pathfinding, clustering, generation,
    /// and inference across ALL modalities (text, image, audio, video, code, weights).
    /// 
    /// The user's landmark-based 3D GEOMETRY projection unifies all high-dimensional
    /// data into a common spatial substrate. These algorithms make that space queryable,
    /// navigable, and generative.
    /// 
    /// Applications:
    /// - A* pathfinding: Semantic navigation through concept space
    /// - Voronoi diagrams: Territory partitioning for multi-model inference
    /// - Convex hull: Boundary detection for concept domains
    /// - Delaunay triangulation: Mesh generation for continuous synthesis
    /// - Point-in-polygon: Concept membership testing
    /// - Nearest neighbor: Foundation for generation and retrieval
    /// 
    /// Each algorithm respects configurable distance metrics, enabling:
    /// - Euclidean for spatial GEOMETRY data
    /// - Cosine for semantic similarity in projected space
    /// - Custom metrics for domain-specific reasoning
    /// </summary>
    public static class ComputationalGeometry
    {
        #region A* Pathfinding

        /// <summary>
        /// A* pathfinding in vector space with configurable distance metric.
        /// 
        /// Finds optimal path from start to goal through high-dimensional space.
        /// Heuristic = distance estimate to goal using configured metric.
        /// 
        /// Applications:
        /// - Semantic navigation: Path from concept A to concept B
        /// - Generation: Intermediate frames between keyframes
        /// - Reasoning: Logical inference chains through belief space
        /// - Planning: Action sequences through state space
        /// </summary>
        /// <param name="start">Starting vector</param>
        /// <param name="goal">Goal vector</param>
        /// <param name="points">All available waypoints in space</param>
        /// <param name="maxNeighbors">Max neighbors to consider per point (for connectivity)</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Sequence of indices forming path from start to goal, or empty if no path</returns>
        public static int[] AStar(float[] start, float[] goal, float[][] points, int maxNeighbors = 10, IDistanceMetric? metric = null)
        {
            metric = metric ?? new EuclideanDistance();

            // Add start and goal to point set
            var allPoints = new List<float[]>(points) { start, goal };
            int startIdx = allPoints.Count - 2;
            int goalIdx = allPoints.Count - 1;

            // Priority queue: (fScore, nodeIdx)
            var openSet = new SortedSet<(double fScore, int nodeIdx)>(Comparer<(double, int)>.Create((a, b) =>
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                return cmp != 0 ? cmp : a.Item2.CompareTo(b.Item2);
            }));

            var gScore = new Dictionary<int, double>();
            var cameFrom = new Dictionary<int, int>();

            for (int i = 0; i < allPoints.Count; i++)
                gScore[i] = double.PositiveInfinity;

            gScore[startIdx] = 0;
            double h = metric.Distance(start, goal);
            openSet.Add((h, startIdx));

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);
                int currentIdx = current.nodeIdx;

                if (currentIdx == goalIdx)
                    return ReconstructPath(cameFrom, currentIdx, startIdx);

                // Find nearest neighbors as potential next steps
                var neighbors = GetNearestNeighbors(allPoints, currentIdx, maxNeighbors, metric);

                foreach (int neighborIdx in neighbors)
                {
                    double tentativeGScore = gScore[currentIdx] + metric.Distance(allPoints[currentIdx], allPoints[neighborIdx]);

                    if (tentativeGScore < gScore[neighborIdx])
                    {
                        cameFrom[neighborIdx] = currentIdx;
                        gScore[neighborIdx] = tentativeGScore;
                        double fScore = tentativeGScore + metric.Distance(allPoints[neighborIdx], goal);

                        // Remove old entry if exists, add new one
                        openSet.RemoveWhere(x => x.nodeIdx == neighborIdx);
                        openSet.Add((fScore, neighborIdx));
                    }
                }
            }

            return Array.Empty<int>(); // No path found
        }

        private static int[] ReconstructPath(Dictionary<int, int> cameFrom, int current, int start)
        {
            var path = new List<int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
                if (current == start)
                    break;
            }
            path.Reverse();
            return path.ToArray();
        }

        private static int[] GetNearestNeighbors(List<float[]> points, int pointIdx, int k, IDistanceMetric metric)
        {
            var distances = new List<(int Index, double Distance)>();
            for (int i = 0; i < points.Count; i++)
            {
                if (i != pointIdx)
                {
                    double dist = metric.Distance(points[pointIdx], points[i]);
                    distances.Add((i, dist));
                }
            }

            return distances.OrderBy(x => x.Distance).Take(k).Select(x => x.Index).ToArray();
        }

        #endregion

        #region Convex Hull (Gift Wrapping)

        /// <summary>
        /// Compute 2D convex hull using Jarvis march (gift wrapping) algorithm.
        /// 
        /// Works on the first 2 dimensions of vectors. Useful for boundary detection
        /// in projected 2D space (e.g., first 2 principal components).
        /// 
        /// Applications:
        /// - Concept boundary detection
        /// - Outlier identification (points outside hull)
        /// - Shape analysis in embedding space
        /// </summary>
        /// <param name="points">2D points (only first 2 dimensions used)</param>
        /// <returns>Indices of points forming convex hull in counter-clockwise order</returns>
        public static int[] ConvexHull2D(float[][] points)
        {
            if (points == null || points.Length < 3)
                return Array.Empty<int>();

            // Find leftmost point (start of hull)
            int leftmost = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i][0] < points[leftmost][0] ||
                    (points[i][0] == points[leftmost][0] && points[i][1] < points[leftmost][1]))
                    leftmost = i;
            }

            var hull = new List<int>();
            int current = leftmost;

            do
            {
                hull.Add(current);
                int next = 0;

                // Find most counter-clockwise point from current
                for (int i = 0; i < points.Length; i++)
                {
                    if (i == current) continue;

                    if (next == current)
                    {
                        next = i;
                    }
                    else
                    {
                        double cross = CrossProduct2D(
                            points[current], points[next], points[i]);

                        if (cross > 0 || (cross == 0 &&
                            Distance2D(points[current], points[i]) >
                            Distance2D(points[current], points[next])))
                        {
                            next = i;
                        }
                    }
                }

                current = next;
            } while (current != leftmost);

            return hull.ToArray();
        }

        private static double CrossProduct2D(float[] o, float[] a, float[] b)
        {
            return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
        }

        private static double Distance2D(float[] a, float[] b)
        {
            double dx = a[0] - b[0];
            double dy = a[1] - b[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        #region Point-in-Polygon Test

        /// <summary>
        /// Test if a point is inside a 2D polygon using ray casting algorithm.
        /// 
        /// Works on first 2 dimensions. Useful for concept membership testing
        /// in projected space.
        /// 
        /// Applications:
        /// - Is this embedding in concept domain X?
        /// - Is this generation within safe boundaries?
        /// - Territory/cluster membership testing
        /// </summary>
        /// <param name="point">Test point (2D)</param>
        /// <param name="polygon">Polygon vertices in order (2D)</param>
        /// <returns>True if point is inside polygon</returns>
        public static bool PointInPolygon2D(float[] point, float[][] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            bool inside = false;
            int j = polygon.Length - 1;

            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i][1] > point[1]) != (polygon[j][1] > point[1]) &&
                    point[0] < (polygon[j][0] - polygon[i][0]) * (point[1] - polygon[i][1]) /
                               (polygon[j][1] - polygon[i][1]) + polygon[i][0])
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        #endregion

        #region K-Nearest Neighbors

        /// <summary>
        /// Find k nearest neighbors with configurable distance metric.
        /// 
        /// FOUNDATION FOR EVERYTHING:
        /// - Generation: Sample from k nearest atoms
        /// - Retrieval: Return k nearest matches
        /// - Inference: Distance-weighted voting
        /// - Clustering: Local density estimation
        /// - Reasoning: Analogical reasoning via neighbors
        /// </summary>
        /// <param name="query">Query vector</param>
        /// <param name="data">Dataset to search</param>
        /// <param name="k">Number of neighbors</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Indices and distances of k nearest neighbors</returns>
        public static (int Index, double Distance)[] KNearestNeighbors(
            float[] query, float[][] data, int k, IDistanceMetric? metric = null)
        {
            metric = metric ?? new EuclideanDistance();

            var distances = new List<(int Index, double Distance)>();
            for (int i = 0; i < data.Length; i++)
            {
                double dist = metric.Distance(query, data[i]);
                distances.Add((i, dist));
            }

            return distances.OrderBy(x => x.Distance).Take(k).ToArray();
        }

        #endregion

        #region Distance to Line Segment

        /// <summary>
        /// Compute minimum distance from point to line segment in N-dimensional space.
        /// 
        /// Applications:
        /// - Path deviation measurement
        /// - Projection onto trajectory
        /// - Interpolation distance estimation
        /// </summary>
        /// <param name="point">Test point</param>
        /// <param name="lineStart">Line segment start</param>
        /// <param name="lineEnd">Line segment end</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Minimum distance from point to line segment</returns>
        public static double DistanceToLineSegment(
            float[] point, float[] lineStart, float[] lineEnd, IDistanceMetric? metric = null)
        {
            metric = metric ?? new EuclideanDistance();

            // Vector from line start to point
            var ap = Subtract(point, lineStart);
            // Vector from line start to end
            var ab = Subtract(lineEnd, lineStart);

            double abDotAb = Dot(ab, ab);
            if (abDotAb < 1e-10)
                return metric.Distance(point, lineStart); // Degenerate line

            double t = Math.Max(0, Math.Min(1, Dot(ap, ab) / abDotAb));

            // Closest point on segment
            var closest = new float[lineStart.Length];
            for (int i = 0; i < lineStart.Length; i++)
                closest[i] = lineStart[i] + (float)t * ab[i];

            return metric.Distance(point, closest);
        }

        private static float[] Subtract(float[] a, float[] b)
        {
            var result = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
                result[i] = a[i] - b[i];
            return result;
        }

        private static double Dot(float[] a, float[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * b[i];
            return sum;
        }

        #endregion

        #region Voronoi Diagrams

        /// <summary>
        /// Compute Voronoi cell membership for query points given site points.
        /// 
        /// APPLICATIONS IN USER'S ARCHITECTURE:
        /// - Multi-model inference: Which model owns this semantic region?
        /// - Territory partitioning: Divide concept space by landmark ownership
        /// - Clustering validation: Voronoi cells = natural cluster boundaries
        /// - Spatial indexing: Nearest-site lookup for fast retrieval
        /// 
        /// For each query point, returns the index of the closest site (Voronoi cell ID).
        /// This is the discrete Voronoi diagram - assigning points to nearest sites.
        /// </summary>
        /// <param name="queryPoints">Points to classify into Voronoi cells</param>
        /// <param name="sites">Site points defining Voronoi regions</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Array of site indices, one per query point</returns>
        public static int[] VoronoiCellMembership(
            float[][] queryPoints, float[][] sites, IDistanceMetric? metric = null)
        {
            metric = metric ?? new EuclideanDistance();

            var cellIds = new int[queryPoints.Length];
            for (int i = 0; i < queryPoints.Length; i++)
            {
                double minDist = double.MaxValue;
                int closestSite = 0;

                for (int j = 0; j < sites.Length; j++)
                {
                    double dist = metric.Distance(queryPoints[i], sites[j]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestSite = j;
                    }
                }

                cellIds[i] = closestSite;
            }

            return cellIds;
        }

        /// <summary>
        /// Compute distance from each query point to its Voronoi cell boundary.
        /// 
        /// APPLICATIONS:
        /// - Confidence estimation: Points far from boundaries = high confidence in cell assignment
        /// - Boundary detection: Points near boundaries = potential cluster edges
        /// - Uncertainty quantification: Distance to boundary = certainty of classification
        /// </summary>
        /// <param name="queryPoints">Points to analyze</param>
        /// <param name="sites">Site points defining Voronoi regions</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Distance to nearest Voronoi boundary for each point</returns>
        public static double[] VoronoiBoundaryDistance(
            float[][] queryPoints, float[][] sites, IDistanceMetric? metric = null)
        {
            metric = metric ?? new EuclideanDistance();

            var boundaryDists = new double[queryPoints.Length];
            for (int i = 0; i < queryPoints.Length; i++)
            {
                // Find two nearest sites
                var distances = new List<(int SiteIdx, double Dist)>();
                for (int j = 0; j < sites.Length; j++)
                {
                    double dist = metric.Distance(queryPoints[i], sites[j]);
                    distances.Add((j, dist));
                }

                var sorted = distances.OrderBy(x => x.Dist).Take(2).ToArray();
                if (sorted.Length < 2)
                {
                    boundaryDists[i] = double.MaxValue; // Only one site
                }
                else
                {
                    // Boundary distance ≈ half the difference between nearest and second-nearest
                    boundaryDists[i] = (sorted[1].Dist - sorted[0].Dist) / 2.0;
                }
            }

            return boundaryDists;
        }

        #endregion

        #region Delaunay Triangulation

        /// <summary>
        /// Compute 2D Delaunay triangulation using Bowyer-Watson algorithm.
        /// 
        /// APPLICATIONS IN USER'S ARCHITECTURE:
        /// - Continuous generation: Mesh for smooth interpolation between atoms
        /// - Synthesis framework: Structural relationships in embedding space
        /// - Path planning: Natural navigation graph through concept space
        /// - Texture generation: Mesh-based blending of visual features
        /// 
        /// Returns triangles as triplets of point indices. Uses first 2 dimensions only.
        /// </summary>
        /// <param name="points">2D points (only first 2 dims used)</param>
        /// <returns>Array of triangles, each is [idx0, idx1, idx2]</returns>
        public static int[][] DelaunayTriangulation2D(float[][] points)
        {
            if (points.Length < 3)
                return new int[0][];

            // Bowyer-Watson algorithm
            var triangles = new List<Triangle>();

            // Create super-triangle that contains all points
            var bounds = GetBounds2D(points);
            float cx = (bounds.MinX + bounds.MaxX) / 2;
            float cy = (bounds.MinY + bounds.MaxY) / 2;
            float size = Math.Max(bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY) * 2;

            var superTriangle = new Triangle(
                new float[] { cx - size, cy - size },
                new float[] { cx + size, cy - size },
                new float[] { cx, cy + size * 1.732f },
                -1, -2, -3 // Negative indices for super-triangle vertices
            );
            triangles.Add(superTriangle);

            // Add points one at a time
            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var badTriangles = new List<Triangle>();

                // Find triangles whose circumcircle contains the point
                foreach (var tri in triangles)
                {
                    if (tri.InCircumcircle(point))
                        badTriangles.Add(tri);
                }

                // Find polygon boundary (edges not shared by bad triangles)
                var polygon = new List<(int, int)>();
                foreach (var tri in badTriangles)
                {
                    var edges = tri.GetEdges();
                    foreach (var edge in edges)
                    {
                        bool isShared = false;
                        foreach (var other in badTriangles)
                        {
                            if (other == tri) continue;
                            if (other.HasEdge(edge.Item1, edge.Item2))
                            {
                                isShared = true;
                                break;
                            }
                        }
                        if (!isShared)
                            polygon.Add(edge);
                    }
                }

                // Remove bad triangles
                foreach (var tri in badTriangles)
                    triangles.Remove(tri);

                // Add new triangles from polygon edges to new point
                foreach (var edge in polygon)
                {
                    triangles.Add(new Triangle(
                        points[edge.Item1],
                        points[edge.Item2],
                        point,
                        edge.Item1, edge.Item2, i
                    ));
                }
            }

            // Remove triangles connected to super-triangle
            triangles.RemoveAll(tri => tri.Idx0 < 0 || tri.Idx1 < 0 || tri.Idx2 < 0);

            // Convert to index array
            var result = new int[triangles.Count][];
            for (int i = 0; i < triangles.Count; i++)
            {
                result[i] = new int[] { triangles[i].Idx0, triangles[i].Idx1, triangles[i].Idx2 };
            }

            return result;
        }

        private class Triangle
        {
            public float[] V0, V1, V2;
            public int Idx0, Idx1, Idx2;
            private float[] circumcenter = null!;
            private double circumradiusSq;

            public Triangle(float[] v0, float[] v1, float[] v2, int idx0, int idx1, int idx2)
            {
                V0 = v0; V1 = v1; V2 = v2;
                Idx0 = idx0; Idx1 = idx1; Idx2 = idx2;
                ComputeCircumcircle();
            }

            private void ComputeCircumcircle()
            {
                // Use 2D coordinates only
                double ax = V0[0], ay = V0[1];
                double bx = V1[0], by = V1[1];
                double cx = V2[0], cy = V2[1];

                double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
                if (Math.Abs(d) < 1e-10)
                {
                    circumcenter = new float[] { 0, 0 };
                    circumradiusSq = double.MaxValue;
                    return;
                }

                double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
                double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

                circumcenter = new float[] { (float)ux, (float)uy };
                double dx = ux - ax;
                double dy = uy - ay;
                circumradiusSq = dx * dx + dy * dy;
            }

            public bool InCircumcircle(float[] point)
            {
                double dx = point[0] - circumcenter[0];
                double dy = point[1] - circumcenter[1];
                return (dx * dx + dy * dy) <= circumradiusSq;
            }

            public (int, int)[] GetEdges()
            {
                return new[] { (Idx0, Idx1), (Idx1, Idx2), (Idx2, Idx0) };
            }

            public bool HasEdge(int a, int b)
            {
                return (Idx0 == a && Idx1 == b) || (Idx1 == a && Idx0 == b) ||
                       (Idx1 == a && Idx2 == b) || (Idx2 == a && Idx1 == b) ||
                       (Idx2 == a && Idx0 == b) || (Idx0 == a && Idx2 == b);
            }
        }

        private static (float MinX, float MaxX, float MinY, float MaxY) GetBounds2D(float[][] points)
        {
            float minX = points[0][0], maxX = points[0][0];
            float minY = points[0][1], maxY = points[0][1];

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i][0] < minX) minX = points[i][0];
                if (points[i][0] > maxX) maxX = points[i][0];
                if (points[i][1] < minY) minY = points[i][1];
                if (points[i][1] > maxY) maxY = points[i][1];
            }

            return (minX, maxX, minY, maxY);
        }

        #endregion
    }
}
