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
    private const int MaxHashLength = 64;

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

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(false); // IsNull flag
            writer.Write(SerializerVersion);
            writer.Write(runs.Count);

            foreach (var run in runs)
            {
                writer.Write(run.Hash.Length);
                writer.Write(run.Hash);
                writer.Write(run.Count);
            }
        }

        return buffer.ToArray();
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

            ValidateHash(descriptor.ComponentHash);

            var hash = Clone(descriptor.ComponentHash);
            var count = descriptor.Quantity;

            if (runs.Count > 0 && HashesEqual(runs[^1].Hash, hash))
            {
                runs[^1] = runs[^1] with { Count = checked(runs[^1].Count + count) };
            }
            else
            {
                runs.Add(new ComponentRun(hash, count));
            }
        }

        return runs;
    }

    private static void ValidateHash(IReadOnlyCollection<byte> hash)
    {
        if (hash.Count == 0 || hash.Count > MaxHashLength)
        {
            throw new ArgumentOutOfRangeException(nameof(hash), $"Component hash length must be between 1 and {MaxHashLength} bytes.");
        }
    }

    private static bool HashesEqual(IReadOnlyList<byte> left, IReadOnlyList<byte> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] Clone(IReadOnlyList<byte> source)
    {
        var clone = new byte[source.Count];
        for (var index = 0; index < source.Count; index++)
        {
            clone[index] = source[index];
        }

        return clone;
    }

    private readonly record struct ComponentRun(byte[] Hash, int Count);
}
