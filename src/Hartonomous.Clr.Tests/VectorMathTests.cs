using System;
using Xunit;
using FluentAssertions;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.Tests
{
    public class VectorMathTests
    {
        [Fact]
        public void DotProduct_Should_ReturnCorrectValue()
        {
            var a = new float[] { 1.0f, 2.0f, 3.0f };
            var b = new float[] { 4.0f, 5.0f, 6.0f };
            var result = VectorMath.DotProduct(a, b);
            result.Should().BeApproximately(32.0f, precision: 0.001f);
        }

        [Fact]
        public void DotProduct_Should_UseSIMD_ForLargeVectors()
        {
            var size = 10000;
            var a = new float[size];
            var b = new float[size];
            for (int i = 0; i < size; i++)
            {
                a[i] = (float)i;
                b[i] = (float)i;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = VectorMath.DotProduct(a, b);
            sw.Stop();
            sw.ElapsedMilliseconds.Should().BeLessThan(10);
        }
    }
}
