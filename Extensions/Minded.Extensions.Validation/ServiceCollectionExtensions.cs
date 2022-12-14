using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;
using Minded.Extensions.Validation.Decorator;

namespace Minded.Extensions.Validation
{
    public class ServiceCollectionExtensions : MindedBuilder
    {
        public ServiceCollectionExtensions(IServiceCollection serviceCollection, Func<AssemblyName, bool> assemblyNameFilter = null) : base(serviceCollection, assemblyNameFilter)
        {
        }

        /// <summary>
        /// Register the Validator classes used to validate commands and entities
        /// </summary>
        public void RegisterValidators()
        {
            RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(ICommandValidator<>));
            RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(IValidator<>));
        }
    }
}
