using Hartonomous.Core.Interfaces.Reasoning;
using FluentAssertions;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Core.Interfaces;

public class ReasoningServiceInterfaceTests
{
    [Fact]
    public void IReasoningService_HasExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IReasoningService);

        // Act
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "ExecuteChainOfThoughtAsync");
        methods.Should().Contain(m => m.Name == "ExecuteTreeOfThoughtAsync");
        methods.Should().Contain(m => m.Name == "GetSessionHistoryAsync");
    }

    [Fact]
    public void ReasoningResult_HasRequiredProperties()
    {
        // Arrange
        var type = typeof(ReasoningResult);

        // Act
        var properties = type.GetProperties();

        // Assert
        properties.Should().Contain(p => p.Name == "Id");
        properties.Should().Contain(p => p.Name == "SessionId");
        properties.Should().Contain(p => p.Name == "Strategy");
        properties.Should().Contain(p => p.Name == "Prompt");
        properties.Should().Contain(p => p.Name == "Conclusion");
        properties.Should().Contain(p => p.Name == "ConfidenceScore");
        properties.Should().Contain(p => p.Name == "ExecutedAt");
        properties.Should().Contain(p => p.Name == "ExecutionTimeMs");
    }

    [Fact]
    public void ReasoningResult_IsSealed()
    {
        // Arrange & Act
        var type = typeof(ReasoningResult);

        // Assert
        type.IsSealed.Should().BeTrue();
    }
}
