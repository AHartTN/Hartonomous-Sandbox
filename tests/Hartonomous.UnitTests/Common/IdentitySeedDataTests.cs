using System;
using System.Linq;
using Hartonomous.Testing.Common;
using Hartonomous.Testing.Common.Seeds;
using Xunit;

namespace Hartonomous.UnitTests.Common;

public sealed class IdentitySeedDataTests
{
    [Fact]
    public void Load_ReturnsExpectedTenantCount()
    {
        var seeds = TestData.Json.Identity.Load();

        Assert.Equal(3, seeds.Tenants.Count);
        Assert.Equal("Atlas Education Consortium", seeds.Tenants[0].Name);
        Assert.True(seeds.Tenants[0].IsActive);
    }

    [Fact]
    public void Load_ReturnsPrincipalsWithRoles()
    {
        var seeds = TestData.Json.Identity.Load();

        Assert.Contains(seeds.Principals, p => p.Upn == "avery.thomas@atlas.example" && p.Roles.Contains("billing"));
    }

    [Fact]
    public void Load_ReturnsPoliciesForTenants()
    {
        var seeds = TestData.Json.Identity.Load();

        var atlasPolicies = Assert.Single(seeds.Policies, p => p.TenantId == Guid.Parse("00000000-0000-0000-0000-000000000001"));
        Assert.Contains(atlasPolicies.Policies, p => p.PolicyKey == "vector.throttle.concurrent" && p.Limit == 16);
    }
}
