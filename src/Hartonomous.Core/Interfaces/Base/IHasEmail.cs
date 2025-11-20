namespace Hartonomous.Core.Interfaces.Base;

/// <summary>
/// Represents an entity that has an email address.
/// Enables methods to depend only on email property without requiring the entire entity.
/// </summary>
public interface IHasEmail
{
    /// <summary>
    /// Gets the email address.
    /// </summary>
    string Email { get; }
}
