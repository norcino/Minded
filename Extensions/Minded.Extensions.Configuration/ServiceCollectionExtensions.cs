using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Minded.Extensions.Configuration
{
    /// <summary>
    /// Extensions for IServiceCollection to register the Minded framework
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Minded framework to the application's DI container, allowing the <see cref="MindedBuilder"/>
        /// to configure the framework behaviour.
        /// </summary>
        /// <param name="serviceCollection">Service Collection used to control the dependency injection.</param>
        /// <param name="configuration">Application configuration instance.</param>
        /// <param name="assemblyFilter">Filter function used to select the assemblies to scan for handlers and decorators.</param>
        /// <param name="mindedBuilderConfiguration">Optional action that receives the <see cref="MindedBuilder"/> and allows further customisation.</param>
        public static void AddMinded(this IServiceCollection serviceCollection, IConfiguration configuration, Func<AssemblyName, bool> assemblyFilter = null,
            Action<MindedBuilder> mindedBuilderConfiguration = null)
        {
            var builder = new MindedBuilder(serviceCollection, configuration, assemblyFilter);
            mindedBuilderConfiguration?.Invoke(builder);
        }
    }
}
