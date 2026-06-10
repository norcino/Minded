using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Minded.Extensions.Authorization;
using Minded.Extensions.Authorization.Attributes;

namespace Minded.Extensions.Authorization.Tests.Evaluation;

[TestClass]
public class DefaultRequestAuthorizationEvaluatorTests
{
    private readonly DefaultRequestAuthorizationEvaluator _evaluator = new();
    private readonly Type _dummyType = typeof(object);

    private static readonly string[] ItemPool =
    [
        "Admin", "Manager", "Editor", "Viewer", "SuperAdmin",
        "orders.read", "orders.write", "users.manage", "reports.view",
        "config.edit", "billing.access", "audit.read", "system.admin"
    ];

    private static Gen<List<string>> NonEmptyDistinctListGen()
    {
        return Gen.NonEmptyListOf(Gen.Elements(ItemPool))
            .Select(l => l.Distinct(StringComparer.OrdinalIgnoreCase).ToList())
            .Where(l => l.Count > 0);
    }

    /// <summary>
    /// Property 1: Match.All evaluates as subset check —
    /// For any set of required items R and context items C, evaluating a clause
    /// with Match = All returns Allowed iff every item in R exists in C.
    /// **Validates: Requirements 3.1, 5.1, 5.2, 5.3, 5.4**
    /// </summary>
    [TestMethod]
    public void Property1_MatchAll_EvaluatesAsSubsetCheck()
    {
        var arb = (from required in NonEmptyDistinctListGen()
                   from context in NonEmptyDistinctListGen()
                   select (required, context)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var descriptor = BuildSingleRoleDescriptor(tuple.required, AuthorizationMatch.All, 0);
            var authContext = new AuthorizationContext(true, tuple.context.AsReadOnly());
            var decision = _evaluator.Evaluate(_dummyType, descriptor, authContext);

            var contextSet = new HashSet<string>(tuple.context.Select(c => c.Trim()), StringComparer.OrdinalIgnoreCase);
            var expected = tuple.required.All(r => contextSet.Contains(r.Trim()));
            return decision.Allowed == expected;
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 2: Match.Any evaluates as intersection check.
    /// **Validates: Requirements 3.2**
    /// </summary>
    [TestMethod]
    public void Property2_MatchAny_EvaluatesAsIntersectionCheck()
    {
        var arb = (from required in NonEmptyDistinctListGen()
                   from context in NonEmptyDistinctListGen()
                   select (required, context)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var descriptor = BuildSingleRoleDescriptor(tuple.required, AuthorizationMatch.Any, 0);
            var authContext = new AuthorizationContext(true, tuple.context.AsReadOnly());
            var decision = _evaluator.Evaluate(_dummyType, descriptor, authContext);

            var contextSet = new HashSet<string>(tuple.context.Select(c => c.Trim()), StringComparer.OrdinalIgnoreCase);
            var expected = tuple.required.Any(r => contextSet.Contains(r.Trim()));
            return decision.Allowed == expected;
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 3: Match.AtLeast evaluates as minimum count check.
    /// **Validates: Requirements 3.3**
    /// </summary>
    [TestMethod]
    public void Property3_MatchAtLeast_EvaluatesAsMinimumCountCheck()
    {
        var arb = (from required in NonEmptyDistinctListGen()
                   from minimum in Gen.Choose(1, Math.Max(1, required.Count))
                   from context in NonEmptyDistinctListGen()
                   select (required, minimum, context)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var descriptor = BuildSingleRoleDescriptor(tuple.required, AuthorizationMatch.AtLeast, tuple.minimum);
            var authContext = new AuthorizationContext(true, tuple.context.AsReadOnly());
            var decision = _evaluator.Evaluate(_dummyType, descriptor, authContext);

            var contextSet = new HashSet<string>(tuple.context.Select(c => c.Trim()), StringComparer.OrdinalIgnoreCase);
            var matchCount = tuple.required.Count(r => contextSet.Contains(r.Trim()));
            return decision.Allowed == (matchCount >= tuple.minimum);
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 4: Match.None evaluates as disjointness check.
    /// **Validates: Requirements 3.4**
    /// </summary>
    [TestMethod]
    public void Property4_MatchNone_EvaluatesAsDisjointnessCheck()
    {
        var arb = (from required in NonEmptyDistinctListGen()
                   from context in NonEmptyDistinctListGen()
                   select (required, context)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var descriptor = BuildSingleRoleDescriptor(tuple.required, AuthorizationMatch.None, 0);
            var authContext = new AuthorizationContext(true, tuple.context.AsReadOnly());
            var decision = _evaluator.Evaluate(_dummyType, descriptor, authContext);

            var contextSet = new HashSet<string>(tuple.context.Select(c => c.Trim()), StringComparer.OrdinalIgnoreCase);
            var expected = !tuple.required.Any(r => contextSet.Contains(r.Trim()));
            return decision.Allowed == expected;
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 5: Case and whitespace normalization does not affect evaluation.
    /// **Validates: Requirements 5.1, 5.2, 5.3, 5.4**
    /// </summary>
    [TestMethod]
    public void Property5_CaseAndWhitespaceNormalization_DoesNotAffectEvaluation()
    {
        var arb = (from required in NonEmptyDistinctListGen()
                   from context in NonEmptyDistinctListGen()
                   from match in Gen.Elements(AuthorizationMatch.All, AuthorizationMatch.Any, AuthorizationMatch.None)
                   select (required, context, match)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var descriptor = BuildSingleRoleDescriptor(tuple.required, tuple.match, 0);

            var originalContext = new AuthorizationContext(true, tuple.context.AsReadOnly());
            var originalDecision = _evaluator.Evaluate(_dummyType, descriptor, originalContext);

            var transformed = tuple.context.Select(c => "  " + c.ToUpperInvariant() + "  ").ToList();
            var transformedContext = new AuthorizationContext(true, transformed.AsReadOnly());
            var transformedDecision = _evaluator.Evaluate(_dummyType, descriptor, transformedContext);

            return originalDecision.Allowed == transformedDecision.Allowed;
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 6: Multiple clauses combine with implicit AND.
    /// **Validates: Requirements 7.5, 21.1, 21.2, 21.3**
    /// </summary>
    [TestMethod]
    public void Property6_MultipleClauses_CombineWithImplicitAnd()
    {
        var arb = (from roleClauses in Gen.ListOf(NonEmptyDistinctListGen()).Where(l => l.Count >= 1 && l.Count <= 3)
                   from permClauses in Gen.ListOf(NonEmptyDistinctListGen()).Where(l => l.Count <= 3)
                   from context in NonEmptyDistinctListGen()
                   select (roleClauses, permClauses, context)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var roles = tuple.roleClauses.Select(r =>
                new RoleClause(r, AuthorizationMatch.All, 0, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())).ToArray();
            var perms = tuple.permClauses.Select(p =>
                new PermissionClause(p, AuthorizationMatch.All, 0, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())).ToArray();

            var descriptor = new AuthorizationDescriptor(true, false, false, roles, perms, Array.Empty<ClaimClause>(), Array.Empty<ResourceClause>());
            var authContext = new AuthorizationContext(true, tuple.context.AsReadOnly(), tuple.context.AsReadOnly());
            var decision = _evaluator.Evaluate(_dummyType, descriptor, authContext);

            var contextSet = new HashSet<string>(tuple.context.Select(c => c.Trim()), StringComparer.OrdinalIgnoreCase);
            var allRolesOk = roles.All(c => c.Roles.All(r => contextSet.Contains(r.Trim())));
            var allPermsOk = perms.All(c => c.Permissions.All(p => contextSet.Contains(p.Trim())));

            return decision.Allowed == (allRolesOk && allPermsOk);
        }).QuickCheckThrowOnFailure();
    }

    private static AuthorizationDescriptor BuildSingleRoleDescriptor(
        IReadOnlyList<string> roles, AuthorizationMatch match, int minimum)
    {
        return new AuthorizationDescriptor(
            isProtected: true,
            allowUnauthenticated: false,
            requireAuthenticationOnly: false,
            roleClauses: new[] { new RoleClause(roles, match, minimum, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()) },
            permissionClauses: Array.Empty<PermissionClause>(),
            claimClauses: Array.Empty<ClaimClause>(),
            resourceClauses: Array.Empty<ResourceClause>());
    }
}
