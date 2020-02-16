using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Minded.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMinded(this IServiceCollection serviceCollection, Func<AssemblyName, bool> assemblyFilter = null,
            Action<MindedBuilder> MindedBuilder = null)
        {
            var builder = new MindedBuilder(serviceCollection, assemblyFilter);

            // Register Mediators
            builder.RegisterMediator();

            // Register the Validators
            builder.RegisterValidators();

            // Register all the query handlers with the related decorators
            builder.RegisterQueryHandlers();

            // Register all the command handlers with the related decorators
            builder.RegisterCommandHandlers();

            MindedBuilder?.Invoke(builder);
        }
    }
}
