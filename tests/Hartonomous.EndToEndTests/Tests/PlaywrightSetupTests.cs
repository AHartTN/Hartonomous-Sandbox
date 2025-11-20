using Microsoft.Playwright;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Concurrent;

namespace Hartonomous.EndToEndTests.Tests;

/// <summary>
/// Collection fixture for Playwright to share browser instance across tests safely.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}

/// <summary>
/// Fixture managing Playwright browser lifecycle. It now buffers logs to be drained into test output.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private readonly ConcurrentQueue<string> _logMessages = new();
    private IPlaywright? _playwright;
    public IBrowser? Browser { get; private set; }

    internal void Log(string message)
    {
        var timedMessage = $"[{DateTime.UtcNow:HH:mm:ss.fff}] [PlaywrightFixture] {message}";
        _logMessages.Enqueue(timedMessage);
    }

    public void DrainLogs(Action<string> writeAction)
    {
        while (_logMessages.TryDequeue(out var message))
        {
            writeAction(message);
        }
    }

    public async Task InitializeAsync()
    {
        Log("===== STARTING INITIALIZATION =====");
        Log($"Thread ID: {Environment.CurrentManagedThreadId}");
        
        try
        {
            Log("Creating Playwright instance...");
            _playwright = await Playwright.CreateAsync();
            Log("✓ Playwright instance created successfully");
            
            Log("Launching Chromium browser...");
            Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Timeout = 30000,
                Args = new[] { "--disable-dev-shm-usage", "--no-sandbox", "--disable-gpu" }
            });
            Log($"✓ Browser launched. IsConnected: {Browser.IsConnected}");
            Log("===== INITIALIZATION COMPLETE =====");
        }
        catch (Exception ex)
        {
            Log($"✗ ERROR during initialization: {ex.GetType().FullName} - {ex.Message}");
            Log($"  Stack: {ex.StackTrace}");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        Log("===== STARTING DISPOSAL =====");
        Log($"Thread ID: {Environment.CurrentManagedThreadId}");
        
        try
        {
            if (Browser != null)
            {
                Log($"Browser state: IsConnected={Browser.IsConnected}");
                if (Browser.IsConnected)
                {
                    Log("Closing browser...");
                    await Browser.CloseAsync();
                    Log("✓ Browser closed");
                }
                else
                {
                    Log("Browser already disconnected, skipping close");
                }
                
                Log("Disposing browser object...");
                await Browser.DisposeAsync();
                Log("✓ Browser object disposed");
            }
            else
            {
                Log("Browser is null, skipping browser cleanup");
            }

            if (_playwright != null)
            {
                Log("Disposing Playwright...");
                _playwright.Dispose();
                Log("✓ Playwright disposed");
            }
            else
            {
                Log("Playwright is null, skipping cleanup");
            }
            
            Log("===== DISPOSAL COMPLETE =====");
        }
        catch (Exception ex)
        {
            Log($"✗ ERROR during disposal: {ex.GetType().FullName} - {ex.Message}");
            Log($"  Stack: {ex.StackTrace}");
        }
    }
}

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
        // This will be called after each test is finished.
        // It drains and prints any logs that the fixture generated.
        _fixture.DrainLogs(_output.WriteLine);
    }
}


/// <summary>
/// E2E tests using Playwright. Inherits from a base class that handles logging.
/// </summary>
public class PlaywrightSetupTests : PlaywrightTestBase
{
    public PlaywrightSetupTests(PlaywrightFixture fixture, ITestOutputHelper output) 
        : base(fixture, output)
    {
        _output.WriteLine($"[Test] Running test in class: {nameof(PlaywrightSetupTests)}");
    }

    [Fact]
    public void Playwright_BrowserIsInitialized()
    {
        _output.WriteLine("[Test] ==> Playwright_BrowserIsInitialized");
        
        // Assert
        _fixture.Browser.Should().NotBeNull();
        _fixture.Browser!.IsConnected.Should().BeTrue();
            
        _output.WriteLine("[Test] <== Playwright_BrowserIsInitialized PASSED");
    }

    [Fact]
    public async Task Playwright_CanNavigateToUrl()
    {
        _output.WriteLine("[Test] ==> Playwright_CanNavigateToUrl");
        
        // Arrange
        _output.WriteLine("[Test] Creating browser context...");
        await using var context = await _fixture.Browser!.NewContextAsync();
        _output.WriteLine("[Test] Creating new page...");
        var page = await context.NewPageAsync();

        try
        {
            // Act
            _output.WriteLine("[Test] Navigating to example.com...");
            var response = await page.GotoAsync("https://www.example.com", new PageGotoOptions
            {
                Timeout = 15000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            _output.WriteLine($"[Test] Navigation complete. Status: {response?.Status}");

            // Assert
            response.Should().NotBeNull();
            response!.Ok.Should().BeTrue();
            page.Url.Should().Contain("example.com");
            
            _output.WriteLine("[Test] <== Playwright_CanNavigateToUrl PASSED");
        }
        finally
        {
            _output.WriteLine("[Test] Cleaning up page and context...");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Playwright_CanExtractPageContent()
    {
        _output.WriteLine("[Test] ==> Playwright_CanExtractPageContent");
        
        // Arrange
        _output.WriteLine("[Test] Creating browser context...");
        await using var context = await _fixture.Browser!.NewContextAsync();
        _output.WriteLine("[Test] Creating new page...");
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine("[Test] Navigating to example.com...");
            await page.GotoAsync("https://www.example.com", new PageGotoOptions
            {
                Timeout = 15000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            // Act
            _output.WriteLine("[Test] Extracting page title...");
            var title = await page.TitleAsync();
            _output.WriteLine($"[Test] Title: '{title}'");
            
            _output.WriteLine("[Test] Extracting page content...");
            var content = await page.ContentAsync();

            // Assert
            title.Should().NotBeNullOrWhiteSpace();
            content.Should().Contain("Example Domain");
            
            _output.WriteLine("[Test] <== Playwright_CanExtractPageContent PASSED");
        }
        finally
        {
            _output.WriteLine("[Test] Cleaning up page and context...");
            await page.CloseAsync();
        }
    }
}
