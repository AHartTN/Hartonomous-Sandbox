using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Hartonomous.DatabaseTests.Tests.Infrastructure;

/// <summary>
/// Database connection tests using hybrid LocalDB/Docker/Azure SQL
/// Auto-detects environment and uses appropriate database:
/// - Local dev: LocalDB (no Docker required)
/// - CI/CD: Docker container (Testcontainers)
/// - Staging/Prod: Azure SQL (via connection string)
/// </summary>
public class DatabaseConnectionTests : DatabaseTestBase
{
    [Fact]
    public async Task Connection_CanConnectToDatabase()
    {
        // Arrange
        await using var connection = new SqlConnection(ConnectionString);

        // Act
        await connection.OpenAsync();

        // Assert
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task Database_CanExecuteQuery()
    {
        // Arrange
        await using var connection = await GetConnectionAsync();

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
        await using var connection = await GetConnectionAsync();

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
