using System;
using System.Threading.Tasks;
using Hartonomous.DatabaseTests.Fixtures;
using Hartonomous.Testing.Common;
using Hartonomous.Testing.Common.Hashing;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests;

[Collection("SqlServerContainer")]
public sealed class SqlServerContainerSmokeTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SqlServerContainerSmokeTests(SqlServerContainerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void DeploymentLogsCaptureClrAssembly()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database container unavailable: {_fixture.SkipReason}");
            return;
        }

        FlushDeploymentTrace();

        Assert.True(_fixture.Baseline.AssemblyCount >= 1, "SqlClrFunctions assembly should be deployed at least once.");
        Assert.True(_fixture.Baseline.ProcedureCount > 0, "Deployed database should expose user stored procedures or functions.");
    }

    [Fact]
    public async Task CanQueryServiceBrokerArtifacts()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database container unavailable: {_fixture.SkipReason}");
            return;
        }

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.service_queues WHERE is_ms_shipped = 0;";

        var scalar = await command.ExecuteScalarAsync();
        var result = Convert.ToInt32(scalar);
        Assert.True(result >= _fixture.Baseline.ServiceQueueCount, "Service Broker user queues should remain available.");
    }

    [Fact]
    public void IdentitySeedAssetsRemainStable()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine($"Database container unavailable: {_fixture.SkipReason}");
            return;
        }

        FlushDeploymentTrace();

        AssetHashValidator.AssertAssetHash(TestData.Json.Identity.TenantsRelativePath, TestData.Json.Identity.TenantsSha256);
        AssetHashValidator.AssertAssetHash(TestData.Json.Identity.PrincipalsRelativePath, TestData.Json.Identity.PrincipalsSha256);
        AssetHashValidator.AssertAssetHash(TestData.Json.Identity.PoliciesRelativePath, TestData.Json.Identity.PoliciesSha256);
    }

    private void FlushDeploymentTrace()
    {
        if (_fixture.DeploymentLog.Count == 0)
        {
            return;
        }

        _output.WriteLine("=== deploy-database.ps1 output ===");
        foreach (var line in _fixture.DeploymentLog)
        {
            _output.WriteLine(line);
        }
    }
}
