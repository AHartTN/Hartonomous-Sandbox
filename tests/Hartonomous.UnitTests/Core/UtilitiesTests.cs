using System;
using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Utilities;
using Hartonomous.Data.Entities;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.UnitTests.Core;

public class UtilitiesTests
{
    [Fact]
    public void VectorUtility_EnsureSupportedDimension_AllowsValidSizes()
    {
        var validDimension = VectorUtility.SqlVectorMaxDimensions;

        VectorUtility.EnsureSupportedDimension(1);
        VectorUtility.EnsureSupportedDimension(validDimension);
    }

    [Fact]
    public void VectorUtility_EnsureSupportedDimension_ThrowsForInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VectorUtility.EnsureSupportedDimension(0));
        Assert.Throws<NotSupportedException>(() => VectorUtility.EnsureSupportedDimension(VectorUtility.SqlVectorMaxDimensions + 1));
    }

    [Fact]
    public void VectorUtility_PadToSqlLength_PreservesDataAndSignalsPadding()
    {
        var source = new float[] { 1f, 2f, 3f };

        var padded = VectorUtility.PadToSqlLength(source, out var usedPadding);

        Assert.True(usedPadding);
        Assert.Equal(VectorUtility.SqlVectorMaxDimensions, padded.Length);
        Assert.Equal(source, padded.Take(source.Length));
        Assert.True(padded.Skip(source.Length).All(value => value == 0f));
    }

    [Fact]
    public void VectorUtility_Materialize_TrimsToRequestedDimension()
    {
        var sqlVector = new SqlVector<float>(new[] { 1f, 2f, 3f, 4f });

        var materialised = VectorUtility.Materialize(sqlVector, 2);

        Assert.Equal(new[] { 1f, 2f }, materialised);
    }

    [Fact]
    public void VectorUtility_MaterializeFromComponents_OrdersAndClamps()
    {
        var components = new[]
        {
            new AtomEmbeddingComponent { ComponentIndex = 3, ComponentValue = 0.2f },
            new AtomEmbeddingComponent { ComponentIndex = 0, ComponentValue = 1.5f },
            new AtomEmbeddingComponent { ComponentIndex = 1, ComponentValue = -0.5f },
        };

        var materialised = VectorUtility.MaterializeFromComponents(components, expectedDimension: 4);

        Assert.Equal(4, materialised.Length);
        Assert.Equal(1.5f, materialised[0]);
        Assert.Equal(-0.5f, materialised[1]);
        Assert.Equal(0f, materialised[2]);
        Assert.Equal(0.2f, materialised[3]);
    }

    [Fact]
    public void VectorUtility_ComputeCosineDistance_HandlesIdenticalAndOrthogonal()
    {
        var identical = VectorUtility.ComputeCosineDistance(new[] { 1f, 2f, 3f }, new[] { 1f, 2f, 3f });
        var orthogonal = VectorUtility.ComputeCosineDistance(new[] { 1f, 0f }, new[] { 0f, 1f });
        var empty = VectorUtility.ComputeCosineDistance(Array.Empty<float>(), Array.Empty<float>());

        Assert.Equal(0d, identical, 5);
        Assert.Equal(1d, orthogonal, 5);
        Assert.Equal(1d, empty, 5);
    }

    [Fact]
    public void HashUtility_ComputeSHA256Hash_ReturnsLowerHex()
    {
        const string input = "test";
        const string expected = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

        var hash = HashUtility.ComputeSHA256Hash(input);
        var bytes = HashUtility.ComputeSHA256Bytes(input);

        Assert.Equal(expected, hash);
        Assert.Equal(expected, Convert.ToHexString(bytes).ToLowerInvariant());
    }

    [Fact]
    public void GeometryConverter_ToLineString_BuildsIndexedCoordinates()
    {
        var weights = new[] { 0.1f, -0.2f, 0.3f };

        var lineString = GeometryConverter.ToLineString(weights, srid: 4326);

        Assert.Equal(3, lineString.NumPoints);
        Assert.Equal(4326, lineString.SRID);

        for (var i = 0; i < weights.Length; i++)
        {
            Assert.Equal(i, lineString.GetCoordinateN(i).X, 5);
            Assert.Equal(weights[i], lineString.GetCoordinateN(i).Y, 5);
        }
    }

    [Fact]
    public void GeometryConverter_FromLineString_RoundTripsWeights()
    {
        var factory = new GeometryFactory();
        var coordinates = new[]
        {
            new Coordinate(0, 0.5),
            new Coordinate(1, -1.25),
            new Coordinate(2, 2.0)
        };
        var line = factory.CreateLineString(coordinates);

        var recovered = GeometryConverter.FromLineString(line);

        Assert.Equal(new[] { 0.5f, -1.25f, 2f }, recovered);
    }

    [Fact]
    public void GeometryConverter_ToLineString_ThrowsWhenWeightsEmpty()
    {
        Assert.Throws<ArgumentException>(() => GeometryConverter.ToLineString(Array.Empty<float>()));
    }
}
