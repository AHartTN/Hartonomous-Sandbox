using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for YamlAtomizer - YAML document parsing and structure extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class YamlAtomizerTests : UnitTestBase
{
    public YamlAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/x-yaml", ".yaml", true)]
    [InlineData("application/x-yaml", ".yml", true)]
    [InlineData("text/yaml", ".yaml", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new YamlAtomizer(CreateLogger<YamlAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic YAML Tests

    [Fact]
    public async Task AtomizeAsync_SimpleYaml_CreatesAtoms()
    {
        // Arrange
        var atomizer = new YamlAtomizer(CreateLogger<YamlAtomizer>());
        var yaml = "name: test\nvalue: 123\n";
        var content = System.Text.Encoding.UTF8.GetBytes(yaml);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.yaml").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_NestedYaml_ExtractsStructure()
    {
        // Arrange
        var atomizer = new YamlAtomizer(CreateLogger<YamlAtomizer>());
        var yaml = @"
parent:
  child1: value1
  child2: value2
items:
  - item1
  - item2
";
        var content = System.Text.Encoding.UTF8.GetBytes(yaml);
        var metadata = CreateSourceMetadataBuilder().WithFileName("nested.yaml").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task AtomizeAsync_YamlList_ExtractsItems()
    {
        // Arrange
        var atomizer = new YamlAtomizer(CreateLogger<YamlAtomizer>());
        var yaml = @"
items:
  - name: item1
    value: 1
  - name: item2
    value: 2
";
        var content = System.Text.Encoding.UTF8.GetBytes(yaml);
        var metadata = CreateSourceMetadataBuilder().WithFileName("list.yaml").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_InvalidYaml_HandlesGracefully()
    {
        // Arrange
        var atomizer = new YamlAtomizer(CreateLogger<YamlAtomizer>());
        var invalid = "invalid:\n  - broken\n    indentation";
        var content = System.Text.Encoding.UTF8.GetBytes(invalid);
        var metadata = CreateSourceMetadataBuilder().WithFileName("invalid.yaml").Build();

        // Act
        Func<Task> act = async () => await atomizer.AtomizeAsync(content, metadata);

        // Assert
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    #endregion
}
