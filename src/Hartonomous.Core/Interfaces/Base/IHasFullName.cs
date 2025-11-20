namespace Hartonomous.Core.Interfaces.Base;

/// <summary>
/// Represents an entity that has a full name composed of first and last name components.
/// Enables methods to depend only on name properties without requiring the entire entity.
/// </summary>
public interface IHasFullName
{
    /// <summary>
    /// Gets the first name.
    /// </summary>
    string FirstName { get; }

    /// <summary>
    /// Gets the last name.
    /// </summary>
    string LastName { get; }

    /// <summary>
    /// Gets the full name as a formatted string (typically "FirstName LastName").
    /// </summary>
    string FullName => $"{FirstName} {LastName}";
}
