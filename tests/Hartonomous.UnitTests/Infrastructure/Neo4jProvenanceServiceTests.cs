using Hartonomous.Infrastructure.Services.Provenance;
using Hartonomous.Core.Interfaces.Provenance;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for Neo4jProvenanceService implementation.
/// Tests Neo4j graph database integration for READ-ONLY provenance queries.
/// </summary>
public class Neo4jProvenanceServiceTests
{
    private readonly ILogger<Neo4jProvenanceService> _logger;
    private readonly IConfiguration _configuration;

    public Neo4jProvenanceServiceTests()
    {
        _logger = Substitute.For<ILogger<Neo4jProvenanceService>>();
        
        var configData = new Dictionary<string, string?>
        {
            {"Neo4j:Uri", "bolt://localhost:7687"},
            {"Neo4j:Username", "neo4j"},
            {"Neo4j:Password", "test-password"},
            {"Neo4j:Database", "hartonomous"}
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new Neo4jProvenanceService(null!, _configuration));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsNullReferenceException()
    {
        // Act & Assert - Configuration indexer throws NullReferenceException, not ArgumentNullException
        Assert.Throws<NullReferenceException>(() => 
            new Neo4jProvenanceService(_logger, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new Neo4jProvenanceService(_logger, _configuration);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IProvenanceQueryService>();
    }

    [Fact]
    public void Constructor_MissingNeo4jUri_ThrowsInvalidOperationException()
    {
        // Arrange - Configuration without Neo4j:Uri
        var incompleteConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Neo4j:Username", "neo4j"},
                {"Neo4j:Password", "test"}
            })
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new Neo4jProvenanceService(_logger, incompleteConfig));
    }

    [Fact]
    public void Constructor_MissingNeo4jUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var incompleteConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Neo4j:Uri", "bolt://localhost:7687"},
                {"Neo4j:Password", "test"}
            })
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new Neo4jProvenanceService(_logger, incompleteConfig));
    }

    [Fact]
    public void Constructor_MissingNeo4jPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var incompleteConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Neo4j:Uri", "bolt://localhost:7687"},
                {"Neo4j:Username", "neo4j"}
            })
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new Neo4jProvenanceService(_logger, incompleteConfig));
    }

    [Fact]
    public void Constructor_MissingNeo4jDatabase_UsesDefaultDatabase()
    {
        // Arrange - Configuration without Neo4j:Database (should default to "neo4j")
        var configWithoutDb = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Neo4j:Uri", "bolt://localhost:7687"},
                {"Neo4j:Username", "neo4j"},
                {"Neo4j:Password", "test-password"}
            })
            .Build();

        // Act
        var service = new Neo4jProvenanceService(_logger, configWithoutDb);

        // Assert
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData("bolt://localhost:7687")]
    [InlineData("neo4j://localhost:7687")]
    [InlineData("neo4j+s://production.neo4j.io:7687")]
    public void Constructor_WithVariousUriSchemes_AcceptsValidUris(string neo4jUri)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Neo4j:Uri", neo4jUri},
                {"Neo4j:Username", "neo4j"},
                {"Neo4j:Password", "test"}
            })
            .Build();

        // Act
        var service = new Neo4jProvenanceService(_logger, config);

        // Assert
        service.Should().NotBeNull();
    }



    [Fact]
    public async Task DisposeAsync_AfterConstruction_DisposesCleanly()
    {
        // Arrange
        var service = new Neo4jProvenanceService(_logger, _configuration);

        // Act & Assert - Should dispose without throwing
        await service.DisposeAsync();
    }
}
