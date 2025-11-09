using System;
using System.Linq;
using SqlClrFunctions.Core;

namespace SqlClrFunctions.MachineLearning
{
    /// <summary>
    /// Proper t-SNE implementation with gradient descent on KL divergence.
    /// REPLACES: DimensionalityReductionAggregates.cs:313-350 (fake "simplified t-SNE" that was actually random projection)
    /// SQL CLR does not support SIMD, so all operations use simple float[] loops.
    /// </summary>
    public class TSNEProjection
    {
        private readonly Random _random;

        public TSNEProjection(int seed = 42)
        {
            _random = new Random(seed);
        }

        /// <summary>
        /// Reduce high-dimensional vectors to target dimensions using proper t-SNE algorithm.
        /// Algorithm: Gradient descent on KL divergence between high-dim and low-dim similarities.
        /// </summary>
        /// <param name="vectors">High-dimensional input vectors (N x D)</param>
        /// <param name="targetDim">Target dimensionality (typically 2 or 3)</param>
        /// <param name="perplexity">Perplexity parameter (typical: 5-50)</param>
        /// <param name="iterations">Number of gradient descent iterations (typical: 1000)</param>
        /// <param name="learningRate">Learning rate for gradient descent</param>
        /// <returns>Low-dimensional embedding (N x targetDim)</returns>
        public float[][] Project(float[][] vectors, int targetDim = 2, double perplexity = 30.0, int iterations = 1000, double learningRate = 200.0)
        {
            if (vectors == null || vectors.Length == 0)
                throw new ArgumentException("Vectors cannot be empty", nameof(vectors));

            int N = vectors.Length;
            int D = vectors[0].Length;

            // Step 1: Compute pairwise distances in high-dimensional space
            var distances = ComputePairwiseDistances(vectors);

            // Step 2: Compute conditional probabilities P_j|i with Gaussian kernel
            var P = ComputeConditionalProbabilities(distances, perplexity);

            // Step 3: Symmetrize to get joint probabilities P_ij
            var P_symmetric = SymmetrizeP(P);

            // Step 4: Initialize low-dimensional embedding randomly
            var Y = InitializeRandomEmbedding(N, targetDim);

            // Step 5: Gradient descent on KL divergence
            var momentum = new float[N][];
            for (int i = 0; i < N; i++)
            {
                momentum[i] = new float[targetDim];
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                // Compute Q (Student-t similarities in low-dim space)
                var Q = ComputeQMatrix(Y);

                // Compute gradient of KL divergence
                var gradient = ComputeGradient(P_symmetric, Q, Y);

                // Update Y with momentum
                double currentMomentum = iter < 250 ? 0.5 : 0.8;
                for (int i = 0; i < N; i++)
                {
                    for (int d = 0; d < targetDim; d++)
                    {
                        momentum[i][d] = (float)(currentMomentum * momentum[i][d] - learningRate * gradient[i][d]);
                        Y[i][d] += momentum[i][d];
                    }
                }

                // Early exaggeration for first iterations
                if (iter == 100)
                {
                    learningRate /= 4.0; // Reduce learning rate after early exaggeration
                }
            }

            return Y;
        }

        private float[][] ComputePairwiseDistances(float[][] vectors)
        {
            int N = vectors.Length;
            var distances = new float[N][];
            
            for (int i = 0; i < N; i++)
            {
                distances[i] = new float[N];
                for (int j = 0; j < N; j++)
                {
                    if (i != j)
                    {
                        distances[i][j] = EuclideanDistance(vectors[i], vectors[j]);
                    }
                }
            }

            return distances;
        }

        private float[][] ComputeConditionalProbabilities(float[][] distances, double perplexity)
        {
            int N = distances.Length;
            var P = new float[N][];
            double targetEntropy = Math.Log(perplexity);

            for (int i = 0; i < N; i++)
            {
                P[i] = new float[N];
                
                // Binary search for sigma_i that produces desired perplexity
                double beta = FindBeta(distances[i], i, targetEntropy);

                // Compute probabilities with found beta
                double sum = 0;
                for (int j = 0; j < N; j++)
                {
                    if (i != j)
                    {
                        P[i][j] = (float)Math.Exp(-distances[i][j] * distances[i][j] * beta);
                        sum += P[i][j];
                    }
                }

                // Normalize
                for (int j = 0; j < N; j++)
                {
                    if (i != j)
                    {
                        P[i][j] /= (float)sum;
                    }
                }
            }

            return P;
        }

        private double FindBeta(float[] distancesRow, int i, double targetEntropy)
        {
            double betaMin = -double.MaxValue;
            double betaMax = double.MaxValue;
            double beta = 1.0;
            const int maxIter = 50;
            const double tolerance = 1e-5;

            for (int iter = 0; iter < maxIter; iter++)
            {
                // Compute probabilities with current beta
                double sum = 0;
                double entropy = 0;

                for (int j = 0; j < distancesRow.Length; j++)
                {
                    if (i != j)
                    {
                        double p = Math.Exp(-distancesRow[j] * distancesRow[j] * beta);
                        sum += p;
                    }
                }

                for (int j = 0; j < distancesRow.Length; j++)
                {
                    if (i != j)
                    {
                        double p = Math.Exp(-distancesRow[j] * distancesRow[j] * beta) / sum;
                        if (p > 1e-12)
                        {
                            entropy -= p * Math.Log(p);
                        }
                    }
                }

                // Check convergence
                double entropyDiff = entropy - targetEntropy;
                if (Math.Abs(entropyDiff) < tolerance)
                {
                    break;
                }

                // Binary search adjustment
                if (entropyDiff > 0)
                {
                    betaMin = beta;
                    beta = (betaMax == double.MaxValue) ? beta * 2 : (beta + betaMax) / 2;
                }
                else
                {
                    betaMax = beta;
                    beta = (betaMin == -double.MaxValue) ? beta / 2 : (beta + betaMin) / 2;
                }
            }

            return beta;
        }

        private float[][] SymmetrizeP(float[][] P)
        {
            int N = P.Length;
            var P_symmetric = new float[N][];
            
            for (int i = 0; i < N; i++)
            {
                P_symmetric[i] = new float[N];
                for (int j = 0; j < N; j++)
                {
                    P_symmetric[i][j] = (P[i][j] + P[j][i]) / (2.0f * N);
                }
            }

            return P_symmetric;
        }

        private float[][] InitializeRandomEmbedding(int N, int targetDim)
        {
            var Y = new float[N][];
            for (int i = 0; i < N; i++)
            {
                Y[i] = new float[targetDim];
                for (int d = 0; d < targetDim; d++)
                {
                    Y[i][d] = (float)(_random.NextDouble() * 0.0001); // Small random initialization
                }
            }
            return Y;
        }

        private float[][] ComputeQMatrix(float[][] Y)
        {
            int N = Y.Length;
            var Q = new float[N][];
            double sum = 0;

            // Compute numerators (Student-t kernel with df=1)
            for (int i = 0; i < N; i++)
            {
                Q[i] = new float[N];
                for (int j = 0; j < N; j++)
                {
                    if (i != j)
                    {
                        float dist = EuclideanDistance(Y[i], Y[j]);
                        Q[i][j] = 1.0f / (1.0f + dist * dist);
                        sum += Q[i][j];
                    }
                }
            }

            // Normalize
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    Q[i][j] = (float)(Q[i][j] / Math.Max(sum, 1e-12));
                }
            }

            return Q;
        }

        private float[][] ComputeGradient(float[][] P, float[][] Q, float[][] Y)
        {
            int N = Y.Length;
            int targetDim = Y[0].Length;
            var gradient = new float[N][];

            for (int i = 0; i < N; i++)
            {
                gradient[i] = new float[targetDim];
                for (int j = 0; j < N; j++)
                {
                    if (i != j)
                    {
                        float pq_diff = P[i][j] - Q[i][j];
                        float dist = EuclideanDistance(Y[i], Y[j]);
                        float multiplier = pq_diff / (1.0f + dist * dist);

                        for (int d = 0; d < targetDim; d++)
                        {
                            gradient[i][d] += 4.0f * multiplier * (Y[i][d] - Y[j][d]);
                        }
                    }
                }
            }

            return gradient;
        }

        private float EuclideanDistance(float[] a, float[] b)
        {
            // Delegate to VectorMath - SQL CLR does not support SIMD
            return VectorMath.EuclideanDistance(a, b);
        }
    }
}
