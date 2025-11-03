using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hartonomous.DatabaseTests.Fixtures;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Contracts;

public sealed class SqlContractTests : IClassFixture<SqlServerContainerFixture>
{
    private static readonly string[] ExpectedStoredProcedures =
    {
        "Common.ClrBindings",
        "Common.CreateSpatialIndexes",
        "Common.Helpers",
        "Deduplication.SimilarityCheck",
        "Embedding.TextToVector",
        "Feedback.ModelWeightUpdates",
        "Generation.AudioFromPrompt",
        "Generation.ImageFromPrompt",
        "Generation.TextFromVector",
        "Generation.VideoFromPrompt",
        "Graph.AtomSurface",
        "Inference.AdvancedAnalytics",
        "Inference.MultiModelEnsemble",
        "Inference.SpatialGenerationSuite",
        "Inference.VectorSearchSuite",
        "Messaging.EventHubCheckpoint",
        "Operations.IndexMaintenance",
        "provenance.AtomicStreamFactory",
        "provenance.AtomicStreamSegments",
        "Search.SemanticSearch",
        "Semantics.FeatureExtraction",
        "Spatial.ProjectionSystem"
    };

    private readonly SqlServerContainerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SqlContractTests(SqlServerContainerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task RequiredStoredProceduresArePresent()
    {
        if (ShouldSkip())
        {
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        var procedures = await QuerySingleColumnAsync(connection, @"SELECT s.name + '.' + p.name
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE p.is_ms_shipped = 0;");

        foreach (var expected in ExpectedStoredProcedures)
        {
            Assert.Contains(procedures, actual => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task AtomicStreamTypeIsRegistered()
    {
        if (ShouldSkip())
        {
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.types WHERE name = 'AtomicStream' AND schema_id = SCHEMA_ID('provenance');";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0, "provenance.AtomicStream user-defined type must be installed.");
    }

    private bool ShouldSkip()
    {
        if (_fixture.IsAvailable)
        {
            return false;
        }

        _output.WriteLine($"Database container unavailable: {_fixture.SkipReason}");
        return true;
    }

    private static async Task<List<string>> QuerySingleColumnAsync(SqlConnection connection, string sql)
    {
        var results = new List<string>();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                results.Add(reader.GetString(0));
            }
        }

        return results;
    }
}
