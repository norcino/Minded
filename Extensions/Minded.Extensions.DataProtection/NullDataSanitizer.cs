using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Minded.Extensions.DataProtection.Abstractions;

namespace Minded.Extensions.DataProtection
{
    /// <summary>
    /// No-op implementation of IDataSanitizer that passes data through unchanged.
    /// This is used as a fallback when DataProtection is not explicitly configured,
    /// allowing Logging and Exception decorators to work without requiring DataProtection setup.
    /// </summary>
    /// <remarks>
    /// This implementation:
    /// - Does NOT protect sensitive data
    /// - Returns all properties unchanged, including those marked with [SensitiveData]
    /// - Provides a simple dictionary representation of objects
    /// - Used automatically when AddDataProtection() is not called
    /// 
    /// To enable actual data protection, call builder.AddDataProtection() in your configuration.
    /// </remarks>
    public class NullDataSanitizer : IDataSanitizer
    {
        /// <inheritdoc/>
        /// <remarks>
        /// This implementation creates a simple dictionary representation without any sanitization.
        /// All properties are included, even those marked with [SensitiveData].
        /// </remarks>
        public IDictionary<string, object> Sanitize(object obj)
        {
            if (obj == null)
                return null;

            var result = new Dictionary<string, object>();
            Type type = obj.GetType();

            // Handle primitive types and strings directly
            if (type.IsPrimitive || type == typeof(string) || type == typeof(System.DateTime) || 
                type == typeof(System.Guid) || type.IsEnum)
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
                    if (!property.CanRead)
                        continue;

                    var value = property.GetValue(obj);
                    result[property.Name] = FormatValue(value);
                }
                catch
                {
                    // If we can't read a property, skip it
                    continue;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This implementation sanitizes each item in the collection without any data protection.
        /// </remarks>
        public IEnumerable<IDictionary<string, object>> SanitizeCollection(IEnumerable<object> collection)
        {
            if (collection == null)
                return null;

            return collection.Select(item => Sanitize(item)).ToList();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This implementation always returns false since it doesn't perform any sanitization.
        /// The property sensitivity is not checked in the no-op implementation.
        /// </remarks>
        public bool IsSensitiveProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return false;

            return propertyInfo.GetCustomAttribute<SensitiveDataAttribute>() != null;
        }

        /// <summary>
        /// Formats a property value without any sanitization.
        /// </summary>
        private object FormatValue(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            // Handle primitive types, strings, and common value types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(System.DateTime) || 
                type == typeof(System.Guid) || type.IsEnum || type == typeof(decimal))
            {
                return value;
            }

            // Handle collections - limit to 10 items to prevent huge outputs
            if (value is IEnumerable enumerable && !(value is string))
            {
                var items = new List<object>();
                var count = 0;
                const int maxItems = 10;

                foreach (var item in enumerable)
                {
                    if (count++ >= maxItems)
                    {
                        items.Add("... (truncated)");
                        break;
                    }
                    items.Add(FormatValue(item));
                }

                return items;
            }

            // For complex objects, just return a simple representation
            return value.ToString();
        }
    }
}

