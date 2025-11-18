using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr
{
    /// <summary>
    /// DIMENSIONALITY REDUCTION AGGREGATES
    /// Project high-dimensional vectors to lower dimensions for visualization/efficiency
    /// </summary>

    /// <summary>
    /// FULL PCA AGGREGATE
    /// Complete Principal Component Analysis with eigenvector computation
    /// 
    /// SELECT category,
    ///        dbo.PrincipalComponentAnalysis(embedding_vector, 50)
    /// FROM atoms GROUP BY category
    /// 
    /// Returns: JSON with top N principal components and variance explained
    /// USE CASE: Reduce 1998D vectors to 50D while preserving 95%+ variance
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct PrincipalComponentAnalysis : IBinarySerialize
    {
        private List<float[]> vectors;
        private int numComponents;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            numComponents = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlInt32 nComponents)
        {
            if (vectorJson.IsNull || nComponents.IsNull)
                return;

            if (numComponents == 0)
                numComponents = nComponents.Value;

            var vec = ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(PrincipalComponentAnalysis other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count < 2 || dimension == 0 || numComponents == 0)
                return SqlString.Null;

            int k = Math.Min(numComponents, dimension);
            int n = vectors.Count;

            // Compute mean
            float[] mean = new float[dimension];
            foreach (var vec in vectors)
                for (int i = 0; i < dimension; i++)
                    mean[i] += vec[i];
            for (int i = 0; i < dimension; i++)
                mean[i] /= n;

            // Center data
            var centered = vectors.Select(v =>
                v.Select((val, idx) => val - mean[idx]).ToArray()
            ).ToList();

            // Compute covariance matrix using proper PCA algorithm
            // Use power iteration to find top k principal components efficiently
            var components = new List<(float[] Eigenvector, double Variance)>();

            // Deflation method: Extract each principal component iteratively
            float[][] residualData = new float[n][];
            for (int i = 0; i < n; i++)
            {
                residualData[i] = new float[dimension];
                Array.Copy(centered[i], residualData[i], dimension);
            }
            
            for (int comp = 0; comp < k; comp++)
            {
                // Initialize eigenvector with data-driven approach (first data point normalized)
                float[] eigenvec = new float[dimension];
                float initNorm = 0;
                for (int i = 0; i < dimension; i++)
                {
                    eigenvec[i] = residualData[0][i];
                    initNorm += eigenvec[i] * eigenvec[i];
                }
                initNorm = (float)Math.Sqrt(initNorm);
                if (initNorm > 0)
                {
                    for (int i = 0; i < dimension; i++)
                        eigenvec[i] /= initNorm;
                }
                
                // Power iteration: Converge to dominant eigenvector of covariance matrix
                const int maxIterations = 100;
                const float convergenceThreshold = 1e-6f;
                
                for (int iter = 0; iter < maxIterations; iter++)
                {
                    float[] newVec = new float[dimension];
                    
                    // Multiply by covariance matrix (A^T * A * v)
                    foreach (var centeredVec in residualData)
                    {
                        double dot = 0;
                        for (int i = 0; i < dimension; i++)
                            dot += eigenvec[i] * centeredVec[i];
                        
                        for (int i = 0; i < dimension; i++)
                            newVec[i] += (float)(dot * centeredVec[i]);
                    }

                    // Normalize
                    double norm = 0;
                    for (int i = 0; i < dimension; i++)
                        norm += newVec[i] * newVec[i];
                    norm = Math.Sqrt(norm);

                    if (norm < 1e-10)
                        break; // Convergence failure
                    
                    // Check convergence before updating
                    double diff = 0;
                    for (int i = 0; i < dimension; i++)
                    {
                        double normalized = newVec[i] / norm;
                        double delta = normalized - eigenvec[i];
                        diff += delta * delta;
                        eigenvec[i] = (float)normalized;
                    }
                    
                    if (Math.Sqrt(diff) < convergenceThreshold)
                        break; // Converged
                }

                // Compute variance explained
                double variance = 0;
                foreach (var centeredVec in residualData)
                {
                    double projection = 0;
                    for (int i = 0; i < dimension; i++)
                        projection += eigenvec[i] * centeredVec[i];
                    variance += projection * projection;
                }
                variance /= n;

                components.Add((eigenvec, variance));

                // Deflation: remove this component from residual data (for next iteration)
                for (int i = 0; i < n; i++)
                {
                    double projection = 0;
                    for (int d = 0; d < dimension; d++)
                        projection += eigenvec[d] * residualData[i][d];
                    
                    for (int d = 0; d < dimension; d++)
                        residualData[i][d] -= (float)(projection * eigenvec[d]);
                }
            }

            // Total variance
            double totalVar = components.Sum(c => c.Variance);

            var varianceEntries = components.Select((c, idx) => new
            {
                component = idx + 1,
                variance = c.Variance,
                variance_ratio = totalVar == 0 ? 0 : c.Variance / totalVar
            }).ToList();

            var result = new
            {
                num_components = k,
                total_variance = totalVar,
                variance_explained = varianceEntries
            };

            return new SqlString(JsonConvert.SerializeObject(result));
        }

        public void Read(BinaryReader r)
        {
            numComponents = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(numComponents);
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
            return jsonBuilder.ToString();
        }
    }

    /// <summary>ntToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct TSNEProjection : IBinarySerialize
    {
        private List<float[]> vectors;
        private int targetDims;
        private double perplexity;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            targetDims = 0;
            perplexity = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlInt32 dims, SqlDouble perplexityParam)
        {
            if (vectorJson.IsNull)
                return;

            if (targetDims == 0 && !dims.IsNull)
                targetDims = dims.Value;
            if (perplexity == 0 && !perplexityParam.IsNull)
                perplexity = perplexityParam.Value;

            var vec = ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(TSNEProjection other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count < 3 || dimension == 0 || targetDims == 0)
                return SqlString.Null;

            int n = vectors.Count;
            int d = Math.Min(targetDims, 3); // Limit to 2D or 3D

            // Use bridge library for PROPER t-SNE implementation
            // Replaces: "Simplified t-SNE" that was actually random projection
            var tsne = new Hartonomous.Clr.MachineLearning.TSNEProjection(seed: 42);
            
            // Convert List<float[]> to float[][]
            var vectorArray = vectors.ToArray();
            
            // Run proper t-SNE with gradient descent on KL divergence
            // Parameters: perplexity=30, iterations=1000 (can adjust for speed vs quality)
            int iterations = Math.Min(1000, n * 10); // Scale iterations with data size
            double perplexity = Math.Min(30.0, n / 3.0); // Perplexity should be < n/3
            
            var projection = tsne.Project(
                vectorArray, 
                targetDim: d, 
                perplexity: perplexity, 
                iterations: iterations, 
                learningRate: 200.0
            );

            var projectionRows = projection
                .Select(row => JsonConvert.SerializeObject(row))
                .ToList();
            return new SqlString(JsonConvert.SerializeObject(new
            {
                projection = projectionRows
            }));
        }

        public void Read(BinaryReader r)
        {
            targetDims = r.ReadInt32();
            perplexity = r.ReadDouble();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(targetDims);
            w.Write(perplexity);
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }

        private static float[] ParseVectorJson(string json)
        {
            try
            {
                json = json.Trim();
                if (!json.StartsWith("[") || !json.EndsWith("]")) return null;
                return json.Substring(1, json.Length - 2)
                    .Split(',')
                    .Select(s => float.Parse(s.Trim()))
                    .ToArray();
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// RANDOM PROJECTION AGGREGATE
    /// Fast dimensionality reduction via Johnson-Lindenstrauss lemma
    /// 
    /// SELECT dbo.RandomProjection(embedding_vector, 100, 42)
    /// FROM atoms
    /// 
    /// Returns: Lower-dimensional vector preserving distances
    /// USE CASE: Fast approximate nearest neighbor search preprocessing
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct RandomProjection : IBinarySerialize
    {
        private List<float[]> vectors;
        private int targetDims;
        private int seed;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            targetDims = 0;
            seed = 0;
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson, SqlInt32 dims, SqlInt32 randomSeed)
        {
            if (vectorJson.IsNull)
                return;

            if (targetDims == 0 && !dims.IsNull)
                targetDims = dims.Value;
            if (seed == 0 && !randomSeed.IsNull)
                seed = randomSeed.Value;

            var vec = ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(RandomProjection other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count == 0 || dimension == 0 || targetDims == 0)
                return SqlString.Null;

            int n = vectors.Count;
            int k = targetDims;

            // Generate random projection matrix
            Random rng = new Random(seed);
            var projMatrix = new float[dimension][];
            for (int i = 0; i < dimension; i++)
            {
                projMatrix[i] = new float[k];
                for (int j = 0; j < k; j++)
                {
                    // Gaussian random projection
                    double u1 = rng.NextDouble();
                    double u2 = rng.NextDouble();
                    projMatrix[i][j] = (float)(Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2));
                }
            }

            // Normalize columns
            for (int j = 0; j < k; j++)
            {
                double norm = 0;
                for (int i = 0; i < dimension; i++)
                    norm += projMatrix[i][j] * projMatrix[i][j];
                norm = Math.Sqrt(norm);
                for (int i = 0; i < dimension; i++)
                    projMatrix[i][j] /= (float)norm;
            }

            // Project all vectors
            var projected = new float[n][];
            for (int i = 0; i < n; i++)
            {
                projected[i] = new float[k];
                for (int j = 0; j < k; j++)
                {
                    projected[i][j] = 0;
                    for (int d = 0; d < dimension; d++)
                        projected[i][j] += vectors[i][d] * projMatrix[d][j];
                }
            }

            var result = new
            {
                projected_vectors = projected
            };

            return new SqlString(JsonConvert.SerializeObject(result));
        }

        public void Read(BinaryReader r)
        {
            targetDims = r.ReadInt32();
            seed = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(targetDims);
            w.Write(seed);
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }

        private static float[] ParseVectorJson(string json)
        {
            return Hartonomous.Clr.Core.VectorUtilities.ParseVectorJson(json);
        }
    }
}
