using System;

namespace Minded.Extensions.Authorization.Configuration
{
    /// <summary>
    /// Configuration options for the authorization decorator.
    /// Controls enforce-authentication policies for commands and queries.
    /// All properties support both static values and dynamic providers for runtime configuration (e.g., feature flags).
    /// </summary>
    public class AuthorizationOptions
    {
        /// <summary>
        /// Gets or sets whether all commands require authentication by default.
        /// When true, commands without RBAC attributes and without AllowUnauthenticatedAttribute
        /// will require an authenticated principal.
        /// This property is used as the default value when RequireAuthenticationForAllCommandsProvider is not set.
        /// Default: false
        /// </summary>
        public bool RequireAuthenticationForAllCommands { get; set; } = false;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether all commands require authentication.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over RequireAuthenticationForAllCommands.
        /// Default: null (uses RequireAuthenticationForAllCommands instead)
        /// </summary>
        public Func<bool> RequireAuthenticationForAllCommandsProvider { get; set; }

        /// <summary>
        /// Gets or sets whether all queries require authentication by default.
        /// When true, queries without RBAC attributes and without AllowUnauthenticatedAttribute
        /// will require an authenticated principal.
        /// This property is used as the default value when RequireAuthenticationForAllQueriesProvider is not set.
        /// Default: false
        /// </summary>
        public bool RequireAuthenticationForAllQueries { get; set; } = false;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether all queries require authentication.
        /// This allows runtime configuration changes (e.g., from feature flags).
        /// When set, this takes precedence over RequireAuthenticationForAllQueries.
        /// Default: null (uses RequireAuthenticationForAllQueries instead)
        /// </summary>
        public Func<bool> RequireAuthenticationForAllQueriesProvider { get; set; }

        /// <summary>
        /// Gets the effective state for requiring authentication for all commands.
        /// Uses RequireAuthenticationForAllCommandsProvider if set, otherwise falls back to RequireAuthenticationForAllCommands.
        /// </summary>
        public bool GetEffectiveRequireAuthenticationForAllCommands()
            => RequireAuthenticationForAllCommandsProvider?.Invoke() ?? RequireAuthenticationForAllCommands;

        /// <summary>
        /// Gets the effective state for requiring authentication for all queries.
        /// Uses RequireAuthenticationForAllQueriesProvider if set, otherwise falls back to RequireAuthenticationForAllQueries.
        /// </summary>
        public bool GetEffectiveRequireAuthenticationForAllQueries()
            => RequireAuthenticationForAllQueriesProvider?.Invoke() ?? RequireAuthenticationForAllQueries;
    }
}
