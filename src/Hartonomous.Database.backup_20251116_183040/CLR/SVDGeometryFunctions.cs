using System;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Hartonomous.Clr.Core;
using Hartonomous.Clr.MachineLearning;

namespace Hartonomous.Clr
{
    /// <summary>
    /// SQL CLR functions for SVD-as-GEOMETRY pipeline.
    /// Exposes SVD decomposition and landmark projection for model weight atomization.
    /// </summary>
    public static class SVDGeometryFunctions
    {
        /// <summary>
        /// Decomposes a tensor weight array using SVD and returns the decomposition result.
        /// Input: JSON array of float values representing tensor weights
        /// Output: JSON object with { "U": [[...]], "S": [...], "V": [[...]] }
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlString clr_SvdDecompose(
            SqlString weightArrayJson,
            SqlInt32 rows,
            SqlInt32 cols,
            SqlInt32 maxRank)
        {
            if (weightArrayJson.IsNull || rows.IsNull || cols.IsNull)
                return SqlString.Null;

            try
            {
                // Parse input weight array
                var weights = JsonConvert.DeserializeObject<float[]>(weightArrayJson.Value);
                if (weights == null || weights.Length != rows.Value * cols.Value)
                    return SqlString.Null;

                // Reshape to matrix (rows x cols)
                var matrix = new float[rows.Value][];
                int idx = 0;
                for (int i = 0; i < rows.Value; i++)
                {
                    matrix[i] = new float[cols.Value];
                    for (int j = 0; j < cols.Value; j++)
                    {
                        matrix[i][j] = weights[idx++];
                    }
                }

                // Perform SVD using MathNet.Numerics
                var dataMatrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Create(
                    rows.Value, cols.Value, (i, j) => matrix[i][j]);

                var svd = dataMatrix.Svd(computeVectors: true);

                // Extract components up to maxRank
                int rank = Math.Min(maxRank.Value, Math.Min(rows.Value, cols.Value));

                // U matrix (rows x rank)
                var U = new float[rows.Value][];
                for (int i = 0; i < rows.Value; i++)
                {
                    U[i] = new float[rank];
                    for (int j = 0; j < rank; j++)
                    {
                        U[i][j] = (float)svd.U[i, j];
                    }
                }

                // Singular values (rank)
                var S = new float[rank];
                for (int i = 0; i < rank; i++)
                {
                    S[i] = (float)svd.S[i];
                }

                // V^T matrix (rank x cols) - transposed
                var VT = new float[rank][];
                for (int i = 0; i < rank; i++)
                {
                    VT[i] = new float[cols.Value];
                    for (int j = 0; j < cols.Value; j++)
                    {
                        VT[i][j] = (float)svd.VT[i, j];
                    }
                }

                // Return as JSON
                var result = new
                {
                    U = U,
                    S = S,
                    VT = VT,
                    Rank = rank,
                    ExplainedVariance = CalculateExplainedVariance(S)
                };

                return new SqlString(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                // Return error as JSON for debugging
                return new SqlString(JsonConvert.SerializeObject(new { error = ex.Message }));
            }
        }

        /// <summary>
        /// Projects a high-dimensional vector to 3D GEOMETRY point using landmark projection.
        /// Input: JSON array of float values (1998-dimensional vector)
        /// Output: JSON object with { "X": double, "Y": double, "Z": double }
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlString clr_ProjectToPoint(SqlString vectorJson)
        {
            if (vectorJson.IsNull)
                return SqlString.Null;

            try
            {
                var vector = JsonConvert.DeserializeObject<float[]>(vectorJson.Value);
                if (vector == null)
                    return SqlString.Null;

                // Pad or truncate to 1998 dimensions
                if (vector.Length != 1998)
                {
                    var padded = new float[1998];
                    Array.Copy(vector, padded, Math.Min(vector.Length, 1998));
                    vector = padded;
                }

                // Project to 3D using landmark projection
                var (x, y, z) = LandmarkProjection.ProjectTo3D(vector);

                var result = new { X = x, Y = y, Z = z };
                return new SqlString(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                return new SqlString(JsonConvert.SerializeObject(new { error = ex.Message }));
            }
        }

        /// <summary>
        /// Creates a GEOMETRY point with M coordinate (importance) from projected coordinates.
        /// Input: X, Y, Z coordinates and importance value
        /// Output: WKT string for GEOMETRY::STPointFromText
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlString clr_CreateGeometryPointWithImportance(
            SqlDouble x,
            SqlDouble y,
            SqlDouble z,
            SqlDouble importance)
        {
            if (x.IsNull || y.IsNull || z.IsNull || importance.IsNull)
                return SqlString.Null;

            // Create WKT for POINT with Z and M coordinates
            // Format: POINT ZM (x y z m)
            var wkt = $"POINT ZM ({x.Value:F6} {y.Value:F6} {z.Value:F6} {importance.Value:F6})";
            return new SqlString(wkt);
        }

        /// <summary>
        /// Reconstructs tensor weights from SVD components and atomId references.
        /// Used for "student model" synthesis: shape → content generation.
        /// Input: JSON arrays for selected U vectors, S values, VT matrix
        /// Output: JSON array of reconstructed weights
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlString clr_ReconstructFromSVD(
            SqlString UJson,
            SqlString SJson,
            SqlString VTJson)
        {
            if (UJson.IsNull || SJson.IsNull || VTJson.IsNull)
                return SqlString.Null;

            try
            {
                var U = JsonConvert.DeserializeObject<float[][]>(UJson.Value);
                var S = JsonConvert.DeserializeObject<float[]>(SJson.Value);
                var VT = JsonConvert.DeserializeObject<float[][]>(VTJson.Value);

                if (U == null || S == null || VT == null)
                    return SqlString.Null;

                int rows = U.Length;
                int rank = S.Length;
                int cols = VT[0].Length;

                // Reconstruct: X = U * diag(S) * VT
                var reconstructed = new float[rows * cols];
                int idx = 0;

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        float sum = 0;
                        for (int k = 0; k < rank; k++)
                        {
                            sum += U[i][k] * S[k] * VT[k][j];
                        }
                        reconstructed[idx++] = sum;
                    }
                }

                return new SqlString(JsonConvert.SerializeObject(reconstructed));
            }
            catch (Exception ex)
            {
                return new SqlString(JsonConvert.SerializeObject(new { error = ex.Message }));
            }
        }

        /// <summary>
        /// Helper: Calculate explained variance ratio from singular values
        /// </summary>
        private static float[] CalculateExplainedVariance(float[] singularValues)
        {
            var totalVariance = singularValues.Sum(s => s * s);
            return singularValues.Select(s => (s * s) / totalVariance).ToArray();
        }
    }
}
