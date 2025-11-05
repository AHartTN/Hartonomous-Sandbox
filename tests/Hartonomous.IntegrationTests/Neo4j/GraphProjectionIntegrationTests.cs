using Neo4j.Driver;
using Xunit;

namespace Hartonomous.IntegrationTests.Neo4j;

public sealed class GraphProjectionIntegrationTests : IAsyncLifetime
{
    private IDriver? _driver;
    private IAsyncSession? _session;
    private readonly string _testLabel = $"TestNode_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        // Connect to Neo4j (assumes local instance at bolt://127.0.0.1:7687)
        _driver = GraphDatabase.Driver(
            "bolt://127.0.0.1:7687",
            AuthTokens.Basic("neo4j", "neo4jneo4j"));

        _session = _driver.AsyncSession();

        // Verify connectivity
        await _session.RunAsync("RETURN 1");
    }

    public async Task DisposeAsync()
    {
        // Cleanup test nodes
        if (_session != null)
        {
            await _session.RunAsync($"MATCH (n:{_testLabel}) DETACH DELETE n");
            await _session.CloseAsync();
        }

        _driver?.Dispose();
    }

    [Fact]
    public async Task CreateInferenceNode_WithMetadata_NodeIsCreated()
    {
        // Arrange
        var inferenceId = 12345;
        var taskType = "text-generation";
        var confidence = 0.87;

        // Act
        var query = $@"
            CREATE (inf:{_testLabel}:Inference {{
                inference_id: $inferenceId,
                task_type: $taskType,
                confidence: $confidence,
                timestamp: datetime()
            }})
            RETURN inf.inference_id AS id
        ";

        var cursor = await _session!.RunAsync(query, new
        {
            inferenceId,
            taskType,
            confidence
        });

        var result = await cursor.SingleAsync();

        // Assert
        Assert.Equal(inferenceId, result["id"].As<int>());
    }

    [Fact]
    public async Task CreateModelRelationship_WithContributionWeight_RelationshipExists()
    {
        // Arrange
        await _session!.RunAsync($@"
            CREATE (inf:{_testLabel}:Inference {{inference_id: 1001}})
            CREATE (m:{_testLabel}:Model {{model_id: 1, name: 'gpt-4'}})
        ");

        // Act
        await _session.RunAsync($@"
            MATCH (inf:{_testLabel}:Inference {{inference_id: 1001}})
            MATCH (m:{_testLabel}:Model {{model_id: 1}})
            CREATE (inf)-[:USED_MODEL {{
                contribution_weight: 0.75,
                individual_confidence: 0.90,
                duration_ms: 150
            }}]->(m)
        ");

        // Assert
        var cursor = await _session.RunAsync($@"
            MATCH (inf:{_testLabel}:Inference {{inference_id: 1001}})-[r:USED_MODEL]->(m:{_testLabel}:Model)
            RETURN r.contribution_weight AS weight, m.name AS model_name
        ");

        var result = await cursor.SingleAsync();
        Assert.Equal(0.75, result["weight"].As<double>());
        Assert.Equal("gpt-4", result["model_name"].As<string>());
    }

    [Fact]
    public async Task QueryReasoningMode_ByInference_ReturnsCorrectMode()
    {
        // Arrange
        await _session!.RunAsync($@"
            CREATE (inf:{_testLabel}:Inference {{inference_id: 2001}})
            CREATE (rm:{_testLabel}:ReasoningMode {{type: 'chain-of-thought', description: 'Step-by-step reasoning'}})
            CREATE (inf)-[:USED_REASONING {{weight: 1.0, num_operations: 5}}]->(rm)
        ");

        // Act
        var cursor = await _session.RunAsync($@"
            MATCH (inf:{_testLabel}:Inference {{inference_id: 2001}})-[r:USED_REASONING]->(rm:{_testLabel}:ReasoningMode)
            RETURN rm.type AS reasoning_type, r.weight AS weight
        ");

        var result = await cursor.SingleAsync();

        // Assert
        Assert.Equal("chain-of-thought", result["reasoning_type"].As<string>());
        Assert.Equal(1.0, result["weight"].As<double>());
    }

    [Fact]
    public async Task TraverseProvenanceGraph_MultiHop_ReturnsPath()
    {
        // Arrange - Create a simple provenance chain
        await _session!.RunAsync($@"
            CREATE (inf1:{_testLabel}:Inference {{inference_id: 3001}})
            CREATE (inf2:{_testLabel}:Inference {{inference_id: 3002}})
            CREATE (inf3:{_testLabel}:Inference {{inference_id: 3003}})
            CREATE (inf1)-[:INFLUENCED_BY]->(inf2)
            CREATE (inf2)-[:INFLUENCED_BY]->(inf3)
        ");

        // Act
        var cursor = await _session.RunAsync($@"
            MATCH path = (start:{_testLabel}:Inference {{inference_id: 3001}})-[:INFLUENCED_BY*]->(end:{_testLabel}:Inference)
            RETURN length(path) AS path_length, end.inference_id AS final_inference
            ORDER BY path_length DESC
            LIMIT 1
        ");

        var result = await cursor.SingleAsync();

        // Assert
        Assert.Equal(2, result["path_length"].As<int>()); // 2 hops
        Assert.Equal(3003, result["final_inference"].As<int>());
    }

    [Fact]
    public async Task DeleteInference_WithRelationships_CascadesCorrectly()
    {
        // Arrange
        await _session!.RunAsync($@"
            CREATE (inf:{_testLabel}:Inference {{inference_id: 4001}})
            CREATE (m:{_testLabel}:Model {{model_id: 2, name: 'test-model'}})
            CREATE (inf)-[:USED_MODEL]->(m)
        ");

        // Act - DETACH DELETE removes node and relationships
        await _session.RunAsync($@"
            MATCH (inf:{_testLabel}:Inference {{inference_id: 4001}})
            DETACH DELETE inf
        ");

        // Assert - Inference node should be gone
        var cursor = await _session.RunAsync($@"
            MATCH (inf:{_testLabel}:Inference {{inference_id: 4001}})
            RETURN count(inf) AS count
        ");

        var result = await cursor.SingleAsync();
        Assert.Equal(0, result["count"].As<int>());

        // Model should still exist (only relationship was deleted)
        var modelCursor = await _session.RunAsync($@"
            MATCH (m:{_testLabel}:Model {{model_id: 2}})
            RETURN count(m) AS count
        ");

        var modelResult = await modelCursor.SingleAsync();
        Assert.Equal(1, modelResult["count"].As<int>());
    }
}
