using System;
using System.Collections.Generic;

namespace Minded.Framework.CQRS.Abstractions.Sanitization
{
    /// <summary>
    /// Sanitizer that processes object dictionaries to remove, filter, or transform properties
    /// before logging or serialization. Part of the centralized logging sanitization pipeline.
    /// </summary>
    /// <remarks>
    /// Sanitizers are applied in the order they are registered in the pipeline.
    /// Each sanitizer receives the output of the previous sanitizer, allowing for
    /// composition of sanitization logic.
    /// 
    /// Common use cases:
    /// - Removing sensitive data (passwords, tokens, PII)
    /// - Excluding framework-specific properties (e.g., ILoggable properties)
    /// - Transforming values for logging (e.g., truncating long strings)
    /// - Adding metadata or context information
    /// </remarks>
    public interface ILoggingSanitizer
    {
        /// <summary>
        /// Sanitizes a dictionary representation of an object.
        /// Can remove properties, transform values, or add metadata.
        /// </summary>
        /// <param name="data">Dictionary to sanitize (property/field name -> value).
        /// This is the output from the previous sanitizer in the pipeline.</param>
        /// <param name="sourceType">Original type of the object before conversion to dictionary.
        /// Useful for type-specific sanitization logic.</param>
        /// <returns>Sanitized dictionary. Can return the same instance (modified) or a new dictionary.</returns>
        IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType);
    }
}

