using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.EndToEndTests.Infrastructure;

/// <summary>
/// Base class for Playwright tests to handle fixture logging.
/// </summary>
[Collection("Playwright")]
public abstract class PlaywrightTestBase : IDisposable
{
    protected readonly PlaywrightFixture _fixture;
    protected readonly ITestOutputHelper _output;

    protected PlaywrightTestBase(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.Log($"Test class '{GetType().Name}' is being constructed.");
    }

    public void Dispose()
    {
        // Drains and prints any logs that the fixture generated after each test
        _fixture.DrainLogs(_output.WriteLine);
    }
}
