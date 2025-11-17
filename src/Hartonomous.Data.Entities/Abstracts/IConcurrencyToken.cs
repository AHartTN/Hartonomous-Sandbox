namespace Hartonomous.Data.Entities;

/// <summary>
/// Interface for entities with optimistic concurrency control.
/// Prevents data loss from concurrent updates (crash/timeout prevention).
/// EF Core uses RowVersion for conflict detection.
/// </summary>
public interface IConcurrencyToken
{
    /// <summary>
    /// Row version for optimistic concurrency. 
    /// Timestamp/rowversion in SQL Server, auto-incremented by database.
    /// </summary>
    byte[]? RowVersion { get; set; }
}
