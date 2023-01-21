using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Minded.Extensions.Configuration
{
    /// <summary>
    /// Extensions for IServiceCollection to register the Minded framework
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Minded framework allowing to use teh MindedBuilder to customise the framework behaviour
        /// </summary>
        /// <param name="serviceCollection">Service Collection used to control the dependency injection</param>
        /// <param name="assemblyFilter">Filter function used to select the assemblies to scan</param>
        /// <param name="MindedBuilder"><paramref name="MindedBuilder"/></param>
        public static void AddMinded(this IServiceCollection serviceCollection, Func<AssemblyName, bool> assemblyFilter = null,
            Action<MindedBuilder> MindedBuilder = null)
        {
            var builder = new MindedBuilder(serviceCollection, assemblyFilter);

            // Register Mediators
            //builder.RegisterMediator();

            //// Register the Validators
            //builder.RegisterValidators();

            //// Register all the query handlers with the related decorators
            //builder.RegisterQueryHandlers();

            //// Register all the command handlers with the related decorators
            //builder.RegisterCommandHandlers();

            
            MindedBuilder?.Invoke(builder);
        }
    }
}
