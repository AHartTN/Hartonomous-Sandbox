using System;
using System.Collections.Generic;
using System.Linq;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Database-level smoke tests that validate the Hartonomous SQL Server instance is provisioned as expected.
/// </summary>
public sealed class DatabaseSmokeTests : IntegrationTestBase
{
    public DatabaseSmokeTests(SqlServerTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task RequiredStoredProcedures_Exist()
    {
        var expected = new[]
        {
            "sp_ExactVectorSearch",
            "sp_ApproxSpatialSearch",
            "sp_MultiResolutionSearch",
            "sp_SpatialAttention",
            "sp_SpatialNextToken",
            "sp_CognitiveActivation",
            "sp_ComputeSpatialProjection"
        };

        var parameters = expected
            .Select((name, index) => new SqlParameter($"@p{index}", name))
            .ToArray();

        var inClause = string.Join(", ", parameters.Select(p => p.ParameterName));
        var sql = $"SELECT name FROM sys.procedures WHERE name IN ({inClause})";

        await using var connection = new SqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            found.Add(reader.GetString(0));
        }

        var missing = expected.Where(name => !found.Contains(name)).ToArray();
        Assert.True(missing.Length == 0, $"Missing stored procedures: {string.Join(", ", missing)}");
    }

    [Fact]
    public async Task VectorDistance_CompletesSuccessfully()
    {
        const string sql = "SELECT VECTOR_DISTANCE('cosine', CAST('[1.0,0.0,0.0]' AS VECTOR(3)), CAST('[0.5,0.5,0.0]' AS VECTOR(3)))";

        await using var connection = new SqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();

        var distance = Convert.ToDouble(result);
        Assert.InRange(distance, 0d, 2d);
    }

    [Fact]
    public async Task AtomEmbeddings_ArePresent()
    {
        var count = await Fixture.DbContext!.AtomEmbeddings.CountAsync();
        Assert.True(count > 0, "Database contains no atom embeddings; run ingestion before executing integration tests.");
    }

    [Fact]
    public void VectorUtility_ComputesCosineDistance()
    {
        var vectorA = new[] { 1f, 0f, 0f };
        var vectorB = new[] { 0f, 1f, 0f };

        var distance = VectorUtility.ComputeCosineDistance(vectorA, vectorB);
        Assert.Equal(1d, distance, 3);
    }
}
