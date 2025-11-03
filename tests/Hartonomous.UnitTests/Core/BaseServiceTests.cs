using System;
using Hartonomous.Core.Services;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;
using LogEntry = Hartonomous.Testing.Common.LogEntry;

namespace Hartonomous.UnitTests.Core;

public sealed class BaseServiceTests
{
    [Fact]
    public void Constructor_Throws_WhenLoggerMissing()
    {
        Assert.Throws<ArgumentNullException>(() => new TestService(null!));
    }

    [Fact]
    public async Task InitializeAsync_LogsLifecycleMessages()
    {
    var logger = TestLogger.Create<TestService>();
        var service = new TestService(logger);

        await service.InitializeAsync();

        Assert.Collection(logger.Entries,
            entry => AssertLog(entry, LogLevel.Information, "TestService initializing..."),
            entry => AssertLog(entry, LogLevel.Information, "TestService initialized"));
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsTrue()
    {
    var service = new TestService(TestLogger.Create<TestService>());

        var healthy = await service.IsHealthyAsync();

        Assert.True(healthy);
    }

    [Fact]
    public void HelperLogging_Methods_EmitExpectedLevels()
    {
    var logger = TestLogger.Create<TestService>();
        var service = new TestService(logger);

        service.EmitDebug("debug message");
        service.EmitInformation("info message");
        service.EmitWarning("warning message");
        service.EmitError(new InvalidOperationException("boom"), "error message");

        Assert.Collection(logger.Entries,
            entry => AssertLog(entry, LogLevel.Debug, "debug message"),
            entry => AssertLog(entry, LogLevel.Information, "info message"),
            entry => AssertLog(entry, LogLevel.Warning, "warning message"),
            entry =>
            {
                Assert.Equal(LogLevel.Error, entry.Level);
                Assert.Equal("error message", entry.Message);
                Assert.IsType<InvalidOperationException>(entry.Exception);
            });
    }

    private static void AssertLog(LogEntry entry, LogLevel level, string expectedMessage)
    {
        Assert.Equal(level, entry.Level);
        Assert.Equal(expectedMessage, entry.Message);
    }

    private sealed class TestService : BaseService
    {
        public TestService(ILogger? logger) : base(logger ?? throw new ArgumentNullException(nameof(logger)))
        {
        }

        public override string ServiceName => nameof(TestService);

        public void EmitDebug(string message) => LogDebug(message);
        public void EmitInformation(string message) => LogInformation(message);
        public void EmitWarning(string message) => LogWarning(message);
        public void EmitError(Exception exception, string message) => LogError(exception, message);
    }

}

public sealed class BaseConfigurableServiceTests
{
    [Fact]
    public void Constructor_Throws_WhenConfigMissing()
    {
    Assert.Throws<ArgumentNullException>(() => new TestConfigurableService(TestLogger.Create<TestConfigurableService>(), null!));
    }

    [Fact]
    public async Task InitializeAsync_LogsConfigurationType()
    {
    var logger = TestLogger.Create<TestConfigurableService>();
        var config = new SampleConfig { Name = "primary" };
        var service = new TestConfigurableService(logger, config);

        await service.InitializeAsync();

        Assert.Collection(logger.Entries,
            entry => Assert.Equal(LogLevel.Information, entry.Level),
            entry => Assert.Equal(LogLevel.Information, entry.Level),
            entry =>
            {
                Assert.Equal(LogLevel.Debug, entry.Level);
                Assert.Contains(nameof(SampleConfig), entry.Message, StringComparison.Ordinal);
            });
    }

    [Fact]
    public void ConfigProperty_IsExposedToDerivedTypes()
    {
        var config = new SampleConfig { Name = "primary" };
    var service = new TestConfigurableService(TestLogger.Create<TestConfigurableService>(), config);

        Assert.Equal("primary", service.CurrentConfig.Name);
    }

    private sealed class SampleConfig
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestConfigurableService : BaseConfigurableService<SampleConfig>
    {
        public TestConfigurableService(ILogger logger, SampleConfig config) : base(logger, config)
        {
        }

        public override string ServiceName => "TestConfigurable";

        public SampleConfig CurrentConfig => Config;
    }
}
