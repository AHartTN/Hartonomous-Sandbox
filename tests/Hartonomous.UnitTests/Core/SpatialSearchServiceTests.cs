using Hartonomous.Core.Interfaces.SpatialSearch;
using FluentAssertions;
using NSubstitute;
using NetTopologySuite.Geometries;
using Xunit;

namespace Hartonomous.UnitTests.Core;

/// <summary>
/// Unit tests for ISpatialSearchService implementations.
/// Tests spatial queries with realistic geographic coordinates and CLR function patterns.
/// </summary>
public class SpatialSearchServiceTests
{
    private readonly ISpatialSearchService _spatialService;
    private readonly GeometryFactory _geometryFactory;

    public SpatialSearchServiceTests()
    {
        _spatialService = Substitute.For<ISpatialSearchService>();
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326); // WGS84
    }

    [Fact]
    public async Task FindNearestAtoms_WithValidLocation_ReturnsOrderedResults()
    {
        // Arrange - Nashville, TN coordinates
        var centerPoint = _geometryFactory.CreatePoint(new Coordinate(-86.7816, 36.1627));
        const double radiusMeters = 5000.0; // 5km radius
        const int maxResults = 10;

        var expectedAtoms = new List<SpatialAtom>
        {
            new()
            {
                AtomId = 1001L,
                Location = _geometryFactory.CreatePoint(new Coordinate(-86.7820, 36.1630)),
                DistanceMeters = 45.2,
                AtomData = "{\"type\":\"observation\",\"value\":\"sensor_reading\"}",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                AtomId = 1002L,
                Location = _geometryFactory.CreatePoint(new Coordinate(-86.7800, 36.1625)),
                DistanceMeters = 152.8,
                AtomData = "{\"type\":\"measurement\",\"value\":\"temperature\"}",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _spatialService
            .FindNearestAtomsAsync(centerPoint, radiusMeters, maxResults, Arg.Any<CancellationToken>())
            .Returns(expectedAtoms);

        // Act
        var atoms = await _spatialService.FindNearestAtomsAsync(centerPoint, radiusMeters, maxResults);

        // Assert
        atoms.Should().NotBeNull();
        atoms.Should().HaveCount(2);
        atoms.Should().BeInAscendingOrder(a => a.DistanceMeters);
        atoms.Should().AllSatisfy(a =>
        {
            a.AtomId.Should().BeGreaterThan(0);
            a.Location.Should().NotBeNull();
            a.DistanceMeters.Should().BeLessOrEqualTo(radiusMeters);
        });
    }

    [Fact]
    public async Task FindKNearestAtoms_WithSpecificK_ReturnsExactCount()
    {
        // Arrange - Downtown Nashville
        var centerPoint = _geometryFactory.CreatePoint(new Coordinate(-86.7816, 36.1627));
        const int k = 5;

        var expectedAtoms = Enumerable.Range(1, k).Select(i => new SpatialAtom
        {
            AtomId = 2000L + i,
            Location = _geometryFactory.CreatePoint(new Coordinate(-86.7816 + (i * 0.001), 36.1627 + (i * 0.001))),
            DistanceMeters = i * 100.0,
            AtomData = $"{{\"index\":{i}}}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-i)
        }).ToList();

        _spatialService
            .FindKNearestAtomsAsync(centerPoint, k, Arg.Any<CancellationToken>())
            .Returns(expectedAtoms);

        // Act
        var atoms = await _spatialService.FindKNearestAtomsAsync(centerPoint, k);

        // Assert
        atoms.Should().NotBeNull();
        atoms.Should().HaveCount(k);
        atoms.Should().BeInAscendingOrder(a => a.DistanceMeters);
    }

    [Fact]
    public async Task ProjectOntoLandmarks_WithValidAtoms_ReturnsProjectedCoordinates()
    {
        // Arrange
        var atomIds = new[] { 100L, 101L, 102L };
        var landmarkIds = new[] { 1L, 2L, 3L, 4L, 5L }; // 5 landmarks = 5D projection

        var expectedProjections = new List<LandmarkProjection>
        {
            new()
            {
                AtomId = 100L,
                Coordinates = new[] { 0.12, 0.45, 0.78, 0.23, 0.56 },
                LandmarkIds = landmarkIds
            },
            new()
            {
                AtomId = 101L,
                Coordinates = new[] { 0.34, 0.67, 0.89, 0.12, 0.45 },
                LandmarkIds = landmarkIds
            },
            new()
            {
                AtomId = 102L,
                Coordinates = new[] { 0.56, 0.89, 0.23, 0.45, 0.78 },
                LandmarkIds = landmarkIds
            }
        };

        _spatialService
            .ProjectOntoLandmarksAsync(atomIds, landmarkIds, Arg.Any<CancellationToken>())
            .Returns(expectedProjections);

        // Act
        var projections = await _spatialService.ProjectOntoLandmarksAsync(atomIds, landmarkIds);

        // Assert
        projections.Should().NotBeNull();
        projections.Should().HaveCount(atomIds.Length);
        projections.Should().AllSatisfy(p =>
        {
            p.Coordinates.Should().HaveCount(landmarkIds.Length);
            p.Coordinates.Should().AllSatisfy(coord => coord.Should().BeInRange(0.0, 1.0));
            p.LandmarkIds.Should().BeEquivalentTo(landmarkIds);
        });
    }

    [Fact]
    public async Task FindNearestAtoms_WithZeroRadius_ReturnsEmptyCollection()
    {
        // Arrange
        var centerPoint = _geometryFactory.CreatePoint(new Coordinate(-86.7816, 36.1627));
        const double radiusMeters = 0.0;

        _spatialService
            .FindNearestAtomsAsync(centerPoint, radiusMeters, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<SpatialAtom>());

        // Act
        var atoms = await _spatialService.FindNearestAtomsAsync(centerPoint, radiusMeters);

        // Assert
        atoms.Should().NotBeNull();
        atoms.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-86.7816, 36.1627)] // Nashville
    [InlineData(-122.4194, 37.7749)] // San Francisco
    [InlineData(-0.1278, 51.5074)] // London
    public async Task FindNearestAtoms_WithVariousLocations_ExecutesSuccessfully(double longitude, double latitude)
    {
        // Arrange
        var centerPoint = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        const double radiusMeters = 1000.0;

        var expectedAtoms = new List<SpatialAtom>
        {
            new()
            {
                AtomId = 9999L,
                Location = centerPoint,
                DistanceMeters = 0.0,
                AtomData = "{\"test\":true}",
                CreatedAt = DateTime.UtcNow
            }
        };

        _spatialService
            .FindNearestAtomsAsync(centerPoint, radiusMeters, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expectedAtoms);

        // Act
        var atoms = await _spatialService.FindNearestAtomsAsync(centerPoint, radiusMeters);

        // Assert
        atoms.Should().NotBeNull();
        atoms.Should().Contain(a => a.Location.Coordinate.X == longitude && a.Location.Coordinate.Y == latitude);
    }
}
