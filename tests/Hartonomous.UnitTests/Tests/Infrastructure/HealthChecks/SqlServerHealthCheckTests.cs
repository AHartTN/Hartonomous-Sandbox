using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Infrastructure.HealthChecks;

public class SqlServerHealthCheckTests
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly IOptions<DatabaseOptions> _options;

    public SqlServerHealthCheckTests()
    {
        _logger = Substitute.For<ILogger<SqlServerHealthCheck>>();
        
        var databaseOptions = new DatabaseOptions
        {
            HartonomousDb = "Server=tcp:mock-server.database.windows.net,1433;Initial Catalog=Hartonomous;"
        };
        
        _options = Options.Create(databaseOptions);
    }

    [Fact]
    public async Task CheckHealthAsync_WithMissingConnectionString_ReturnsUnhealthy()
    {
        // Arrange
        var emptyOptions = Options.Create(new DatabaseOptions { HartonomousDb = string.Empty });
        var healthCheck = new SqlServerHealthCheck(_logger, emptyOptions);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("connection string not configured");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SqlServerHealthCheck(null!, _options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SqlServerHealthCheck(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
