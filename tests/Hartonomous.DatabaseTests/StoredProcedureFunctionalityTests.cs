using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hartonomous.DatabaseTests.Fixtures;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests;

/// <summary>
/// Functional tests for stored procedures - validates ACTUAL BEHAVIOR not just code execution
/// Tests REAL scenarios with SQL Server 2025 VECTOR type and temporal tables
/// </summary>
[Collection("SqlServerContainer")]
public sealed class StoredProcedureFunctionalityTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public StoredProcedureFunctionalityTests(SqlServerContainerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task sp_TemporalVectorSearch_RanksBySimilarity_ReturnsTopKResults()
    {
        // FUNCTIONALITY TEST: Does it correctly rank vectors by cosine similarity?
        // Real-world scenario: Search for similar embeddings, should return most similar first
        
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database unavailable: {_fixture.SkipReason}");
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Arrange: Create known test vectors with predictable similarity scores
        var testModelId = 9999;
        var dimension = 1998;
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0);

        // Clean up any existing test data
        await CleanupTestData(connection, testModelId);

        // Create Query Vector: All 1.0s (normalized later)
        var queryVector = CreateNormalizedVector(dimension, i => 1.0f);

        // Vector 1: Perfect match (all 1.0s) - should have similarity ~1.0
        var vector1 = CreateNormalizedVector(dimension, i => 1.0f);
        
        // Vector 2: High similarity (0.9 in all dimensions) - should have similarity ~0.9
        var vector2 = CreateNormalizedVector(dimension, i => 0.9f);
        
        // Vector 3: Medium similarity (half positive, half zero) - should have similarity ~0.707
        var vector3 = CreateNormalizedVector(dimension, i => i < dimension / 2 ? 1.0f : 0.0f);
        
        // Vector 4: Orthogonal (all zeros) - should have similarity ~0.0
        var vector4 = CreateNormalizedVector(dimension, i => 0.0f);

        // Insert test atoms with temporal metadata
        await InsertTestAtom(connection, 90001, "text", "Test 1", baseTime);
        await InsertTestAtom(connection, 90002, "text", "Test 2", baseTime.AddHours(1));
        await InsertTestAtom(connection, 90003, "text", "Test 3", baseTime.AddHours(2));
        await InsertTestAtom(connection, 90004, "text", "Test 4", baseTime.AddHours(3));

        // Insert embeddings
        await InsertTestEmbedding(connection, 90001, vector1, dimension, testModelId, baseTime);
        await InsertTestEmbedding(connection, 90002, vector2, dimension, testModelId, baseTime.AddHours(1));
        await InsertTestEmbedding(connection, 90003, vector3, dimension, testModelId, baseTime.AddHours(2));
        await InsertTestEmbedding(connection, 90004, vector4, dimension, testModelId, baseTime.AddHours(3));

        // Act: Execute stored procedure
        await using var command = new SqlCommand("dbo.sp_TemporalVectorSearch", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        var vectorParam = command.Parameters.Add("@QueryVector", SqlDbType.Udt);
        vectorParam.UdtTypeName = "VECTOR(1998)";
        vectorParam.Value = SerializeVector(queryVector);

        command.Parameters.AddWithValue("@TopK", 3);
        command.Parameters.AddWithValue("@StartTime", baseTime);
        command.Parameters.AddWithValue("@EndTime", baseTime.AddHours(10));
        command.Parameters.AddWithValue("@Modality", "text");
        command.Parameters.AddWithValue("@EmbeddingType", DBNull.Value);
        command.Parameters.AddWithValue("@ModelId", testModelId);
        command.Parameters.AddWithValue("@Dimension", dimension);

        var jsonResult = await command.ExecuteScalarAsync();
        
        // Assert: Validate functional behavior
        Assert.NotNull(jsonResult);
        
        var results = JsonSerializer.Deserialize<List<TemporalSearchResult>>(jsonResult.ToString()!);
        
        Assert.NotNull(results);
        Assert.Equal(3, results.Count); // TopK=3
        
        // CRITICAL FUNCTIONAL VALIDATION: Results must be ordered by similarity DESC
        Assert.True(results[0].Similarity >= results[1].Similarity, 
            $"First result similarity ({results[0].Similarity:F3}) should be >= second ({results[1].Similarity:F3})");
        Assert.True(results[1].Similarity >= results[2].Similarity,
            $"Second result similarity ({results[1].Similarity:F3}) should be >= third ({results[2].Similarity:F3})");
        
        // FUNCTIONAL VALIDATION: Highest similarity should be Vector 1 (perfect match)
        Assert.Equal(90001, results[0].AtomId);
        Assert.InRange(results[0].Similarity, 0.95, 1.0); // Near-perfect match
        
        // FUNCTIONAL VALIDATION: Second should be Vector 2 (high similarity)
        Assert.Equal(90002, results[1].AtomId);
        Assert.InRange(results[1].Similarity, 0.85, 0.95);
        
        // FUNCTIONAL VALIDATION: Third should be Vector 3 (medium similarity)
        Assert.Equal(90003, results[2].AtomId);
        Assert.InRange(results[2].Similarity, 0.6, 0.8);
        
        _output.WriteLine("✓ Vector similarity ranking validated");
        _output.WriteLine($"  Result 1: Similarity={results[0].Similarity:F3} (expected ~1.0)");
        _output.WriteLine($"  Result 2: Similarity={results[1].Similarity:F3} (expected ~0.9)");
        _output.WriteLine($"  Result 3: Similarity={results[2].Similarity:F3} (expected ~0.7)");

        // Cleanup
        await CleanupTestData(connection, testModelId);
    }

    [Fact]
    public async Task sp_TemporalVectorSearch_TemporalFiltering_ExcludesOutOfRangeResults()
    {
        // FUNCTIONALITY TEST: Does temporal filtering actually work?
        // Real-world scenario: Search only within specific time window
        
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database unavailable: {_fixture.SkipReason}");
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var testModelId = 9998;
        var dimension = 1998;
        var baseTime = new DateTime(2025, 1, 1, 0, 0, 0);

        await CleanupTestData(connection, testModelId);

        var queryVector = CreateNormalizedVector(dimension, i => 1.0f);
        var testVector = CreateNormalizedVector(dimension, i => 1.0f); // All vectors identical for this test

        // Create atoms at different times
        await InsertTestAtom(connection, 80001, "text", "Early", baseTime); // Hour 0
        await InsertTestAtom(connection, 80002, "text", "Middle", baseTime.AddHours(12)); // Hour 12
        await InsertTestAtom(connection, 80003, "text", "Late", baseTime.AddHours(24)); // Hour 24

        await InsertTestEmbedding(connection, 80001, testVector, dimension, testModelId, baseTime);
        await InsertTestEmbedding(connection, 80002, testVector, dimension, testModelId, baseTime.AddHours(12));
        await InsertTestEmbedding(connection, 80003, testVector, dimension, testModelId, baseTime.AddHours(24));

        // Act: Query only middle 16 hours (should exclude "Late" atom)
        await using var command = new SqlCommand("dbo.sp_TemporalVectorSearch", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        var vectorParam = command.Parameters.Add("@QueryVector", SqlDbType.Udt);
        vectorParam.UdtTypeName = "VECTOR(1998)";
        vectorParam.Value = SerializeVector(queryVector);

        command.Parameters.AddWithValue("@TopK", 10);
        command.Parameters.AddWithValue("@StartTime", baseTime);
        command.Parameters.AddWithValue("@EndTime", baseTime.AddHours(16)); // Only includes atoms at 0h and 12h
        command.Parameters.AddWithValue("@Modality", DBNull.Value);
        command.Parameters.AddWithValue("@EmbeddingType", DBNull.Value);
        command.Parameters.AddWithValue("@ModelId", testModelId);
        command.Parameters.AddWithValue("@Dimension", dimension);

        var jsonResult = await command.ExecuteScalarAsync();
        var results = JsonSerializer.Deserialize<List<TemporalSearchResult>>(jsonResult?.ToString() ?? "[]");

        // Assert: FUNCTIONAL VALIDATION - temporal filtering works
        Assert.NotNull(results);
        Assert.Equal(2, results.Count); // Should only include 2 atoms (not the 24h one)
        
        Assert.Contains(results, r => r.AtomId == 80001); // Early atom included
        Assert.Contains(results, r => r.AtomId == 80002); // Middle atom included
        Assert.DoesNotContain(results, r => r.AtomId == 80003); // Late atom EXCLUDED
        
        _output.WriteLine("✓ Temporal filtering validated");
        _output.WriteLine($"  Expected 2 results within 16-hour window, got {results.Count}");

        await CleanupTestData(connection, testModelId);
    }

    #region Helper Methods

    private static float[] CreateNormalizedVector(int dimension, Func<int, float> valueFunc)
    {
        var vector = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            vector[i] = valueFunc(i);
        }
        
        // Normalize to unit length
        var magnitude = Math.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (int i = 0; i < dimension; i++)
            {
                vector[i] /= (float)magnitude;
            }
        }
        
        return vector;
    }

    private static byte[] SerializeVector(float[] vector)
    {
        // SQL Server 2025 VECTOR type expects binary representation
        var bytes = new byte[vector.Length * 4];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static async Task InsertTestAtom(SqlConnection connection, long atomId, string modality, string text, DateTime createdAt)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO dbo.Atom (AtomId, Modality, Subtype, ContentHash, CanonicalText, CreatedAt)
            VALUES (@AtomId, @Modality, 'test', @Hash, @Text, @CreatedAt)";
        
        cmd.Parameters.AddWithValue("@AtomId", atomId);
        cmd.Parameters.AddWithValue("@Modality", modality);
        cmd.Parameters.AddWithValue("@Hash", $"HASH{atomId}");
        cmd.Parameters.AddWithValue("@Text", text);
        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
        
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertTestEmbedding(SqlConnection connection, long atomId, float[] vector, 
        int dimension, int modelId, DateTime createdAt)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO dbo.AtomEmbedding (AtomId, EmbeddingVector, Dimension, EmbeddingType, ModelId, CreatedAt)
            VALUES (@AtomId, @Vector, @Dimension, 'test-embedding', @ModelId, @CreatedAt)";
        
        cmd.Parameters.AddWithValue("@AtomId", atomId);
        
        var vectorParam = cmd.Parameters.Add("@Vector", SqlDbType.Udt);
        vectorParam.UdtTypeName = "VECTOR(1998)";
        vectorParam.Value = SerializeVector(vector);
        
        cmd.Parameters.AddWithValue("@Dimension", dimension);
        cmd.Parameters.AddWithValue("@ModelId", modelId);
        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
        
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CleanupTestData(SqlConnection connection, int modelId)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM dbo.AtomEmbedding WHERE ModelId = @ModelId;
            DELETE FROM dbo.Atom WHERE AtomId BETWEEN 80000 AND 90999;";
        cmd.Parameters.AddWithValue("@ModelId", modelId);
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region DTOs

    private class TemporalSearchResult
    {
        public long AtomEmbeddingId { get; set; }
        public long AtomId { get; set; }
        public string? Modality { get; set; }
        public string? Subtype { get; set; }
        public string? SourceUri { get; set; }
        public string? CanonicalText { get; set; }
        public double Similarity { get; set; }
        public DateTime CreatedAt { get; set; }
        public double TemporalDistanceHours { get; set; }
    }

    #endregion
}
