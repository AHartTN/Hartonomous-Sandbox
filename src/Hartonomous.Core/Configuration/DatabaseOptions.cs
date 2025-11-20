using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Database connection configuration options.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// SQL Server connection string for the Hartonomous database.
    /// </summary>
    [Required]
    public string HartonomousDb { get; set; } = string.Empty;
}
