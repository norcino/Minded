using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Authorization.Decorator;

namespace Minded.Extensions.Authorization.Tests.Descriptors;

[TestClass]
public class AttributeValidationTests
{
    // --- Test types for each validation rule ---

    [RequireRoles]
    private class EmptyRolesRequest { }

    [RequirePermissions]
    private class EmptyPermissionsRequest { }

    [RequireRoles("Admin", "")]
    private class BlankRoleItemRequest { }

    [RequirePermissions("read", "  ")]
    private class WhitespacePermissionItemRequest { }

    [RequireRoles("Admin", "admin")]
    private class DuplicateRolesRequest { }

    [RequirePermissions("Read", " read ")]
    private class DuplicatePermissionsRequest { }

    [RequireRoles("Admin", "Manager", Match = AuthorizationMatch.AtLeast, Minimum = 0)]
    private class AtLeastMinimumZeroRequest { }

    [RequireRoles("Admin", "Manager", Match = AuthorizationMatch.AtLeast, Minimum = -1)]
    private class AtLeastMinimumNegativeRequest { }

    [RequireRoles("Admin", Match = AuthorizationMatch.AtLeast, Minimum = 5)]
    private class AtLeastMinimumExceedsCountRequest { }

    [RequireRoles("Admin", Match = AuthorizationMatch.All, Minimum = 1)]
    private class NonAtLeastWithMinimumRequest { }

    [RequirePermissions("read", Match = AuthorizationMatch.Any, Minimum = 2)]
    private class NonAtLeastPermWithMinimumRequest { }

    [RequirePermissions("read", Match = AuthorizationMatch.None, Minimum = 1)]
    private class NoneMatchWithMinimumRequest { }

    [AllowUnauthenticated]
    [RequireRoles("Admin")]
    private class AllowUnauthWithRolesRequest { }

    [AllowUnauthenticated]
    [RequirePermissions("read")]
    private class AllowUnauthWithPermissionsRequest { }

    [AllowUnauthenticated]
    [RequireAuthentication]
    private class AllowUnauthWithRequireAuthRequest { }

    // Valid types that should NOT throw
    [RequireRoles("Admin", "Manager")]
    private class ValidRolesRequest { }

    [RequirePermissions("read", "write", Match = AuthorizationMatch.AtLeast, Minimum = 1)]
    private class ValidAtLeastRequest { }

    [AllowUnauthenticated]
    private class ValidAllowUnauthRequest { }

    private class ValidUnattributedRequest { }

    /// <summary>
    /// Property 10: Invalid attribute configurations are rejected at validation —
    /// For any invalid configuration (empty/null items, blank items, duplicates,
    /// AtLeast with bad Minimum, non-AtLeast with Minimum != 0,
    /// AllowUnauthenticated + RBAC conflict), validation throws InvalidOperationException.
    /// **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 23.6**
    /// </summary>
    [TestMethod]
    public void Property10_InvalidAttributeConfigurations_AreRejectedAtValidation()
    {
        var invalidTypes = new[]
        {
            typeof(EmptyRolesRequest),
            typeof(EmptyPermissionsRequest),
            typeof(BlankRoleItemRequest),
            typeof(WhitespacePermissionItemRequest),
            typeof(DuplicateRolesRequest),
            typeof(DuplicatePermissionsRequest),
            typeof(AtLeastMinimumZeroRequest),
            typeof(AtLeastMinimumNegativeRequest),
            typeof(AtLeastMinimumExceedsCountRequest),
            typeof(NonAtLeastWithMinimumRequest),
            typeof(NonAtLeastPermWithMinimumRequest),
            typeof(NoneMatchWithMinimumRequest),
            typeof(AllowUnauthWithRolesRequest),
            typeof(AllowUnauthWithPermissionsRequest),
            typeof(AllowUnauthWithRequireAuthRequest),
        };

        var validTypes = new[]
        {
            typeof(ValidRolesRequest),
            typeof(ValidAtLeastRequest),
            typeof(ValidAllowUnauthRequest),
            typeof(ValidUnattributedRequest),
        };

        // Property: all invalid types throw InvalidOperationException
        var invalidArb = Gen.Elements(invalidTypes).ToArbitrary();
        Prop.ForAll(invalidArb, invalidType =>
        {
            try
            {
                AttributeValidator.Validate(invalidType);
                return false; // Should have thrown
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }).QuickCheckThrowOnFailure();

        // Property: all valid types do NOT throw
        var validArb = Gen.Elements(validTypes).ToArbitrary();
        Prop.ForAll(validArb, validType =>
        {
            try
            {
                AttributeValidator.Validate(validType);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false; // Should not have thrown
            }
        }).QuickCheckThrowOnFailure();
    }
}
