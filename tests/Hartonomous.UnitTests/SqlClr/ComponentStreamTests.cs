using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Reflection;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;

namespace Hartonomous.UnitTests.SqlClr;

public static class ComponentStreamTestData
{
    public static IReadOnlyList<AtomComponentDescriptor> BuildSampleComponents() => new List<AtomComponentDescriptor>
    {
        new(Convert.FromHexString("AA01"), 1),
        new(Convert.FromHexString("AA01"), 2),
        new(Convert.FromHexString("BBFF00"), 3),
        new(Convert.FromHexString("CC10"), 1),
        new(Convert.FromHexString("CC10"), 4)
    };

    public static IReadOnlyList<(byte[] Hash, int Count)> BuildExpectedRuns() => new List<(byte[] Hash, int Count)>
    {
        (Convert.FromHexString("AA01"), 3),
        (Convert.FromHexString("BBFF00"), 3),
        (Convert.FromHexString("CC10"), 5)
    };
}

public class ComponentStreamTests
{
    [Fact]
    public void ComponentStream_RoundtripMatchesClrImplementation()
    {
        var components = ComponentStreamTestData.BuildSampleComponents();
        var expectedRuns = ComponentStreamTestData.BuildExpectedRuns();

        var encoded = ComponentStreamEncoder.Encode(components);
        Assert.NotNull(encoded);
        var expectedBase64 = Convert.ToBase64String(encoded!);

        var componentStreamType = LoadComponentStreamType();
        var componentStream = Activator.CreateInstance(componentStreamType)!;

        componentStream = InvokeInitialize(componentStreamType, componentStream);

        foreach (var descriptor in components)
        {
            componentStream = InvokeAppend(componentStreamType, componentStream, descriptor.ComponentHash, descriptor.Quantity);
        }

        var clrBase64 = InvokeToString(componentStreamType, componentStream);
        Assert.Equal(expectedBase64, clrBase64);

        var parsed = InvokeParse(componentStreamType, expectedBase64);
        var roundtripBase64 = InvokeToString(componentStreamType, parsed);
        Assert.Equal(expectedBase64, roundtripBase64);

        var totalComponents = ((SqlInt64)GetProperty(componentStreamType, parsed, "TotalComponents")).Value;
        Assert.Equal(components.Sum(c => c.Quantity), totalComponents);

        var runCount = ((SqlInt32)GetProperty(componentStreamType, parsed, "RunCount")).Value;
        Assert.Equal(expectedRuns.Count, runCount);

        for (var index = 0; index < expectedRuns.Count; index++)
        {
            var actualHash = InvokeGetComponentHash(componentStreamType, parsed, index);
            Assert.Equal(expectedRuns[index].Hash, actualHash.Value);

            var actualCount = ((SqlInt32)InvokeGetRepetitionCount(componentStreamType, parsed, index)).Value;
            Assert.Equal(expectedRuns[index].Count, actualCount);
        }
    }

    private static Type LoadComponentStreamType()
    {
        var assemblyPath = ResolveSqlClrAssemblyPath();
        Assert.True(File.Exists(assemblyPath), $"SqlClrFunctions.dll not found at '{assemblyPath}'. Build the SqlClr project before running tests.");

        var assembly = Assembly.LoadFrom(assemblyPath);
        return assembly.GetType("SqlClrFunctions.ComponentStream", throwOnError: true)!;
    }

    private static string ResolveSqlClrAssemblyPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var configurationDirectory = Directory.GetParent(baseDirectory)!.Name;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "src", "SqlClr", "bin", configurationDirectory, "SqlClrFunctions.dll");
    }

    private static object InvokeInitialize(Type componentStreamType, object target)
    {
        var method = componentStreamType.GetMethod("Initialize", Type.EmptyTypes)!;
        return method.Invoke(target, Array.Empty<object>())!;
    }

    private static object InvokeAppend(Type componentStreamType, object target, byte[] hash, int repetitions)
    {
        var method = componentStreamType.GetMethod("Append", new[] { typeof(SqlBytes), typeof(SqlInt32) })!;
        return method.Invoke(target, new object[] { new SqlBytes(hash), new SqlInt32(repetitions) })!;
    }

    private static string InvokeToString(Type componentStreamType, object target)
    {
        var method = componentStreamType.GetMethod("ToString", Type.EmptyTypes)!;
        return (string)method.Invoke(target, Array.Empty<object>())!;
    }

    private static object InvokeParse(Type componentStreamType, string base64)
    {
        var method = componentStreamType.GetMethod("Parse", new[] { typeof(SqlString) })!;
        return method.Invoke(null, new object[] { new SqlString(base64) })!;
    }

    private static SqlBytes InvokeGetComponentHash(Type componentStreamType, object target, int ordinal)
    {
        var method = componentStreamType.GetMethod("GetComponentHash", new[] { typeof(SqlInt32) })!;
        return (SqlBytes)method.Invoke(target, new object[] { new SqlInt32(ordinal) })!;
    }

    private static SqlInt32 InvokeGetRepetitionCount(Type componentStreamType, object target, int ordinal)
    {
        var method = componentStreamType.GetMethod("GetRepetitionCount", new[] { typeof(SqlInt32) })!;
        return (SqlInt32)method.Invoke(target, new object[] { new SqlInt32(ordinal) })!;
    }

    private static object GetProperty(Type componentStreamType, object target, string propertyName)
    {
        var property = componentStreamType.GetProperty(propertyName)!;
        return property.GetValue(target)!;
    }
}
