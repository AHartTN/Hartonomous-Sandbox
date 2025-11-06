using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Hartonomous.Shared.Contracts.Responses;
using Xunit;

namespace Hartonomous.IntegrationTests.Api;

/// <summary>
/// Integration tests for API authentication and authorization.
/// Tests JWT authentication, tenant isolation, role hierarchy, and rate limiting.
/// </summary>
public class AuthenticationAuthorizationTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;
    private readonly ApiTestWebApplicationFactory _factory;

    public AuthenticationAuthorizationTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
        if (!fixture.IsAvailable)
        {
            Skip.If(!fixture.IsAvailable, fixture.SkipReason);
        }

        _factory = new ApiTestWebApplicationFactory(fixture.ConnectionString);
    }

    #region Authentication Tests

    [Fact]
    public async Task UnauthenticatedRequest_Returns401()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_Returns200()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200 or 204, got {response.StatusCode}");
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task TenantIsolation_UserCanAccessOwnTenant()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act - Accessing own tenant data
        var response = await client.GetAsync("/api/models?tenantId=1");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.NoContent,
            $"Expected success, got {response.StatusCode}");
    }

    [Fact]
    public async Task TenantIsolation_UserCannotAccessOtherTenant()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act - Attempting to access tenant 2's data
        var response = await client.GetAsync("/api/models?tenantId=2");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_AdminBypassesRestrictions()
    {
        // Arrange
        var client = _factory.CreateAdminClient();

        // Act - Admin accessing any tenant
        var response1 = await client.GetAsync("/api/models?tenantId=1");
        var response2 = await client.GetAsync("/api/models?tenantId=999");

        // Assert - Admin can access all tenants
        Assert.True(response1.IsSuccessStatusCode || response1.StatusCode == HttpStatusCode.NoContent);
        Assert.True(response2.IsSuccessStatusCode || response2.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region Role Hierarchy Tests

    [Fact]
    public async Task RoleHierarchy_UserCanAccessUserEndpoints()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RoleHierarchy_UserCannotAccessDataScientistEndpoints()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User");

        // Act - Trying to access DataScientist-level analytics
        var response = await client.GetAsync("/api/analytics/model-performance");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleHierarchy_DataScientistCanAccessOwnEndpoints()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");

        // Act
        var response = await client.GetAsync("/api/analytics/model-performance");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RoleHierarchy_DataScientistInheritsUserPermissions()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");

        // Act - DataScientist should access User-level endpoints
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RoleHierarchy_AdminHasFullAccess()
    {
        // Arrange
        var client = _factory.CreateAdminClient();

        // Act - Admin accessing all levels
        var userResponse = await client.GetAsync("/api/models");
        var dsResponse = await client.GetAsync("/api/analytics/model-performance");
        var autonomyResponse = await client.GetAsync("/api/autonomy/status");

        // Assert
        Assert.True(userResponse.IsSuccessStatusCode || userResponse.StatusCode == HttpStatusCode.NoContent);
        Assert.True(dsResponse.IsSuccessStatusCode || dsResponse.StatusCode == HttpStatusCode.NoContent);
        Assert.True(autonomyResponse.IsSuccessStatusCode || autonomyResponse.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task RateLimiting_FreeTierHitsLimit()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User", tier: "Free");
        var endpoint = "/api/models";

        // Act - Make 15 requests (Free tier limit: 10/min)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 15; i++)
        {
            responses.Add(await client.GetAsync(endpoint));
        }

        // Assert - First 10 should succeed, rest should be rate limited
        var successCount = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NoContent);
        var rateLimitedCount = responses.Count(r => r.StatusCode == (HttpStatusCode)429);

        Assert.True(successCount >= 10, $"Expected at least 10 successes, got {successCount}");
        Assert.True(rateLimitedCount >= 1, $"Expected rate limit (429), got {rateLimitedCount}");
    }

    [Fact]
    public async Task RateLimiting_PremiumTierHasHigherLimit()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 2, role: "User", tier: "Premium");
        var endpoint = "/api/models";

        // Act - Make 50 requests (Premium tier limit: 500/min)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 50; i++)
        {
            responses.Add(await client.GetAsync(endpoint));
        }

        // Assert - All should succeed (well below limit)
        var successCount = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NoContent);
        Assert.True(successCount >= 45, $"Expected at least 45 successes, got {successCount}");
    }

    [Fact]
    public async Task RateLimiting_RetryAfterHeaderPresent()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "User", tier: "Free");
        var endpoint = "/api/models";

        // Act - Exceed rate limit
        HttpResponseMessage? rateLimitedResponse = null;
        for (int i = 0; i < 15; i++)
        {
            var response = await client.GetAsync(endpoint);
            if (response.StatusCode == (HttpStatusCode)429)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        // Assert - Retry-After header should be present
        Assert.NotNull(rateLimitedResponse);
        Assert.True(rateLimitedResponse!.Headers.Contains("Retry-After"),
            "Rate limited response should include Retry-After header");
    }

    #endregion

    #region Combined Authorization Tests

    [Fact]
    public async Task CombinedAuth_DataScientistWithTenantIsolation()
    {
        // Arrange
        var client = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");

        // Act
        var ownTenantResponse = await client.GetAsync("/api/analytics/model-performance?tenantId=1");
        var otherTenantResponse = await client.GetAsync("/api/analytics/model-performance?tenantId=2");

        // Assert
        Assert.True(ownTenantResponse.IsSuccessStatusCode || ownTenantResponse.StatusCode == HttpStatusCode.NoContent,
            "DataScientist should access own tenant");
        Assert.Equal(HttpStatusCode.Forbidden, otherTenantResponse.StatusCode);
    }

    [Fact]
    public async Task CombinedAuth_RoleAndResourceCheck()
    {
        // Arrange
        var userClient = _factory.CreateTenantClient(tenantId: 1, role: "User");
        var dsClient = _factory.CreateTenantClient(tenantId: 1, role: "DataScientist");

        // Act - Assuming atom 1 belongs to tenant 1
        var userResponse = await userClient.GetAsync("/api/search/atoms/1");
        var dsResponse = await dsClient.GetAsync("/api/search/atoms/1");

        // Assert - Both should succeed (resource belongs to tenant)
        Assert.True(userResponse.IsSuccessStatusCode || userResponse.StatusCode == HttpStatusCode.NoContent);
        Assert.True(dsResponse.IsSuccessStatusCode || dsResponse.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task TokenValidation_MissingTenantClaim_ReturnsForbidden()
    {
        // Arrange - Create client with no tenant_id claim
        var client = _factory.CreateAuthenticatedClient(
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Name, "test-user"));

        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TokenValidation_InvalidTenantClaim_ReturnsForbidden()
    {
        // Arrange - Create client with invalid tenant_id
        var client = _factory.CreateAuthenticatedClient(
            new Claim("tenant_id", "invalid"),
            new Claim(ClaimTypes.Role, "User"));

        // Act
        var response = await client.GetAsync("/api/models");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion
}
