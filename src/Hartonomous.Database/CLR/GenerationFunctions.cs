using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using Hartonomous.Core.Utilities;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr
{
    /// <summary>
    /// SQL CLR functions for generating text sequences using model ensembles and vector search.
    /// Supports autoregressive generation with temperature sampling and top-K filtering.
    /// </summary>
    public static class GenerationFunctions
    {
        private const double MinWeight = 0.0001d;
        private const double MinTemperature = 0.0001d;

        /// <summary>
        /// Generates a sequence of tokens using ensemble model predictions and vector similarity search.
        /// Returns table with step number, atom ID, token text, score, distance, model count, and duration.
        /// </summary>
        /// <param name="seedEmbedding">Initial embedding vector to start generation.</param>
        /// <param name="modelsJson">JSON array of model IDs to use in ensemble.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature for score softmax (higher = more random).</param>
        /// <param name="topK">Number of top candidates to consider at each step.</param>
        /// <param name="requiredModality">Optional modality filter (e.g., "text", "image").</param>
        /// <returns>Table of generated sequence steps with metadata.</returns>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            FillRowMethodName = nameof(FillSequenceRow),
            TableDefinition = "step_number INT, atom_id BIGINT, token NVARCHAR(400), score FLOAT, distance FLOAT, model_count INT, duration_ms INT")]
        public static IEnumerable GenerateSequence(
            SqlBytes seedEmbedding,
            SqlString modelsJson,
            SqlInt32 maxTokens,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlString requiredModality)
        {
            foreach (var row in EnumerateSequence(seedEmbedding, modelsJson, maxTokens, temperature, topK, requiredModality))
            {
                yield return row;
            }
        }

        /// <summary>
        /// Generates a text-only sequence (modality fixed to "text").
        /// Convenience wrapper around GenerateSequence for text generation scenarios.
        /// </summary>
        /// <param name="seedEmbedding">Initial embedding vector to start generation.</param>
        /// <param name="modelsJson">JSON array of model IDs to use in ensemble.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Sampling temperature for score softmax.</param>
        /// <param name="topK">Number of top candidates to consider at each step.</param>
        /// <returns>Table of generated text tokens with metadata (no step number column).</returns>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            FillRowMethodName = nameof(FillTextSequenceRow),
            TableDefinition = "atom_id BIGINT, token NVARCHAR(400), score FLOAT, distance FLOAT, model_count INT, duration_ms INT")]
        public static IEnumerable GenerateTextSequence(
            SqlBytes seedEmbedding,
            SqlString modelsJson,
            SqlInt32 maxTokens,
            SqlDouble temperature,
            SqlInt32 topK)
        {
            var modality = new SqlString("text");
            foreach (var row in EnumerateSequence(seedEmbedding, modelsJson, maxTokens, temperature, topK, modality))
            {
                yield return row;
            }
        }

        public static void FillSequenceRow(
            object rowObject,
            out int stepNumber,
            out long atomId,
            out string token,
            out double score,
            out double distance,
            out int modelCount,
            out int durationMs)
        {
            var row = (SequenceRow)rowObject;
            stepNumber = row.StepNumber;
            atomId = row.AtomId;
            token = row.Token;
            score = row.Score;
            distance = row.Distance;
            modelCount = row.ModelCount;
            durationMs = row.DurationMs;
        }

        public static void FillTextSequenceRow(
            object rowObject,
            out long atomId,
            out string token,
            out double score,
            out double distance,
            out int modelCount,
            out int durationMs)
        {
            var row = (SequenceRow)rowObject;
            atomId = row.AtomId;
            token = row.Token;
            score = row.Score;
            distance = row.Distance;
            modelCount = row.ModelCount;
            durationMs = row.DurationMs;
        }

        private static IEnumerable<SequenceRow> EnumerateSequence(
            SqlBytes seedEmbedding,
            SqlString modelsJson,
            SqlInt32 maxTokens,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlString requiredModality)
        {
            if (seedEmbedding == null || seedEmbedding.IsNull || seedEmbedding.Length == 0)
            {
                yield break;
            }

            var maxTokenCount = maxTokens.IsNull || maxTokens.Value <= 0 ? 1 : maxTokens.Value;
            var temperatureValue = temperature.IsNull || temperature.Value <= 0 ? MinTemperature : Math.Max(temperature.Value, MinTemperature);
            var topKValue = topK.IsNull || topK.Value <= 0 ? 1 : topK.Value;
            var topPerModel = Math.Max(1, Math.Min(256, topKValue * 2));
            var modelsJsonValue = modelsJson.IsNull ? string.Empty : modelsJson.Value;
            var modalityFilter = requiredModality.IsNull ? null : requiredModality.Value;

            // Use deterministic seed based on seedEmbedding content for reproducible sampling
            var seed = ComputeEmbeddingSeed(seedEmbedding.Value);
            var random = new Random(seed);
            var visitedAtoms = new HashSet<long>();
            var currentEmbedding = seedEmbedding.Value;

            using (var connection = new SqlConnection("context connection=true"))
            {
                connection.Open();

                for (var step = 1; step <= maxTokenCount; step++)
                {
                    var iterationStopwatch = Stopwatch.StartNew();
                    var candidates = QueryCandidates(connection, currentEmbedding, modelsJsonValue, topPerModel, modalityFilter);
                    var filtered = PrepareCandidates(candidates, visitedAtoms, topKValue);
                    if (filtered.Count == 0)
                    {
                        yield break;
                    }

                    var selection = SelectCandidate(filtered, random, temperatureValue);
                    if (selection == null)
                    {
                        yield break;
                    }

                    visitedAtoms.Add(selection.AtomId);

                    var nextEmbedding = LoadAtomEmbedding(connection, selection.AtomId);
                    if (nextEmbedding == null || nextEmbedding.Length == 0)
                    {
                        yield break;
                    }

                    iterationStopwatch.Stop();
                    var durationMs = (int)Math.Min(int.MaxValue, Math.Max(0, iterationStopwatch.ElapsedMilliseconds));

                    yield return new SequenceRow(
                        step,
                        selection.AtomId,
                        selection.Token,
                        selection.Score,
                        selection.Distance,
                        selection.ModelCount,
                        durationMs);

                    currentEmbedding = nextEmbedding;

                    if (IsTerminal(selection.Token))
                    {
                        yield break;
                    }
                }
            }
        }

        private static List<CandidateAggregate> QueryCandidates(
            SqlConnection connection,
            byte[] embeddingBinary,
            string modelsJson,
            int topPerModel,
            string? modality)
        {
            using (var command = connection.CreateCommand())
            {
                // Convert VARBINARY back to NVARCHAR (JSON) for VECTOR type
                // SQL Server 2025 VECTOR can only convert from VARCHAR/NVARCHAR/JSON, not VARBINARY
                command.CommandText = @"
DECLARE @embeddingJson NVARCHAR(MAX) = CAST(@embeddingBinary AS NVARCHAR(MAX));
DECLARE @embedding VECTOR(1998) = CAST(@embeddingJson AS VECTOR(1998));
SELECT
    AtomId,
    CanonicalText,
    WeightedScore,
    Distance,
    ModelId
FROM dbo.fn_EnsembleAtomScores(@embedding, @modelsJson, @topPerModel, @requiredModality)
WHERE CanonicalText IS NOT NULL AND LTRIM(RTRIM(CanonicalText)) <> '';
";

                var embeddingParameter = command.Parameters.Add("@embeddingBinary", SqlDbType.VarBinary, -1);
                embeddingParameter.Value = embeddingBinary;

                var modelsParameter = command.Parameters.Add("@modelsJson", SqlDbType.NVarChar, -1);
                modelsParameter.Value = string.IsNullOrEmpty(modelsJson) ? (object)DBNull.Value : modelsJson;

                var topParameter = command.Parameters.Add("@topPerModel", SqlDbType.Int);
                topParameter.Value = topPerModel;

                var modalityParameter = command.Parameters.Add("@requiredModality", SqlDbType.NVarChar, 64);
                modalityParameter.Value = modality == null ? (object)DBNull.Value : modality;

                using (var reader = command.ExecuteReader())
                {
                    var aggregates = new Dictionary<long, CandidateAggregate>();

                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                        {
                            continue;
                        }

                        var atomId = reader.GetInt64(0);
                        var canonicalText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        var token = canonicalText?.Trim();
                        if (string.IsNullOrEmpty(token))
                        {
                            continue;
                        }

                        var weightedScore = reader.IsDBNull(2) ? 0d : SafeGetDouble(reader.GetValue(2));
                        var distance = reader.IsDBNull(3) ? 1d : SafeGetDouble(reader.GetValue(3));
                        var modelId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);

                        if (!aggregates.TryGetValue(atomId, out var aggregate))
                        {
                            aggregate = new CandidateAggregate(atomId, token);
                            aggregates.Add(atomId, aggregate);
                        }

                        aggregate.Score += weightedScore;
                        aggregate.DistanceSum += distance;
                        aggregate.DistanceCount++;
                        if (modelId.HasValue)
                        {
                            aggregate.ModelIds.Add(modelId.Value);
                        }
                    }

                    return new List<CandidateAggregate>(aggregates.Values);
                }
            }
        }

        private static List<CandidateAggregate> PrepareCandidates(
            List<CandidateAggregate> candidates,
            HashSet<long> visitedAtoms,
            int topK)
        {
            var prepared = new List<CandidateAggregate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                if (visitedAtoms.Contains(candidate.AtomId))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(candidate.Token))
                {
                    continue;
                }

                candidate.Distance = candidate.DistanceCount > 0
                    ? candidate.DistanceSum / candidate.DistanceCount
                    : 1d;

                candidate.ModelCount = candidate.ModelIds.Count;

                prepared.Add(candidate);
            }

            prepared.Sort((left, right) =>
            {
                var scoreCompare = right.Score.CompareTo(left.Score);
                if (scoreCompare != 0)
                {
                    return scoreCompare;
                }

                return left.Distance.CompareTo(right.Distance);
            });

            if (prepared.Count > topK)
            {
                prepared.RemoveRange(topK, prepared.Count - topK);
            }

            return prepared;
        }

        private static CandidateAggregate? SelectCandidate(
            List<CandidateAggregate> candidates,
            Random random,
            double temperature)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            var weights = new double[candidates.Count];
            double total = 0d;

            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var score = candidate.Score <= 0 ? MinWeight : candidate.Score;
                var weight = Math.Exp(-candidate.Distance / temperature) * score;
                if (weight <= 0 || double.IsNaN(weight) || double.IsInfinity(weight))
                {
                    weight = MinWeight;
                }

                weights[index] = weight;
                total += weight;
            }

            if (total <= 0)
            {
                for (var i = 0; i < weights.Length; i++)
                {
                    weights[i] = 1d;
                }

                total = weights.Length;
            }

            var threshold = random.NextDouble() * total;
            double cumulative = 0d;

            for (var index = 0; index < candidates.Count; index++)
            {
                cumulative += weights[index];
                if (threshold <= cumulative)
                {
                    return candidates[index];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static byte[]? LoadAtomEmbedding(SqlConnection connection, long atomId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (1) CONVERT(VARBINARY(MAX), ae.EmbeddingVector)
FROM dbo.AtomEmbeddings ae
WHERE ae.AtomId = @atomId AND ae.EmbeddingVector IS NOT NULL
ORDER BY ae.CreatedAt DESC;
";
                var atomParameter = command.Parameters.Add("@atomId", SqlDbType.BigInt);
                atomParameter.Value = atomId;

                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : (byte[])result;
            }
        }

        private static bool IsTerminal(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var trimmed = token.Trim();
            return string.Equals(trimmed, "[EOS]", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "</s>", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "<|endoftext|>", StringComparison.Ordinal);
        }

        private static double SafeGetDouble(object value)
        {
            if (value is double d)
            {
                return d;
            }

            if (value is float f)
            {
                return f;
            }

            if (value is decimal m)
            {
                return (double)m;
            }

            return Convert.ToDouble(value);
        }

        private sealed class CandidateAggregate
        {
            internal CandidateAggregate(long atomId, string? token)
            {
                AtomId = atomId;
                Token = token ?? string.Empty;
                ModelIds = new HashSet<int>();
            }

            internal long AtomId { get; }

            internal string Token { get; }

            internal double Score { get; set; }

            internal double DistanceSum { get; set; }

            internal int DistanceCount { get; set; }

            internal HashSet<int> ModelIds { get; }

            internal double Distance { get; set; }

            internal int ModelCount { get; set; }
        }

        private sealed class SequenceRow
        {
            internal SequenceRow(int stepNumber, long atomId, string token, double score, double distance, int modelCount, int durationMs)
            {
                StepNumber = stepNumber;
                AtomId = atomId;
                Token = token;
                Score = score;
                Distance = distance;
                ModelCount = modelCount;
                DurationMs = durationMs;
            }

            internal int StepNumber { get; }

            internal long AtomId { get; }

            internal string Token { get; }

            internal double Score { get; }

            internal double Distance { get; }

            internal int ModelCount { get; }

            internal int DurationMs { get; }
        }

        /// <summary>
        /// Computes deterministic seed from embedding bytes for reproducible random sampling.
        /// </summary>
        private static int ComputeEmbeddingSeed(byte[] embedding)
        {
            return HashUtilities.ComputeFNV1aHash(embedding);
        }
    }
}

