using Microsoft.Data.SqlClient;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Abstraction for executing parameterised SQL Server commands with consistent connection handling and telemetry.
/// </summary>
public interface ISqlCommandExecutor
{
    /// <summary>
    /// Executes the supplied delegate against a managed <see cref="SqlCommand"/> and returns a result.
    /// </summary>
    Task<TResult> ExecuteAsync<TResult>(Func<SqlCommand, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied delegate against a managed <see cref="SqlCommand"/>.
    /// </summary>
    Task ExecuteAsync(Func<SqlCommand, CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
