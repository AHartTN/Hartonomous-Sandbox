using Hartonomous.Core.Services;
using Microsoft.Extensions.Configuration;

namespace Hartonomous.UnitTests.Core;

public sealed class ConfigurationServiceTests : IDisposable
{
    private readonly List<(string Key, string? Original)> _env = new();

    [Fact]
    public void GetValue_ReturnsConfiguredValue_WhenPresent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hartonomous:Queue:Name"] = "PrimaryQueue"
            })
            .Build();

        var service = new ConfigurationService(config);

        var value = service.GetValue("Hartonomous:Queue:Name");

        Assert.Equal("PrimaryQueue", value);
    }

    [Fact]
    public void GetValue_FallsBackToEnvironment_WithDoubleUnderscore()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        const string key = "Hartonomous:Queue:Retries";
        SetEnv("Hartonomous__Queue__Retries", "5");

        var service = new ConfigurationService(config);

        var value = service.GetValue(key);

        Assert.Equal("5", value);
    }

    [Fact]
    public void GetValue_ReturnsDefault_WhenMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var service = new ConfigurationService(config);

        var value = service.GetValue("Missing:Key", "fallback");

        Assert.Equal("fallback", value);
    }

    [Fact]
    public void GetRequiredValue_Throws_WhenMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var service = new ConfigurationService(config);

        var ex = Assert.Throws<InvalidOperationException>(() => service.GetRequiredValue("Missing:Required"));
        Assert.Contains("Missing:Required", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("8", 0, 8)]
    [InlineData("not-an-int", 3, 3)]
    [InlineData(null, 4, 4)]
    public void GetValue_Generic_ParsesOrFallsBack(string? stored, int defaultValue, int expected)
    {
        var store = new Dictionary<string, string?>();
        if (stored is not null)
        {
            store["Hartonomous:Queue:MaxParallelism"] = stored;
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(store).Build();

        var service = new ConfigurationService(config);

        var value = service.GetValue("Hartonomous:Queue:MaxParallelism", defaultValue);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void GetConnectionString_UsesConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PrimaryDatabase"] = "Server=.;Database=Hartonomous;"
            })
            .Build();

        var service = new ConfigurationService(config);

        var value = service.GetConnectionString("PrimaryDatabase");

        Assert.Equal("Server=.;Database=Hartonomous;", value);
    }

    [Fact]
    public void GetConnectionString_FallsBackToEnvironment()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        SetEnv("ConnectionStrings__OperationalDb", "Server=cloud;Database=Ops;");

        var service = new ConfigurationService(config);

        var value = service.GetConnectionString("OperationalDb");

        Assert.Equal("Server=cloud;Database=Ops;", value);
    }

    [Fact]
    public void GetRequiredConnectionString_Throws_WhenNotFound()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var service = new ConfigurationService(config);

        var ex = Assert.Throws<InvalidOperationException>(() => service.GetRequiredConnectionString("MissingDb"));
        Assert.Contains("MissingDb", ex.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        foreach (var (key, original) in _env)
        {
            Environment.SetEnvironmentVariable(key, original);
        }
        _env.Clear();
    }

    private void SetEnv(string key, string? value)
    {
        _env.Add((key, Environment.GetEnvironmentVariable(key)));
        Environment.SetEnvironmentVariable(key, value);
    }
}
