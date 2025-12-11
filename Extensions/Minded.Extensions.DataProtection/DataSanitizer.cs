using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Minded.Extensions.DataProtection.Abstractions;

namespace Minded.Extensions.DataProtection
{
    /// <summary>
    /// Default implementation of IDataSanitizer that protects sensitive data in logs and exception messages.
    /// Uses reflection to inspect objects and omits properties marked with [SensitiveData] attribute.
    /// The behavior is configured via DataProtectionOptions.ShowSensitiveData.
    /// </summary>
    /// <remarks>
    /// This implementation:
    /// - Recursively inspects objects up to a maximum depth of 3 levels
    /// - Truncates collections to a maximum of 10 items
    /// - Handles primitive types, strings, DateTime, Guid, enums, and decimal
    /// - Thread-safe and can be registered as a singleton
    /// </remarks>
    public class DataSanitizer : IDataSanitizer
    {
        private readonly IOptions<DataProtectionOptions> _options;
        private const int MaxDepth = 3; // Prevent infinite recursion in nested objects

        /// <summary>
        /// Initializes a new instance of the DataSanitizer class.
        /// </summary>
        /// <param name="options">Data protection options containing the ShowSensitiveData configuration.</param>
        public DataSanitizer(IOptions<DataProtectionOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public IDictionary<string, object> Sanitize(object obj)
        {
            if (obj == null)
                return new Dictionary<string, object>();

            return SanitizeInternal(obj, 0);
        }

        /// <inheritdoc/>
        public IEnumerable<IDictionary<string, object>> SanitizeCollection(IEnumerable<object> collection)
        {
            if (collection == null)
                return new List<IDictionary<string, object>>();

            // Truncate to 10 items to prevent excessive logging
            return collection.Take(10).Select(item => Sanitize(item)).ToList();
        }

        /// <inheritdoc/>
        public bool IsSensitiveProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return false;

            return propertyInfo.GetCustomAttribute<SensitiveDataAttribute>() != null;
        }

        /// <summary>
        /// Internal recursive sanitization method with depth tracking to prevent infinite loops.
        /// </summary>
        private IDictionary<string, object> SanitizeInternal(object obj, int depth)
        {
            if (obj == null || depth >= MaxDepth)
                return null;

            var result = new Dictionary<string, object>();
            Type type = obj.GetType();

            // Handle primitive types and strings directly
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(Guid) || type.IsEnum)
            {
                result["Value"] = obj;
                return result;
            }

            // Get all public properties
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    // Skip properties that can't be read
                    if (!property.CanRead)
                        continue;

                    var value = property.GetValue(obj);
                    var showSensitiveData = _options.Value.GetEffectiveShowSensitiveData();

                    // Check if property is sensitive
                    var isSensitive = property.GetCustomAttribute<SensitiveDataAttribute>() != null;

                    if (isSensitive && !showSensitiveData)
                    {
                        // Sensitive property and we're hiding it - don't add to result
                        continue;
                    }

                    // Include the property (either non-sensitive, or sensitive but ShowSensitiveData is true)
                    result[property.Name] = FormatValue(value, depth);
                }
                catch
                {
                    // If we can't read a property, skip it
                    continue;
                }
            }

            return result;
        }

        /// <summary>
        /// Formats a property value for logging, handling nested objects and collections.
        /// </summary>
        private object FormatValue(object value, int depth)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            // Handle primitive types, strings, and common value types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || 
                type == typeof(Guid) || type.IsEnum || type == typeof(decimal))
            {
                return value;
            }

            // Handle collections
            if (value is IEnumerable enumerable && !(value is string))
            {
                var items = new List<object>();
                var count = 0;
                const int maxItems = 10; // Limit collection size in logs

                foreach (var item in enumerable)
                {
                    if (count++ >= maxItems)
                    {
                        items.Add("... (truncated)");
                        break;
                    }
                    items.Add(FormatValue(item, depth + 1));
                }

                return items;
            }

            // Handle nested objects (recursively sanitize)
            return SanitizeInternal(value, depth + 1);
        }
    }
}

