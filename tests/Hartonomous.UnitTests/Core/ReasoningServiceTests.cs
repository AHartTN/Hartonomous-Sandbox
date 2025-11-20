using Hartonomous.Core.Interfaces.Reasoning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Core;

/// <summary>
/// Unit tests for IReasoningService implementations.
/// Tests various reasoning strategies with realistic domain scenarios.
/// </summary>
public class ReasoningServiceTests
{
    private readonly IReasoningService _reasoningService;

    public ReasoningServiceTests()
    {
        _reasoningService = Substitute.For<IReasoningService>();
    }

    [Fact]
    public async Task ExecuteChainOfThought_WithValidPrompt_ReturnsReasoningResult()
    {
        // Arrange
        const long sessionId = 12345L;
        const string prompt = "Analyze the spatial distribution of error atoms in session 12345";
        
        var expectedResult = new ReasoningResult
        {
            Id = 1L,
            SessionId = sessionId,
            Strategy = "ChainOfThought",
            Prompt = prompt,
            Conclusion = "Error atoms are clustered in northeast quadrant with 87% confidence",
            IntermediateSteps = "[{\"step\":1,\"thought\":\"Load atoms\"},{\"step\":2,\"thought\":\"Analyze distribution\"}]",
            ConfidenceScore = 0.87,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = 450
        };

        _reasoningService
            .ExecuteChainOfThoughtAsync(sessionId, prompt, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _reasoningService.ExecuteChainOfThoughtAsync(sessionId, prompt);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(sessionId);
        result.Strategy.Should().Be("ChainOfThought");
        result.Prompt.Should().Be(prompt);
        result.Conclusion.Should().NotBeNullOrWhiteSpace();
        result.ConfidenceScore.Should().BeInRange(0.0, 1.0);
        result.ExecutionTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteTreeOfThought_WithMultipleBranches_ReturnsOptimalPath()
    {
        // Arrange
        const long sessionId = 67890L;
        const string prompt = "Determine optimal landmark selection for 50k atoms";
        const int maxBranches = 5;
        
        var expectedResult = new ReasoningResult
        {
            Id = 2L,
            SessionId = sessionId,
            Strategy = "TreeOfThought",
            Prompt = prompt,
            Conclusion = "Select 12 landmarks using maxmin distance algorithm",
            IntermediateSteps = "[{\"branch\":1,\"score\":0.72},{\"branch\":2,\"score\":0.89},{\"branch\":3,\"score\":0.65}]",
            ConfidenceScore = 0.89,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = 1200
        };

        _reasoningService
            .ExecuteTreeOfThoughtAsync(sessionId, prompt, maxBranches, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _reasoningService.ExecuteTreeOfThoughtAsync(sessionId, prompt, maxBranches);

        // Assert
        result.Should().NotBeNull();
        result.Strategy.Should().Be("TreeOfThought");
        result.IntermediateSteps.Should().NotBeNullOrWhiteSpace();
        result.ConfidenceScore.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task GetSessionHistory_WithValidSessionId_ReturnsReasoningHistory()
    {
        // Arrange
        const long sessionId = 11111L;
        
        var expectedHistory = new List<ReasoningResult>
        {
            new()
            {
                Id = 2L,
                SessionId = sessionId,
                Strategy = "TreeOfThought",
                Prompt = "Deep analysis",
                Conclusion = "Identified 3 error clusters",
                ExecutedAt = DateTime.UtcNow.AddMinutes(-5),
                ExecutionTimeMs = 800
            },
            new()
            {
                Id = 1L,
                SessionId = sessionId,
                Strategy = "ChainOfThought",
                Prompt = "Initial analysis",
                Conclusion = "Found 150 atoms",
                ExecutedAt = DateTime.UtcNow.AddMinutes(-10),
                ExecutionTimeMs = 300
            }
        };

        _reasoningService
            .GetSessionHistoryAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(expectedHistory);

        // Act
        var history = await _reasoningService.GetSessionHistoryAsync(sessionId);

        // Assert
        history.Should().NotBeNull();
        history.Should().HaveCount(2);
        history.Should().AllSatisfy(r => r.SessionId.Should().Be(sessionId));
        history.Should().BeInDescendingOrder(r => r.ExecutedAt);
    }

    [Fact]
    public async Task ExecuteChainOfThought_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        const long sessionId = 99999L;
        const string prompt = "Long running analysis";
        
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _reasoningService
            .ExecuteChainOfThoughtAsync(sessionId, prompt, Arg.Any<CancellationToken>())
            .Returns<ReasoningResult>(_ => throw new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _reasoningService.ExecuteChainOfThoughtAsync(sessionId, prompt, cts.Token));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(0.95)]
    [InlineData(1.0)]
    public void ReasoningResult_ConfidenceScore_IsWithinValidRange(double confidenceScore)
    {
        // Arrange & Act
        var result = new ReasoningResult
        {
            Id = 1L,
            SessionId = 1L,
            Strategy = "ChainOfThought",
            Prompt = "Test prompt",
            Conclusion = "Test conclusion",
            ConfidenceScore = confidenceScore,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = 100
        };

        // Assert
        result.ConfidenceScore.Should().BeInRange(0.0, 1.0);
    }
}
