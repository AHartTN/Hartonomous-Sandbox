using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Api;

public class WeatherForecastIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WeatherForecastIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WeatherForecast_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/WeatherForecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task WeatherForecast_ReturnsValidJson()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/WeatherForecast");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        forecasts.Should().NotBeNull();
        forecasts.Should().HaveCount(5);
    }

    [Fact]
    public async Task WeatherForecast_ReturnsForecastsWithFutureDates()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/WeatherForecast");
        var content = await response.Content.ReadAsStringAsync();
        var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        var today = DateOnly.FromDateTime(DateTime.Now);
        forecasts.Should().AllSatisfy(f => f.Date.Should().BeAfter(today));
    }

    private class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF { get; set; }
        public string? Summary { get; set; }
    }
}
