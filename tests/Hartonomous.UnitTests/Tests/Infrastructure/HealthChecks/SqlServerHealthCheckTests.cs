using Hartonomous.Infrastructure.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Infrastructure.HealthChecks;

public class SqlServerHealthCheckTests
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public SqlServerHealthCheckTests()
    {
        _logger = Substitute.For<ILogger<SqlServerHealthCheck>>();
        _configuration = Substitute.For<IConfiguration>();
    }

    [Fact]
    public async Task CheckHealthAsync_WithMissingConnectionString_ReturnsUnhealthy()
    {
        // Arrange
        _configuration.GetConnectionString("HartonomousDb").Returns((string?)null);
        var healthCheck = new SqlServerHealthCheck(_logger, _configuration);
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
        Action act = () => new SqlServerHealthCheck(null!, _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SqlServerHealthCheck(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }
}
