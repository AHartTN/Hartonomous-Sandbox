using FluentAssertions;
using Hartonomous.Core.Services;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Core.Services;

/// <summary>
/// Tests for IngestionResult domain model.
/// Validates success/failure states and message formatting.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Fast")]
public class IngestionResultTests
{
    [Fact]
    public void IngestionResult_DefaultConstructor_CreatesInstance()
    {
        // Act
        var result = new IngestionResult();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse(); // Default
        result.ItemsProcessed.Should().Be(0);
        result.Message.Should().BeNull();
    }

    [Fact]
    public void IngestionResult_SuccessWithItems_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var result = new IngestionResult
        {
            Success = true,
            ItemsProcessed = 42,
            Message = "Successfully processed"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsProcessed.Should().Be(42);
        result.Message.Should().Be("Successfully processed");
    }

    [Fact]
    public void IngestionResult_Failure_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var result = new IngestionResult
        {
            Success = false,
            ItemsProcessed = 0,
            Message = "Error occurred"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ItemsProcessed.Should().Be(0);
        result.Message.Should().Be("Error occurred");
    }

    [Fact]
    public void IngestionResult_PartialSuccess_AllowsNonZeroItems()
    {
        // Arrange & Act
        var result = new IngestionResult
        {
            Success = false, // Failed overall
            ItemsProcessed = 10, // But some items were processed
            Message = "Partial success - 10 of 20 items processed"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ItemsProcessed.Should().Be(10);
    }
}
