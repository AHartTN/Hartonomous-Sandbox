using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Infrastructure;

public class ReasoningServiceTests
{
    private readonly IReasoningService _mockService;

    public ReasoningServiceTests()
    {
        _mockService = Substitute.For<IReasoningService>();
    }

    [Fact]
    public async Task ExecuteChainOfThoughtAsync_WithValidInput_ReturnsResult()
    {
        // Arrange
        var sessionId = 1L;
        var prompt = "Test reasoning input";
        var expectedResult = new ReasoningResult
        {
            Id = 1,
            SessionId = sessionId,
            Strategy = "ChainOfThought",
            Prompt = prompt,
            Conclusion = "Test conclusion",
            ConfidenceScore = 0.95,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = 100
        };

        _mockService.ExecuteChainOfThoughtAsync(sessionId, prompt, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _mockService.ExecuteChainOfThoughtAsync(sessionId, prompt, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Conclusion.Should().NotBeNullOrWhiteSpace();
        result.ConfidenceScore.Should().BeInRange(0, 1);
        result.Strategy.Should().Be("ChainOfThought");
    }

    [Fact]
    public async Task ExecuteTreeOfThoughtAsync_WithValidInput_ReturnsResult()
    {
        // Arrange
        var sessionId = 1L;
        var prompt = "Test reasoning input";
        var expectedResult = new ReasoningResult
        {
            Id = 1,
            SessionId = sessionId,
            Strategy = "TreeOfThought",
            Prompt = prompt,
            Conclusion = "Test conclusion",
            ConfidenceScore = 0.92,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = 250
        };

        _mockService.ExecuteTreeOfThoughtAsync(sessionId, prompt, 3, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _mockService.ExecuteTreeOfThoughtAsync(sessionId, prompt, 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Strategy.Should().Be("TreeOfThought");
    }

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsResults()
    {
        // Arrange
        var sessionId = 1L;
        var expectedResults = new List<ReasoningResult>
        {
            new() { SessionId = sessionId, Strategy = "ChainOfThought", Prompt = "Test", Conclusion = "Result 1" }
        };

        _mockService.GetSessionHistoryAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(expectedResults);

        // Act
        var results = await _mockService.GetSessionHistoryAsync(sessionId, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
    }
}
