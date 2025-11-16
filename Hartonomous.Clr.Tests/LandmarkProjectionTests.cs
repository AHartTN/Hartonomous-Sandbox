using System;
using Xunit;
using FluentAssertions;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.Tests
{
    public class LandmarkProjectionTests
    {
        [Fact]
        public void ProjectTo3D_WithZeroVector_ShouldReturnOrigin()
        {
            var zero = new float[1998];
            var result = LandmarkProjection.ProjectTo3D(zero);
            result.X.Should().BeApproximately(0, 0.001);
            result.Y.Should().BeApproximately(0, 0.001);
            result.Z.Should().BeApproximately(0, 0.001);
        }

        [Fact]
        public void ProjectTo3D_IsDeterministic()
        {
            var vector = CreateRandomVector(seed: 42);
            var result1 = LandmarkProjection.ProjectTo3D(vector);
            var result2 = LandmarkProjection.ProjectTo3D(vector);
            result1.X.Should().Be(result2.X);
            result1.Y.Should().Be(result2.Y);
            result1.Z.Should().Be(result2.Z);
        }

        private static float[] CreateRandomVector(int seed)
        {
            var random = new Random(seed);
            var vector = new float[1998];
            for (int i = 0; i < 1998; i++)
            {
                vector[i] = (float)(random.NextDouble() * 2 - 1);
            }
            return vector;
        }
    }
}
