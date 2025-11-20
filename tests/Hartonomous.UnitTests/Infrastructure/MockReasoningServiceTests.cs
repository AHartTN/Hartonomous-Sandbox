using Hartonomous.Infrastructure.Services.Reasoning;
using Hartonomous.Core.Interfaces.Reasoning;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for MockReasoningService - tests the REAL mock implementation
/// used for marketing site demonstrations without database connectivity.
/// </summary>
public class MockReasoningServiceTests
{
    private readonly ILogger<MockReasoningService> _logger;
    private readonly MockReasoningService _service;

    public MockReasoningServiceTests()
    {
        _logger = Substitute.For<ILogger<MockReasoningService>>();
        _service = new MockReasoningService(_logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MockReasoningService(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Act
        var service = new MockReasoningService(_logger);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IReasoningService>();
    }

    [Fact]
    public async Task ExecuteChainOfThoughtAsync_ReturnsValidResult()
    {
        // Arrange
        const long sessionId = 12345L;
        const string prompt = "Test prompt for chain of thought";

        // Act
        var result = await _service.ExecuteChainOfThoughtAsync(sessionId, prompt);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.Strategy.Should().Be("ChainOfThought");
        result.Prompt.Should().Be(prompt);
        result.Conclusion.Should().NotBeNullOrWhiteSpace();
        result.IntermediateSteps.Should().NotBeNullOrWhiteSpace();
        result.ConfidenceScore.Should().BeInRange(0.0, 1.0);
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ExecutionTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteChainOfThoughtAsync_IntermediateStepsIsValidJson()
    {
        // Arrange
        const long sessionId = 12345L;
        const string prompt = "Test prompt";

        // Act
        var result = await _service.ExecuteChainOfThoughtAsync(sessionId, prompt);

        // Assert
        var json = result.IntermediateSteps;
        json.Should().NotBeNullOrWhiteSpace();
        
        // Validate it's parseable JSON
        var parseAction = () => System.Text.Json.JsonDocument.Parse(json!);
        parseAction.Should().NotThrow("IntermediateSteps should be valid JSON");
    }

    [Fact]
    public async Task ExecuteTreeOfThoughtAsync_ReturnsValidResult()
    {
        // Arrange
        const long sessionId = 67890L;
        const string prompt = "Test prompt for tree of thought";
        const int maxBranches = 5;

        // Act
        var result = await _service.ExecuteTreeOfThoughtAsync(sessionId, prompt, maxBranches);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.Strategy.Should().Be("TreeOfThought");
        result.Prompt.Should().Be(prompt);
        result.Conclusion.Should().NotBeNullOrWhiteSpace();
        result.IntermediateSteps.Should().NotBeNullOrWhiteSpace();
        result.ConfidenceScore.Should().BeInRange(0.0, 1.0);
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ExecutionTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteTreeOfThoughtAsync_IntermediateStepsIsValidJson()
    {
        // Arrange
        const long sessionId = 67890L;
        const string prompt = "Test prompt";

        // Act
        var result = await _service.ExecuteTreeOfThoughtAsync(sessionId, prompt, 3);

        // Assert
        var json = result.IntermediateSteps;
        json.Should().NotBeNullOrWhiteSpace();
        
        // Validate it's parseable JSON
        var parseAction = () => System.Text.Json.JsonDocument.Parse(json!);
        parseAction.Should().NotThrow("IntermediateSteps should be valid JSON");
    }

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsHistoricalResults()
    {
        // Arrange
        const long sessionId = 11111L;

        // Act
        var history = await _service.GetSessionHistoryAsync(sessionId);

        // Assert
        history.Should().NotBeNull();
        history.Should().NotBeEmpty();
        history.Should().AllSatisfy(r =>
        {
            r.SessionId.Should().Be(sessionId);
            r.Strategy.Should().NotBeNullOrWhiteSpace();
            r.Prompt.Should().NotBeNullOrWhiteSpace();
            r.Conclusion.Should().NotBeNullOrWhiteSpace();
            r.ConfidenceScore.Should().BeInRange(0.0, 1.0);
        });
    }

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsResultsInChronologicalOrder()
    {
        // Arrange
        const long sessionId = 11111L;

        // Act
        var history = (await _service.GetSessionHistoryAsync(sessionId)).ToList();

        // Assert
        history.Should().HaveCountGreaterThan(1);
        
        for (int i = 0; i < history.Count - 1; i++)
        {
            history[i].ExecutedAt.Should().BeBefore(history[i + 1].ExecutedAt,
                "history should be ordered chronologically");
        }
    }

    [Fact]
    public async Task ExecuteChainOfThoughtAsync_LogsDemoOperation()
    {
        // Arrange
        const long sessionId = 12345L;
        const string prompt = "Test prompt";

        // Act
        await _service.ExecuteChainOfThoughtAsync(sessionId, prompt);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DEMO]") && o.ToString()!.Contains("Chain-of-Thought")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteTreeOfThoughtAsync_LogsDemoOperation()
    {
        // Arrange
        const long sessionId = 67890L;
        const string prompt = "Test prompt";

        // Act
        await _service.ExecuteTreeOfThoughtAsync(sessionId, prompt, 3);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DEMO]") && o.ToString()!.Contains("Tree-of-Thought")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GetSessionHistoryAsync_LogsDemoOperation()
    {
        // Arrange
        const long sessionId = 11111L;

        // Act
        await _service.GetSessionHistoryAsync(sessionId);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("[DEMO]") && o.ToString()!.Contains("history")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteChainOfThoughtAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        const long sessionId = 99999L;
        const string prompt = "Test prompt";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _service.ExecuteChainOfThoughtAsync(sessionId, prompt, cts.Token));
        
        exception.Should().NotBeNull();
    }
}
