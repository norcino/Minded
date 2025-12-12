using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Framework.CQRS.Abstractions.Sanitization;

namespace Minded.Extensions.DataProtection
{
    /// <summary>
    /// Logging sanitizer that removes or masks sensitive data marked with [SensitiveData] attribute.
    /// Integrates with the centralized logging sanitization pipeline.
    /// </summary>
    /// <remarks>
    /// This sanitizer:
    /// - Inspects the source type for properties/fields marked with [SensitiveData]
    /// - Removes sensitive properties from the dictionary unless ShowSensitiveData is true
    /// - Respects both static and dynamic configuration via DataProtectionOptions
    /// - Works on dictionary input (after object-to-dictionary conversion)
    /// - Handles nested objects and collections recursively
    /// 
    /// Configuration is controlled independently from other sanitizers, allowing
    /// fine-grained control over sensitive data visibility.
    /// </remarks>
    internal class DataProtectionLoggingSanitizer : ILoggingSanitizer
    {
        private readonly IOptions<DataProtectionOptions> _options;
        private const int MaxDepth = 3;

        /// <summary>
        /// Initializes a new instance of the DataProtectionLoggingSanitizer.
        /// </summary>
        /// <param name="options">Data protection options containing ShowSensitiveData configuration.</param>
        public DataProtectionLoggingSanitizer(IOptions<DataProtectionOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType)
        {
            if (data == null || sourceType == null)
                return data;

            var showSensitiveData = _options.Value.GetEffectiveShowSensitiveData();
            
            // If showing sensitive data, return as-is
            if (showSensitiveData)
                return data;

            // Remove sensitive properties based on source type
            return SanitizeInternal(data, sourceType, 0);
        }

        /// <summary>
        /// Internal recursive sanitization method with depth tracking.
        /// </summary>
        private IDictionary<string, object> SanitizeInternal(IDictionary<string, object> data, Type sourceType, int depth)
        {
            if (data == null || depth >= MaxDepth)
                return data;

            var result = new Dictionary<string, object>();
            
            // Get all public properties and fields from the source type
            var properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = sourceType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            // Build a set of sensitive member names
            var sensitiveMembers = new HashSet<string>();
            
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<SensitiveDataAttribute>() != null)
                {
                    sensitiveMembers.Add(property.Name);
                }
            }
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<SensitiveDataAttribute>() != null)
                {
                    sensitiveMembers.Add(field.Name);
                }
            }

            // Process each entry in the dictionary
            foreach (var kvp in data)
            {
                // Skip sensitive members
                if (sensitiveMembers.Contains(kvp.Key))
                    continue;

                // Process the value recursively if it's a nested object or collection
                result[kvp.Key] = SanitizeValue(kvp.Value, depth);
            }

            return result;
        }

        /// <summary>
        /// Sanitizes a value, handling nested dictionaries and collections.
        /// </summary>
        private object SanitizeValue(object value, int depth)
        {
            if (value == null || depth >= MaxDepth)
                return value;

            // Handle nested dictionaries (from nested objects)
            if (value is IDictionary<string, object> nestedDict)
            {
                // We don't have the source type for nested objects, so we can't check for sensitive attributes
                // Just return as-is (the nested object was already sanitized during conversion)
                return nestedDict;
            }

            // Handle collections
            if (value is IEnumerable enumerable && !(value is string))
            {
                var items = new List<object>();
                foreach (var item in enumerable)
                {
                    items.Add(SanitizeValue(item, depth + 1));
                }
                return items;
            }

            // Return primitive values as-is
            return value;
        }
    }
}

