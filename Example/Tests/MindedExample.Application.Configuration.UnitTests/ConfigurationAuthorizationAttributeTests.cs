using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Authorization.Attributes;
using MindedExample.Application.Configuration.Command;
using MindedExample.Application.Configuration.Query;

namespace MindedExample.Application.Configuration.UnitTests
{
    /// <summary>
    /// Verifies that global-admin-restricted commands and queries carry the correct
    /// [RequireClaim("is_global_admin", "true")] attribute so that the authorization
    /// decorator enforces the restriction without requiring inline SecurityException guards.
    /// </summary>
    [TestClass]
    public class ConfigurationAuthorizationAttributeTests
    {
        [TestMethod]
        public void CreateTenantCommand_RequiresGlobalAdminClaim()
        {
            var attrs = typeof(CreateTenantCommand).GetCustomAttributes<RequireClaimAttribute>();

            attrs.Should().Contain(a =>
                a.ClaimType == "is_global_admin" &&
                a.Values != null &&
                System.Array.IndexOf(a.Values, "true") >= 0);
        }

        [TestMethod]
        public void DeleteTenantCommand_RequiresGlobalAdminClaim()
        {
            var attrs = typeof(DeleteTenantCommand).GetCustomAttributes<RequireClaimAttribute>();

            attrs.Should().Contain(a =>
                a.ClaimType == "is_global_admin" &&
                a.Values != null &&
                System.Array.IndexOf(a.Values, "true") >= 0);
        }

        [TestMethod]
        public void GetAdminTenantSummariesQuery_RequiresGlobalAdminClaim()
        {
            var attrs = typeof(GetAdminTenantSummariesQuery).GetCustomAttributes<RequireClaimAttribute>();

            attrs.Should().Contain(a =>
                a.ClaimType == "is_global_admin" &&
                a.Values != null &&
                System.Array.IndexOf(a.Values, "true") >= 0);
        }
    }
}
