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

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationDescriptor"/>.
        /// </summary>
        public AuthorizationDescriptor(
            bool isProtected,
            bool allowUnauthenticated,
            bool requireAuthenticationOnly,
            IReadOnlyList<RoleClause> roleClauses,
            IReadOnlyList<PermissionClause> permissionClauses)
        {
            IsProtected = isProtected;
            AllowUnauthenticated = allowUnauthenticated;
            RequireAuthenticationOnly = requireAuthenticationOnly;
            RoleClauses = roleClauses;
            PermissionClauses = permissionClauses;
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

        /// <summary>
        /// Initializes a new instance of <see cref="RoleClause"/>.
        /// </summary>
        public RoleClause(IReadOnlyList<string> roles, AuthorizationMatch match, int minimum)
        {
            Roles = roles;
            Match = match;
            Minimum = minimum;
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

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionClause"/>.
        /// </summary>
        public PermissionClause(IReadOnlyList<string> permissions, AuthorizationMatch match, int minimum)
        {
            Permissions = permissions;
            Match = match;
            Minimum = minimum;
        }
    }
}
