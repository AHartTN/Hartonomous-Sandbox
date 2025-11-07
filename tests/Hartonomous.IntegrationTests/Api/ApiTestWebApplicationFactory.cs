using Hartonomous.Api;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;

namespace Hartonomous.IntegrationTests.Api;

/// <summary>
/// Custom WebApplicationFactory for API integration tests.
/// Configures test services, authentication, and tenant context.
/// </summary>
public class ApiTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiTestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace DbContext with test database
            services.RemoveAll<DbContextOptions<HartonomousDbContext>>();
            services.AddDbContext<HartonomousDbContext>(options =>
            {
                options.UseSqlServer(_connectionString, sql => sql.UseNetTopologySuite());
            });

            // Add test authentication scheme
            services.AddAuthentication("TestAuth")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "TestAuth", options => { });
        });
    }

    /// <summary>
    /// Creates an authenticated HTTP client with specified claims.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(params Claim[] claims)
    {
        var client = CreateClient();
        
        // Add test authentication header with serialized claims
        var claimData = System.Text.Json.JsonSerializer.Serialize(
            claims.Select(c => new { c.Type, c.Value }).ToArray());
        
        client.DefaultRequestHeaders.Add("X-Test-Claims", claimData);
        
        return client;
    }

    /// <summary>
    /// Creates an HTTP client for a specific tenant with role.
    /// </summary>
    public HttpClient CreateTenantClient(int tenantId, string role = "User", string tier = "Basic")
    {
        return CreateAuthenticatedClient(
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("tenant_tier", tier),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, $"test-user-{tenantId}"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        );
    }

    /// <summary>
    /// Creates an admin HTTP client that bypasses tenant isolation.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        return CreateAuthenticatedClient(
            new Claim("tenant_id", "999"),
            new Claim("tenant_tier", "Admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "test-admin"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        );
    }

    /// <summary>
    /// Creates an unauthenticated HTTP client (for testing 401 responses).
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }
}
