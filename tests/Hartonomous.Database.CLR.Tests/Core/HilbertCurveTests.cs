using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Hartonomous.Database.CLR.Tests.Core;

/// <summary>
/// Verification tests for Hilbert Curve locality preservation.
/// Based on audit requirements from verification matrix.
/// </summary>
public class HilbertCurveTests
{
    [Fact]
    public void Hilbert3D_ShouldBeDeterministic()
    {
        // Arrange
        long x = 100, y = 200, z = 300;
        int bits = 21;

        // Act
        var result1 = HilbertMath.Hilbert3D(x, y, z, bits);
        var result2 = HilbertMath.Hilbert3D(x, y, z, bits);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Hilbert3D_AndInverse_ShouldRoundTrip()
    {
        // Arrange
        long originalX = 123456;
        long originalY = 654321;
        long originalZ = 987654;
        int bits = 21;

        // Act
        long hilbert = HilbertMath.Hilbert3D(originalX, originalY, originalZ, bits);
        var (reconstructedX, reconstructedY, reconstructedZ) = HilbertMath.InverseHilbert3D(hilbert, bits);

        // Assert
        reconstructedX.Should().Be(originalX);
        reconstructedY.Should().Be(originalY);
        reconstructedZ.Should().Be(originalZ);
    }

    [Theory]
    [InlineData(0, 0, 0, 21)]
    [InlineData(1, 0, 0, 21)]
    [InlineData(0, 1, 0, 21)]
    [InlineData(0, 0, 1, 21)]
    [InlineData(1048575, 1048575, 1048575, 21)] // Max for 21 bits (2^21 - 1)
    public void Hilbert3D_ShouldHandleCornerCases(long x, long y, long z, int bits)
    {
        // Act
        var hilbert = HilbertMath.Hilbert3D(x, y, z, bits);
        var (rx, ry, rz) = HilbertMath.InverseHilbert3D(hilbert, bits);

        // Assert
        rx.Should().Be(x);
        ry.Should().Be(y);
        rz.Should().Be(z);
    }

    [Fact]
    public void Hilbert3D_ShouldPreserveLocality_ForNearbyPoints()
    {
        // Arrange - create nearby points in 3D space
        var center = (x: 500000L, y: 500000L, z: 500000L);
        var nearby1 = (x: 500001L, y: 500000L, z: 500000L); // 1 unit away in X
        var nearby2 = (x: 500000L, y: 500001L, z: 500000L); // 1 unit away in Y
        var faraway = (x: 900000L, y: 900000L, z: 900000L); // Much farther

        int bits = 21;

        // Act - compute Hilbert values
        long hCenter = HilbertMath.Hilbert3D(center.x, center.y, center.z, bits);
        long hNearby1 = HilbertMath.Hilbert3D(nearby1.x, nearby1.y, nearby1.z, bits);
        long hNearby2 = HilbertMath.Hilbert3D(nearby2.x, nearby2.y, nearby2.z, bits);
        long hFaraway = HilbertMath.Hilbert3D(faraway.x, faraway.y, faraway.z, bits);

        // Calculate 1D distances on Hilbert curve
        long distToNearby1 = Math.Abs(hCenter - hNearby1);
        long distToNearby2 = Math.Abs(hCenter - hNearby2);
        long distToFaraway = Math.Abs(hCenter - hFaraway);

        // Assert - nearby points should have smaller Hilbert distance than far points
        // This validates locality preservation
        distToNearby1.Should().BeLessThan(distToFaraway, 
            "Nearby point 1 should be closer on Hilbert curve than faraway point");
        distToNearby2.Should().BeLessThan(distToFaraway,
            "Nearby point 2 should be closer on Hilbert curve than faraway point");
    }

    [Fact]
    public void Hilbert3D_ShouldProduceUniqueValues_ForDifferentCoordinates()
    {
        // Arrange - create a grid of points
        int bits = 10; // Smaller for faster test
        var hilbertValues = new HashSet<long>();
        
        // Act - compute Hilbert values for a grid
        for (long x = 0; x < 32; x += 4)
        {
            for (long y = 0; y < 32; y += 4)
            {
                for (long z = 0; z < 32; z += 4)
                {
                    long hilbert = HilbertMath.Hilbert3D(x, y, z, bits);
                    hilbertValues.Add(hilbert);
                }
            }
        }

        // Assert - all values should be unique (no collisions)
        int expectedCount = (32 / 4) * (32 / 4) * (32 / 4); // 8 * 8 * 8 = 512
        hilbertValues.Count.Should().Be(expectedCount, 
            "Each distinct 3D coordinate should map to a unique Hilbert value");
    }

    [Fact]
    public void Hilbert3D_CollisionRate_ShouldBeLow_AtScale()
    {
        // Arrange - simulate dataset scale check
        int bits = 21;
        long maxCoord = (1L << bits) - 1; // 2^21 - 1 = 2,097,151
        var random = new Random(42);
        int sampleSize = 10000;
        var hilbertValues = new HashSet<long>();

        // Act - generate random points and compute Hilbert values
        for (int i = 0; i < sampleSize; i++)
        {
            long x = (long)(random.NextDouble() * maxCoord);
            long y = (long)(random.NextDouble() * maxCoord);
            long z = (long)(random.NextDouble() * maxCoord);
            
            long hilbert = HilbertMath.Hilbert3D(x, y, z, bits);
            hilbertValues.Add(hilbert);
        }

        // Calculate collision rate
        int collisions = sampleSize - hilbertValues.Count;
        double collisionRate = (double)collisions / sampleSize;

        // Assert - collision rate should be very low (< 0.01%)
        collisionRate.Should().BeLessThan(0.0001, 
            $"Collision rate should be < 0.01%, but was {collisionRate:P4} ({collisions}/{sampleSize})");
    }

    [Fact]
    public void Hilbert3D_ShouldBeFastEnough_For1000Points()
    {
        // Arrange
        int bits = 21;
        var random = new Random(42);
        var points = new (long x, long y, long z)[1000];
        long maxCoord = (1L << bits) - 1;

        for (int i = 0; i < 1000; i++)
        {
            points[i] = (
                (long)(random.NextDouble() * maxCoord),
                (long)(random.NextDouble() * maxCoord),
                (long)(random.NextDouble() * maxCoord)
            );
        }

        // Act - measure time to compute 1000 Hilbert values
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            HilbertMath.Hilbert3D(points[i].x, points[i].y, points[i].z, bits);
        }
        stopwatch.Stop();

        // Assert - should complete in reasonable time (< 100ms for 1000 points)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            $"Computing 1000 Hilbert values took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }
}
