using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Minded.Extensions.Authorization;

namespace Minded.Extensions.Authorization.Tests.Context;

[TestClass]
public class AuthorizationContextTests
{
    /// <summary>
    /// Property 7: AuthorizationContext collections are never null —
    /// For any construction including null Roles or null Permissions,
    /// the properties are non-null (defaulting to empty collections).
    /// **Validates: Requirements 6.3, 6.4, 6.5, 6.6**
    /// </summary>
    [TestMethod]
    public void Property7_AuthorizationContext_CollectionsAreNeverNull()
    {
        var nullableListGen = Gen.OneOf(
            Gen.Constant<IReadOnlyCollection<string>>(null!),
            Gen.Constant<IReadOnlyCollection<string>>(Array.Empty<string>()),
            Gen.NonEmptyListOf(Gen.Elements("Admin", "Manager", "Editor", "read", "write", "delete"))
                .Select(l => (IReadOnlyCollection<string>)l.ToList().AsReadOnly()));

        var arb = (from hasPrincipal in Gen.Elements(true, false)
                   from roles in nullableListGen
                   from permissions in nullableListGen
                   select (hasPrincipal, roles, permissions)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var context = new AuthorizationContext(tuple.hasPrincipal, tuple.roles, tuple.permissions);
            return context.Roles != null && context.Permissions != null;
        }).QuickCheckThrowOnFailure();
    }
}
