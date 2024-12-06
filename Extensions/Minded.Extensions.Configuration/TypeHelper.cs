using System;

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
            // If the type is the generic interface definition itself
            if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType)
            {
                return true;
            }

            // If the type is a constructed generic interface or an implementation that implements the generic interface
            if (type.IsInterface)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType)
                {
                    return true;
                }
            }

            // For types that implement interfaces, check implemented interfaces
            Type[] interfaces = type.GetInterfaces();
            foreach (Type iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == interfaceType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
