using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Hartonomous.DatabaseTests.Infrastructure;

/// <summary>
/// Base class for database tests using Testcontainers (Docker)
/// - Provides isolated SQL Server instance per test class
/// - Works consistently across local dev and CI/CD
/// - Requires Docker to be running
/// </summary>
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    protected string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Always use Testcontainers for consistency
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd123!")
            .Build();

        await _sqlContainer.StartAsync();
        ConnectionString = _sqlContainer.GetConnectionString();

        Console.WriteLine($"? SQL Server container started: {ConnectionString}");
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
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
