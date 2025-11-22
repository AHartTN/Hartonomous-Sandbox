using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for JsonAtomizer - JSON document parsing and structure extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class JsonAtomizerTests : UnitTestBase
{
    public JsonAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/json", ".json", true)]
    [InlineData("text/json", ".json", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new JsonAtomizer(CreateLogger<JsonAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic JSON Tests

    [Fact]
    public async Task AtomizeAsync_SimpleJson_CreatesAtoms()
    {
        // Arrange
        var atomizer = new JsonAtomizer(CreateLogger<JsonAtomizer>());
        var json = "{\"name\":\"test\",\"value\":123}";
        var content = System.Text.Encoding.UTF8.GetBytes(json);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.json").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_NestedJson_ExtractsStructure()
    {
        // Arrange
        var atomizer = new JsonAtomizer(CreateLogger<JsonAtomizer>());
        var json = "{\"user\":{\"name\":\"John\",\"age\":30},\"items\":[1,2,3]}";
        var content = System.Text.Encoding.UTF8.GetBytes(json);
        var metadata = CreateSourceMetadataBuilder().WithFileName("nested.json").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Array Tests

    [Fact]
    public async Task AtomizeAsync_JsonArray_ExtractsElements()
    {
        // Arrange
        var atomizer = new JsonAtomizer(CreateLogger<JsonAtomizer>());
        var json = "[{\"id\":1},{\"id\":2},{\"id\":3}]";
        var content = System.Text.Encoding.UTF8.GetBytes(json);
        var metadata = CreateSourceMetadataBuilder().WithFileName("array.json").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_InvalidJson_HandlesGracefully()
    {
        // Arrange
        var atomizer = new JsonAtomizer(CreateLogger<JsonAtomizer>());
        var invalid = "{broken json";
        var content = System.Text.Encoding.UTF8.GetBytes(invalid);
        var metadata = CreateSourceMetadataBuilder().WithFileName("invalid.json").Build();

        // Act
        Func<Task> act = async () => await atomizer.AtomizeAsync(content, metadata);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion
}
