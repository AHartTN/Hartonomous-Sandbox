using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;

namespace ModelIngestion.Tests;

/// <summary>
/// Quick test to verify SqlVector<T> availability in Microsoft.Data.SqlClient 6.1.2
/// </summary>
public class TestSqlVector
{
    public static void VerifyAvailability()
    {
        // Test 1: Create SqlVector from float array
        var vector1 = new SqlVector<float>(new float[] { 1.0f, 2.0f, 3.0f });
        Console.WriteLine($"✓ SqlVector<float> created: Type={vector1.GetType()}, IsNull={vector1.IsNull}, Length={vector1.Length}");

        // Test 2: Access vector data
        float[] values = vector1.Memory.ToArray();
        Console.WriteLine($"✓ Vector values: [{string.Join(", ", values)}]");

        // Test 3: Create null vector
        var nullVector = SqlVector<float>.CreateNull(768);
        Console.WriteLine($"✓ Null vector created: IsNull={nullVector.IsNull}, Length={nullVector.Length}");

        Console.WriteLine("\n✅ SUCCESS: SqlVector<T> is available in Microsoft.Data.SqlClient 6.1.2");
    }
}
