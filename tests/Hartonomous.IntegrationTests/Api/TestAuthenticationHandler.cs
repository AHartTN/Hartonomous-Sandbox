using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Hartonomous.IntegrationTests.Api;

/// <summary>
/// Test authentication handler that reads claims from request headers.
/// Used for integration testing without real JWT tokens.
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if test claims header exists
        if (!Request.Headers.TryGetValue("X-Test-Claims", out var claimsHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Deserialize claims from header
            var claimData = JsonSerializer.Deserialize<TestClaim[]>(claimsHeader.ToString());
            if (claimData == null || claimData.Length == 0)
            {
                return Task.FromResult(AuthenticateResult.Fail("No claims provided"));
            }

            var claims = claimData.Select(c => new Claim(c.Type, c.Value)).ToList();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Invalid claims format: {ex.Message}"));
        }
    }

    private class TestClaim
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}
