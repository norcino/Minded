using System;
using System.Collections.Generic;
using System.Linq;
using Minded.Extensions.Authorization.Attributes;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Authorization.Decorator
{
    /// <summary>
    /// Validates RBAC attribute configurations at startup, throwing
    /// <see cref="InvalidOperationException"/> for any invalid configuration.
    /// </summary>
    internal static class AttributeValidator
    {
        /// <summary>
        /// Validates all RBAC attributes on the specified request type.
        /// </summary>
        public static void Validate(Type requestType)
        {
            var attributes = Attribute.GetCustomAttributes(requestType, true);

            bool hasRbac = false;
            bool hasAllowUnauthenticated = false;
            bool hasRequireAuthentication = false;
            bool hasRequireResourceAccess = false;

            foreach (var attr in attributes)
            {
                if (attr is RequireRolesAttribute rolesAttr)
                {
                    hasRbac = true;
                    ValidateItems(requestType, rolesAttr.Roles, rolesAttr.Match, rolesAttr.Minimum, "RequireRolesAttribute");
                    ValidateOrItems(requestType, rolesAttr.OrAnyRole, "RequireRolesAttribute.OrAnyRole");
                    ValidateOrItems(requestType, rolesAttr.OrAnyPermission, "RequireRolesAttribute.OrAnyPermission");
                    ValidateOrItems(requestType, rolesAttr.OrAnyClaim, "RequireRolesAttribute.OrAnyClaim");
                }
                else if (attr is RequirePermissionsAttribute permsAttr)
                {
                    hasRbac = true;
                    ValidateItems(requestType, permsAttr.Permissions, permsAttr.Match, permsAttr.Minimum, "RequirePermissionsAttribute");
                    ValidateOrItems(requestType, permsAttr.OrAnyRole, "RequirePermissionsAttribute.OrAnyRole");
                    ValidateOrItems(requestType, permsAttr.OrAnyPermission, "RequirePermissionsAttribute.OrAnyPermission");
                    ValidateOrItems(requestType, permsAttr.OrAnyClaim, "RequirePermissionsAttribute.OrAnyClaim");
                }
                else if (attr is RequireClaimAttribute claimAttr)
                {
                    hasRbac = true;
                    ValidateClaim(requestType, claimAttr);
                }
                else if (attr is RequireResourceAccessAttribute resourceAttr)
                {
                    hasRequireResourceAccess = true;
                    ValidateRequireResourceAccess(requestType, resourceAttr);
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

            if (hasAllowUnauthenticated && (hasRbac || hasRequireAuthentication || hasRequireResourceAccess))
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has AllowUnauthenticatedAttribute combined with RBAC, RequireAuthenticationAttribute, or RequireResourceAccessAttribute. " +
                    "These are contradictory and cannot be used together.");
            }
        }

        private static void ValidateItems(Type requestType, string[] items, AuthorizationMatch match, int minimum, string attributeName)
        {
            if (items == null || items.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has {attributeName} with an empty or null item array.");
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    throw new InvalidOperationException(
                        $"Type '{requestType.Name}' has {attributeName} with a blank or whitespace-only item name.");
                }
            }

            var normalized = items.Select(i => i.Trim()).ToList();
            var distinct = new HashSet<string>(normalized, StringComparer.OrdinalIgnoreCase);
            if (distinct.Count < normalized.Count)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has {attributeName} with duplicate values after case-insensitive trimmed normalization.");
            }

            if (match == AuthorizationMatch.AtLeast)
            {
                if (minimum <= 0)
                {
                    throw new InvalidOperationException(
                        $"Type '{requestType.Name}' has {attributeName} with Match=AtLeast and Minimum <= 0.");
                }

                if (minimum > items.Length)
                {
                    throw new InvalidOperationException(
                        $"Type '{requestType.Name}' has {attributeName} with Match=AtLeast and Minimum ({minimum}) exceeds item count ({items.Length}).");
                }
            }
            else
            {
                if (minimum != 0)
                {
                    throw new InvalidOperationException(
                        $"Type '{requestType.Name}' has {attributeName} with Match={match} and Minimum != 0.");
                }
            }
        }

        private static void ValidateClaim(Type requestType, RequireClaimAttribute claimAttr)
        {
            if (string.IsNullOrWhiteSpace(claimAttr.ClaimType))
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireClaimAttribute with blank ClaimType.");
            }

            if (string.IsNullOrWhiteSpace(claimAttr.MatchProperty))
            {
                ValidateItems(requestType, claimAttr.Values, claimAttr.Match, claimAttr.Minimum, "RequireClaimAttribute");
            }
            else
            {
                var property = requestType.GetProperty(claimAttr.MatchProperty);
                if (property == null)
                {
                    throw new InvalidOperationException(
                        $"Type '{requestType.Name}' has RequireClaimAttribute with MatchProperty '{claimAttr.MatchProperty}' which does not exist.");
                }
            }

            bool hasValues = claimAttr.Values != null && claimAttr.Values.Length > 0;
            bool hasMatchProperty = !string.IsNullOrWhiteSpace(claimAttr.MatchProperty);
            if (!hasValues && !hasMatchProperty)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireClaimAttribute with neither Values nor MatchProperty configured.");
            }

            ValidateOrItems(requestType, claimAttr.OrAnyRole, "RequireClaimAttribute.OrAnyRole");
            ValidateOrItems(requestType, claimAttr.OrAnyPermission, "RequireClaimAttribute.OrAnyPermission");
            ValidateOrItems(requestType, claimAttr.OrAnyClaim, "RequireClaimAttribute.OrAnyClaim");
        }

        private static void ValidateRequireResourceAccess(Type requestType, RequireResourceAccessAttribute resourceAttr)
        {
            if (string.IsNullOrWhiteSpace(resourceAttr.ResourceIdProperty))
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with blank resourceIdProperty.");
            }

            if (string.IsNullOrWhiteSpace(resourceAttr.ClaimName))
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with blank claimName.");
            }

            var resourceProperty = requestType.GetProperty(resourceAttr.ResourceIdProperty);
            if (resourceProperty == null)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute referencing property '{resourceAttr.ResourceIdProperty}' which does not exist.");
            }

            if (resourceAttr.QueryType == null)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with null queryType.");
            }

            var interfaces = resourceAttr.QueryType.GetInterfaces();
            bool implementsSupportedQuery = interfaces.Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IQuery<>)
                && IsSupportedQueryResultType(i.GetGenericArguments()[0]));

            if (!implementsSupportedQuery)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with queryType '{resourceAttr.QueryType.Name}' which does not implement IQuery<bool> or IQuery<IQueryResponse<bool>>.");
            }

            var constructor = resourceAttr.QueryType.GetConstructors()
                .FirstOrDefault(ctor =>
                {
                    var parameters = ctor.GetParameters();

                    if (parameters.Length < 2)
                        return false;

                    if (parameters[0].ParameterType != typeof(object))
                        return false;

                    if (parameters[1].ParameterType != typeof(string))
                        return false;

                    // Any parameters beyond the first two must be optional
                    return parameters.Skip(2).All(p => p.IsOptional);
                });

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has RequireResourceAccessAttribute with queryType '{resourceAttr.QueryType.Name}' which does not have a constructor accepting (object resourceId, string claimValue).");
            }

            ValidateOrItems(requestType, resourceAttr.OrAnyRole, "RequireResourceAccessAttribute.OrAnyRole");
            ValidateOrItems(requestType, resourceAttr.OrAnyPermission, "RequireResourceAccessAttribute.OrAnyPermission");
            ValidateOrItems(requestType, resourceAttr.OrAnyClaim, "RequireResourceAccessAttribute.OrAnyClaim");
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

        private static void ValidateOrItems(Type requestType, string[] items, string memberName)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    throw new InvalidOperationException(
                        $"Type '{requestType.Name}' has {memberName} with a blank or whitespace-only value.");
                }
            }
        }
    }
}
