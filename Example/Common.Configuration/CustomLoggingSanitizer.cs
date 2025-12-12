using System;
using System.Collections.Generic;
using Minded.Framework.CQRS.Abstractions.Sanitization;

namespace Common.Configuration
{
    /// <summary>
    /// Example custom logging sanitizer that demonstrates how to create custom sanitization logic.
    /// This sanitizer removes properties containing "Internal" in their name from logs.
    /// </summary>
    /// <remarks>
    /// Custom sanitizers are automatically discovered and applied by the logging sanitization pipeline
    /// when registered as ILoggingSanitizer implementations in the DI container.
    /// 
    /// Use cases for custom sanitizers:
    /// - Remove internal/debug properties from production logs
    /// - Mask or hash specific property values
    /// - Add metadata or context to logged objects
    /// - Transform complex objects into simpler representations
    /// </remarks>
    public class CustomLoggingSanitizer : ILoggingSanitizer
    {
        /// <summary>
        /// Sanitizes the dictionary by removing properties containing "Internal" in their name.
        /// This demonstrates a simple filtering approach for custom sanitization.
        /// </summary>
        /// <param name="data">Dictionary representation of the object to sanitize</param>
        /// <param name="sourceType">Original type of the object (can be used for type-specific logic)</param>
        /// <returns>Sanitized dictionary with internal properties removed</returns>
        public IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType)
        {
            if (data == null)
                return new Dictionary<string, object>();

            var sanitized = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                // Skip properties containing "Internal" in the name
                // This is just an example - you can implement any custom logic here
                if (!kvp.Key.Contains("Internal", StringComparison.OrdinalIgnoreCase))
                {
                    sanitized[kvp.Key] = kvp.Value;
                }
            }

            return sanitized;
        }
    }
}

