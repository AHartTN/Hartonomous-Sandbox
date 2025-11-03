using System;

namespace Hartonomous.Core.Models;

/// <summary>
/// Describes an atom component contribution identified by a hash and repetition count.
/// </summary>
public sealed class AtomComponentDescriptor
{
    /// <summary>
    /// Creates a descriptor for a component hash and the number of times it appears in the aggregate atom.
    /// </summary>
    /// <param name="componentHash">Hash bytes identifying the component payload.</param>
    /// <param name="quantity">Number of occurrences for the component hash.</param>
    public AtomComponentDescriptor(byte[] componentHash, int quantity = 1)
    {
        if (componentHash is null)
        {
            throw new ArgumentNullException(nameof(componentHash));
        }

        if (componentHash.Length == 0)
        {
            throw new ArgumentException("Component hash must contain at least one byte.", nameof(componentHash));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        ComponentHash = Clone(componentHash);
        Quantity = quantity;
    }

    /// <summary>
    /// Copy of the component hash bytes.
    /// </summary>
    public byte[] ComponentHash { get; }

    /// <summary>
    /// Number of times the component occurs in sequence.
    /// </summary>
    public int Quantity { get; }

    private static byte[] Clone(byte[] source)
    {
        var clone = new byte[source.Length];
        Buffer.BlockCopy(source, 0, clone, 0, source.Length);
        return clone;
    }
}
