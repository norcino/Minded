using System.Collections.Generic;

namespace Minded.Extensions.DataProtection.Abstractions
{
    /// <summary>
    /// Service responsible for sanitizing sensitive data in objects before logging or exception handling.
    /// Inspects objects for properties marked with [SensitiveData] attribute
    /// and omits them unless explicitly configured to show them.
    /// </summary>
    /// <remarks>
    /// This interface is part of the Data Protection abstractions and can be implemented
    /// by different packages (e.g., Minded.Extensions.DataProtection) or custom implementations.
    /// When no implementation is registered, decorators will use a no-op implementation that
    /// passes data through unchanged.
    /// </remarks>
    public interface IDataSanitizer
    {
        /// <summary>
        /// Sanitizes an object by creating a dictionary representation with sensitive data protected.
        /// Properties marked with [SensitiveData] are omitted unless ShowSensitiveData is true.
        /// </summary>
        /// <param name="obj">The object to sanitize. Can be null.</param>
        /// <returns>
        /// A dictionary where keys are property names and values are either:
        /// - Original values (if not sensitive or ShowSensitiveData is true)
        /// - Omitted entirely (if sensitive and ShowSensitiveData is false)
        /// Returns null if the input object is null.
        /// </returns>
        IDictionary<string, object> Sanitize(object obj);

        /// <summary>
        /// Sanitizes a collection of objects.
        /// Each object in the collection is sanitized individually.
        /// </summary>
        /// <param name="collection">The collection to sanitize. Can be null.</param>
        /// <returns>
        /// A collection of sanitized dictionaries.
        /// Returns null if the input collection is null.
        /// </returns>
        IEnumerable<IDictionary<string, object>> SanitizeCollection(IEnumerable<object> collection);

        /// <summary>
        /// Checks if a property should be considered sensitive based on its attributes.
        /// A property is sensitive if it has the [SensitiveData] attribute.
        /// </summary>
        /// <param name="propertyInfo">The property to check.</param>
        /// <returns>True if the property is marked as sensitive, false otherwise.</returns>
        bool IsSensitiveProperty(System.Reflection.PropertyInfo propertyInfo);
    }
}

