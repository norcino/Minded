using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Exception
{
    /// <summary>
    /// Sanitizes objects for diagnostic logging by removing non-serializable types and excluded properties.
    /// This sanitizer is applied before the IDataSanitizer to ensure safe serialization.
    /// </summary>
    /// <remarks>
    /// This sanitizer:
    /// - Removes properties of types that cannot be serialized (CancellationToken, Task, Func, Action, Stream, etc.)
    /// - Removes properties marked with [ExcludeFromSerializedDiagnosticLogging] attribute
    /// - Removes properties marked with [JsonIgnore] attribute
    /// - Handles nested objects recursively with depth limiting
    /// - Truncates collections to prevent excessive output
    /// </remarks>
    public static class DiagnosticDataSanitizer
    {
        private const int MaxDepth = 3;
        private const int MaxCollectionItems = 10;

        /// <summary>
        /// Set of types that should always be excluded from serialization because they cannot be serialized
        /// or would cause issues during serialization.
        /// </summary>
        private static readonly HashSet<Type> NonSerializableTypes = new HashSet<Type>
        {
            typeof(CancellationToken),
            typeof(CancellationTokenSource),
            typeof(Stream),
            typeof(MemoryStream),
            typeof(FileStream),
            typeof(TextReader),
            typeof(TextWriter),
            typeof(StreamReader),
            typeof(StreamWriter),
            typeof(IntPtr),
            typeof(UIntPtr)
        };

        /// <summary>
        /// Sanitizes an object by removing non-serializable properties and properties marked for exclusion.
        /// Returns a dictionary representation safe for JSON serialization.
        /// </summary>
        /// <param name="obj">The object to sanitize.</param>
        /// <returns>A dictionary containing only serializable properties.</returns>
        public static IDictionary<string, object> Sanitize(object obj)
        {
            if (obj == null)
                return new Dictionary<string, object>();

            return SanitizeInternal(obj, 0);
        }

        private static IDictionary<string, object> SanitizeInternal(object obj, int depth)
        {
            if (obj == null || depth >= MaxDepth)
                return null;

            var result = new Dictionary<string, object>();
            Type type = obj.GetType();

            // Handle primitive types and common value types directly
            if (IsPrimitiveOrSimpleType(type))
            {
                result["Value"] = obj;
                return result;
            }

            // Get all public instance properties
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    if (!property.CanRead)
                        continue;

                    // Check if property should be excluded
                    if (ShouldExcludeProperty(property))
                        continue;

                    // Check if property type is non-serializable
                    if (IsNonSerializableType(property.PropertyType))
                        continue;

                    var value = property.GetValue(obj);
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
        /// Determines if a property should be excluded based on attributes.
        /// </summary>
        private static bool ShouldExcludeProperty(PropertyInfo property)
        {
            // Check for ExcludeFromSerializedDiagnosticLogging attribute
            if (property.GetCustomAttribute<ExcludeFromSerializedDiagnosticLoggingAttribute>() != null)
                return true;

            // Check for JsonIgnore attribute
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a type is non-serializable and should be excluded.
        /// </summary>
        private static bool IsNonSerializableType(Type type)
        {
            // Check for exact type match
            if (NonSerializableTypes.Contains(type))
                return true;

            // Check for nullable version of non-serializable types
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null && NonSerializableTypes.Contains(underlyingType))
                return true;

            // Check for Task types
            if (typeof(Task).IsAssignableFrom(type))
                return true;

            // Check for ValueTask types
            if (type.IsGenericType && type.GetGenericTypeDefinition().FullName?.StartsWith("System.Threading.Tasks.ValueTask") == true)
                return true;

            // Check for delegate types (Func<>, Action<>, etc.)
            if (typeof(Delegate).IsAssignableFrom(type))
                return true;

            // Check for Stream types (covers all derived streams)
            if (typeof(Stream).IsAssignableFrom(type))
                return true;

            // Check for Type (System.Type is not serializable in a meaningful way)
            if (typeof(Type).IsAssignableFrom(type) || type == typeof(RuntimeTypeHandle))
                return true;

            // Check for HttpContext and related web types (if present)
            if (type.FullName?.StartsWith("Microsoft.AspNetCore.Http") == true)
                return true;

            // Check for IServiceProvider
            if (typeof(IServiceProvider).IsAssignableFrom(type))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a type is a primitive or simple type that can be serialized directly.
        /// </summary>
        private static bool IsPrimitiveOrSimpleType(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || type == typeof(decimal)
                || type.IsEnum;
        }

        /// <summary>
        /// Formats a property value for serialization, handling nested objects and collections.
        /// </summary>
        private static object FormatValue(object value, int depth)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            // Handle primitive types and common value types directly
            if (IsPrimitiveOrSimpleType(type))
            {
                return value;
            }

            // Skip non-serializable values
            if (IsNonSerializableType(type))
            {
                return null;
            }

            // Handle collections (but not strings which are IEnumerable)
            if (value is IEnumerable enumerable && !(value is string))
            {
                List<object> items = new List<object>();
                int count = 0;

                foreach (object item in enumerable)
                {
                    if (count++ >= MaxCollectionItems)
                    {
                        items.Add("... (truncated)");
                        break;
                    }

                    // Skip non-serializable items
                    if (item != null && IsNonSerializableType(item.GetType()))
                        continue;

                    items.Add(FormatValue(item, depth + 1));
                }

                return items;
            }

            // Handle nested objects (recursively sanitize)
            return SanitizeInternal(value, depth + 1);
        }
    }
}

