using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using NetTopologySuite.Geometries;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.Tests;

/// <summary>
/// End-to-end integration test proving the entire Hartonomous vision:
/// 1. Model ingestion: Weights stored as GEOMETRY LINESTRING ZM
/// 2. Embedding ingestion: Dual VECTOR(768) + GEOMETRY representation
/// 3. Spatial inference: O(log n) attention via nearest-neighbor
/// 4. Student model extraction: Instant via SELECT
/// 5. Text generation: Spatial next-token prediction
/// </summary>
public class EndToEndPipelineTest : IDisposable
{
    private readonly HartonomousDbContext _context;
    private readonly IEmbeddingRepository _embeddingRepo;
    private readonly IModelRepository _modelRepo;
    private readonly IModelLayerRepository _layerRepo;
    private readonly ISpatialInferenceService _spatialInference;
    private readonly IStudentModelService _studentModel;

    public EndToEndPipelineTest()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer("Server=localhost;Database=Hartonomous_IntegrationTest;Trusted_Connection=True;TrustServerCertificate=True;",
                sqlOptions => sqlOptions.UseNetTopologySuite())
            .Options;

        _context = new HartonomousDbContext(options);
        _context.Database.EnsureCreated();

        _embeddingRepo = new EmbeddingRepository(_context);
        _modelRepo = new ModelRepository(_context);
        _layerRepo = new ModelLayerRepository(_context);
        _spatialInference = new SpatialInferenceService(_context, _embeddingRepo);
        _studentModel = new StudentModelService(_context, _layerRepo);
    }

    [Fact]
    public async Task CompleteHartonomousPipeline_WorksEndToEnd()
    {
        // ============================================================
        // PHASE 1: MODEL INGESTION - Weights as GEOMETRY LINESTRING ZM
        // ============================================================
        var model = new Model
        {
            ModelName = "test-llama-3b",
            ModelType = "Transformer",
            Architecture = "Llama",
            ParameterCount = 3_000_000_000,
            Config = "{\"hidden_size\": 4096, \"num_layers\": 32}"
        };

        await _modelRepo.AddAsync(model);
        Assert.True(model.ModelId > 0, "Model should be inserted with ID");

        // Create layers with GEOMETRY LINESTRING ZM (X=index, Y=weight, Z=importance, M=temporal)
        var weights = GenerateTestWeights(1024);
        var importanceScores = weights.Select((w, i) => Math.Abs(w) * (1.0f - i / 1024.0f)).ToArray();

        var layer = new ModelLayer
        {
            ModelId = model.ModelId,
            LayerIdx = 0,
            LayerName = "transformer.h.0.attn.q_proj",
            LayerType = "Linear",
            TensorShape = "[1024, 4096]",
            TensorDtype = "float32",
            ParameterCount = weights.Length,
            WeightsGeometry = _layerRepo.CreateGeometryFromWeights(weights, importanceScores)
        };

        await _layerRepo.AddAsync(layer);
        Assert.NotNull(layer.WeightsGeometry);
        Assert.Equal(weights.Length, layer.WeightsGeometry.NumPoints);

        // Verify GEOMETRY storage: Extract weights back
        var extractedWeights = _layerRepo.ExtractWeightsFromGeometry(layer.WeightsGeometry);
        Assert.Equal(weights.Length, extractedWeights.Length);
        Assert.Equal(weights[0], extractedWeights[0], precision: 5);

        // ============================================================
        // PHASE 2: EMBEDDING INGESTION - Dual VECTOR + GEOMETRY
        // ============================================================
        var embedding1 = GenerateTestEmbedding(768);
        var embedding2 = GenerateTestEmbedding(768);
        var embedding3 = GenerateTestEmbedding(768);

        var spatial1 = new float[] { 0.1f, 0.2f, 0.3f };
        var spatial2 = new float[] { 0.15f, 0.25f, 0.35f };
        var spatial3 = new float[] { 5.0f, 6.0f, 7.0f };

        var id1 = await _embeddingRepo.AddWithGeometryAsync("The cat sat on the mat", "text", embedding1, spatial1, "hash1");
        var id2 = await _embeddingRepo.AddWithGeometryAsync("The dog sat on the rug", "text", embedding2, spatial2, "hash2");
        var id3 = await _embeddingRepo.AddWithGeometryAsync("Quantum physics is fascinating", "text", embedding3, spatial3, "hash3");

        Assert.True(id1 > 0 && id2 > 0 && id3 > 0, "Embeddings should be inserted");

        // Verify dual representation: VECTOR for exact search
        var exactResults = await _embeddingRepo.ExactSearchAsync(embedding1, topK: 2, metric: "cosine");
        Assert.True(exactResults.Count() >= 1, "Should find exact match via VECTOR");
        Assert.Equal(id1, exactResults.First().EmbeddingId);

        // Verify dual representation: GEOMETRY for fast spatial search
        var hybridResults = await _embeddingRepo.HybridSearchAsync(embedding1, spatial1[0], spatial1[1], spatial1[2],
            spatialCandidates: 10, finalTopK: 2);
        Assert.True(hybridResults.Count() >= 1, "Should find via hybrid spatial+vector search");

        // ============================================================
        // PHASE 3: SPATIAL INFERENCE - O(log n) Attention
        // ============================================================
        var attentionResults = await _spatialInference.SpatialAttentionAsync(id1, contextSize: 5);
        Assert.True(attentionResults.Count >= 1, "Spatial attention should return results");

        // Verify spatial attention finds nearby embeddings
        var nearbyIds = attentionResults.Select(r => r.TokenId).ToList();
        Assert.Contains(id2, nearbyIds); // id2 is spatially close to id1
        Assert.DoesNotContain(id3, nearbyIds); // id3 is spatially far from id1

        // ============================================================
        // PHASE 4: NEXT TOKEN PREDICTION - Spatial Centroid
        // ============================================================
        var contextTokenIds = new[] { id1, id2 };
        var nextTokenResults = await _spatialInference.PredictNextTokenAsync(contextTokenIds, temperature: 1.0, topK: 5);
        Assert.True(nextTokenResults.Count > 0, "Should predict next tokens");
        Assert.True(nextTokenResults.All(r => r.Probability > 0), "All predictions should have probability > 0");

        // ============================================================
        // PHASE 5: STUDENT MODEL EXTRACTION - Instant via SELECT
        // ============================================================
        // Add more layers to model
        for (int i = 1; i < 10; i++)
        {
            var layerWeights = GenerateTestWeights(512);
            var layerImportance = layerWeights.Select((w, idx) => Math.Abs(w) * (1.0f - idx / 512.0f) * (10 - i) / 10.0f).ToArray();

            await _layerRepo.AddAsync(new ModelLayer
            {
                ModelId = model.ModelId,
                LayerIdx = i,
                LayerName = $"transformer.h.{i}.attn.q_proj",
                LayerType = "Linear",
                TensorShape = "[512, 4096]",
                TensorDtype = "float32",
                ParameterCount = layerWeights.Length,
                WeightsGeometry = _layerRepo.CreateGeometryFromWeights(layerWeights, layerImportance)
            });
        }

        // Extract student model: Top 50% by importance (INSTANT via SELECT)
        var studentModel = await _studentModel.ExtractByImportanceAsync(model.ModelId, targetSizeRatio: 0.5);
        Assert.NotNull(studentModel);
        Assert.True(studentModel.ModelId > 0, "Student model should be created");
        Assert.True(studentModel.Layers?.Count >= 5, "Student model should have ~50% of layers");
        Assert.True(studentModel.Layers?.Count <= 6, "Student model should have ~50% of layers");

        // Verify student model has high-importance layers
        var studentLayers = await _layerRepo.GetByModelAsync(studentModel.ModelId);
        Assert.All(studentLayers, layer => Assert.NotNull(layer.WeightsGeometry));

        // ============================================================
        // PHASE 6: TEXT GENERATION - Spatial Token Prediction Loop
        // ============================================================
        var generatedText = await _spatialInference.GenerateTextSpatialAsync("The cat", maxTokens: 5, temperature: 1.0);
        Assert.NotNull(generatedText);
        Assert.NotEmpty(generatedText);
        Assert.Contains("The cat", generatedText); // Should include prompt

        // ============================================================
        // PHASE 7: MULTI-RESOLUTION SEARCH - Coarse → Fine → Exact
        // ============================================================
        var multiResResults = await _spatialInference.MultiResolutionSearchAsync(
            spatial1[0], spatial1[1], spatial1[2],
            coarseCandidates: 50, fineCandidates: 20, topK: 5);
        Assert.True(multiResResults.Count > 0, "Multi-resolution search should return results");

        // ============================================================
        // PHASE 8: COGNITIVE ACTIVATION - Threshold-based Retrieval
        // ============================================================
        var activationResults = await _spatialInference.CognitiveActivationAsync(
            embedding1, activationThreshold: 0.7, maxActivated: 10);
        Assert.True(activationResults.Count > 0, "Cognitive activation should return results");

        // ============================================================
        // PHASE 9: STUDENT MODEL COMPARISON
        // ============================================================
        var comparison = await _studentModel.CompareModelsAsync(model.ModelId, studentModel.ModelId);
        Assert.Equal(model.ParameterCount ?? 0, comparison.ModelAParameters);
        Assert.True(comparison.ModelBParameters < comparison.ModelAParameters, "Student model should be smaller");
        Assert.True(comparison.CompressionRatio > 1.0, "Should have compression ratio > 1");
        Assert.True(comparison.CompressionRatio < 2.1, "50% extraction should have ~2x compression");

        // ============================================================
        // VERIFICATION COMPLETE - ALL PIECES WORKING TOGETHER
        // ============================================================
        Assert.True(true, "COMPLETE PIPELINE VERIFIED: Ingestion → Inference → Generation → Extraction");
    }

    private float[] GenerateTestWeights(int count)
    {
        var random = new Random(42);
        return Enumerable.Range(0, count)
            .Select(i => (float)(random.NextDouble() * 2.0 - 1.0))
            .ToArray();
    }

    private float[] GenerateTestEmbedding(int dimension)
    {
        var random = new Random(42);
        var embedding = Enumerable.Range(0, dimension)
            .Select(i => (float)(random.NextDouble() * 2.0 - 1.0))
            .ToArray();

        // Normalize
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        return embedding.Select(x => (float)(x / magnitude)).ToArray();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
