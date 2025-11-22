using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Data;

/// <summary>
/// Factory for creating authenticated SQL connections with managed identity support.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Creates and opens an authenticated SQL connection.
    /// Supports both password-based and managed identity authentication.
    /// </summary>
    Task<SqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates an authenticated SQL connection without opening it.
    /// Useful when you need to configure the connection before opening.
    /// </summary>
    SqlConnection CreateConnection();
}
