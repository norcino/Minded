using System.Collections.Generic;
using Minded.Extensions.Authorization.Attributes;

namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// An immutable descriptor compiled from RBAC attributes on a request type.
    /// Contains all role clauses and permission clauses for authorization evaluation.
    /// </summary>
    public sealed class AuthorizationDescriptor
    {
        /// <summary>Gets a value indicating whether the request type has any RBAC or RequireAuthentication attributes.</summary>
        public bool IsProtected { get; }

        /// <summary>Gets a value indicating whether the request type carries the AllowUnauthenticatedAttribute.</summary>
        public bool AllowUnauthenticated { get; }

        /// <summary>
        /// Gets a value indicating whether the request type carries the RequireAuthenticationAttribute
        /// without any RBAC clauses (roles or permissions).
        /// </summary>
        public bool RequireAuthenticationOnly { get; }

        /// <summary>Gets the role clauses compiled from RequireRolesAttribute instances.</summary>
        public IReadOnlyList<RoleClause> RoleClauses { get; }

        /// <summary>Gets the permission clauses compiled from RequirePermissionsAttribute instances.</summary>
        public IReadOnlyList<PermissionClause> PermissionClauses { get; }

        /// <summary>Gets the claim clauses compiled from RequireClaimAttribute instances.</summary>
        public IReadOnlyList<ClaimClause> ClaimClauses { get; }

        /// <summary>Gets the resource clauses compiled from RequireResourceAccessAttribute instances.</summary>
        public IReadOnlyList<ResourceClause> ResourceClauses { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationDescriptor"/>.
        /// </summary>
        public AuthorizationDescriptor(
            bool isProtected,
            bool allowUnauthenticated,
            bool requireAuthenticationOnly,
            IReadOnlyList<RoleClause> roleClauses,
            IReadOnlyList<PermissionClause> permissionClauses,
            IReadOnlyList<ClaimClause> claimClauses,
            IReadOnlyList<ResourceClause> resourceClauses)
        {
            IsProtected = isProtected;
            AllowUnauthenticated = allowUnauthenticated;
            RequireAuthenticationOnly = requireAuthenticationOnly;
            RoleClauses = roleClauses;
            PermissionClauses = permissionClauses;
            ClaimClauses = claimClauses;
            ResourceClauses = resourceClauses;
        }
    }

    /// <summary>
    /// An immutable clause representing one RequireRolesAttribute instance,
    /// containing the role list, match mode, and minimum count.
    /// </summary>
    public sealed class RoleClause
    {
        /// <summary>Gets the role names required by this clause.</summary>
        public IReadOnlyList<string> Roles { get; }

        /// <summary>Gets how the roles are matched.</summary>
        public AuthorizationMatch Match { get; }

        /// <summary>Gets the minimum number of matching roles when Match is AtLeast.</summary>
        public int Minimum { get; }

        /// <summary>Gets role names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyRole { get; }

        /// <summary>Gets permission names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyPermission { get; }

        /// <summary>Gets claim keys that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyClaim { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RoleClause"/>.
        /// </summary>
        public RoleClause(
            IReadOnlyList<string> roles,
            AuthorizationMatch match,
            int minimum,
            IReadOnlyList<string> orAnyRole,
            IReadOnlyList<string> orAnyPermission,
            IReadOnlyList<string> orAnyClaim)
        {
            Roles = roles;
            Match = match;
            Minimum = minimum;
            OrAnyRole = orAnyRole;
            OrAnyPermission = orAnyPermission;
            OrAnyClaim = orAnyClaim;
        }
    }

    /// <summary>
    /// An immutable clause representing one RequirePermissionsAttribute instance,
    /// containing the permission list, match mode, and minimum count.
    /// </summary>
    public sealed class PermissionClause
    {
        /// <summary>Gets the permission names required by this clause.</summary>
        public IReadOnlyList<string> Permissions { get; }

        /// <summary>Gets how the permissions are matched.</summary>
        public AuthorizationMatch Match { get; }

        /// <summary>Gets the minimum number of matching permissions when Match is AtLeast.</summary>
        public int Minimum { get; }

        /// <summary>Gets role names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyRole { get; }

        /// <summary>Gets permission names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyPermission { get; }

        /// <summary>Gets claim keys that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyClaim { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionClause"/>.
        /// </summary>
        public PermissionClause(
            IReadOnlyList<string> permissions,
            AuthorizationMatch match,
            int minimum,
            IReadOnlyList<string> orAnyRole,
            IReadOnlyList<string> orAnyPermission,
            IReadOnlyList<string> orAnyClaim)
        {
            Permissions = permissions;
            Match = match;
            Minimum = minimum;
            OrAnyRole = orAnyRole;
            OrAnyPermission = orAnyPermission;
            OrAnyClaim = orAnyClaim;
        }
    }

    /// <summary>
    /// An immutable clause representing one RequireClaimAttribute instance.
    /// </summary>
    public sealed class ClaimClause
    {
        /// <summary>Gets the claim key to evaluate.</summary>
        public string ClaimType { get; }

        /// <summary>Gets the allowed claim values for static matching.</summary>
        public IReadOnlyList<string> Values { get; }

        /// <summary>Gets how the values are matched.</summary>
        public AuthorizationMatch Match { get; }

        /// <summary>Gets the minimum number of matching values when Match is AtLeast.</summary>
        public int Minimum { get; }

        /// <summary>Gets the optional request property used for dynamic matching.</summary>
        public string MatchProperty { get; }

        /// <summary>Gets role names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyRole { get; }

        /// <summary>Gets permission names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyPermission { get; }

        /// <summary>Gets claim keys that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyClaim { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ClaimClause"/>.
        /// </summary>
        public ClaimClause(
            string claimType,
            IReadOnlyList<string> values,
            AuthorizationMatch match,
            int minimum,
            string matchProperty,
            IReadOnlyList<string> orAnyRole,
            IReadOnlyList<string> orAnyPermission,
            IReadOnlyList<string> orAnyClaim)
        {
            ClaimType = claimType;
            Values = values;
            Match = match;
            Minimum = minimum;
            MatchProperty = matchProperty;
            OrAnyRole = orAnyRole;
            OrAnyPermission = orAnyPermission;
            OrAnyClaim = orAnyClaim;
        }
    }

    /// <summary>
    /// An immutable clause representing one RequireResourceAccessAttribute instance.
    /// </summary>
    public sealed class ResourceClause
    {
        /// <summary>Gets the request property name containing the resource identifier.</summary>
        public string ResourceIdProperty { get; }

        /// <summary>Gets the claim key containing caller identifier.</summary>
        public string ClaimName { get; }

        /// <summary>Gets the authorization query type.</summary>
        public System.Type QueryType { get; }

        /// <summary>Gets role names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyRole { get; }

        /// <summary>Gets permission names that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyPermission { get; }

        /// <summary>Gets claim keys that short-circuit this clause when any one is present on the caller.</summary>
        public IReadOnlyList<string> OrAnyClaim { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ResourceClause"/>.
        /// </summary>
        public ResourceClause(
            string resourceIdProperty,
            string claimName,
            System.Type queryType,
            IReadOnlyList<string> orAnyRole,
            IReadOnlyList<string> orAnyPermission,
            IReadOnlyList<string> orAnyClaim)
        {
            ResourceIdProperty = resourceIdProperty;
            ClaimName = claimName;
            QueryType = queryType;
            OrAnyRole = orAnyRole;
            OrAnyPermission = orAnyPermission;
            OrAnyClaim = orAnyClaim;
        }
    }
}
