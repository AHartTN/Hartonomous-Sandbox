using Hartonomous.Core.Interfaces.Reasoning;
using FluentAssertions;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Core;

public class ReasoningResultTests
{
    [Fact]
    public void ReasoningResult_WithValidProperties_CreatesInstance()
    {
        // Arrange & Act
        var result = new ReasoningResult
        {
            Id = 1,
            SessionId = 100,
            Strategy = "ChainOfThought",
            Prompt = "Test prompt",
            Conclusion = "Test conclusion",
            ConfidenceScore = 0.95,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = 150
        };

        // Assert
        result.Strategy.Should().Be("ChainOfThought");
        result.ConfidenceScore.Should().Be(0.95);
        result.Conclusion.Should().Be("Test conclusion");
        result.Prompt.Should().Be("Test prompt");
        result.SessionId.Should().Be(100);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ReasoningResult_WithValidConfidenceScores_Succeeds(double confidence)
    {
        // Arrange & Act
        var result = new ReasoningResult
        {
            Strategy = "Test",
            Prompt = "Test",
            Conclusion = "Test",
            ConfidenceScore = confidence
        };

        // Assert
        result.ConfidenceScore.Should().Be(confidence);
    }

    [Fact]
    public void ReasoningResult_IntermediateSteps_CanBeNull()
    {
        // Arrange & Act
        var result = new ReasoningResult
        {
            Strategy = "Test",
            Prompt = "Test",
            Conclusion = "Test",
            IntermediateSteps = null
        };

        // Assert
        result.IntermediateSteps.Should().BeNull();
    }

    [Fact]
    public void ReasoningResult_IntermediateSteps_CanContainJson()
    {
        // Arrange
        var json = "{\"steps\":[1,2,3]}";

        // Act
        var result = new ReasoningResult
        {
            Strategy = "Test",
            Prompt = "Test",
            Conclusion = "Test",
            IntermediateSteps = json
        };

        // Assert
        result.IntermediateSteps.Should().Be(json);
    }

    [Fact]
    public void ReasoningResult_ExecutionTimeMs_CanBeSet()
    {
        // Arrange & Act
        var result = new ReasoningResult
        {
            Strategy = "Test",
            Prompt = "Test",
            Conclusion = "Test",
            ExecutionTimeMs = 250
        };

        // Assert
        result.ExecutionTimeMs.Should().Be(250);
    }
}
