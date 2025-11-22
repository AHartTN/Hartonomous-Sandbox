using Microsoft.Playwright;
using System.Collections.Concurrent;
using Xunit;

namespace Hartonomous.EndToEndTests.Infrastructure;

/// <summary>
/// Fixture managing Playwright browser lifecycle. Buffers logs to be drained into test output.
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
            Log("? Playwright instance created successfully");
            
            Log("Launching Chromium browser...");
            Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Timeout = 30000,
                Args = new[] { "--disable-dev-shm-usage", "--no-sandbox", "--disable-gpu" }
            });
            Log($"? Browser launched. IsConnected: {Browser.IsConnected}");
            Log("===== INITIALIZATION COMPLETE =====");
        }
        catch (Exception ex)
        {
            Log($"? ERROR during initialization: {ex.GetType().FullName} - {ex.Message}");
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
                    Log("? Browser closed");
                }
                else
                {
                    Log("Browser already disconnected, skipping close");
                }
                
                Log("Disposing browser object...");
                await Browser.DisposeAsync();
                Log("? Browser object disposed");
            }
            else
            {
                Log("Browser is null, skipping browser cleanup");
            }

            if (_playwright != null)
            {
                Log("Disposing Playwright...");
                _playwright.Dispose();
                Log("? Playwright disposed");
            }
            else
            {
                Log("Playwright is null, skipping cleanup");
            }
            
            Log("===== DISPOSAL COMPLETE =====");
        }
        catch (Exception ex)
        {
            Log($"? ERROR during disposal: {ex.GetType().FullName} - {ex.Message}");
            Log($"  Stack: {ex.StackTrace}");
        }
    }
}
