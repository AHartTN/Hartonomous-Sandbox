using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for CodeFileAtomizer.
/// Tests source code parsing, function extraction, and multi-language support.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "Code")]
public class CodeFileAtomizerTests : UnitTestBase
{
    public CodeFileAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("text/x-csharp", ".cs", true)]
    [InlineData("text/x-python", ".py", true)]
    [InlineData("application/javascript", ".js", true)]
    [InlineData("application/typescript", ".ts", true)]
    [InlineData("text/x-java", ".java", true)]
    [InlineData("text/x-c", ".c", true)]
    [InlineData("text/x-cpp", ".cpp", true)]
    [InlineData("text/plain", ".txt", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region C# Code Tests

    [Fact]
    public async Task AtomizeAsync_CSharpClass_ExtractsClass()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
public class TestClass
{
    public void Method() { }
}";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("Test.cs")
            .WithContentType("text/x-csharp")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "class");
        result.Atoms.Should().Contain(a => a.CanonicalText == "TestClass");
    }

    [Fact]
    public async Task AtomizeAsync_CSharpMethods_ExtractsMethods()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
public class TestClass
{
    public void Method1() { }
    private int Method2(string param) { return 0; }
}";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder().WithFileName("Test.cs").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "method");
    }

    #endregion

    #region Python Code Tests

    [Fact]
    public async Task AtomizeAsync_PythonFunction_ExtractsFunction()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
def test_function(param):
    return param * 2
";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.py")
            .WithContentType("text/x-python")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "function");
    }

    [Fact]
    public async Task AtomizeAsync_PythonClass_ExtractsClass()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
class TestClass:
    def method(self):
        pass
";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.py").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "class");
    }

    #endregion

    #region JavaScript Code Tests

    [Fact]
    public async Task AtomizeAsync_JavaScriptFunction_ExtractsFunction()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
function testFunction(param) {
    return param * 2;
}
";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.js")
            .WithContentType("application/javascript")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "function");
    }

    #endregion

    #region Import/Using Tests

    [Fact]
    public async Task AtomizeAsync_CSharpUsings_ExtractsImports()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
using System;
using System.Collections.Generic;

public class Test { }
";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder().WithFileName("Test.cs").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "import");
    }

    #endregion

    #region Comment Tests

    [Fact]
    public async Task AtomizeAsync_Comments_ExtractsComments()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = @"
// This is a comment
public class Test
{
    // Another comment
    public void Method() { }
}
";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder().WithFileName("Test.cs").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "code-comment");
    }

    #endregion

    #region Modality Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveCodeModality()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = "public class Test { }";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder().WithFileName("Test.cs").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "code" || a.Modality == "text");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_EmptyFile_ReturnsFileMetadata()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var content = System.Text.Encoding.UTF8.GetBytes("");
        var metadata = CreateSourceMetadataBuilder().WithFileName("Empty.cs").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty(); // At least file metadata
    }

    [Fact]
    public async Task AtomizeAsync_MalformedCode_HandlesGracefully()
    {
        // Arrange
        var atomizer = new CodeFileAtomizer();
        var code = "public class {{{{{ broken syntax";
        var content = System.Text.Encoding.UTF8.GetBytes(code);
        var metadata = CreateSourceMetadataBuilder().WithFileName("Broken.cs").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
