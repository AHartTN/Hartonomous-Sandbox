using Hartonomous.Api;
using Hartonomous.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Tests.Api.Controllers;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _controller;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastControllerTests()
    {
        _logger = Substitute.For<ILogger<WeatherForecastController>>();
        _controller = new WeatherForecastController(_logger);
    }

    [Fact]
    public void Get_ReturnsApiResponse()
    {
        // Act
        var result = _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<WeatherForecast[]>();
    }

    [Fact]
    public void Get_ReturnsExpectedNumberOfForecasts()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var forecasts = result!.Value as WeatherForecast[];

        // Assert
        forecasts.Should().HaveCount(5);
    }

    [Fact]
    public void Get_ReturnsWeatherForecastsWithValidData()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var forecasts = (result!.Value as WeatherForecast[])!.ToList();

        // Assert
        forecasts.Should().AllSatisfy(forecast =>
        {
            forecast.Date.Should().BeAfter(DateOnly.FromDateTime(DateTime.Now));
            forecast.TemperatureC.Should().BeInRange(-20, 55);
            forecast.Summary.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void Get_ReturnsUniqueForecasts()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var forecasts = (result!.Value as WeatherForecast[])!.ToList();

        // Assert
        forecasts.Select(f => f.Date).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Get_ReturnsConsecutiveDates()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var forecasts = (result!.Value as WeatherForecast[])!.OrderBy(f => f.Date).ToList();

        // Assert
        for (int i = 1; i < forecasts.Count; i++)
        {
            var daysDifference = forecasts[i].Date.DayNumber - forecasts[i - 1].Date.DayNumber;
            daysDifference.Should().Be(1);
        }
    }

    [Fact]
    public void Get_TemperatureF_CalculatedCorrectly()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var forecasts = (result!.Value as WeatherForecast[])!.ToList();

        // Assert
        forecasts.Should().AllSatisfy(forecast =>
        {
            var expectedF = 32 + (int)(forecast.TemperatureC / 0.5556);
            forecast.TemperatureF.Should().Be(expectedF);
        });
    }
}
