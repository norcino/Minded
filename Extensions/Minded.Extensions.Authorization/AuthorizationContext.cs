using System;
using System.Collections.Generic;

namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// Represents the authorization context of the current caller,
    /// exposing whether a principal is present and the caller's roles and permissions.
    /// </summary>
    public class AuthorizationContext
    {
        /// <summary>Gets a value indicating whether the current caller has an authenticated principal.</summary>
        public bool HasPrincipal { get; }

        /// <summary>Gets the roles associated with the current caller. Never null.</summary>
        public IReadOnlyCollection<string> Roles { get; }

        /// <summary>Gets the permissions associated with the current caller. Never null.</summary>
        public IReadOnlyCollection<string> Permissions { get; }

        /// <summary>
        /// Gets the claims associated with the current caller as key-value pairs. Never null.
        /// Claim keys are matched case-insensitively.
        /// </summary>
        public IReadOnlyDictionary<string, string> Claims { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationContext"/>.
        /// </summary>
        /// <param name="hasPrincipal">Whether the current caller has an authenticated principal.</param>
        /// <param name="roles">The roles associated with the caller, or null to default to an empty collection.</param>
        /// <param name="permissions">The permissions associated with the caller, or null to default to an empty collection.</param>
        /// <param name="claims">The claims associated with the caller, or null to default to an empty dictionary.</param>
        public AuthorizationContext(
            bool hasPrincipal,
            IReadOnlyCollection<string> roles = null,
            IReadOnlyCollection<string> permissions = null,
            IReadOnlyDictionary<string, string> claims = null)
        {
            HasPrincipal = hasPrincipal;
            Roles = roles ?? Array.Empty<string>();
            Permissions = permissions ?? Array.Empty<string>();

            var normalizedClaims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (claims != null)
            {
                foreach (var pair in claims)
                {
                    normalizedClaims[pair.Key] = pair.Value;
                }
            }

            Claims = normalizedClaims;
        }
    }
}
