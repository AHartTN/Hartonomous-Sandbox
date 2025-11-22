using FluentAssertions;
using Hartonomous.Core.Exceptions;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Hartonomous.Infrastructure.Services;
using Hartonomous.UnitTests.Infrastructure.Builders;
using Hartonomous.UnitTests.Infrastructure.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive unit tests for IngestionService.
/// Tests cover: validation, file type detection, atomization, embedding job creation, and error handling.
/// Uses in-memory EF Core provider for fast, isolated tests without Docker dependency.
/// Thread-safe for parallel execution with unique database names.
/// </summary>
public class IngestionServiceTests
{
    private readonly InMemoryDbContextFixture _dbFixture;

    public IngestionServiceTests()
    {
        _dbFixture = new InMemoryDbContextFixture();
    }

    #region Input Validation Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Fast")]
    public async Task IngestFileAsync_ThrowsArgumentNullException_WhenFileDataIsNull()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);
        byte[]? nullFile = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.IngestFileAsync(nullFile!, "test.txt", tenantId: 1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileDataIsEmpty()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);
        var emptyFile = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(emptyFile, "test.txt", tenantId: 1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileNameIsNull()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, null!, tenantId: 1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileNameIsEmpty()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, string.Empty, tenantId: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenTenantIdIsInvalid(int invalidTenantId)
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, "test.txt", tenantId: invalidTenantId));
    }

    #endregion

    #region Successful Ingestion Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Integration")]
    public async Task IngestFileAsync_TextFile_CreatesAtomsSuccessfully()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("text/plain", FileCategory.Text);
        var atomizer = new MockAtomizerBuilder()
            .WithAtomCount(5)
            .WithModality("text")
            .WithPriority(10)
            .Build();
        var backgroundJobService = new Mock<IBackgroundJobService>();
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { atomizer },
            backgroundJobService.Object,
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithTextContent("This is a test document with multiple sentences.")
            .WithFileName("test.txt")
            .Build();

        // Act
        var result = await service.IngestFileAsync(content, fileName, tenantId: 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ItemsProcessed.Should().Be(5);
        result.Message.Should().Contain("Successfully ingested 5 atoms");

        // Verify atoms were created - Note: sp_IngestAtoms would be called in real scenario
        // In unit test, we verify the method completes successfully
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_ImageFile_SelectsImageAtomizer()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("image/png", FileCategory.ImageRaster);
        
        var textAtomizer = new MockAtomizerBuilder()
            .WithModality("text")
            .WithPriority(5)
            .CanHandle(false)
            .Build();
        
        var imageAtomizer = new MockAtomizerBuilder()
            .WithAtomCount(10)
            .WithModality("image")
            .WithPriority(10)
            .CanHandle(true)
            .Build();
        
        var backgroundJobService = new Mock<IBackgroundJobService>();
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { textAtomizer, imageAtomizer },
            backgroundJobService.Object,
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithPngHeader()
            .Build();

        // Act
        var result = await service.IngestFileAsync(content, fileName, tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsProcessed.Should().Be(10); // Image atomizer produces 10 atoms
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_MultipleAtomizers_SelectsHighestPriority()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("text/plain", FileCategory.Text);
        
        var lowPriorityAtomizer = new MockAtomizerBuilder()
            .WithAtomCount(1)
            .WithPriority(1)
            .Build();
        
        var highPriorityAtomizer = new MockAtomizerBuilder()
            .WithAtomCount(5)
            .WithPriority(10)
            .Build();
        
        var backgroundJobService = new Mock<IBackgroundJobService>();
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { lowPriorityAtomizer, highPriorityAtomizer },
            backgroundJobService.Object,
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithTextContent("Test content")
            .Build();

        // Act
        var result = await service.IngestFileAsync(content, fileName, tenantId: 1);

        // Assert
        result.ItemsProcessed.Should().Be(5); // High priority atomizer produces 5 atoms
    }

    #endregion

    #region Embedding Job Creation Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_TextModality_CreatesEmbeddingJobs()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("text/plain", FileCategory.Text);
        var atomizer = new MockAtomizerBuilder()
            .WithAtomCount(3)
            .WithModality("text")
            .Build();
        
        var backgroundJobService = new Mock<IBackgroundJobService>();
        backgroundJobService
            .Setup(x => x.CreateJobAsync(
                "GenerateEmbedding",
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { atomizer },
            backgroundJobService.Object,
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithTextContent("Test content")
            .Build();

        // Act
        await service.IngestFileAsync(content, fileName, tenantId: 1);

        // Assert
        backgroundJobService.Verify(
            x => x.CreateJobAsync(
                "GenerateEmbedding",
                It.IsAny<string>(),
                1,
                It.IsAny<CancellationToken>()),
            Times.Exactly(3)); // One job per atom
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_BinaryModality_DoesNotCreateEmbeddingJobs()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("application/octet-stream", FileCategory.Binary);
        var atomizer = new MockAtomizerBuilder()
            .WithAtomCount(3)
            .WithModality("binary")
            .Build();
        
        var backgroundJobService = new Mock<IBackgroundJobService>();
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { atomizer },
            backgroundJobService.Object,
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithRawBytes(new byte[] { 0x01, 0x02, 0x03 })
            .Build();

        // Act
        await service.IngestFileAsync(content, fileName, tenantId: 1);

        // Assert
        backgroundJobService.Verify(
            x => x.CreateJobAsync(
                "GenerateEmbedding",
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never); // Binary modality doesn't need embeddings
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_UnknownFileType_ThrowsInvalidFileFormatException()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("application/octet-stream", FileCategory.Unknown);
        var atomizer = new MockAtomizerBuilder().Build();
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { atomizer },
            Mock.Of<IBackgroundJobService>(),
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithRawBytes(new byte[] { 0xFF, 0xFF })
            .WithFileName("unknown.xyz")
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidFileFormatException>(
            () => service.IngestFileAsync(content, fileName, tenantId: 1));
        
        exception.Message.Should().Contain("Unsupported file format");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestFileAsync_NoAtomizerAvailable_ThrowsInvalidFileFormatException()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        
        var fileTypeDetector = CreateMockFileTypeDetector("text/plain", FileCategory.Text);
        var atomizer = new MockAtomizerBuilder()
            .CanHandle(false) // Atomizer doesn't support this file type
            .Build();
        
        var service = new IngestionService(
            context,
            fileTypeDetector,
            new[] { atomizer },
            Mock.Of<IBackgroundJobService>(),
            Mock.Of<ILogger<IngestionService>>());

        var (content, fileName) = new TestFileBuilder()
            .WithTextContent("Test")
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidFileFormatException>(
            () => service.IngestFileAsync(content, fileName, tenantId: 1));
        
        exception.Message.Should().Contain("No atomizer found");
    }

    #endregion

    #region URL Ingestion Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestUrlAsync_ThrowsArgumentException_WhenUrlIsNull()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestUrlAsync(null!, tenantId: 1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestUrlAsync_ThrowsArgumentException_WhenUrlIsEmpty()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestUrlAsync(string.Empty, tenantId: 1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestUrlAsync_ThrowsArgumentException_WhenUrlIsInvalid()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestUrlAsync("not-a-valid-url", tenantId: 1));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/test.txt")]
    [InlineData("javascript:alert(1)")]
    [Trait("Category", "Unit")]
    public async Task IngestUrlAsync_ThrowsInvalidOperationException_WhenSchemeNotAllowed(string url)
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.IngestUrlAsync(url, tenantId: 1));
    }

    #endregion

    #region Database Ingestion Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IngestDatabaseAsync_ThrowsNotImplementedException()
    {
        // Arrange
        using var context = _dbFixture.CreateContext();
        var service = CreateIngestionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.IngestDatabaseAsync("Server=localhost", "SELECT * FROM Test", tenantId: 1));
    }

    #endregion

    #region Helper Methods

    private IngestionService CreateIngestionService(HartonomousDbContext context)
    {
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var mockAtomizer = Substitute.For<IAtomizer<byte[]>>();
        var atomizers = new[] { mockAtomizer };
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        
        return new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);
    }

    private IFileTypeDetector CreateMockFileTypeDetector(string contentType, FileCategory category)
    {
        var mock = new Mock<IFileTypeDetector>();
        
        mock.Setup(x => x.Detect(It.IsAny<ReadOnlySpan<byte>>(), It.IsAny<string>()))
            .Returns(new FileTypeInfo
            {
                ContentType = contentType,
                Category = category,
                SpecificFormat = contentType,
                Confidence = 0.95,
                Extension = Path.GetExtension(contentType)
            });
        
        mock.Setup(x => x.Detect(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(new FileTypeInfo
            {
                ContentType = contentType,
                Category = category,
                SpecificFormat = contentType,
                Confidence = 0.95,
                Extension = Path.GetExtension(contentType)
            });
        
        return mock.Object;
    }

    #endregion
}
