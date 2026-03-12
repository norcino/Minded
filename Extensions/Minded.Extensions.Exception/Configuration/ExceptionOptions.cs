using System;

namespace Minded.Extensions.Exception.Configuration
{
    /// <summary>
    /// Configuration options for the exception decorator.
    /// Controls whether commands or queries should be serialized and included in exception messages.
    /// </summary>
    public class ExceptionOptions
    {
        /// <summary>
        /// Gets or sets whether commands/queries should be serialized and included in exception messages.
        /// When true, the full command/query object (sanitized) is serialized to JSON and included in the exception.
        /// When false, only the command/query type name is included.
        /// This property is used as the default value when SerializeProvider is not set.
        /// Default: true (for backward compatibility and detailed debugging)
        /// </summary>
        /// <remarks>
        /// Serialization can be expensive for large commands/queries. Consider disabling in production
        /// if you don't need the full details in exception logs.
        /// </remarks>
        public bool Serialize { get; set; } = true;

        /// <summary>
        /// Gets or sets a function that dynamically determines whether commands/queries should be serialized.
        /// This allows runtime configuration changes (e.g., from feature flags, environment).
        /// When set, this takes precedence over Serialize.
        /// The function is called each time an exception occurs in a command or query handler.
        /// Example: () => _environment.IsDevelopment()
        /// Default: null (uses Serialize instead)
        /// </summary>
        public Func<bool> SerializeProvider { get; set; }

        /// <summary>
        /// Gets the effective setting for serialization.
        /// Uses SerializeProvider if set, otherwise falls back to Serialize.
        /// This method is called each time an exception occurs in a command or query handler.
        /// </summary>
        /// <returns>True if commands/queries should be serialized, false otherwise.</returns>
        public bool GetEffectiveSerialize()
        {
            return SerializeProvider?.Invoke() ?? Serialize;
        }
    }
}

