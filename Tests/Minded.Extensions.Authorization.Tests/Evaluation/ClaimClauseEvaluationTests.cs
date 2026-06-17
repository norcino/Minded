using FluentAssertions;
using Minded.Extensions.Authorization;
using Minded.Extensions.Authorization.Attributes;

namespace Minded.Extensions.Authorization.Tests.Evaluation;

[TestClass]
public class ClaimClauseEvaluationTests
{
    private static readonly DefaultRequestAuthorizationEvaluator Evaluator = new();
    private static readonly Type RequestType = typeof(ClaimClauseEvaluationTests);

    [TestMethod]
    public void Evaluate_WhenClaimIsMissing_Denies()
    {
        var descriptor = BuildDescriptor(
            new ClaimClause("Region", new[] { "EU" }, AuthorizationMatch.Any, 0, null,
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));

        var context = new AuthorizationContext(hasPrincipal: true, claims: new Dictionary<string, string>());

        var decision = Evaluator.Evaluate(RequestType, descriptor, context);

        decision.Allowed.Should().BeFalse();
    }

    [TestMethod]
    public void Evaluate_ClaimValues_AreComparedCaseInsensitiveAndTrimmed()
    {
        var descriptor = BuildDescriptor(
            new ClaimClause("Region", new[] { "  eu " }, AuthorizationMatch.Any, 0, null,
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));

        var context = new AuthorizationContext(hasPrincipal: true, claims: new Dictionary<string, string>
        {
            ["region"] = " EU "
        });

        var decision = Evaluator.Evaluate(RequestType, descriptor, context);

        decision.Allowed.Should().BeTrue();
    }

    [TestMethod]
    public void Evaluate_OrClauseSatisfied_SkipsPrimaryClaimCheck()
    {
        var descriptor = BuildDescriptor(
            new ClaimClause("Region", new[] { "EU" }, AuthorizationMatch.Any, 0, null,
                new[] { "Admin" }, Array.Empty<string>(), Array.Empty<string>()));

        var context = new AuthorizationContext(
            hasPrincipal: true,
            roles: new[] { "Admin" },
            claims: new Dictionary<string, string>());

        var decision = Evaluator.Evaluate(RequestType, descriptor, context);

        decision.Allowed.Should().BeTrue();
    }

    [TestMethod]
    public void Evaluate_ClaimAtLeast_WorksWithDuplicateAllowedValues()
    {
        var descriptor = BuildDescriptor(
            new ClaimClause("Region", new[] { "EU", "eu" }, AuthorizationMatch.AtLeast, 2, null,
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));

        var context = new AuthorizationContext(hasPrincipal: true, claims: new Dictionary<string, string>
        {
            ["Region"] = "eu"
        });

        var decision = Evaluator.Evaluate(RequestType, descriptor, context);

        decision.Allowed.Should().BeTrue();
    }

    private static AuthorizationDescriptor BuildDescriptor(ClaimClause claimClause)
    {
        return new AuthorizationDescriptor(
            isProtected: true,
            allowUnauthenticated: false,
            requireAuthenticationOnly: false,
            roleClauses: Array.Empty<RoleClause>(),
            permissionClauses: Array.Empty<PermissionClause>(),
            claimClauses: new[] { claimClause },
            resourceClauses: Array.Empty<ResourceClause>());
    }
}
