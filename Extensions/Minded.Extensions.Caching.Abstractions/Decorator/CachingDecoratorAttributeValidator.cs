using System;
using System.Linq;
using System.Reflection;
using Minded.Extensions.Caching.Decorator;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Caching.Abstractions.Decorator
{
    /// <summary>
    /// Enforces implementations of <see cref="CacheAttribute"/> to also implement <see cref="IGenerateCacheKey"/>, necessary to generate unique cache keys.
    /// </summary>
    public class CachingDecoratorAttributeValidator : IDecoratingAttributeValidator
    {
        /// <summary>
        /// Throws an exception if the configuration is invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Validate(Func<AssemblyName, bool> assemblyFilter)
        {
            if (assemblyFilter == null)
                assemblyFilter = (a) => { return true; };

            // Load all assemblies whitelisted in the filter
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => assemblyFilter(a.GetName()))
                .ToList();

            // Extract the types implementing the CacheAttribute
            var typesWithCacheAttribute = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(CacheAttribute), true).Length > 0);

            foreach (var type in typesWithCacheAttribute)
            {
                if (!typeof(IGenerateCacheKey).IsAssignableFrom(type))
                {
                    throw new InvalidOperationException($"The class {type.FullName} has a CacheAttribute (or a derived class) but does not implement {nameof(IGenerateCacheKey)}.");
                }
            }
        }
    }
}
