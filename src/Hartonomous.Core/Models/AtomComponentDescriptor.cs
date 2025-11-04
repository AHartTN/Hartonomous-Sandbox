using System;

namespace Hartonomous.Core.Models;

/// <summary>
/// Describes an atom component contribution identified by an atom identifier and repetition count.
/// </summary>
public sealed class AtomComponentDescriptor
{
    /// <summary>
    /// Creates a descriptor for a component atom and the number of times it appears in the aggregate atom.
    /// </summary>
    /// <param name="atomId">Identifier of the component atom.</param>
    /// <param name="quantity">Number of occurrences for the component hash.</param>
    public AtomComponentDescriptor(long atomId, int quantity = 1)
    {
        if (atomId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(atomId), "Atom identifier must be positive.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        AtomId = atomId;
        Quantity = quantity;
    }

    /// <summary>
    /// Identifier of the component atom.
    /// </summary>
    public long AtomId { get; }

    /// <summary>
    /// Number of times the component occurs in sequence.
    /// </summary>
    public int Quantity { get; }
}
