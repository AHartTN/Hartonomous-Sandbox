using System;
using System.IO;
using FluentAssertions;
using Hartonomous.Clr.Core;
using Xunit;

namespace Hartonomous.Clr.Tests
{
    public class BinarySerializationHelpersTests
    {
        [Fact]
        public void WriteFloatArray_ValidArray_WritesCorrectly()
        {
            // Arrange
            var array = new[] { 1.5f, 2.5f, 3.5f };
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteFloatArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadFloatArray();
            result.Should().BeEquivalentTo(array);
        }

        [Fact]
        public void WriteFloatArray_NullArray_WritesNegativeLength()
        {
            // Arrange
            float[] array = null;
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteFloatArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadFloatArray();
            result.Should().BeNull();
        }

        [Fact]
        public void WriteFloatArray_EmptyArray_WritesZeroLength()
        {
            // Arrange
            var array = Array.Empty<float>();
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteFloatArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadFloatArray();
            result.Should().BeEmpty();
        }

        [Fact]
        public void ReadFloatArray_ValidData_ReturnsCorrectArray()
        {
            // Arrange
            var expected = new[] { 10.5f, 20.5f, 30.5f, 40.5f };
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.WriteFloatArray(expected);
            writer.Flush();

            // Act
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadFloatArray();

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void WriteDoubleArray_ValidArray_WritesCorrectly()
        {
            // Arrange
            var array = new[] { 1.5, 2.5, 3.5 };
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteDoubleArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadDoubleArray();
            result.Should().BeEquivalentTo(array);
        }

        [Fact]
        public void WriteDoubleArray_NullArray_WritesNegativeLength()
        {
            // Arrange
            double[] array = null;
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteDoubleArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadDoubleArray();
            result.Should().BeNull();
        }

        [Fact]
        public void RoundTrip_LargeFloatArray_PreservesData()
        {
            // Arrange
            var array = new float[1000];
            for (int i = 0; i < array.Length; i++)
                array[i] = (float)Math.Sin(i * 0.1);

            using var ms = new MemoryStream();

            // Act
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteFloatArray(array);
                writer.Flush();
            }

            ms.Position = 0;
            float[] result;
            using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                result = reader.ReadFloatArray();
            }

            // Assert
            result.Should().HaveCount(1000);
            result.Should().BeEquivalentTo(array);
        }

        [Fact]
        public void RoundTrip_LargeDoubleArray_PreservesData()
        {
            // Arrange
            var array = new double[1000];
            for (int i = 0; i < array.Length; i++)
                array[i] = Math.Cos(i * 0.1);

            using var ms = new MemoryStream();

            // Act
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteDoubleArray(array);
                writer.Flush();
            }

            ms.Position = 0;
            double[] result;
            using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                result = reader.ReadDoubleArray();
            }

            // Assert
            result.Should().HaveCount(1000);
            result.Should().BeEquivalentTo(array);
        }

        [Fact]
        public void WriteIntArray_ValidArray_WritesCorrectly()
        {
            // Arrange
            var array = new[] { 1, 2, 3, 4, 5 };
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteIntArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadIntArray();
            result.Should().BeEquivalentTo(array);
        }

        [Fact]
        public void WriteLongArray_ValidArray_WritesCorrectly()
        {
            // Arrange
            var array = new[] { 100L, 200L, 300L };
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Act
            writer.WriteLongArray(array);
            writer.Flush();

            // Assert
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var result = reader.ReadLongArray();
            result.Should().BeEquivalentTo(array);
        }

        [Fact]
        public void MultipleArrays_InSequence_ReadCorrectly()
        {
            // Arrange
            var floatArray = new[] { 1.5f, 2.5f };
            var doubleArray = new[] { 10.5, 20.5 };
            var intArray = new[] { 100, 200 };

            using var ms = new MemoryStream();

            // Act - Write
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteFloatArray(floatArray);
                writer.WriteDoubleArray(doubleArray);
                writer.WriteIntArray(intArray);
                writer.Flush();
            }

            // Act - Read
            ms.Position = 0;
            float[] resultFloat;
            double[] resultDouble;
            int[] resultInt;

            using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                resultFloat = reader.ReadFloatArray();
                resultDouble = reader.ReadDoubleArray();
                resultInt = reader.ReadIntArray();
            }

            // Assert
            resultFloat.Should().BeEquivalentTo(floatArray);
            resultDouble.Should().BeEquivalentTo(doubleArray);
            resultInt.Should().BeEquivalentTo(intArray);
        }
    }
}
