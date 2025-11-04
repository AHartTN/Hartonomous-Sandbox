using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
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
        new(101, 1),
        new(101, 2),
        new(25001, 3),
        new(98765, 1),
        new(98765, 4)
    };

    public static IReadOnlyList<(long AtomId, int Count)> BuildExpectedRuns() => new List<(long AtomId, int Count)>
    {
        (101, 3),
        (25001, 3),
        (98765, 5)
    };
}

public class ComponentStreamTests
{
    [RequiresUnreferencedCode("Reflection-based verification of SQL CLR component stream")]
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
            componentStream = InvokeAppend(componentStreamType, componentStream, descriptor.AtomId, descriptor.Quantity);
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
            var actualAtomId = ((SqlInt64)InvokeGetComponentAtomId(componentStreamType, parsed, index)).Value;
            Assert.Equal(expectedRuns[index].AtomId, actualAtomId);

            var actualCount = ((SqlInt32)InvokeGetRepetitionCount(componentStreamType, parsed, index)).Value;
            Assert.Equal(expectedRuns[index].Count, actualCount);
        }
    }

    [RequiresUnreferencedCode("Reflection-based loading of SQL CLR component stream type for tests.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
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
        var tfmDirectory = new DirectoryInfo(baseDirectory).Name;
        var configurationDirectory = Directory.GetParent(baseDirectory)!.Name;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", ".."));

        var binRoot = Path.Combine(repoRoot, "src", "SqlClr", "bin");
        if (Directory.Exists(binRoot))
        {
            var matches = Directory.GetFiles(binRoot, "SqlClrFunctions.dll", SearchOption.AllDirectories);
            var prioritized = matches
                .OrderByDescending(path => path.Contains(configurationDirectory, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(path => path.Contains(tfmDirectory, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(prioritized))
            {
                return prioritized;
            }
        }

        return Path.Combine(binRoot, configurationDirectory, "SqlClrFunctions.dll");
    }

    private static object InvokeInitialize([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentStreamType, object target)
    {
        var method = componentStreamType.GetMethod("Initialize", Type.EmptyTypes)!;
        return method.Invoke(target, Array.Empty<object>())!;
    }

    private static object InvokeAppend([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentStreamType, object target, long atomId, int repetitions)
    {
        var method = componentStreamType.GetMethod("Append", new[] { typeof(SqlInt64), typeof(SqlInt32) })!;
        return method.Invoke(target, new object[] { new SqlInt64(atomId), new SqlInt32(repetitions) })!;
    }

    private static string InvokeToString([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentStreamType, object target)
    {
        var method = componentStreamType.GetMethod("ToString", Type.EmptyTypes)!;
        return (string)method.Invoke(target, Array.Empty<object>())!;
    }

    [RequiresUnreferencedCode("Reflection-based invocation of ComponentStream.Parse")] // trim-safety not relevant for test harness
    private static object InvokeParse([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentStreamType, string base64)
    {
        var method = componentStreamType.GetMethod("Parse", new[] { typeof(SqlString) })!;
        return method.Invoke(null, new object[] { new SqlString(base64) })!;
    }

    [RequiresUnreferencedCode("Reflection-based invocation for SQL CLR verification")]
    private static SqlInt64 InvokeGetComponentAtomId([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentStreamType, object target, int ordinal)
    {
        var method = componentStreamType.GetMethod("GetComponentAtomId", new[] { typeof(SqlInt32) })!;
        return (SqlInt64)method.Invoke(target, new object[] { new SqlInt32(ordinal) })!;
    }

    [RequiresUnreferencedCode("Reflection-based invocation for SQL CLR verification")]
    private static SqlInt32 InvokeGetRepetitionCount([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentStreamType, object target, int ordinal)
    {
        var method = componentStreamType.GetMethod("GetRepetitionCount", new[] { typeof(SqlInt32) })!;
        return (SqlInt32)method.Invoke(target, new object[] { new SqlInt32(ordinal) })!;
    }

    [RequiresUnreferencedCode("Reflection-based invocation for SQL CLR verification")]
    private static object GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type componentStreamType, object target, string propertyName)
    {
        var property = componentStreamType.GetProperty(propertyName)!;
        return property.GetValue(target)!;
    }
}
