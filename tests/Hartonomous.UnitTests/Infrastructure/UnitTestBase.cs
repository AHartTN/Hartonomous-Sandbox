using Hartonomous.UnitTests.Infrastructure.Builders;
using Hartonomous.UnitTests.Infrastructure.TestFixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Infrastructure;

/// <summary>
/// Base class for all unit tests providing common infrastructure.
/// Includes test output, fixtures, and builder access.
/// </summary>
public abstract class UnitTestBase
{
    /// <summary>
    /// xUnit test output helper for writing test diagnostics.
    /// </summary>
    protected readonly ITestOutputHelper Output;

    /// <summary>
    /// Fixture for creating in-memory database contexts.
    /// </summary>
    protected readonly InMemoryDbContextFixture DbFixture;

    protected UnitTestBase(ITestOutputHelper output)
    {
        Output = output;
        DbFixture = new InMemoryDbContextFixture();
    }

    #region Builder Factory Methods

    /// <summary>
    /// Creates a new MockAtomizerBuilder for fluent atomizer mocking.
    /// </summary>
    protected MockAtomizerBuilder CreateAtomizerBuilder() => new();

    /// <summary>
    /// Creates a new TestFileBuilder for fluent test file creation.
    /// </summary>
    protected TestFileBuilder CreateFileBuilder() => new();

    /// <summary>
    /// Creates a new MockBackgroundJobServiceBuilder for job service mocking.
    /// </summary>
    protected MockBackgroundJobServiceBuilder CreateJobServiceBuilder() => new();

    /// <summary>
    /// Creates a new MockFileTypeDetectorBuilder for file type detector mocking.
    /// </summary>
    protected MockFileTypeDetectorBuilder CreateFileTypeDetectorBuilder() => new();

    /// <summary>
    /// Creates a new TestAtomDataBuilder for atom data creation.
    /// </summary>
    protected TestAtomDataBuilder CreateAtomBuilder() => new();

    /// <summary>
    /// Creates a new TestSourceMetadataBuilder for source metadata creation.
    /// </summary>
    protected TestSourceMetadataBuilder CreateSourceMetadataBuilder() => new();

    #endregion

    #region Mock Factory Methods

    /// <summary>
    /// Creates a mock logger that writes to test output.
    /// </summary>
    protected ILogger<T> CreateLogger<T>()
    {
        var mock = new Mock<ILogger<T>>();
        
        // Setup logging to write to xUnit output
        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
        .Callback((LogLevel level, EventId eventId, object state, Exception? ex, Delegate formatter) =>
        {
            var message = formatter.DynamicInvoke(state, ex)?.ToString();
            Output.WriteLine($"[{level}] {message}");
        });

        return mock.Object;
    }

    #endregion

    #region Test Data Helpers

    /// <summary>
    /// Generates a random tenant ID for test isolation.
    /// </summary>
    protected int GenerateRandomTenantId() => Random.Shared.Next(1, 10000);

    /// <summary>
    /// Generates a unique filename for tests.
    /// </summary>
    protected string GenerateUniqueFileName(string extension = "txt") 
        => $"test_{Guid.NewGuid():N}.{extension}";

    /// <summary>
    /// Creates test content with specified size in bytes.
    /// </summary>
    protected byte[] CreateTestContent(int sizeBytes)
    {
        var content = new byte[sizeBytes];
        Random.Shared.NextBytes(content);
        return content;
    }

    #endregion

    #region Assertion Helpers

    /// <summary>
    /// Writes a test section header to output.
    /// </summary>
    protected void WriteTestSection(string title)
    {
        Output.WriteLine("");
        Output.WriteLine($"??? {title} ???");
        Output.WriteLine("");
    }

    /// <summary>
    /// Writes test details to output for diagnostics.
    /// </summary>
    protected void WriteTestDetail(string key, object? value)
    {
        Output.WriteLine($"  {key}: {value}");
    }

    #endregion
}
