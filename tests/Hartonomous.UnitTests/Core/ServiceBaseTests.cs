using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces.Reasoning;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Hartonomous.UnitTests.Core;

/// <summary>
/// Unit tests for ServiceBase abstract class - tests the REAL infrastructure pattern
/// that all services inherit: telemetry, logging, error handling, validation.
/// </summary>
public class ServiceBaseTests
{
    private readonly ILogger<TestService> _logger;

    public ServiceBaseTests()
    {
        _logger = Substitute.For<ILogger<TestService>>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestService(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_SetsLoggerProperty()
    {
        // Act
        var service = new TestService(_logger);

        // Assert
        service.Should().NotBeNull();
        service.LoggerIsSet.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_LogsOperationStart()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act
        await service.TestExecuteWithTelemetry("TestOperation", () => Task.FromResult(42));

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Starting operation")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_LogsOperationCompletion()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act
        await service.TestExecuteWithTelemetry("TestOperation", () => Task.FromResult(42));

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Completed operation")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_ReturnsOperationResult()
    {
        // Arrange
        var service = new TestService(_logger);
        const int expectedResult = 42;

        // Act
        var result = await service.TestExecuteWithTelemetry("TestOperation", () => Task.FromResult(expectedResult));

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteWithTelemetryAsync_WhenOperationThrows_LogsError()
    {
        // Arrange
        var service = new TestService(_logger);
        var expectedException = new InvalidOperationException("Test error");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TestExecuteWithTelemetry<int>("TestOperation", () => throw expectedException));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Operation failed")),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteWithTelemetryAsync_WhenCancelled_LogsWarning()
    {
        // Arrange
        var service = new TestService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.TestExecuteWithTelemetry<int>("TestOperation", 
                () => Task.FromCanceled<int>(cts.Token), 
                cts.Token));

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Operation cancelled")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ValidateNotNullOrWhiteSpace_WithNull_ThrowsArgumentException()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            service.TestValidateNotNullOrWhiteSpace(null, "testParam"));

        ex.ParamName.Should().Be("testParam");
    }

    [Fact]
    public void ValidateNotNullOrWhiteSpace_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            service.TestValidateNotNullOrWhiteSpace("", "testParam"));
        
        ex.ParamName.Should().Be("testParam");
    }

    [Fact]
    public void ValidateNotNullOrWhiteSpace_WithWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            service.TestValidateNotNullOrWhiteSpace("   ", "testParam"));
        
        ex.ParamName.Should().Be("testParam");
    }

    [Fact]
    public void ValidateNotNullOrWhiteSpace_WithValidString_DoesNotThrow()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        service.TestValidateNotNullOrWhiteSpace("valid value", "testParam");
    }

    [Fact]
    public void ValidateRange_WithValueBelowMin_ThrowsArgumentException()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.TestValidateRange(0, "testParam", 1, 100));

        ex.ParamName.Should().Be("testParam");
        ex.Message.Should().Contain("must be between 1 and 100");
    }

    [Fact]
    public void ValidateRange_WithValueAboveMax_ThrowsArgumentException()
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.TestValidateRange(101, "testParam", 1, 100));

        ex.ParamName.Should().Be("testParam");
        ex.Message.Should().Contain("must be between 1 and 100");
    }

    [Theory]
    [InlineData(1, 1, 100)]
    [InlineData(50, 1, 100)]
    [InlineData(100, 1, 100)]
    public void ValidateRange_WithValueInRange_DoesNotThrow(long value, long min, long max)
    {
        // Arrange
        var service = new TestService(_logger);

        // Act & Assert
        service.TestValidateRange(value, "testParam", min, max);
    }

    // Test service that exposes protected methods for testing
    public class TestService : ServiceBase<TestService>
    {
        public TestService(ILogger<TestService> logger) : base(logger) { }

        public bool LoggerIsSet => Logger != null;

        public Task<T> TestExecuteWithTelemetry<T>(string operationName, Func<Task<T>> operation, CancellationToken cancellationToken = default)
            => ExecuteWithTelemetryAsync(operationName, operation, cancellationToken);

        public void TestValidateNotNullOrWhiteSpace(string? value, string paramName)
            => ValidateNotNullOrWhiteSpace(value, paramName);

        public void TestValidateRange(long value, string paramName, long min, long max)
            => ValidateRange(value, paramName, min, max);
    }
}
