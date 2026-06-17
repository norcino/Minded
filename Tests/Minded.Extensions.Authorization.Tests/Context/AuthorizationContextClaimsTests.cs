using System.Collections.Generic;
using FluentAssertions;
using Minded.Extensions.Authorization;

namespace Minded.Extensions.Authorization.Tests.Context;

[TestClass]
public class AuthorizationContextClaimsTests
{
    [TestMethod]
    public void Constructor_WithoutClaims_InitializesEmptyClaims()
    {
        var context = new AuthorizationContext(hasPrincipal: true);

        context.Claims.Should().NotBeNull();
        context.Claims.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_WithNullClaims_InitializesEmptyClaims()
    {
        var context = new AuthorizationContext(
            hasPrincipal: true,
            roles: new[] { "Admin" },
            permissions: new[] { "orders.read" },
            claims: null);

        context.Claims.Should().NotBeNull();
        context.Claims.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_WithClaims_PreservesValues_WithCaseInsensitiveLookup()
    {
        var input = new Dictionary<string, string>
        {
            ["TenantId"] = "t-1",
            ["Region"] = "EU"
        };

        var context = new AuthorizationContext(hasPrincipal: true, claims: input);

        context.Claims["tenantid"].Should().Be("t-1");
        context.Claims["REGION"].Should().Be("EU");
    }

    [TestMethod]
    public void ExistingConstructorShape_RemainsCompatible()
    {
        var context = new AuthorizationContext(
            hasPrincipal: true,
            roles: new[] { "Admin" },
            permissions: new[] { "orders.read" });

        context.Roles.Should().ContainSingle().Which.Should().Be("Admin");
        context.Permissions.Should().ContainSingle().Which.Should().Be("orders.read");
        context.Claims.Should().BeEmpty();
    }
}
