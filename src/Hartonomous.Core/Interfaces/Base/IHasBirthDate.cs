namespace Hartonomous.Core.Interfaces.Base;

/// <summary>
/// Represents an entity that has a birth date.
/// Enables methods to depend only on birth date property without requiring the entire entity.
/// </summary>
public interface IHasBirthDate
{
    /// <summary>
    /// Gets the birth date.
    /// </summary>
    DateTime BirthDate { get; }

    /// <summary>
    /// Gets the age in years as of the current date.
    /// </summary>
    int Age => (int)((DateTime.UtcNow - BirthDate).TotalDays / 365.25);
}
