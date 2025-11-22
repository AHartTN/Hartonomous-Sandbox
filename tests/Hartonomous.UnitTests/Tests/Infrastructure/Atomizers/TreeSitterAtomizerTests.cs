using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Tests for TreeSitterAtomizer - Polyglot code parsing with Tree-sitter patterns.
/// Supports Python, JavaScript, TypeScript, Go, Rust, Ruby, Java, PHP, Swift, Kotlin, Scala.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "Code")]
public class TreeSitterAtomizerTests : UnitTestBase
{
    public TreeSitterAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("text/x-python", "py", true)]
    [InlineData("text/x-python", "pyw", true)]
    [InlineData("text/x-javascript", "js", true)]
    [InlineData("text/x-typescript", "ts", true)]
    [InlineData("text/x-go", "go", true)]
    [InlineData("text/x-rust", "rs", true)]
    [InlineData("text/x-ruby", "rb", true)]
    [InlineData("text/x-java", "java", true)]
    [InlineData("text/x-php", "php", true)]
    [InlineData("text/x-swift", "swift", true)]
    [InlineData("text/x-kotlin", "kt", true)]
    [InlineData("text/x-scala", "scala", true)]
    [InlineData("text/plain", "txt", false)]
    public void CanHandle_VariousLanguages_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Python Tests

    [Fact]
    public async Task AtomizeAsync_PythonFile_ExtractsFunctionsAndClasses()
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();
        var pythonCode = @"
class MyClass:
    def __init__(self):
        pass
    
    def my_method(self, param):
        return param * 2

def standalone_function(x, y):
    return x + y
";
        var content = System.Text.Encoding.UTF8.GetBytes(pythonCode);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.py")
            .WithContentType("text/x-python")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Should().Contain(a => a.Subtype == "python-class");
        result.Atoms.Should().Contain(a => a.Subtype == "python-function");
    }

    #endregion

    #region JavaScript Tests

    [Fact]
    public async Task AtomizeAsync_JavaScriptFile_ExtractsFunctionsAndClasses()
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();
        var jsCode = @"
class MyClass {
    constructor() {
        this.value = 0;
    }
    
    myMethod(param) {
        return param * 2;
    }
}

function standaloneFunction(x, y) {
    return x + y;
}

const arrowFunction = (x) => x * 2;
";
        var content = System.Text.Encoding.UTF8.GetBytes(jsCode);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.js")
            .WithContentType("text/x-javascript")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "javascript-class");
        result.Atoms.Should().Contain(a => a.Subtype == "javascript-function");
    }

    #endregion

    #region Go Tests

    [Fact]
    public async Task AtomizeAsync_GoFile_ExtractsFunctionsAndStructs()
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();
        var goCode = @"
package main

type MyStruct struct {
    Value int
}

func (m *MyStruct) Method() int {
    return m.Value * 2
}

func standaloneFunction(x, y int) int {
    return x + y
}
";
        var content = System.Text.Encoding.UTF8.GetBytes(goCode);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.go")
            .WithContentType("text/x-go")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "go-class");
        result.Atoms.Should().Contain(a => a.Subtype == "go-function");
    }

    #endregion

    #region Rust Tests

    [Fact]
    public async Task AtomizeAsync_RustFile_ExtractsFunctionsAndStructs()
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();
        var rustCode = @"
struct MyStruct {
    value: i32,
}

impl MyStruct {
    fn new(value: i32) -> Self {
        MyStruct { value }
    }
    
    fn method(&self) -> i32 {
        self.value * 2
    }
}

fn standalone_function(x: i32, y: i32) -> i32 {
    x + y
}
";
        var content = System.Text.Encoding.UTF8.GetBytes(rustCode);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.rs")
            .WithContentType("text/x-rust")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "rust-class");
        result.Atoms.Should().Contain(a => a.Subtype == "rust-function");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_EmptyFile_ReturnsFileMetadata()
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();
        var content = System.Text.Encoding.UTF8.GetBytes("");
        var metadata = CreateSourceMetadataBuilder().WithFileName("empty.py").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty(); // At least file metadata
    }

    [Fact]
    public async Task AtomizeAsync_UnsupportedLanguage_HandlesGracefully()
    {
        // Arrange
        var atomizer = new TreeSitterAtomizer();
        var content = System.Text.Encoding.UTF8.GetBytes("some random content");
        var metadata = CreateSourceMetadataBuilder().WithFileName("unknown.xyz").Build();

        // Act
        var result = await atomizer.AtomizeAsync(content, metadata);

        // Assert
        result.Should().NotBeNull();
        result.ProcessingInfo.Warnings.Should().NotBeNull();
    }

    #endregion
}
