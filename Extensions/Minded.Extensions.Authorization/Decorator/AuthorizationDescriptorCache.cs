using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Minded.Extensions.Authorization.Attributes;

namespace Minded.Extensions.Authorization.Decorator
{
    /// <summary>
    /// Thread-safe cache that compiles and stores <see cref="AuthorizationDescriptor"/> instances
    /// per request type. Each type is compiled at most once via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    internal static class AuthorizationDescriptorCache
    {
        private static readonly ConcurrentDictionary<Type, AuthorizationDescriptor> _cache = new ConcurrentDictionary<Type, AuthorizationDescriptor>();

        /// <summary>
        /// Gets or creates the <see cref="AuthorizationDescriptor"/> for the specified request type.
        /// </summary>
        public static AuthorizationDescriptor GetOrCreate(Type requestType) =>
            _cache.GetOrAdd(requestType, Compile);

        /// <summary>
        /// Clears the cache. Intended for test isolation only.
        /// </summary>
        internal static void Clear() => _cache.Clear();

        private static AuthorizationDescriptor Compile(Type requestType)
        {
            var attributes = Attribute.GetCustomAttributes(requestType, true);

            var roleClauses = new List<RoleClause>();
            var permissionClauses = new List<PermissionClause>();
            bool hasRequireAuthentication = false;
            bool hasAllowUnauthenticated = false;

            foreach (var attr in attributes)
            {
                if (attr is RequireRolesAttribute rolesAttr)
                {
                    roleClauses.Add(new RoleClause(
                        Array.AsReadOnly(rolesAttr.Roles),
                        rolesAttr.Match,
                        rolesAttr.Minimum));
                }
                else if (attr is RequirePermissionsAttribute permsAttr)
                {
                    permissionClauses.Add(new PermissionClause(
                        Array.AsReadOnly(permsAttr.Permissions),
                        permsAttr.Match,
                        permsAttr.Minimum));
                }
                else if (attr is RequireAuthenticationAttribute)
                {
                    hasRequireAuthentication = true;
                }
                else if (attr is AllowUnauthenticatedAttribute)
                {
                    hasAllowUnauthenticated = true;
                }
            }

            bool hasRbacClauses = roleClauses.Count > 0 || permissionClauses.Count > 0;
            bool isProtected = hasRbacClauses || hasRequireAuthentication;
            bool requireAuthenticationOnly = hasRequireAuthentication && !hasRbacClauses;

            return new AuthorizationDescriptor(
                isProtected: isProtected,
                allowUnauthenticated: hasAllowUnauthenticated,
                requireAuthenticationOnly: requireAuthenticationOnly,
                roleClauses: roleClauses.AsReadOnly(),
                permissionClauses: permissionClauses.AsReadOnly());
        }
    }
}
