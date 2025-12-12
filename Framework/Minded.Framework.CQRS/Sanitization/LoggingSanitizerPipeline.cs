using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Minded.Framework.CQRS.Abstractions.Sanitization;

namespace Minded.Framework.CQRS.Sanitization
{
    /// <summary>
    /// Default implementation of ILoggingSanitizerPipeline.
    /// Converts objects to dictionaries and applies registered sanitizers in order.
    /// Handles non-serializable types and property/field exclusions for optimal performance.
    /// </summary>
    /// <remarks>
    /// This implementation:
    /// - Recursively inspects objects up to a maximum depth of 3 levels
    /// - Truncates collections to a maximum of 10 items
    /// - Handles both public properties and fields
    /// - Excludes non-serializable types (CancellationToken, Task, Stream, etc.)
    /// - Uses HashSet for O(1) property exclusion lookups
    /// - Thread-safe for sanitization operations (registration methods are not thread-safe)
    /// </remarks>
    internal class LoggingSanitizerPipeline : ILoggingSanitizerPipeline
    {
        private readonly List<ILoggingSanitizer> _sanitizers = new List<ILoggingSanitizer>();
        private readonly HashSet<(Type InterfaceType, string MemberName)> _excludedMembers =
            new HashSet<(Type, string)>();

        private const int MaxDepth = 3;
        private const int MaxCollectionItems = 10;

        // Types that cannot be serialized and should be excluded
        private static readonly HashSet<Type> NonSerializableTypes = new HashSet<Type>
        {
            typeof(System.Threading.CancellationToken),
            typeof(System.Threading.Tasks.Task),
            typeof(System.IO.Stream),
            typeof(Delegate),
            typeof(MulticastDelegate)
        };

        /// <summary>
        /// Initializes a new instance of the LoggingSanitizerPipeline.
        /// Automatically registers all ILoggingSanitizer implementations from DI.
        /// </summary>
        /// <param name="sanitizers">Collection of sanitizers to register (injected from DI).</param>
        public LoggingSanitizerPipeline(IEnumerable<ILoggingSanitizer> sanitizers)
        {
            if (sanitizers != null)
            {
                _sanitizers.AddRange(sanitizers);
            }
        }

        /// <inheritdoc/>
        public IDictionary<string, object> Sanitize(object obj)
        {
            if (obj == null)
                return new Dictionary<string, object>();

            // Phase 1: Convert object to dictionary
            var dictionary = ConvertToDictionary(obj, 0);
            
            // Phase 2: Apply all registered sanitizers
            var sourceType = obj.GetType();
            foreach (var sanitizer in _sanitizers)
            {
                dictionary = sanitizer.Sanitize(dictionary, sourceType);
                if (dictionary == null)
                    return new Dictionary<string, object>();
            }
            
            return dictionary;
        }

        /// <inheritdoc/>
        public void RegisterSanitizer(ILoggingSanitizer sanitizer)
        {
            if (sanitizer == null)
                throw new ArgumentNullException(nameof(sanitizer));
                
            _sanitizers.Add(sanitizer);
        }

        /// <inheritdoc/>
        public void ExcludeProperties(Type interfaceType, params string[] memberNames)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            
            if (memberNames == null || memberNames.Length == 0)
                return;
            
            foreach (var memberName in memberNames)
            {
                if (!string.IsNullOrWhiteSpace(memberName))
                {
                    _excludedMembers.Add((interfaceType, memberName));
                }
            }
        }

        /// <summary>
        /// Converts an object to a dictionary representation, handling non-serializable types
        /// and applying property/field exclusions.
        /// </summary>
        private IDictionary<string, object> ConvertToDictionary(object obj, int depth)
        {
            if (obj == null || depth >= MaxDepth)
                return null;

            var type = obj.GetType();
            
            // Handle primitive types and common value types
            if (IsPrimitiveOrValueType(type))
            {
                return new Dictionary<string, object> { ["Value"] = obj };
            }

            var result = new Dictionary<string, object>();
            
            // Get all public properties and fields
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            // Process properties
            foreach (var property in properties)
            {
                ProcessMember(obj, type, property.Name, () => property.GetValue(obj), 
                    property.PropertyType, result, depth);
            }
            
            // Process fields
            foreach (var field in fields)
            {
                ProcessMember(obj, type, field.Name, () => field.GetValue(obj), 
                    field.FieldType, result, depth);
            }
            
            return result;
        }

        /// <summary>
        /// Processes a single property or field, applying exclusion rules and type checks.
        /// </summary>
        private void ProcessMember(object obj, Type objType, string memberName, 
            Func<object> getValue, Type memberType, Dictionary<string, object> result, int depth)
        {
            try
            {
                // Check if member should be excluded based on interface
                if (ShouldExcludeMember(objType, memberName))
                    return;
                
                // Skip non-serializable types
                if (IsNonSerializableType(memberType))
                    return;

                var value = getValue();
                result[memberName] = FormatValue(value, depth);
            }
            catch
            {
                // If we can't read a member, skip it
            }
        }

        /// <summary>
        /// Checks if a member should be excluded based on registered exclusions.
        /// Uses O(1) HashSet lookup for performance.
        /// </summary>
        private bool ShouldExcludeMember(Type objType, string memberName)
        {
            // Check all interfaces implemented by the object type
            var interfaces = objType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (_excludedMembers.Contains((interfaceType, memberName)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a type is non-serializable and should be excluded.
        /// </summary>
        private static bool IsNonSerializableType(Type type)
        {
            if (NonSerializableTypes.Contains(type))
                return true;

            // Check if type is a delegate
            if (typeof(Delegate).IsAssignableFrom(type))
                return true;

            // Check for generic Task types
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>))
                return true;

            // Check for Action and Func types
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef.FullName?.StartsWith("System.Action") == true ||
                    genericTypeDef.FullName?.StartsWith("System.Func") == true)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a type is a primitive or common value type that can be serialized directly.
        /// </summary>
        private static bool IsPrimitiveOrValueType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(Guid) ||
                   type == typeof(decimal) ||
                   type == typeof(TimeSpan) ||
                   type.IsEnum;
        }

        /// <summary>
        /// Formats a value for inclusion in the dictionary, handling nested objects and collections.
        /// </summary>
        private object FormatValue(object value, int depth)
        {
            if (value == null)
                return null;

            var type = value.GetType();

            // Handle primitive types and common value types
            if (IsPrimitiveOrValueType(type))
                return value;

            // Handle collections
            if (value is IEnumerable enumerable && !(value is string))
            {
                var items = new List<object>();
                var count = 0;

                foreach (var item in enumerable)
                {
                    if (count++ >= MaxCollectionItems)
                    {
                        items.Add("... (truncated)");
                        break;
                    }
                    items.Add(FormatValue(item, depth + 1));
                }

                return items;
            }

            // Handle nested objects (recursively convert to dictionary)
            return ConvertToDictionary(value, depth + 1);
        }
    }
}


