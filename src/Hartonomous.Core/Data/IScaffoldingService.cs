namespace Hartonomous.Core.Data;

/// <summary>
/// Service interface for EF Core entity scaffolding operations.
/// Encapsulates database-first schema synchronization.
/// </summary>
public interface IScaffoldingService
{
    /// <summary>
    /// Scaffolds entity classes and DbContext from the database schema.
    /// </summary>
    /// <param name="options">Scaffolding configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the scaffolding operation.</returns>
    Task<ScaffoldingResult> ScaffoldAsync(
        ScaffoldingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the current entity model matches the database schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any discrepancies found.</returns>
    Task<ValidationResult> ValidateSchemaAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for database scaffolding.
/// </summary>
public sealed class ScaffoldingOptions
{
    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Gets or sets the database provider (e.g., "Microsoft.EntityFrameworkCore.SqlServer").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets or sets the project path where entities will be generated.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Gets or sets the output directory for entity classes.
    /// </summary>
    public string OutputDirectory { get; init; } = "Entities";

    /// <summary>
    /// Gets or sets the context directory for DbContext class.
    /// </summary>
    public string ContextDirectory { get; init; } = ".";

    /// <summary>
    /// Gets or sets the namespace for entity classes.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Gets or sets the namespace for DbContext class.
    /// </summary>
    public string? ContextNamespace { get; init; }

    /// <summary>
    /// Gets or sets the name of the DbContext class.
    /// </summary>
    public string ContextName { get; init; } = "ApplicationDbContext";

    /// <summary>
    /// Gets or sets a value indicating whether to preserve database names.
    /// </summary>
    public bool UseDatabaseNames { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to suppress OnConfiguring generation.
    /// </summary>
    public bool NoOnConfiguring { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to force overwrite existing files.
    /// </summary>
    public bool Force { get; init; } = true;

    /// <summary>
    /// Gets or sets the specific tables to scaffold (null for all tables).
    /// </summary>
    public IReadOnlyList<string>? Tables { get; init; }

    /// <summary>
    /// Gets or sets the specific schemas to scaffold (null for all schemas).
    /// </summary>
    public IReadOnlyList<string>? Schemas { get; init; }
}

/// <summary>
/// Result of a scaffolding operation.
/// </summary>
public sealed class ScaffoldingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or sets the number of entities generated.
    /// </summary>
    public int EntitiesGenerated { get; init; }

    /// <summary>
    /// Gets or sets the output directory path.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets or sets any error messages.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets detailed output from the scaffolding process.
    /// </summary>
    public string? DetailedOutput { get; init; }

    /// <summary>
    /// Gets or sets the list of files that were modified.
    /// </summary>
    public IReadOnlyList<string> ModifiedFiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Result of schema validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets or sets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the collection of validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
