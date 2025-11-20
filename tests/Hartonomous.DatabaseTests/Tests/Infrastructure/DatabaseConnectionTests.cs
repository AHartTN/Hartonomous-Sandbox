using FluentAssertions;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Hartonomous.DatabaseTests.Tests.Infrastructure;

public class DatabaseConnectionTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("YourStrong@Passw0rd")
        .Build();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task Connection_CanConnectToDatabase()
    {
        // Arrange
        var connectionString = _sqlContainer.GetConnectionString();

        // Act
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task Database_CanExecuteQuery()
    {
        // Arrange
        var connectionString = _sqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 AS Value";
        var result = await command.ExecuteScalarAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task Database_SupportsGeographyType()
    {
        // Arrange
        var connectionString = _sqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT geography::Point(47.65100, -122.34900, 4326).STAsText() AS Point";
        var result = await command.ExecuteScalarAsync();

        // Assert
        result.Should().NotBeNull();
        result.ToString().Should().Contain("POINT");
    }
}
