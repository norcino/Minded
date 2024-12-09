using System;
using System.Linq;

namespace Minded.Extensions.Configuration
{
    /// <summary>
    /// Helper class to support Type operations
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Checks if a given type is a specific interface or implements it
        /// </summary>
        /// <param name="interfaceType">Interface to test</param>
        /// <param name="type">Type to test</param>
        /// <returns>True if type is or implementes interface type</returns>
        public static bool IsInterfaceOrImplementation(Type interfaceType, Type type)
        {
            if (interfaceType == null || type == null)
                throw new ArgumentNullException(interfaceType == null ? nameof(interfaceType) : nameof(type));

            if (interfaceType.IsGenericTypeDefinition)
            {
                return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType) ||
                       (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType);
            }

            return interfaceType.IsAssignableFrom(type);
        }
    }
}
