using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Contract for entities that maintain reference tracking metadata within the atom substrate.
/// </summary>
public interface IReferenceTrackedEntity
{
    /// <summary>
    /// Gets or sets the number of references recorded for the entity.
    /// </summary>
    long ReferenceCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was first recorded.
    /// </summary>
    DateTime FirstSeen { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was last referenced.
    /// </summary>
    DateTime LastReferenced { get; set; }
}
