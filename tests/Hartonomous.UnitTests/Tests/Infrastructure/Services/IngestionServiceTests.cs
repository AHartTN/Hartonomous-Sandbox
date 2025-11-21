using FluentAssertions;
using Hartonomous.Core.Exceptions;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Hartonomous.DatabaseTests.Infrastructure;
using Hartonomous.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Services;

/// <summary>
/// Integration tests for IngestionService using Testcontainers (Docker SQL Server).
/// Tests use transaction rollback for idempotent execution - database state unchanged after tests.
/// Thread-safe for parallel execution with isolated containers.
/// </summary>
public class IngestionServiceTests : DatabaseTestBase
{
    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileDataIsNull()
    {
        // Arrange - Create DbContext using container connection string
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new HartonomousDbContext(options);
        
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var atomizers = Enumerable.Empty<IAtomizer<byte[]>>();
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        var service = new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);
        byte[]? nullFile = null;

        // Act & Assert - Read-only validation test (no transaction needed)
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(nullFile!, "test.txt", tenantId: 1));
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileDataIsEmpty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new HartonomousDbContext(options);
        
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var atomizers = Enumerable.Empty<IAtomizer<byte[]>>();
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        var service = new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);
        var emptyFile = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(emptyFile, "test.txt", tenantId: 1));
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileNameIsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new HartonomousDbContext(options);
        
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var atomizers = Enumerable.Empty<IAtomizer<byte[]>>();
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        var service = new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, null!, tenantId: 1));
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileNameIsEmpty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new HartonomousDbContext(options);
        
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var atomizers = Enumerable.Empty<IAtomizer<byte[]>>();
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        var service = new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, string.Empty, tenantId: 1));
    }

    [Fact]
    public async Task IngestUrlAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new HartonomousDbContext(options);
        
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var atomizers = Enumerable.Empty<IAtomizer<byte[]>>();
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        var service = new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.IngestUrlAsync("https://example.com", tenantId: 1));
    }

    [Fact]
    public async Task IngestDatabaseAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new HartonomousDbContext(options);
        
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var atomizers = Enumerable.Empty<IAtomizer<byte[]>>();
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        var service = new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.IngestDatabaseAsync("Server=localhost", "SELECT * FROM Test", tenantId: 1));
    }
}
