using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Hartonomous.Clr.Core;
using Xunit;

namespace Hartonomous.Database.CLR.Tests.Core;

/// <summary>
/// Verification tests for LandmarkProjection determinism and correctness.
/// Based on audit requirements from verification matrix.
/// </summary>
public class LandmarkProjectionTests
{
    [Fact]
    public void ProjectTo3D_ShouldBeDeterministic_WhenCalledMultipleTimes()
    {
        // Arrange
        var vector = CreateTestVector(seed: 42, dimensions: 1998);

        // Act
        var result1 = LandmarkProjection.ProjectTo3D(vector);
        var result2 = LandmarkProjection.ProjectTo3D(vector);

        // Assert - should be EXACTLY the same (bitwise identical)
        result1.X.Should().Be(result2.X);
        result1.Y.Should().Be(result2.Y);
        result1.Z.Should().Be(result2.Z);
    }

    [Fact]
    public void ProjectTo3D_ShouldProduceSameResults_AcrossMultipleInstances()
    {
        // Arrange
        var vector = CreateTestVector(seed: 123, dimensions: 1998);

        // Act - call multiple times to ensure static initialization is consistent
        var results = new List<(double X, double Y, double Z)>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(LandmarkProjection.ProjectTo3D(vector));
        }

        // Assert - all should be identical
        var first = results[0];
        foreach (var result in results.Skip(1))
        {
            result.X.Should().Be(first.X);
            result.Y.Should().Be(first.Y);
            result.Z.Should().Be(first.Z);
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void ProjectTo3D_ShouldBeDeterministic_ForLargeBatches(int batchSize)
    {
        // Arrange - create unique vectors
        var vectors = Enumerable.Range(0, batchSize)
            .Select(i => CreateTestVector(seed: i, dimensions: 1998))
            .ToList();

        // Act - project all vectors twice
        var firstPass = vectors.Select(v => LandmarkProjection.ProjectTo3D(v)).ToList();
        var secondPass = vectors.Select(v => LandmarkProjection.ProjectTo3D(v)).ToList();

        // Assert - compare each pair
        for (int i = 0; i < batchSize; i++)
        {
            firstPass[i].X.Should().Be(secondPass[i].X, because: $"Vector {i} X coordinate should be deterministic");
            firstPass[i].Y.Should().Be(secondPass[i].Y, because: $"Vector {i} Y coordinate should be deterministic");
            firstPass[i].Z.Should().Be(secondPass[i].Z, because: $"Vector {i} Z coordinate should be deterministic");
        }
    }

    [Fact]
    public void ProjectTo3D_ShouldNotProduceNaN_ForValidVectors()
    {
        // Arrange
        var testVectors = new[]
        {
            CreateTestVector(seed: 1, dimensions: 1998),
            CreateTestVector(seed: 999, dimensions: 1998),
            CreateZeroVector(dimensions: 1998),
            CreateOnesVector(dimensions: 1998)
        };

        // Act & Assert
        foreach (var vector in testVectors)
        {
            var result = LandmarkProjection.ProjectTo3D(vector);
            
            double.IsNaN(result.X).Should().BeFalse("X should not be NaN");
            double.IsNaN(result.Y).Should().BeFalse("Y should not be NaN");
            double.IsNaN(result.Z).Should().BeFalse("Z should not be NaN");
            
            double.IsInfinity(result.X).Should().BeFalse("X should not be infinity");
            double.IsInfinity(result.Y).Should().BeFalse("Y should not be infinity");
            double.IsInfinity(result.Z).Should().BeFalse("Z should not be infinity");
        }
    }

    [Fact]
    public void ProjectTo3D_ShouldPreserveRelativeDistances_Approximately()
    {
        // Arrange - create 3 vectors where we know relative distances
        var v1 = CreateTestVector(seed: 1, dimensions: 1998);
        var v2 = CreateTestVector(seed: 2, dimensions: 1998);
        var v3 = CreateTestVector(seed: 100, dimensions: 1998); // More distant seed

        // Calculate high-dimensional distances
        var highDimDist12 = EuclideanDistance(v1, v2);
        var highDimDist13 = EuclideanDistance(v1, v3);

        // Act - project to 3D
        var proj1 = LandmarkProjection.ProjectTo3D(v1);
        var proj2 = LandmarkProjection.ProjectTo3D(v2);
        var proj3 = LandmarkProjection.ProjectTo3D(v3);

        // Calculate 3D distances
        var lowDimDist12 = Distance3D(proj1, proj2);
        var lowDimDist13 = Distance3D(proj1, proj3);

        // Assert - relative ordering should be preserved
        // Not exact distances (that's impossible with dimensionality reduction),
        // but if v1-v2 is closer than v1-v3 in high-dim space,
        // it should also be closer in 3D space
        if (highDimDist12 < highDimDist13)
        {
            lowDimDist12.Should().BeLessThan(lowDimDist13, 
                "Projection should preserve relative distance ordering");
        }
    }

    [Fact]
    public void ProjectTo3D_ShouldThrow_WhenVectorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LandmarkProjection.ProjectTo3D(null!));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(2000)]
    public void ProjectTo3D_ShouldThrow_WhenVectorHasWrongDimensions(int wrongDimension)
    {
        // Arrange
        var wrongVector = new float[wrongDimension];

        // Act & Assert
        if (wrongDimension != 1998)
        {
            Assert.Throws<ArgumentException>(() => LandmarkProjection.ProjectTo3D(wrongVector));
        }
    }

    #region Helper Methods

    private static float[] CreateTestVector(int seed, int dimensions)
    {
        var random = new Random(seed);
        var vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = (float)random.NextDouble();
        }
        return vector;
    }

    private static float[] CreateZeroVector(int dimensions)
    {
        return new float[dimensions]; // All zeros
    }

    private static float[] CreateOnesVector(int dimensions)
    {
        var vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = 1.0f;
        }
        return vector;
    }

    private static double EuclideanDistance(float[] v1, float[] v2)
    {
        double sum = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            double diff = v1[i] - v2[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    private static double Distance3D((double X, double Y, double Z) p1, (double X, double Y, double Z) p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        double dz = p1.Z - p2.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    #endregion
}
