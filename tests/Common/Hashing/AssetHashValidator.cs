using System;
using Hartonomous.Testing.Common;
using Xunit;

namespace Hartonomous.Testing.Common.Hashing;

public static class AssetHashValidator
{
    public static void AssertAssetHash(string relativePath, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            throw new ArgumentException("Expected hash must be provided", nameof(expectedSha256));
        }

        var actual = TestData.ComputeSha256(relativePath);
        Assert.Equal(expectedSha256, actual);
    }
}
