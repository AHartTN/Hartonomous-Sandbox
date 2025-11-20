using FluentAssertions;
using System.Net;

namespace Hartonomous.IntegrationTests.Tests.Api;

public class HealthCheckTests : IntegrationTestBase<HartonomousWebApplicationFactory>, 
    IClassFixture<HartonomousWebApplicationFactory>
{
    public HealthCheckTests(HartonomousWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Health check failed with response: {content}");
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task ReadyCheck_ReturnsHealthy()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        response.Should().BeSuccessful();
    }

    [Fact]
    public async Task LiveCheck_ReturnsHealthy()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.Should().BeSuccessful();
    }
}
