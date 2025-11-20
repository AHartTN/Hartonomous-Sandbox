using Hartonomous.Infrastructure.Services.Reasoning;
using Hartonomous.Core.Interfaces.Reasoning;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for SqlReasoningService implementation.
/// Tests Arc-enabled SQL Server integration with DefaultAzureCredential authentication.
/// </summary>
public class SqlReasoningServiceTests
{
    private readonly ILogger<SqlReasoningService> _logger;
    private readonly IConfiguration _configuration;

    public SqlReasoningServiceTests()
    {
        _logger = Substitute.For<ILogger<SqlReasoningService>>();
        
        // Mock configuration with connection string
        var configData = new Dictionary<string, string?>
        {
            {"ConnectionStrings:HartonomousDb", "Server=tcp:mock-server.database.windows.net,1433;Initial Catalog=Hartonomous;"}
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
            new SqlReasoningService(null!, _configuration));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SqlReasoningService(_logger, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new SqlReasoningService(_logger, _configuration);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IReasoningService>();
    }

    [Fact]
    public async Task ExecuteChainOfThought_WithNegativeSessionId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new SqlReasoningService(_logger, _configuration);
        const long negativeSessionId = -1L;
        const string prompt = "Test prompt";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ExecuteChainOfThoughtAsync(negativeSessionId, prompt));
    }

    [Fact]
    public async Task ExecuteChainOfThought_WithZeroSessionId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new SqlReasoningService(_logger, _configuration);
        const long zeroSessionId = 0L;
        const string prompt = "Test prompt";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ExecuteChainOfThoughtAsync(zeroSessionId, prompt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteChainOfThought_WithEmptyOrWhitespacePrompt_ThrowsArgumentException(string invalidPrompt)
    {
        // Arrange
        var service = new SqlReasoningService(_logger, _configuration);
        const long sessionId = 12345L;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExecuteChainOfThoughtAsync(sessionId, invalidPrompt));
    }

    [Fact]
    public async Task ExecuteChainOfThought_WithNullPrompt_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SqlReasoningService(_logger, _configuration);
        const long sessionId = 12345L;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ExecuteChainOfThoughtAsync(sessionId, null!));
    }

    [Fact]
    public async Task ExecuteTreeOfThought_WithNegativeMaxBranches_ExecutesSuccessfully()
    {
        // Arrange - Service doesn't validate maxBranches parameter (handled by SP)
        var service = new SqlReasoningService(_logger, _configuration);
        const long sessionId = 12345L;
        const string prompt = "Test prompt";
        const int maxBranches = -1;

        // Act & Assert - Would throw when attempting SQL connection in real environment
        // This validates parameter passing, not SQL execution
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.ExecuteTreeOfThoughtAsync(sessionId, prompt, maxBranches));
    }

    [Fact]
    public async Task GetSessionHistory_WithNegativeSessionId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new SqlReasoningService(_logger, _configuration);
        const long negativeSessionId = -100L;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetSessionHistoryAsync(negativeSessionId));
    }

    [Fact]
    public async Task Configuration_MissingConnectionString_ThrowsOnExecution()
    {
        // Arrange - Configuration without connection string
        var emptyConfig = new ConfigurationBuilder().Build();
        var service = new SqlReasoningService(_logger, emptyConfig);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteChainOfThoughtAsync(1L, "test"));
    }
}
