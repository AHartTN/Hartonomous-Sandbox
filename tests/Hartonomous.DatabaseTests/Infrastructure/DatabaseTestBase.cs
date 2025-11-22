using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Hartonomous.DatabaseTests.Infrastructure;

/// <summary>
/// Base class for database tests using Testcontainers (Docker) when available
/// - Provides isolated SQL Server instance per test class (if Docker available)
/// - Tests should be written to work without Docker by using EF Core in-memory provider
/// - Works consistently across local dev and CI/CD
/// </summary>
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    protected string ConnectionString { get; private set; } = null!;
    protected bool IsDockerAvailable { get; private set; } = false;

    public async Task InitializeAsync()
    {
        try
        {
            // Try to use Testcontainers
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong@Passw0rd123!")
                .Build();

            await _sqlContainer.StartAsync();
            ConnectionString = _sqlContainer.GetConnectionString();
            IsDockerAvailable = true;

            Console.WriteLine($"? SQL Server container started: {ConnectionString}");
        }
        catch (Exception ex) when (ex.Message.Contains("Docker"))
        {
            // Docker not available - tests will be skipped
            Console.WriteLine("?? Docker not available - database tests will be skipped");
            IsDockerAvailable = false;
            ConnectionString = string.Empty;
        }
    }

    public async Task DisposeAsync()
    {
        if (_sqlContainer != null)
        {
            await _sqlContainer.DisposeAsync();
            Console.WriteLine("? SQL Server container stopped");
        }
    }

    protected async Task<SqlConnection> GetConnectionAsync()
    {
        if (!IsDockerAvailable)
        {
            throw new InvalidOperationException("Docker not available - use EF Core in-memory provider instead");
        }
        
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    #region SQL Execution Helpers

    /// <summary>
    /// Executes a SQL command and returns scalar result.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(string sql, params SqlParameter[] parameters)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new SqlCommand(sql, connection);
        
        if (parameters.Length > 0)
            command.Parameters.AddRange(parameters);

        var result = await command.ExecuteScalarAsync();
        return result == DBNull.Value ? default : (T?)result;
    }

    /// <summary>
    /// Executes a SQL command and returns rows affected.
    /// </summary>
    protected async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new SqlCommand(sql, connection);
        
        if (parameters.Length > 0)
            command.Parameters.AddRange(parameters);

        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executes a stored procedure and returns scalar result.
    /// </summary>
    protected async Task<T?> ExecuteStoredProcedureScalarAsync<T>(string procedureName, params SqlParameter[] parameters)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        
        if (parameters.Length > 0)
            command.Parameters.AddRange(parameters);

        var result = await command.ExecuteScalarAsync();
        return result == DBNull.Value ? default : (T?)result;
    }

    /// <summary>
    /// Executes a stored procedure.
    /// </summary>
    protected async Task ExecuteStoredProcedureAsync(string procedureName, params SqlParameter[] parameters)
    {
        await using var connection = await GetConnectionAsync();
        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        
        if (parameters.Length > 0)
            command.Parameters.AddRange(parameters);

        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Schema Validation Helpers

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    protected async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
            AND TABLE_NAME = @tableName";

        var count = await ExecuteScalarAsync<int>(sql, new SqlParameter("@tableName", tableName));
        return count > 0;
    }

    /// <summary>
    /// Checks if a stored procedure exists in the database.
    /// </summary>
    protected async Task<bool> StoredProcedureExistsAsync(string procedureName)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.ROUTINES
            WHERE ROUTINE_TYPE = 'PROCEDURE'
            AND ROUTINE_SCHEMA = 'dbo'
            AND ROUTINE_NAME = @procedureName";

        var count = await ExecuteScalarAsync<int>(sql, new SqlParameter("@procedureName", procedureName));
        return count > 0;
    }

    /// <summary>
    /// Checks if a CLR function exists in the database.
    /// </summary>
    protected async Task<bool> ClrFunctionExistsAsync(string functionName)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM sys.objects
            WHERE type IN ('FN', 'FS', 'FT') -- Scalar, Table-valued, Assembly
            AND name = @functionName";

        var count = await ExecuteScalarAsync<int>(sql, new SqlParameter("@functionName", functionName));
        return count > 0;
    }

    #endregion

    #region Data Cleanup Helpers

    /// <summary>
    /// Cleans up test data from a table by tenant ID.
    /// </summary>
    protected async Task CleanupByTenantAsync(string tableName, int tenantId)
    {
        var sql = $"DELETE FROM dbo.{tableName} WHERE TenantId = @tenantId";
        await ExecuteNonQueryAsync(sql, new SqlParameter("@tenantId", tenantId));
        Console.WriteLine($"Cleaned up {tableName} for tenant {tenantId}");
    }

    /// <summary>
    /// Truncates a table (use with caution in shared test environments).
    /// </summary>
    protected async Task TruncateTableAsync(string tableName)
    {
        var sql = $"TRUNCATE TABLE dbo.{tableName}";
        await ExecuteNonQueryAsync(sql);
        Console.WriteLine($"Truncated table {tableName}");
    }

    #endregion

    #region Test Data Helpers

    /// <summary>
    /// Generates a unique tenant ID for test isolation.
    /// </summary>
    protected int GenerateTestTenantId() => Random.Shared.Next(10000, 99999);

    #endregion
}
