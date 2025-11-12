using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Generation;

/// <summary>
/// MULTIMODAL ENSEMBLE GENERATION - THE REAL ARCHITECTURE
/// 
/// This is NOT a RAG system. This implements your full vision:
/// - Multimodal context: text + image + audio + video + sensor atoms unified
/// - Multi-model ensemble: queries ALL models via TensorAtoms.SpatialSignature
/// - Hybrid generation: retrieval + synthetic SIMULTANEOUSLY, weighted by attention
/// - Cross-modal: text→image, audio→video, image→audio using GEOMETRY spatial joins
/// - Zero VRAM: CPU SIMD only (no GPU dependency)
/// - Explainable: AtomicStream nano-provenance + Neo4j graph
/// - Self-modifying: distillation, pruning, weight updates via OODA loop
/// 
/// Core: sp_GenerateWithAttention queries TensorAtoms.WeightsGeometry via STPointN()
/// to extract weights, performs multi-head attention over mixed-modality embeddings,
/// samples from unified atom space (not separate text/image/audio tables).
/// </summary>
public sealed class MultimodalEnsembleGenerator
{
    private readonly string _connectionString;
    private readonly ILogger<MultimodalEnsembleGenerator> _logger;

    public MultimodalEnsembleGenerator(
        string connectionString,
        ILogger<MultimodalEnsembleGenerator> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Universal generation method - accepts multimodal context, queries all models,
    /// blends retrieval + synthetic weighted by attention.
    /// </summary>
    /// <param name="contextAtomIds">Multimodal context: text, image, audio, video, sensor atom IDs</param>
    /// <param name="targetModality">Desired output modality (null = infer from context)</param>
    /// <param name="maxTokens">Maximum atoms to generate</param>
    /// <param name="temperature">Sampling temperature</param>
    /// <param name="topK">Top-K candidates per step</param>
    /// <param name="topP">Nucleus sampling threshold</param>
    /// <param name="attentionHeads">Multi-head attention heads</param>
    /// <param name="retrievalWeight">Weight for retrieval candidates (0-1)</param>
    /// <param name="syntheticWeight">Weight for synthetic generation (0-1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generation result with provenance tracking</returns>
    public async Task<MultimodalGenerationResult> GenerateAsync(
        IEnumerable<long> contextAtomIds,
        string targetModality = null,
        int maxTokens = 100,
        double temperature = 0.7,
        int topK = 50,
        double topP = 0.9,
        int attentionHeads = 8,
        double retrievalWeight = 0.5,
        double syntheticWeight = 0.5,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var atomIdList = contextAtomIds.ToList();

        if (!atomIdList.Any())
            throw new ArgumentException("Context must contain at least one atom", nameof(contextAtomIds));

        // Validate weights sum to 1.0
        if (Math.Abs(retrievalWeight + syntheticWeight - 1.0) > 0.01)
            throw new ArgumentException("retrievalWeight + syntheticWeight must sum to 1.0");

        _logger.LogInformation(
            "Starting multimodal ensemble generation with {ContextCount} context atoms, target modality: {Modality}",
            atomIdList.Count, targetModality ?? "auto");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Step 1: Analyze context modalities
        var contextAnalysis = await AnalyzeContextModalitiesAsync(connection, atomIdList, cancellationToken);
        
        _logger.LogInformation(
            "Context analysis: {TextCount} text, {ImageCount} images, {AudioCount} audio, {VideoCount} video atoms",
            contextAnalysis.TextAtomCount, contextAnalysis.ImageAtomCount, 
            contextAnalysis.AudioAtomCount, contextAnalysis.VideoAtomCount);

        // Step 2: Query relevant model components via TensorAtoms spatial similarity
        // This is key - we don't use a single @ModelId, we query ALL models and let
        // spatial similarity determine which tensor components are relevant
        var modelComponents = await QueryRelevantTensorAtomsAsync(
            connection, 
            atomIdList, 
            targetModality,
            topK: 100, // Get more tensor candidates
            cancellationToken);

        _logger.LogInformation(
            "Found {ComponentCount} relevant tensor components across {ModelCount} models",
            modelComponents.TotalComponents, modelComponents.UniqueModels);

        // Step 3: Call sp_GenerateWithAttention with context metadata
        // The stored procedure will:
        // - Load embeddings for ALL context atoms (multimodal)
        // - Compute multi-head attention over mixed modalities
        // - Query TensorAtoms.WeightsGeometry via STPointN() for model weights
        // - Generate new atoms using attention mechanism
        // - Track complete provenance via AtomicStream
        
        var contextJson = JsonSerializer.Serialize(new
        {
            targetModality,
            retrievalWeight,
            syntheticWeight,
            contextModalities = new
            {
                text = contextAnalysis.TextAtomCount,
                image = contextAnalysis.ImageAtomCount,
                audio = contextAnalysis.AudioAtomCount,
                video = contextAnalysis.VideoAtomCount,
                sensor = contextAnalysis.SensorAtomCount
            },
            modelComponents = new
            {
                count = modelComponents.TotalComponents,
                models = modelComponents.UniqueModels
            }
        });

        long generationStreamId;
        
        await using (var command = new SqlCommand("dbo.sp_GenerateWithAttention", connection))
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 600; // 10 minutes for complex generation

            // Note: We pass ModelId = NULL to signal multi-model ensemble mode
            // The CLR function will query TensorAtoms dynamically
            command.Parameters.AddWithValue("@ModelId", DBNull.Value);
            command.Parameters.AddWithValue("@InputAtomIds", string.Join(",", atomIdList));
            command.Parameters.AddWithValue("@ContextJson", contextJson);
            command.Parameters.AddWithValue("@MaxTokens", maxTokens);
            command.Parameters.AddWithValue("@Temperature", temperature);
            command.Parameters.AddWithValue("@TopK", topK);
            command.Parameters.AddWithValue("@TopP", topP);
            command.Parameters.AddWithValue("@AttentionHeads", attentionHeads);
            command.Parameters.AddWithValue("@TenantId", DBNull.Value); // TODO: Get from ITenantContext

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("sp_GenerateWithAttention returned no results");

            generationStreamId = reader.GetInt64(reader.GetOrdinal("GenerationStreamId"));
        }

        // Step 4: Retrieve generated atoms from provenance stream
        var generatedAtoms = await RetrieveGeneratedAtomsAsync(
            connection, 
            generationStreamId, 
            cancellationToken);

        var result = new MultimodalGenerationResult
        {
            GenerationStreamId = generationStreamId,
            ContextAtomIds = atomIdList,
            GeneratedAtomIds = generatedAtoms.AtomIds,
            GeneratedModalities = generatedAtoms.Modalities,
            ContextAnalysis = contextAnalysis,
            ModelComponents = modelComponents,
            AttentionScores = generatedAtoms.AttentionScores,
            RetrievalCandidates = generatedAtoms.RetrievalCandidates,
            SyntheticContribution = generatedAtoms.SyntheticContribution,
            DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
        };

        _logger.LogInformation(
            "Generation completed: {AtomCount} atoms generated in {DurationMs}ms, " +
            "retrieval: {RetrievalPct}%, synthetic: {SyntheticPct}%",
            result.GeneratedAtomIds.Count, result.DurationMs,
            (int)(retrievalWeight * 100), (int)(syntheticWeight * 100));

        return result;
    }

    private async Task<ContextAnalysis> AnalyzeContextModalitiesAsync(
        SqlConnection connection,
        List<long> atomIds,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            SELECT 
                a.Modality,
                COUNT(*) AS AtomCount
            FROM dbo.Atoms a
            WHERE a.AtomId IN (SELECT value FROM STRING_SPLIT(@AtomIds, ','))
            GROUP BY a.Modality", connection);

        command.Parameters.AddWithValue("@AtomIds", string.Join(",", atomIds));

        var analysis = new ContextAnalysis();
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var modality = reader.GetString(0);
            var count = reader.GetInt32(1);

            switch (modality.ToLowerInvariant())
            {
                case "text":
                    analysis.TextAtomCount = count;
                    break;
                case "image":
                    analysis.ImageAtomCount = count;
                    break;
                case "audio":
                    analysis.AudioAtomCount = count;
                    break;
                case "video":
                    analysis.VideoAtomCount = count;
                    break;
                case "sensor":
                    analysis.SensorAtomCount = count;
                    break;
            }
        }

        return analysis;
    }

    private async Task<ModelComponentAnalysis> QueryRelevantTensorAtomsAsync(
        SqlConnection connection,
        List<long> contextAtomIds,
        string targetModality,
        int topK,
        CancellationToken cancellationToken)
    {
        // Query TensorAtoms.SpatialSignature to find relevant model components
        // across ALL models using spatial similarity to context embeddings
        await using var command = new SqlCommand(@"
            WITH ContextEmbeddings AS (
                SELECT ae.EmbeddingVector, ae.SpatialGeometry
                FROM dbo.AtomEmbeddings ae
                WHERE ae.AtomId IN (SELECT value FROM STRING_SPLIT(@AtomIds, ','))
            ),
            ContextCentroid AS (
                SELECT 
                    geometry::UnionAggregate(SpatialGeometry) AS CentroidGeometry
                FROM ContextEmbeddings
            )
            SELECT TOP (@TopK)
                ta.TensorAtomId,
                ta.ModelId,
                m.ModelName,
                m.ModelType,
                ta.LayerId,
                ml.LayerName,
                ml.LayerType,
                ta.ImportanceScore,
                ta.SpatialSignature.STDistance(cc.CentroidGeometry) AS SpatialRelevance
            FROM dbo.TensorAtoms ta
            INNER JOIN dbo.Models m ON ta.ModelId = m.ModelId
            INNER JOIN dbo.ModelLayers ml ON ta.LayerId = ml.LayerId
            CROSS JOIN ContextCentroid cc
            WHERE ta.SpatialSignature IS NOT NULL
              AND m.IsActive = 1
            ORDER BY ta.SpatialSignature.STDistance(cc.CentroidGeometry) ASC", connection);

        command.Parameters.AddWithValue("@AtomIds", string.Join(",", contextAtomIds));
        command.Parameters.AddWithValue("@TopK", topK);

        var components = new List<TensorComponent>();
        var uniqueModels = new HashSet<int>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var modelId = reader.GetInt32(reader.GetOrdinal("ModelId"));
            uniqueModels.Add(modelId);

            components.Add(new TensorComponent
            {
                TensorAtomId = reader.GetInt64(reader.GetOrdinal("TensorAtomId")),
                ModelId = modelId,
                ModelName = reader.GetString(reader.GetOrdinal("ModelName")),
                ModelType = reader.GetString(reader.GetOrdinal("ModelType")),
                LayerName = reader.GetString(reader.GetOrdinal("LayerName")),
                SpatialRelevance = reader.GetDouble(reader.GetOrdinal("SpatialRelevance"))
            });
        }

        return new ModelComponentAnalysis
        {
            TotalComponents = components.Count,
            UniqueModels = uniqueModels.Count,
            Components = components
        };
    }

    private async Task<GeneratedAtomsResult> RetrieveGeneratedAtomsAsync(
        SqlConnection connection,
        long generationStreamId,
        CancellationToken cancellationToken)
    {
        // Retrieve atoms from the generation stream with provenance
        await using var command = new SqlCommand(@"
            SELECT 
                a.AtomId,
                a.Modality,
                ags.AttentionScore,
                ags.WasRetrieved,
                ags.RetrievalScore,
                ags.SyntheticScore
            FROM provenance.GenerationStreams gs
            CROSS APPLY OPENJSON(gs.GeneratedAtomIds) WITH (AtomId BIGINT '$') AS gen
            INNER JOIN dbo.Atoms a ON a.AtomId = gen.AtomId
            LEFT JOIN provenance.AttentionGenerationScores ags ON ags.GenerationStreamId = gs.GenerationStreamId AND ags.AtomId = a.AtomId
            WHERE gs.GenerationStreamId = @GenerationStreamId
            ORDER BY a.CreatedAt", connection);

        command.Parameters.AddWithValue("@GenerationStreamId", generationStreamId);

        var atomIds = new List<long>();
        var modalities = new Dictionary<string, int>();
        var attentionScores = new List<double>();
        var retrievalCandidates = new List<RetrievalCandidate>();
        double syntheticContribution = 0.0;
        int totalAtoms = 0;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var atomId = reader.GetInt64(0);
            var modality = reader.GetString(1);
            
            atomIds.Add(atomId);
            
            if (!modalities.ContainsKey(modality))
                modalities[modality] = 0;
            modalities[modality]++;

            if (!reader.IsDBNull(2))
            {
                var attentionScore = reader.GetDouble(2);
                attentionScores.Add(attentionScore);

                var wasRetrieved = !reader.IsDBNull(3) && reader.GetBoolean(3);
                if (wasRetrieved)
                {
                    retrievalCandidates.Add(new RetrievalCandidate
                    {
                        AtomId = atomId,
                        RetrievalScore = reader.GetDouble(4)
                    });
                }
                else
                {
                    syntheticContribution += reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5);
                }
            }

            totalAtoms++;
        }

        if (totalAtoms > 0)
            syntheticContribution /= totalAtoms;

        return new GeneratedAtomsResult
        {
            AtomIds = atomIds,
            Modalities = modalities,
            AttentionScores = attentionScores,
            RetrievalCandidates = retrievalCandidates,
            SyntheticContribution = syntheticContribution
        };
    }
}

public class MultimodalGenerationResult
{
    public long GenerationStreamId { get; set; }
    public List<long> ContextAtomIds { get; set; }
    public List<long> GeneratedAtomIds { get; set; }
    public Dictionary<string, int> GeneratedModalities { get; set; }
    public ContextAnalysis ContextAnalysis { get; set; }
    public ModelComponentAnalysis ModelComponents { get; set; }
    public List<double> AttentionScores { get; set; }
    public List<RetrievalCandidate> RetrievalCandidates { get; set; }
    public double SyntheticContribution { get; set; }
    public int DurationMs { get; set; }
}

public class ContextAnalysis
{
    public int TextAtomCount { get; set; }
    public int ImageAtomCount { get; set; }
    public int AudioAtomCount { get; set; }
    public int VideoAtomCount { get; set; }
    public int SensorAtomCount { get; set; }
}

public class ModelComponentAnalysis
{
    public int TotalComponents { get; set; }
    public int UniqueModels { get; set; }
    public List<TensorComponent> Components { get; set; }
}

public class TensorComponent
{
    public long TensorAtomId { get; set; }
    public int ModelId { get; set; }
    public string ModelName { get; set; }
    public string ModelType { get; set; }
    public string LayerName { get; set; }
    public double SpatialRelevance { get; set; }
}

public class GeneratedAtomsResult
{
    public List<long> AtomIds { get; set; }
    public Dictionary<string, int> Modalities { get; set; }
    public List<double> AttentionScores { get; set; }
    public List<RetrievalCandidate> RetrievalCandidates { get; set; }
    public double SyntheticContribution { get; set; }
}

public class RetrievalCandidate
{
    public long AtomId { get; set; }
    public double RetrievalScore { get; set; }
}
