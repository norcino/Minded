using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MindedExample.Api.OData
{
    /// <summary>
    /// Custom JSON contract resolver that ignores virtual navigation properties unless they are
    /// explicitly requested via OData $expand parameter.
    /// </summary>
    /// <remarks>
    /// This resolver works in conjunction with <see cref="ODataExpandActionFilter"/> to control
    /// which navigation properties are serialized in JSON responses.
    ///
    /// The filter captures the $expand parameter from OData requests and stores the list of
    /// expanded properties in HttpContext.Items. This resolver then checks that list during
    /// JSON serialization to determine which navigation properties to include.
    ///
    /// This prevents automatic serialization of Entity Framework navigation properties while
    /// still allowing explicitly expanded properties to be serialized, avoiding:
    /// - Circular reference errors
    /// - Performance issues from loading unwanted data
    /// - Exposing more data than intended
    /// </remarks>
    public class IgnoreNavigationPropertiesResolver : DefaultContractResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Func<Type, bool> _isNavigationProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreNavigationPropertiesResolver"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor for the current HTTP context</param>
        /// <param name="isNavigationProperty">Optional custom function to determine if a type is a navigation property.
        /// If not provided, uses default logic that checks for types in the MindedExample.Domain namespace.</param>
        public IgnoreNavigationPropertiesResolver(
            IHttpContextAccessor httpContextAccessor,
            Func<Type, bool> isNavigationProperty = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _isNavigationProperty = isNavigationProperty ?? DefaultIsNavigationProperty;
        }

        /// <summary>
        /// Creates properties for JSON serialization with custom value providers for navigation properties.
        /// </summary>
        /// <param name="type">The type to create properties for</param>
        /// <param name="memberSerialization">The member serialization mode</param>
        /// <returns>List of JSON properties with conditional serialization for navigation properties</returns>
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            foreach (JsonProperty property in properties)
            {
                PropertyInfo propertyInfo = type.GetProperty(property.PropertyName);
                if (propertyInfo == null)
                    continue;

                // Skip if not virtual
                if (!IsVirtualProperty(propertyInfo))
                    continue;

                // Skip if explicitly marked with [JsonProperty] attribute
                if (propertyInfo.GetCustomAttribute<JsonPropertyAttribute>() != null)
                    continue;

                Type propertyType = propertyInfo.PropertyType;

                // Check if it's a navigation property (collection or reference)
                if (IsCollectionType(propertyType) || _isNavigationProperty(propertyType))
                {
                    var propertyName = property.PropertyName;

                    // Use ShouldSerialize to conditionally serialize based on OData $expand
                    property.ShouldSerialize = instance =>
                    {
                        // Get the list of expanded properties from HttpContext
                        var expandedProperties = _httpContextAccessor?.HttpContext?.Items[ODataConstants.ExpandedPropertiesKey] as HashSet<string>;

                        // If no expand list exists, don't serialize any navigation properties
                        if (expandedProperties == null)
                            return false;

                        // Only serialize if this property was explicitly expanded
                        return expandedProperties.Contains(propertyName);
                    };
                }
            }

            return properties;
        }

        /// <summary>
        /// Determines if a property is virtual.
        /// </summary>
        private bool IsVirtualProperty(PropertyInfo propertyInfo)
        {
            MethodInfo getMethod = propertyInfo.GetGetMethod();
            return getMethod != null && getMethod.IsVirtual && !getMethod.IsFinal;
        }

        /// <summary>
        /// Determines if a type is a collection type that likely represents a navigation property.
        /// Collections of primitive types (string, int, etc.) are excluded.
        /// </summary>
        private bool IsCollectionType(Type type)
        {
            if (type == typeof(string))
                return false;

            if (!type.IsGenericType)
                return false;

            var genericDef = type.GetGenericTypeDefinition();
            bool isCollection = genericDef == typeof(ICollection<>) ||
                                genericDef == typeof(IEnumerable<>) ||
                                genericDef == typeof(IList<>) ||
                                genericDef == typeof(List<>) ||
                                genericDef == typeof(HashSet<>);

            if (!isCollection)
                return false;

            // Collections of primitive/value types or strings are not navigation properties
            Type elementType = type.GetGenericArguments()[0];
            if (elementType.IsPrimitive || elementType == typeof(string) || elementType == typeof(decimal) ||
                elementType == typeof(DateTime) || elementType == typeof(DateTimeOffset) ||
                elementType == typeof(Guid) || elementType.IsEnum || elementType.IsValueType)
                return false;

            return true;
        }

        /// <summary>
        /// Default implementation for determining if a type is a reference navigation property.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is likely a navigation property, false otherwise</returns>
        /// <remarks>
        /// This default implementation checks if the type is in the MindedExample.Domain namespace.
        /// You can provide a custom implementation via the constructor to support different
        /// entity namespaces or more sophisticated detection logic.
        /// </remarks>
        private bool DefaultIsNavigationProperty(Type type)
        {
            // Exclude primitive types, strings, and common framework types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) || type == typeof(Guid) ||
                type.IsEnum || type.IsValueType)
                return false;

            // If it's a class type from the MindedExample.Domain namespace, it's likely a navigation property
            // This can be customized by providing a custom function in the constructor
            if (type.Namespace != null && type.Namespace.StartsWith("MindedExample.Domain"))
                return true;

            return false;
        }
    }
}

