using System;
using System.Collections.Generic;

namespace Minded.Framework.CQRS.Abstractions.Sanitization
{
    /// <summary>
    /// Pipeline that orchestrates multiple ILoggingSanitizer implementations.
    /// Decorators can register sanitizers during their initialization to customize
    /// how commands and queries are sanitized before logging or exception handling.
    /// </summary>
    /// <remarks>
    /// The pipeline performs sanitization in two phases:
    /// 1. Object-to-Dictionary conversion: Converts the input object to a dictionary,
    ///    handling non-serializable types and applying property exclusions.
    /// 2. Sanitizer application: Applies all registered ILoggingSanitizer implementations
    ///    in registration order.
    /// 
    /// This design allows decorators to add their own sanitization logic without
    /// creating dependencies between extension packages.
    /// 
    /// Thread-safety: Implementations should be thread-safe for sanitization operations.
    /// Registration methods (RegisterSanitizer, ExcludeProperties) should only be called
    /// during application startup/configuration.
    /// </remarks>
    public interface ILoggingSanitizerPipeline
    {
        /// <summary>
        /// Converts an object to a dictionary and applies all registered sanitizers.
        /// This is the main entry point for sanitizing commands/queries before logging.
        /// </summary>
        /// <param name="obj">Object to sanitize (typically a command or query).
        /// Can be null.</param>
        /// <returns>Sanitized dictionary ready for serialization.
        /// Returns empty dictionary if input is null.</returns>
        IDictionary<string, object> Sanitize(object obj);
        
        /// <summary>
        /// Registers a sanitizer to be applied during sanitization.
        /// Sanitizers are applied in registration order.
        /// </summary>
        /// <param name="sanitizer">Sanitizer to add to the pipeline.
        /// Cannot be null.</param>
        /// <remarks>
        /// This method should be called during application startup, typically in
        /// AddXDecorator() extension methods. Not thread-safe - do not call after
        /// application has started processing requests.
        /// </remarks>
        void RegisterSanitizer(ILoggingSanitizer sanitizer);
        
        /// <summary>
        /// Registers properties or fields to exclude from sanitization output.
        /// Uses high-performance HashSet lookup for O(1) exclusion checks.
        /// </summary>
        /// <param name="interfaceType">Interface type that defines the properties/fields.
        /// Used to scope exclusions to specific interfaces (e.g., ILoggable).</param>
        /// <param name="memberNames">Property or field names to exclude.
        /// Case-sensitive.</param>
        /// <remarks>
        /// Example: ExcludeProperties(typeof(ILoggable), "LoggingTemplate", "LoggingParameters")
        /// will exclude these properties from any object implementing ILoggable.
        /// 
        /// This method should be called during application startup, typically in
        /// AddXDecorator() extension methods. Not thread-safe - do not call after
        /// application has started processing requests.
        /// </remarks>
        void ExcludeProperties(Type interfaceType, params string[] memberNames);
    }
}

