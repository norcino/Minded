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
                && descriptor.PermissionClauses.Count == 0)
            {
                return AuthorizationDecision.Allow();
            }

            foreach (var clause in descriptor.RoleClauses)
            {
                if (!EvaluateClause(clause.Roles, context.Roles, clause.Match, clause.Minimum))
                {
                    return AuthorizationDecision.Deny();
                }
            }

            foreach (var clause in descriptor.PermissionClauses)
            {
                if (!EvaluateClause(clause.Permissions, context.Permissions, clause.Match, clause.Minimum))
                {
                    return AuthorizationDecision.Deny();
                }
            }

            return AuthorizationDecision.Allow();
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
