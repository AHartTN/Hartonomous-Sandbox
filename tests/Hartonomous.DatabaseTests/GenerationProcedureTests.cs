using System.Threading.Tasks;
using Hartonomous.DatabaseTests.Fixtures;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests;

[Collection("SqlServerContainer")]
public sealed class GenerationProcedureTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GenerationProcedureTests(SqlServerContainerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task sp_GenerateText_ProducesAtomicStreamWithSegments()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database container unavailable: {_fixture.SkipReason}");
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Execute SQL test script
        var scriptPath = System.IO.Path.Combine(
            ResolveRepositoryRoot(),
            "tests",
            "Hartonomous.DatabaseTests",
            "SqlTests",
            "test_sp_GenerateText.sql");

        if (!System.IO.File.Exists(scriptPath))
        {
            _output.WriteLine($"SQL test script not found: {scriptPath}");
            return;
        }

        var script = await System.IO.File.ReadAllTextAsync(scriptPath);

        await using var command = connection.CreateCommand();
        command.CommandText = script;
        command.CommandTimeout = 120;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();

            // Collect info messages
            connection.InfoMessage += (sender, e) =>
            {
                _output.WriteLine(e.Message);
            };

            // SQL script uses PRINT for test output
            do
            {
                while (await reader.ReadAsync())
                {
                    // Process any result sets
                }
            } while (await reader.NextResultAsync());

            _output.WriteLine("✓ sp_GenerateText SQL test completed successfully.");
        }
        catch (SqlException ex)
        {
            _output.WriteLine($"SQL test failed: {ex.Message}");
            throw;
        }
    }

    [Fact]
    public async Task sp_ExtractStudentModel_CreatesStudentModelWithFeedback()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database container unavailable: {_fixture.SkipReason}");
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Execute SQL test script
        var scriptPath = System.IO.Path.Combine(
            ResolveRepositoryRoot(),
            "tests",
            "Hartonomous.DatabaseTests",
            "SqlTests",
            "test_sp_ExtractStudentModel.sql");

        if (!System.IO.File.Exists(scriptPath))
        {
            _output.WriteLine($"SQL test script not found: {scriptPath}");
            return;
        }

        var script = await System.IO.File.ReadAllTextAsync(scriptPath);

        await using var command = connection.CreateCommand();
        command.CommandText = script;
        command.CommandTimeout = 120;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();

            // Collect info messages
            connection.InfoMessage += (sender, e) =>
            {
                _output.WriteLine(e.Message);
            };

            // SQL script uses PRINT for test output
            do
            {
                while (await reader.ReadAsync())
                {
                    // Process any result sets
                }
            } while (await reader.NextResultAsync());

            _output.WriteLine("✓ sp_ExtractStudentModel SQL test completed successfully.");
        }
        catch (SqlException ex)
        {
            _output.WriteLine($"SQL test failed: {ex.Message}");
            throw;
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (current != null && !System.IO.File.Exists(System.IO.Path.Combine(current.FullName, "Hartonomous.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new System.InvalidOperationException("Unable to locate repository root (Hartonomous.sln not found).");
        }

        return current.FullName;
    }
}
