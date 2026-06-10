using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Query;

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
            var claimClauses = new List<ClaimClause>();
            var resourceClauses = new List<ResourceClause>();
            bool hasRequireAuthentication = false;
            bool hasAllowUnauthenticated = false;

            foreach (var attr in attributes)
            {
                if (attr is RequireRolesAttribute rolesAttr)
                {
                    roleClauses.Add(new RoleClause(
                        Array.AsReadOnly(rolesAttr.Roles),
                        rolesAttr.Match,
                        rolesAttr.Minimum,
                        Array.AsReadOnly(rolesAttr.OrAnyRole ?? Array.Empty<string>()),
                        Array.AsReadOnly(rolesAttr.OrAnyPermission ?? Array.Empty<string>()),
                        Array.AsReadOnly(rolesAttr.OrAnyClaim ?? Array.Empty<string>())));
                }
                else if (attr is RequirePermissionsAttribute permsAttr)
                {
                    permissionClauses.Add(new PermissionClause(
                        Array.AsReadOnly(permsAttr.Permissions),
                        permsAttr.Match,
                        permsAttr.Minimum,
                        Array.AsReadOnly(permsAttr.OrAnyRole ?? Array.Empty<string>()),
                        Array.AsReadOnly(permsAttr.OrAnyPermission ?? Array.Empty<string>()),
                        Array.AsReadOnly(permsAttr.OrAnyClaim ?? Array.Empty<string>())));
                }
                else if (attr is RequireClaimAttribute claimAttr)
                {
                    claimClauses.Add(new ClaimClause(
                        claimAttr.ClaimType,
                        Array.AsReadOnly(claimAttr.Values ?? Array.Empty<string>()),
                        claimAttr.Match,
                        claimAttr.Minimum,
                        claimAttr.MatchProperty,
                        Array.AsReadOnly(claimAttr.OrAnyRole ?? Array.Empty<string>()),
                        Array.AsReadOnly(claimAttr.OrAnyPermission ?? Array.Empty<string>()),
                        Array.AsReadOnly(claimAttr.OrAnyClaim ?? Array.Empty<string>())));
                }
                else if (attr is RequireResourceAccessAttribute resourceAttr)
                {
                    var resourceProperty = requestType.GetProperty(resourceAttr.ResourceIdProperty);
                    if (resourceProperty == null)
                    {
                        throw new InvalidOperationException(
                            $"Type '{requestType.Name}' has RequireResourceAccessAttribute referencing property '{resourceAttr.ResourceIdProperty}' which does not exist.");
                    }

                    ValidateQueryType(requestType, resourceAttr.QueryType);

                    resourceClauses.Add(new ResourceClause(
                        resourceAttr.ResourceIdProperty,
                        resourceAttr.ResourceIdClaim,
                        resourceAttr.QueryType,
                        Array.AsReadOnly(resourceAttr.OrAnyRole ?? Array.Empty<string>()),
                        Array.AsReadOnly(resourceAttr.OrAnyPermission ?? Array.Empty<string>()),
                        Array.AsReadOnly(resourceAttr.OrAnyClaim ?? Array.Empty<string>())));
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

            bool hasRbacClauses = roleClauses.Count > 0 || permissionClauses.Count > 0 || claimClauses.Count > 0;
            bool hasResourceClauses = resourceClauses.Count > 0;
            bool isProtected = hasRbacClauses || hasResourceClauses || hasRequireAuthentication;
            bool requireAuthenticationOnly = hasRequireAuthentication && !hasRbacClauses && !hasResourceClauses;

            return new AuthorizationDescriptor(
                isProtected: isProtected,
                allowUnauthenticated: hasAllowUnauthenticated,
                requireAuthenticationOnly: requireAuthenticationOnly,
                roleClauses: roleClauses.AsReadOnly(),
                permissionClauses: permissionClauses.AsReadOnly(),
                claimClauses: claimClauses.AsReadOnly(),
                resourceClauses: resourceClauses.AsReadOnly());
        }

        private static void ValidateQueryType(Type requestType, Type queryType)
        {
            if (queryType == null)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with null queryType.");
            }

            var interfaces = queryType.GetInterfaces();
            bool implementsSupportedQuery = interfaces.Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IQuery<>)
                && IsSupportedQueryResultType(i.GetGenericArguments()[0]));

            if (!implementsSupportedQuery)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with queryType '{queryType.Name}' which does not implement IQuery<bool> or IQuery<IQueryResponse<bool>>.");
            }

            var constructor = queryType.GetConstructor(new[] { typeof(object), typeof(string) });
            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with queryType '{queryType.Name}' which does not have a constructor accepting (object resourceId, string claimValue).");
            }
        }

        private static bool IsSupportedQueryResultType(Type resultType)
        {
            if (resultType == typeof(bool))
            {
                return true;
            }

            return resultType.IsGenericType
                && resultType.GetGenericTypeDefinition() == typeof(IQueryResponse<>)
                && resultType.GetGenericArguments()[0] == typeof(bool);
        }
    }
}
