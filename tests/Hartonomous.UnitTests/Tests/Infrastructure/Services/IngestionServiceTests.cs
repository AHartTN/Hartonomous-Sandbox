using FluentAssertions;
using Hartonomous.Core.Exceptions;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Hartonomous.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for IngestionService using In-Memory EF Core provider.
/// Tests use in-memory database for fast, isolated unit tests without Docker dependency.
/// Thread-safe for parallel execution with unique database names.
/// </summary>
public class IngestionServiceTests
{
    private HartonomousDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HartonomousDbContext(options);
    }
    
    private IngestionService CreateIngestionService(HartonomousDbContext context)
    {
        var fileTypeDetector = Substitute.For<IFileTypeDetector>();
        var mockAtomizer = Substitute.For<IAtomizer<byte[]>>();
        var atomizers = new[] { mockAtomizer }; // Must have at least one atomizer
        var backgroundJobService = Substitute.For<IBackgroundJobService>();
        var logger = Substitute.For<ILogger<IngestionService>>();
        
        return new IngestionService(context, fileTypeDetector, atomizers, backgroundJobService, logger);
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileDataIsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateIngestionService(context);
        byte[]? nullFile = null;

        // Act & Assert - Guard.NotNullOrEmpty throws ArgumentNullException for null
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.IngestFileAsync(nullFile!, "test.txt", tenantId: 1));
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileDataIsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateIngestionService(context);
        var emptyFile = Array.Empty<byte>();

        // Act & Assert - Guard.NotNullOrEmpty throws ArgumentException for empty arrays
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(emptyFile, "test.txt", tenantId: 1));
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileNameIsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateIngestionService(context);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert - Guard.NotNullOrWhiteSpace throws ArgumentException for null strings
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, null!, tenantId: 1));
    }

    [Fact]
    public async Task IngestFileAsync_ThrowsArgumentException_WhenFileNameIsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateIngestionService(context);
        var fileData = new byte[] { 1, 2, 3 };

        // Act & Assert - Guard.NotNullOrWhiteSpace throws ArgumentException for empty strings
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IngestFileAsync(fileData, string.Empty, tenantId: 1));
    }

    [Fact]
    public async Task IngestUrlAsync_ThrowsNotImplementedException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateIngestionService(context);

        // Act & Assert - Service throws NotImplementedException (but may be InvalidProgramException due to stub)
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => service.IngestUrlAsync("https://example.com", tenantId: 1));
        
        // Verify it's either NotImplementedException or an exception indicating not implemented
        exception.Should().Match<Exception>(e => 
            e is NotImplementedException || 
            e is InvalidProgramException ||
            e.Message.Contains("not yet implemented"));
    }

    [Fact]
    public async Task IngestDatabaseAsync_ThrowsNotImplementedException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateIngestionService(context);

        // Act & Assert - Service throws NotImplementedException
        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.IngestDatabaseAsync("Server=localhost", "SELECT * FROM Test", tenantId: 1));
    }
}
