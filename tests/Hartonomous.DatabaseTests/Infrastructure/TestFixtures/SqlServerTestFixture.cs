using Hartonomous.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Hartonomous.DatabaseTests.Infrastructure.TestFixtures;

/// <summary>
/// Provides real SQL Server via Testcontainers for integration tests.
/// Each test class gets an isolated SQL Server instance with full schema deployed.
/// Requires Docker to be running.
/// </summary>
public class SqlServerTestFixture : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsDockerAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Start SQL Server 2022 container
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong@Passw0rd123!")
                .WithCleanUp(true)
                .Build();

            await _sqlContainer.StartAsync();
            ConnectionString = _sqlContainer.GetConnectionString();
            IsDockerAvailable = true;

            Console.WriteLine($"? SQL Server container started: {ConnectionString}");

            // Deploy schema
            await DeploySchemaAsync();

            // Deploy CLR assemblies (if needed for tests)
            // await DeployClrAssembliesAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("Docker"))
        {
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

    /// <summary>
    /// Creates a DbContext connected to the test database.
    /// </summary>
    public HartonomousDbContext CreateContext()
    {
        if (!IsDockerAvailable)
        {
            throw new InvalidOperationException("Docker not available - cannot create context");
        }

        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer(ConnectionString, sqlOptions =>
            {
                sqlOptions.UseNetTopologySuite(); // Required for spatial types
            })
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        return new HartonomousDbContext(options);
    }

    /// <summary>
    /// Gets a raw SQL connection for stored procedure/CLR testing.
    /// </summary>
    public async Task<SqlConnection> GetConnectionAsync()
    {
        if (!IsDockerAvailable)
        {
            throw new InvalidOperationException("Docker not available");
        }

        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Deploys the database schema from EF Core migrations or SQL scripts.
    /// </summary>
    private async Task DeploySchemaAsync()
    {
        using var context = CreateContext();
        
        // Option 1: Use EF Core migrations
        await context.Database.MigrateAsync();

        // Option 2: Execute SQL scripts (if you have them)
        // await ExecuteSqlScriptAsync("path/to/schema.sql");

        Console.WriteLine("? Database schema deployed");
    }

    /// <summary>
    /// Deploys CLR assemblies for testing CLR functions.
    /// </summary>
    private async Task DeployClrAssembliesAsync()
    {
        // TODO: Deploy CLR assemblies using SQLCMD or ADO.NET
        // This would execute the CLR deployment scripts
        Console.WriteLine("? CLR assemblies deployed");
    }
}
