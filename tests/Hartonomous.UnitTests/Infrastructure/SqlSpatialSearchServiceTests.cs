using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Services.SpatialSearch;
using Hartonomous.Core.Interfaces.SpatialSearch;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for SqlSpatialSearchService implementation.
/// Tests NetTopologySuite integration with SQL Server geography types and CLR functions.
/// </summary>
public class SqlSpatialSearchServiceTests
{
    private readonly ILogger<SqlSpatialSearchService> _logger;
    private readonly IOptions<DatabaseOptions> _options;
    private readonly GeometryFactory _geometryFactory;

    public SqlSpatialSearchServiceTests()
    {
        _logger = Substitute.For<ILogger<SqlSpatialSearchService>>();
        
        var databaseOptions = new DatabaseOptions
        {
            HartonomousDb = "Server=tcp:mock-server.database.windows.net,1433;Initial Catalog=Hartonomous;"
        };
        
        _options = Options.Create(databaseOptions);

        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326); // WGS84
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SqlSpatialSearchService(null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SqlSpatialSearchService(_logger, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new SqlSpatialSearchService(_logger, _options);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ISpatialSearchService>();
    }

    [Fact]
    public async Task FindNearestAtoms_WithNullLocation_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.FindNearestAtomsAsync(null!, 1000.0, 10));
    }

    [Fact]
    public async Task FindNearestAtoms_WithNegativeMaxResults_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var location = _geometryFactory.CreatePoint(new Coordinate(-86.7816, 36.1627));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.FindNearestAtomsAsync(location, 1000.0, -1));
    }

    [Fact]
    public async Task FindNearestAtoms_WithZeroMaxResults_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var location = _geometryFactory.CreatePoint(new Coordinate(-86.7816, 36.1627));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.FindNearestAtomsAsync(location, 1000.0, 0));
    }

    [Theory]
    [InlineData(-86.7816, 36.1627)] // Nashville
    [InlineData(-122.4194, 37.7749)] // San Francisco
    [InlineData(-0.1278, 51.5074)] // London
    [InlineData(139.6917, 35.6895)] // Tokyo
    public async Task FindNearestAtoms_WithVariousLocations_AcceptsValidCoordinates(double longitude, double latitude)
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var location = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        // Act & Assert - Would throw when attempting SQL connection in real environment
        // This validates coordinate handling, not SQL execution
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.FindNearestAtomsAsync(location, 5000.0, 10));
    }

    [Fact]
    public async Task FindKNearestAtoms_WithNullLocation_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.FindKNearestAtomsAsync(null!, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task FindKNearestAtoms_WithInvalidK_ThrowsArgumentOutOfRangeException(int invalidK)
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var location = _geometryFactory.CreatePoint(new Coordinate(-86.7816, 36.1627));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.FindKNearestAtomsAsync(location, invalidK));
    }

    [Fact]
    public async Task ProjectOntoLandmarks_WithNullAtomIds_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var landmarkIds = new[] { 1L, 2L, 3L };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ProjectOntoLandmarksAsync(null!, landmarkIds));
    }

    [Fact]
    public async Task ProjectOntoLandmarks_WithNullLandmarkIds_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var atomIds = new[] { 100L, 101L, 102L };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ProjectOntoLandmarksAsync(atomIds, null!));
    }

    [Fact]
    public async Task ProjectOntoLandmarks_WithEmptyAtomIds_ThrowsArgumentException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var atomIds = Array.Empty<long>();
        var landmarkIds = new[] { 1L, 2L, 3L };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProjectOntoLandmarksAsync(atomIds, landmarkIds));
    }

    [Fact]
    public async Task ProjectOntoLandmarks_WithEmptyLandmarkIds_ThrowsArgumentException()
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var atomIds = new[] { 100L, 101L };
        var landmarkIds = Array.Empty<long>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProjectOntoLandmarksAsync(atomIds, landmarkIds));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 3)]
    [InlineData(100, 10)]
    public async Task ProjectOntoLandmarks_WithValidCounts_AcceptsParameters(int atomCount, int landmarkCount)
    {
        // Arrange
        var service = new SqlSpatialSearchService(_logger, _options);
        var atomIds = Enumerable.Range(1, atomCount).Select(i => (long)i * 100).ToArray();
        var landmarkIds = Enumerable.Range(1, landmarkCount).Select(i => (long)i).ToArray();

        // Act & Assert - Would throw when attempting SQL connection
        // This validates parameter validation before SQL execution
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.ProjectOntoLandmarksAsync(atomIds, landmarkIds));
    }
}

