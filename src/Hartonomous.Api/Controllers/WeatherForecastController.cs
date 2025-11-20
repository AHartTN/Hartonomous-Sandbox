using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ApiControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
        : base(logger)
    {
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IActionResult Get()
    {
        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        return SuccessResult(forecasts);
    }
}
