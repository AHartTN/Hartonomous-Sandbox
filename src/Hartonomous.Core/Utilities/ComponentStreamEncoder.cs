using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hartonomous.Core.Models;

namespace Hartonomous.Core.Utilities;

/// <summary>
/// Encodes component descriptors into the binary format consumed by the provenance.ComponentStream CLR UDT.
/// </summary>
public static class ComponentStreamEncoder
{
    private const int SerializerVersion = 1;

    /// <summary>
    /// Serialises component descriptors into the UDT binary layout, merging contiguous identical hashes.
    /// </summary>
    /// <param name="components">Component descriptors to encode.</param>
    /// <returns>Binary payload suitable for provenance.ComponentStream, or <c>null</c> when no components exist.</returns>
    public static byte[]? Encode(IReadOnlyList<AtomComponentDescriptor>? components)
    {
        if (components is null || components.Count == 0)
        {
            return null;
        }

        var runs = BuildRuns(components);
        if (runs.Count == 0)
        {
            return null;
        }

        return EncodeRuns(runs);
    }

    private static List<ComponentRun> BuildRuns(IEnumerable<AtomComponentDescriptor> components)
    {
        var runs = new List<ComponentRun>();

        foreach (var descriptor in components)
        {
            if (descriptor is null)
            {
                continue;
            }

            ValidateAtom(descriptor.AtomId);

            var count = descriptor.Quantity;
            if (runs.Count > 0 && runs[^1].AtomId == descriptor.AtomId)
            {
                runs[^1] = runs[^1] with { Count = checked(runs[^1].Count + count) };
            }
            else
            {
                runs.Add(new ComponentRun(descriptor.AtomId, count));
            }
        }

        return runs;
    }

    private static byte[] EncodeRuns(List<ComponentRun> runs)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(false);
            writer.Write(SerializerVersion);
            writer.Write(runs.Count);

            foreach (var run in runs)
            {
                writer.Write(run.AtomId);
                writer.Write(run.Count);
            }
        }

        return buffer.ToArray();
    }

    private static void ValidateAtom(long atomId)
    {
        if (atomId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(atomId), "Component atom identifier must be positive.");
        }
    }

    private readonly record struct ComponentRun(long AtomId, int Count);
}
