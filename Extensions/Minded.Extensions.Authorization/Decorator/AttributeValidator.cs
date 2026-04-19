using System;
using System.Collections.Generic;
using System.Linq;
using Minded.Extensions.Authorization.Attributes;

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

            foreach (var attr in attributes)
            {
                if (attr is RequireRolesAttribute rolesAttr)
                {
                    hasRbac = true;
                    ValidateItems(requestType, rolesAttr.Roles, rolesAttr.Match, rolesAttr.Minimum, "RequireRolesAttribute");
                }
                else if (attr is RequirePermissionsAttribute permsAttr)
                {
                    hasRbac = true;
                    ValidateItems(requestType, permsAttr.Permissions, permsAttr.Match, permsAttr.Minimum, "RequirePermissionsAttribute");
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

            if (hasAllowUnauthenticated && (hasRbac || hasRequireAuthentication))
            {
                throw new InvalidOperationException(
                    $"Type '{requestType.Name}' has AllowUnauthenticatedAttribute combined with RBAC or RequireAuthenticationAttribute. " +
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
    }
}
