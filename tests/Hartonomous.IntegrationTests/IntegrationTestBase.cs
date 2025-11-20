using Microsoft.AspNetCore.Mvc.Testing;

namespace Hartonomous.IntegrationTests;

/// <summary>
/// Base class for integration tests that properly manages HttpClient lifecycle.
/// Implements IAsyncLifetime for proper async cleanup.
/// </summary>
/// <typeparam name="TFactory">The WebApplicationFactory type to use</typeparam>
public abstract class IntegrationTestBase<TFactory> : IAsyncLifetime 
    where TFactory : WebApplicationFactory<Program>
{
    protected readonly TFactory Factory;
    protected HttpClient? Client;
    private readonly List<HttpClient> _clients = new();

    protected IntegrationTestBase(TFactory factory)
    {
        Factory = factory;
    }

    /// <summary>
    /// Creates an HttpClient that will be automatically disposed after the test.
    /// </summary>
    protected HttpClient CreateClient()
    {
        var client = Factory.CreateClient();
        _clients.Add(client);
        return client;
    }

    /// <summary>
    /// Gets or creates a shared HttpClient for this test.
    /// Prefer CreateClient() for tests that need multiple clients.
    /// </summary>
    protected HttpClient GetClient()
    {
        if (Client == null)
        {
            Client = CreateClient();
        }
        return Client;
    }

    public virtual Task InitializeAsync()
    {
        // Override in derived classes if needed
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        // Dispose all created clients
        foreach (var client in _clients)
        {
            client?.Dispose();
        }
        _clients.Clear();

        // Give a moment for cleanup
        await Task.Delay(10);
    }
}
