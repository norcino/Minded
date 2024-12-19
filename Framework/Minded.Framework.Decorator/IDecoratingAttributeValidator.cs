using System.Reflection;
using System;

namespace Minded.Framework.Decorator
{
    /// <summary>
    /// This is implemented in decorators to add validation classes which are automatically invoked during the framework configuration at startup.
    /// The validator is meant to throw an exception informing the developer of any misconfigurations.
    /// </summary>
    public interface IDecoratingAttributeValidator
    {
        /// <summary>
        /// Throws an exception if the configuration is invalid.
        /// </summary>
        /// <param name="assemblyFilter">The filter to apply to assemblies during validation.</param>
        void Validate(Func<AssemblyName, bool> assemblyFilter);
    }
}
