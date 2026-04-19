using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Minded.Extensions.Authorization;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Authorization.Decorator;

namespace Minded.Extensions.Authorization.Tests.Descriptors;

[TestClass]
public class AuthorizationDescriptorCacheTests
{
    [TestInitialize]
    public void Setup()
    {
        AuthorizationDescriptorCache.Clear();
    }

    // --- Test types with various attribute combinations ---

    private class UnattributedRequest { }

    [RequireRoles("Admin")]
    private class SingleRoleRequest { }

    [RequirePermissions("orders.read", "orders.write")]
    private class MultiPermissionRequest { }

    [RequireRoles("Admin", "Manager")]
    [RequirePermissions("orders.read")]
    private class RolesAndPermissionsRequest { }

    [RequireAuthentication]
    private class AuthenticationOnlyRequest { }

    [AllowUnauthenticated]
    private class AllowUnauthenticatedRequest { }

    [RequireAuthentication]
    [RequireRoles("Admin")]
    private class AuthenticationWithRolesRequest { }

    [RequireRoles("Admin", Match = AuthorizationMatch.Any)]
    [RequireRoles("Editor", "Viewer", Match = AuthorizationMatch.AtLeast, Minimum = 1)]
    private class MultipleRoleClausesRequest { }

    [RequirePermissions("read", Match = AuthorizationMatch.None)]
    private class NoneMatchPermissionRequest { }

    /// <summary>
    /// Property 8: Descriptor compilation is correct and deterministic —
    /// For any request type with a combination of attributes, the compiled descriptor
    /// has correct IsProtected, AllowUnauthenticated, RequireAuthenticationOnly,
    /// and the correct number of RoleClauses and PermissionClauses.
    /// **Validates: Requirements 8.1, 8.7, 8.8**
    /// </summary>
    [TestMethod]
    public void Property8_DescriptorCompilation_IsCorrectAndDeterministic()
    {
        var testCases = new (Type type, bool isProtected, bool allowUnauth, bool requireAuthOnly, int roleClauses, int permClauses)[]
        {
            (typeof(UnattributedRequest), false, false, false, 0, 0),
            (typeof(SingleRoleRequest), true, false, false, 1, 0),
            (typeof(MultiPermissionRequest), true, false, false, 0, 1),
            (typeof(RolesAndPermissionsRequest), true, false, false, 1, 1),
            (typeof(AuthenticationOnlyRequest), true, false, true, 0, 0),
            (typeof(AllowUnauthenticatedRequest), false, true, false, 0, 0),
            (typeof(AuthenticationWithRolesRequest), true, false, false, 1, 0),
            (typeof(MultipleRoleClausesRequest), true, false, false, 2, 0),
            (typeof(NoneMatchPermissionRequest), true, false, false, 0, 1),
        };

        var arb = Gen.Elements(testCases).ToArbitrary();

        Prop.ForAll(arb, tc =>
        {
            AuthorizationDescriptorCache.Clear();
            var descriptor = AuthorizationDescriptorCache.GetOrCreate(tc.type);

            // Compile a second time to verify determinism
            AuthorizationDescriptorCache.Clear();
            var descriptor2 = AuthorizationDescriptorCache.GetOrCreate(tc.type);

            return descriptor.IsProtected == tc.isProtected
                && descriptor.AllowUnauthenticated == tc.allowUnauth
                && descriptor.RequireAuthenticationOnly == tc.requireAuthOnly
                && descriptor.RoleClauses.Count == tc.roleClauses
                && descriptor.PermissionClauses.Count == tc.permClauses
                // Determinism: second compilation produces same values
                && descriptor2.IsProtected == descriptor.IsProtected
                && descriptor2.AllowUnauthenticated == descriptor.AllowUnauthenticated
                && descriptor2.RequireAuthenticationOnly == descriptor.RequireAuthenticationOnly
                && descriptor2.RoleClauses.Count == descriptor.RoleClauses.Count
                && descriptor2.PermissionClauses.Count == descriptor.PermissionClauses.Count;
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 9: Descriptor cache returns same instance per type —
    /// For any request type, calling GetOrCreate multiple times always returns
    /// the same AuthorizationDescriptor instance (reference equality).
    /// **Validates: Requirements 9.1, 9.2**
    /// </summary>
    [TestMethod]
    public void Property9_DescriptorCache_ReturnsSameInstancePerType()
    {
        var types = new[]
        {
            typeof(UnattributedRequest),
            typeof(SingleRoleRequest),
            typeof(MultiPermissionRequest),
            typeof(RolesAndPermissionsRequest),
            typeof(AuthenticationOnlyRequest),
            typeof(AllowUnauthenticatedRequest),
            typeof(AuthenticationWithRolesRequest),
            typeof(MultipleRoleClausesRequest),
            typeof(NoneMatchPermissionRequest),
        };

        var arb = (from type in Gen.Elements(types)
                   from callCount in Gen.Choose(2, 10)
                   select (type, callCount)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var first = AuthorizationDescriptorCache.GetOrCreate(tuple.type);
            for (int i = 1; i < tuple.callCount; i++)
            {
                var subsequent = AuthorizationDescriptorCache.GetOrCreate(tuple.type);
                if (!ReferenceEquals(first, subsequent))
                    return false;
            }
            return true;
        }).QuickCheckThrowOnFailure();
    }
}
