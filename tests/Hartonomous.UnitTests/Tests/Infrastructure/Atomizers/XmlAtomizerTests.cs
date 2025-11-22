using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for XmlAtomizer - XML document parsing and element extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
public class XmlAtomizerTests : UnitTestBase
{
    public XmlAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/xml", ".xml", true)]
    [InlineData("text/xml", ".xml", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new XmlAtomizer(CreateLogger<XmlAtomizer>());

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Basic XML Tests

    [Fact]
    public async Task AtomizeAsync_SimpleXml_CreatesAtoms()
    {
        // Arrange
        var atomizer = new XmlAtomizer(CreateLogger<XmlAtomizer>());
        var xml = "<?xml version=\"1.0\"?><root><item>test</item></root>";
        var content = System.Text.Encoding.UTF8.GetBytes(xml);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.xml").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AtomizeAsync_NestedElements_ExtractsStructure()
    {
        // Arrange
        var atomizer = new XmlAtomizer(CreateLogger<XmlAtomizer>());
        var xml = "<root><parent><child>value</child></parent></root>";
        var content = System.Text.Encoding.UTF8.GetBytes(xml);
        var metadata = CreateSourceMetadataBuilder().WithFileName("nested.xml").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public async Task AtomizeAsync_WithAttributes_ExtractsAttributes()
    {
        // Arrange
        var atomizer = new XmlAtomizer(CreateLogger<XmlAtomizer>());
        var xml = "<root><item id=\"1\" name=\"test\">value</item></root>";
        var content = System.Text.Encoding.UTF8.GetBytes(xml);
        var metadata = CreateSourceMetadataBuilder().WithFileName("attrs.xml").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_InvalidXml_ThrowsException()
    {
        // Arrange
        var atomizer = new XmlAtomizer(CreateLogger<XmlAtomizer>());
        var invalid = "<root><unclosed>";
        var content = System.Text.Encoding.UTF8.GetBytes(invalid);
        var metadata = CreateSourceMetadataBuilder().WithFileName("invalid.xml").Build();

        // Act
        Func<Task> act = async () => await atomizer.AtomizeAsync(content, metadata);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion
}
