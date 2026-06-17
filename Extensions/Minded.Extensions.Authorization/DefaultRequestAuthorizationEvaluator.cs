using System;
using System.Collections.Generic;
using System.Linq;
using Minded.Extensions.Authorization.Attributes;

namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// Default implementation of <see cref="IRequestAuthorizationEvaluator"/> that evaluates
    /// role and permission clauses using case-insensitive, trimmed string comparison.
    /// </summary>
    public class DefaultRequestAuthorizationEvaluator : IRequestAuthorizationEvaluator
    {
        /// <inheritdoc />
        public AuthorizationDecision Evaluate(Type requestType, AuthorizationDescriptor descriptor, AuthorizationContext context)
        {
            if (!context.HasPrincipal)
            {
                return AuthorizationDecision.NoPrincipal();
            }

            if (descriptor.RequireAuthenticationOnly
                && descriptor.RoleClauses.Count == 0
                && descriptor.PermissionClauses.Count == 0
                && descriptor.ClaimClauses.Count == 0)
            {
                return AuthorizationDecision.Allow();
            }

            foreach (var clause in descriptor.RoleClauses)
            {
                if (IsOrClauseSatisfied(clause.OrAnyRole, clause.OrAnyPermission, clause.OrAnyClaim, context))
                {
                    continue;
                }

                if (!EvaluateClause(clause.Roles, context.Roles, clause.Match, clause.Minimum))
                {
                    return AuthorizationDecision.Deny();
                }
            }

            foreach (var clause in descriptor.PermissionClauses)
            {
                if (IsOrClauseSatisfied(clause.OrAnyRole, clause.OrAnyPermission, clause.OrAnyClaim, context))
                {
                    continue;
                }

                if (!EvaluateClause(clause.Permissions, context.Permissions, clause.Match, clause.Minimum))
                {
                    return AuthorizationDecision.Deny();
                }
            }

            foreach (var clause in descriptor.ClaimClauses)
            {
                if (!string.IsNullOrWhiteSpace(clause.MatchProperty))
                {
                    continue;
                }

                if (IsOrClauseSatisfied(clause.OrAnyRole, clause.OrAnyPermission, clause.OrAnyClaim, context))
                {
                    continue;
                }

                if (!EvaluateClaimClause(clause, context))
                {
                    return AuthorizationDecision.Deny();
                }
            }

            return AuthorizationDecision.Allow();
        }

        private static bool IsOrClauseSatisfied(
            IReadOnlyList<string> orAnyRole,
            IReadOnlyList<string> orAnyPermission,
            IReadOnlyList<string> orAnyClaim,
            AuthorizationContext context)
        {
            if (MatchesAny(orAnyRole, context.Roles))
            {
                return true;
            }

            if (MatchesAny(orAnyPermission, context.Permissions))
            {
                return true;
            }

            if (orAnyClaim != null)
            {
                for (int i = 0; i < orAnyClaim.Count; i++)
                {
                    var key = orAnyClaim[i]?.Trim();
                    if (!string.IsNullOrEmpty(key) && context.Claims.ContainsKey(key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool EvaluateClaimClause(ClaimClause clause, AuthorizationContext context)
        {
            if (!context.Claims.TryGetValue(clause.ClaimType, out var claimValue))
            {
                return false;
            }

            var normalizedClaimValue = (claimValue ?? string.Empty).Trim();
            var values = clause.Values ?? Array.Empty<string>();
            var normalizedAllowedValues = values
                .Select(v => (v ?? string.Empty).Trim())
                .ToList();

            switch (clause.Match)
            {
                case AuthorizationMatch.All:
                    return normalizedAllowedValues.All(v => string.Equals(v, normalizedClaimValue, StringComparison.OrdinalIgnoreCase));

                case AuthorizationMatch.Any:
                    return normalizedAllowedValues.Any(v => string.Equals(v, normalizedClaimValue, StringComparison.OrdinalIgnoreCase));

                case AuthorizationMatch.AtLeast:
                    var matches = normalizedAllowedValues.Count(v => string.Equals(v, normalizedClaimValue, StringComparison.OrdinalIgnoreCase));
                    return matches >= clause.Minimum;

                case AuthorizationMatch.None:
                    return normalizedAllowedValues.All(v => !string.Equals(v, normalizedClaimValue, StringComparison.OrdinalIgnoreCase));

                default:
                    return false;
            }
        }

        private static bool MatchesAny(IReadOnlyList<string> requiredItems, IReadOnlyCollection<string> contextItems)
        {
            if (requiredItems == null || requiredItems.Count == 0)
            {
                return false;
            }

            var trimmedContext = new HashSet<string>(
                contextItems.Select(item => item.Trim()),
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < requiredItems.Count; i++)
            {
                var item = requiredItems[i]?.Trim();
                if (!string.IsNullOrEmpty(item) && trimmedContext.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EvaluateClause(
            IReadOnlyList<string> requiredItems,
            IReadOnlyCollection<string> contextItems,
            AuthorizationMatch match,
            int minimum)
        {
            var trimmedContext = new HashSet<string>(
                contextItems.Select(item => item.Trim()),
                StringComparer.OrdinalIgnoreCase);

            switch (match)
            {
                case AuthorizationMatch.All:
                    return requiredItems.All(item => trimmedContext.Contains(item.Trim()));

                case AuthorizationMatch.Any:
                    return requiredItems.Any(item => trimmedContext.Contains(item.Trim()));

                case AuthorizationMatch.AtLeast:
                    var matchCount = requiredItems.Count(item => trimmedContext.Contains(item.Trim()));
                    return matchCount >= minimum;

                case AuthorizationMatch.None:
                    return !requiredItems.Any(item => trimmedContext.Contains(item.Trim()));

                default:
                    return false;
            }
        }
    }
}
