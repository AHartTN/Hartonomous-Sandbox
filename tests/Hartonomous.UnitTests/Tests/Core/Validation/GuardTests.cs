using FluentAssertions;
using Hartonomous.Core.Validation;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Core.Validation;

/// <summary>
/// Comprehensive tests for Guard validation class.
/// Tests all validation methods for null, empty, range, and custom validations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Fast")]
public class GuardTests
{
    #region NotNull Tests

    [Fact]
    public void NotNull_ValidObject_ReturnsObject()
    {
        // Arrange
        var obj = new object();

        // Act
        var result = Guard.NotNull(obj, nameof(obj));

        // Assert
        result.Should().BeSameAs(obj);
    }

    [Fact]
    public void NotNull_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        object? obj = null;

        // Act
        Action act = () => Guard.NotNull(obj, nameof(obj));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(obj));
    }

    [Fact]
    public void NotNull_NullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? str = null;

        // Act
        Action act = () => Guard.NotNull(str, nameof(str));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region NotNullOrEmpty Tests

    [Fact]
    public void NotNullOrEmpty_ValidArray_ReturnsArray()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = Guard.NotNullOrEmpty(array, nameof(array));

        // Assert
        result.Should().BeSameAs(array);
    }

    [Fact]
    public void NotNullOrEmpty_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        int[]? array = null;

        // Act
        Action act = () => Guard.NotNullOrEmpty(array, nameof(array));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNullOrEmpty_EmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var array = Array.Empty<int>();

        // Act
        Action act = () => Guard.NotNullOrEmpty(array, nameof(array));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(array));
    }

    [Fact]
    public void NotNullOrEmpty_ValidEnumerable_ReturnsEnumerable()
    {
        // Arrange
        var enumerable = new List<int> { 1, 2, 3 };

        // Act
        var result = Guard.NotNullOrEmpty(enumerable, nameof(enumerable));

        // Assert
        result.Should().BeSameAs(enumerable);
    }

    [Fact]
    public void NotNullOrEmpty_EmptyEnumerable_ThrowsArgumentException()
    {
        // Arrange
        var enumerable = Enumerable.Empty<int>();

        // Act
        Action act = () => Guard.NotNullOrEmpty(enumerable, nameof(enumerable));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region NotNullOrWhiteSpace Tests

    [Fact]
    public void NotNullOrWhiteSpace_ValidString_ReturnsString()
    {
        // Arrange
        var str = "valid string";

        // Act
        var result = Guard.NotNullOrWhiteSpace(str, nameof(str));

        // Assert
        result.Should().Be(str);
    }

    [Fact]
    public void NotNullOrWhiteSpace_NullString_ThrowsArgumentException()
    {
        // Arrange
        string? str = null;

        // Act
        Action act = () => Guard.NotNullOrWhiteSpace(str, nameof(str));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrWhiteSpace_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var str = string.Empty;

        // Act
        Action act = () => Guard.NotNullOrWhiteSpace(str, nameof(str));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrWhiteSpace_WhitespaceString_ThrowsArgumentException()
    {
        // Arrange
        var str = "   ";

        // Act
        Action act = () => Guard.NotNullOrWhiteSpace(str, nameof(str));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Positive Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Positive_PositiveInt_ReturnsValue(int value)
    {
        // Act
        var result = Guard.Positive(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Positive_NonPositiveInt_ThrowsArgumentException(int value)
    {
        // Act
        Action act = () => Guard.Positive(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(100L)]
    [InlineData(long.MaxValue)]
    public void Positive_PositiveLong_ReturnsValue(long value)
    {
        // Act
        var result = Guard.Positive(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Positive_NonPositiveLong_ThrowsArgumentException(long value)
    {
        // Act
        Action act = () => Guard.Positive(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region InRange Tests

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void InRange_ValueInRange_ReturnsValue(int value, int min, int max)
    {
        // Act
        var result = Guard.InRange(value, min, max, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    [InlineData(-5, 1, 10)]
    public void InRange_ValueOutOfRange_ThrowsArgumentOutOfRangeException(int value, int min, int max)
    {
        // Act
        Action act = () => Guard.InRange(value, min, max, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region Custom Validation Tests

    [Fact]
    public void Against_ConditionFalse_DoesNotThrow()
    {
        // Arrange
        var condition = false;

        // Act
        Action act = () => Guard.Against(condition, "error message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Against_ConditionTrue_ThrowsInvalidOperationException()
    {
        // Arrange
        var condition = true;

        // Act
        Action act = () => Guard.Against(condition, "error message");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("error message");
    }

    #endregion
}
