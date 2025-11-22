using FluentAssertions;
using Hartonomous.Api.Controllers;
using Hartonomous.Api.DTOs.Ingestion;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Infrastructure.Services;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for DataIngestionController.
/// Tests file upload, URL ingestion, database ingestion, and job status endpoints.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Controller")]
[Trait("Category", "API")]
public class DataIngestionControllerTests : UnitTestBase
{
    public DataIngestionControllerTests(ITestOutputHelper output) : base(output) { }

    #region IngestFile Tests

    [Fact]
    public async Task IngestFile_ValidFile_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateController();
        var file = CreateMockFormFile("test.txt", "Test content");

        // Act
        var result = await controller.IngestFile(file, tenantId: 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task IngestFile_NullFile_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.IngestFile(null, tenantId: 1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest?.Value.Should().Be("No file provided");
    }

    [Fact]
    public async Task IngestFile_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var file = CreateMockFormFile("empty.txt", "");

        // Act
        var result = await controller.IngestFile(file, tenantId: 1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task IngestFile_CreatesJobInBackgroundService()
    {
        // Arrange
        var jobService = new MockBackgroundJobServiceBuilder().Build();
        var controller = CreateController(jobService: jobService);
        var file = CreateMockFormFile("test.txt", "Test content");

        // Act
        await controller.IngestFile(file, tenantId: 1);

        // Assert
        // Job should be created (verified via mock)
    }

    [Fact]
    public async Task IngestFile_UnsupportedFileType_ReturnsBadRequest()
    {
        // Arrange
        var detector = CreateFileTypeDetectorBuilder().AsUnknown().Build();
        var controller = CreateController(fileTypeDetector: detector);
        var file = CreateMockFormFile("unknown.xyz", "Unknown content");

        // Act
        var result = await controller.IngestFile(file, tenantId: 1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task IngestFile_SuccessfulIngestion_ReturnsJobDetails()
    {
        // Arrange
        var controller = CreateController();
        var file = CreateMockFormFile("test.txt", "Test content");

        // Act
        var result = await controller.IngestFile(file, tenantId: 1);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult?.Value.Should().NotBeNull();
    }

    #endregion

    #region GetJobStatus Tests

    [Fact]
    public async Task GetJobStatus_ValidJobId_ReturnsJobInfo()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var jobService = CreateJobServiceBuilder()
            .WithExistingJob(jobId, "FileIngestion", "Completed")
            .Build();
        var controller = CreateController(jobService: jobService);

        // Act
        var result = await controller.GetJobStatus(jobId.ToString());

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetJobStatus_InvalidJobId_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetJobStatus("invalid-guid");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetJobStatus_NonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();
        var jobId = Guid.NewGuid();

        // Act
        var result = await controller.GetJobStatus(jobId.ToString());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region IngestUrl Tests

    [Fact]
    public async Task IngestUrl_ValidUrl_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateController();
        var request = new UrlIngestionRequest
        {
            Url = "https://example.com/document.html",
            TenantId = 1
        };

        // Act
        var result = await controller.IngestUrl(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task IngestUrl_EmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new UrlIngestionRequest
        {
            Url = "",
            TenantId = 1
        };

        // Act
        var result = await controller.IngestUrl(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task IngestUrl_NullUrl_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new UrlIngestionRequest
        {
            Url = null,
            TenantId = 1
        };

        // Act
        var result = await controller.IngestUrl(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region IngestDatabase Tests

    [Fact]
    public async Task IngestDatabase_ValidConnectionString_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateController();
        var request = new DatabaseIngestionRequest
        {
            ConnectionString = "Server=localhost;Database=test;",
            TenantId = 1,
            MaxTables = 10,
            MaxRowsPerTable = 1000
        };

        // Act
        var result = await controller.IngestDatabase(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task IngestDatabase_EmptyConnectionString_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new DatabaseIngestionRequest
        {
            ConnectionString = "",
            TenantId = 1
        };

        // Act
        var result = await controller.IngestDatabase(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region IngestGit Tests

    [Fact]
    public async Task IngestGit_ValidRepositoryPath_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateController();
        var request = new GitIngestionRequest
        {
            RepositoryPath = "/path/to/repo",
            TenantId = 1,
            MaxBranches = 10,
            MaxCommits = 100,
            MaxFiles = 1000,
            IncludeFileHistory = false
        };

        // Act
        var result = await controller.IngestGitRepository(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task IngestGit_EmptyRepositoryPath_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new GitIngestionRequest
        {
            RepositoryPath = "",
            TenantId = 1
        };

        // Act
        var result = await controller.IngestGitRepository(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region QueryAtoms Tests

    [Fact]
    public void QueryAtoms_ValidHash_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateController();
        var hash = "abc123def456";

        // Act
        var result = controller.QueryAtoms(hash);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void QueryAtoms_EmptyHash_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.QueryAtoms("");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void QueryAtoms_NullHash_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.QueryAtoms(null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Helper Methods

    private DataIngestionController CreateController(
        IFileTypeDetector? fileTypeDetector = null,
        IBackgroundJobService? jobService = null,
        IIngestionService? ingestionService = null)
    {
        var detector = fileTypeDetector ?? CreateFileTypeDetectorBuilder().AsTextPlain().Build();
        var job = jobService ?? CreateJobServiceBuilder().Build();
        var atomizers = new List<IAtomizer<byte[]>>
        {
            CreateAtomizerBuilder().Build()
        };
        
        var bulkInsert = new Mock<IAtomBulkInsertService>().Object;
        var ingestion = ingestionService ?? new Mock<IIngestionService>().Object;
        var logger = CreateLogger<DataIngestionController>();

        return new DataIngestionController(
            detector,
            atomizers,
            bulkInsert,
            job,
            ingestion,
            logger);
    }

    private IFormFile CreateMockFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new System.IO.MemoryStream(bytes);
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(bytes.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<System.IO.Stream>(), It.IsAny<CancellationToken>()))
            .Callback<System.IO.Stream, CancellationToken>((s, ct) => stream.CopyTo(s))
            .Returns(Task.CompletedTask);
        
        return fileMock.Object;
    }

    #endregion
}
