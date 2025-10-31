using Microsoft.Data.SqlClient;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides SQL Server connections configured for the Hartonomous platform.
/// </summary>
public interface ISqlServerConnectionFactory
{
    /// <summary>
    /// Creates and opens a <see cref="SqlConnection"/> configured for SQL Server 2025 features.
    /// The caller is responsible for disposing the returned connection.
    /// </summary>
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
