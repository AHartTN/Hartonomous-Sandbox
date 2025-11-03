using Microsoft.Data.SqlTypes;

namespace Hartonomous.UnitTests.Core;

public class SqlVectorAvailabilityTests
{
    [Fact]
    public void SqlVectorFloat_CanBeCreatedAndMaterialised()
    {
        var vector = new SqlVector<float>(new float[] { 1f, 2f, 3f });

        Assert.False(vector.IsNull);
        Assert.Equal(3, vector.Length);
        Assert.Equal(new[] { 1f, 2f, 3f }, vector.Memory.ToArray());

        var nullVector = SqlVector<float>.CreateNull(768);
        Assert.True(nullVector.IsNull);
        Assert.Equal(768, nullVector.Length);
    }
}
