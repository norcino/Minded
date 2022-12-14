using Minded.CommandQuery.Query;
using Minded.Common;
using Minded.Decorator.Exception;
using Minded.Decorator.Logging;
using Minded.Decorator.Transaction;
using Minded.Decorator.Validation;
using Minded.Mediator;
using Minded.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Minded.Configuration
{
    /// <summary>
    /// Builder class to configure the Minded framework
    /// </summary>
    public class MindedBuilder
    {
        /// <summary>
        /// Action used to filter the assemblies by name to be used to register decorators
        /// </summary>
        private Func<AssemblyName, bool> AssemblyNameFilter;
        private IServiceCollection serviceCollection;

        public MindedBuilder(IServiceCollection serviceCollection, Func<AssemblyName, bool> assemblyNameFilter = null)
        {
            this.serviceCollection = serviceCollection;
            AssemblyNameFilter = assemblyNameFilter;
        }

        /// <summary>
        /// Add a Command Handler Decorator to the current Command handling setup, the last decorator added will be the first to process the incoming query.
        /// </summary>
        /// <param name="genericDecoratorType">Decorator type to use for the current decoration</param>
        /// <param name="requiredAttributeType">Optional attribute that if provided must be present in the command in order to enable the current decoration</param>
        /// <param name="optionalDependencyType">Optional interface type that if provided will be used to create an instance passed as parameter to the current decorator</param>
        /// <returns></returns>
        public MindedBuilder AddCommandHandlerDecorator(Type genericDecoratorType, Type requiredAttributeType = null, Type optionalDependencyType = null)
        {
            foreach (var assembly in ServiceAssemblies)
            {
                var queryHandlers = GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(ICommandHandler<>));

                foreach (var handlerType in queryHandlers)
                {
                    var interfaceType = GetGenericInterfaceInType(handlerType, typeof(ICommandHandler<>));
                    DecorateHandlerdescriptors(interfaceType, genericDecoratorType, requiredAttributeType, optionalDependencyType);
                }
            }
            return this;
        }

        /// <summary>
        /// Add a Query Handler Decorator to the current Query handling setup, the last decorator added will be the first to process the incoming query.
        /// </summary>
        /// <param name="genericDecoratorType">Decorator type to use for the current decoration</param>
        /// <param name="requiredAttributeType">Optional attribute that if provided must be present in the query in order to enable the current decoration</param>
        /// <param name="optionalDependencyType">Optional interface type that if provided will be used to create an instance passed as parameter to the current decorator</param>
        /// <returns></returns>
        public MindedBuilder AddQueryHandlerDecorator(Type genericDecoratorType, Type requiredAttributeType = null, Type optionalDependencyType = null)
        {
            foreach (var assembly in ServiceAssemblies)
            {
                var queryHandlers = GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(IQueryHandler<,>));

                foreach (var handlerType in queryHandlers)
                {
                    var interfaceType = GetGenericInterfaceInType(handlerType, typeof(IQueryHandler<,>));
                    DecorateHandlerdescriptors(interfaceType, genericDecoratorType, requiredAttributeType, optionalDependencyType);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove all registered decorators leaving only the Query Handler.
        /// This enables custom setup to manually register additional decorators for the Query Handler.
        /// </summary>
        /// <returns></returns>
        public MindedBuilder RemoveAllQueryDecorators()
        {
            serviceCollection.RemoveAll(typeof(IQueryHandler<,>));
            foreach (var assembly in ServiceAssemblies)
            {
                var queryHandlers = GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(IQueryHandler<,>));

                foreach (var handlerType in queryHandlers)
                {
                    var interfaceType = GetGenericInterfaceInType(handlerType, typeof(IQueryHandler<,>));

                    // Register the handler by it's interface
                    serviceCollection.Add(new ServiceDescriptor(interfaceType, handlerType, ServiceLifetime.Transient));

                    // Register the handler by it's own type
                    serviceCollection.Add(new ServiceDescriptor(handlerType, handlerType, ServiceLifetime.Transient));
                }
            }
            return this;
        }

        /// <summary>
        /// Remove all registered decorators leaving only the Command Handler.
        /// This enables custom setup to manually register additional decorators for the Command Handler.
        /// </summary>
        /// <returns></returns>
        public MindedBuilder RemoveAllCommandDecorators()
        {
            serviceCollection.RemoveAll(typeof(ICommandHandler<>));

            foreach (var assembly in ServiceAssemblies)
            {
                var commandHandlers = GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(ICommandHandler<>));

                foreach (var handlerType in commandHandlers)
                {
                    var interfaceType = GetGenericInterfaceInType(handlerType, typeof(ICommandHandler<>));

                    // Register the handler by it's interface
                    serviceCollection.Add(new ServiceDescriptor(interfaceType, handlerType, ServiceLifetime.Transient));

                    // Register the handler by it's own type
                    serviceCollection.Add(new ServiceDescriptor(handlerType, handlerType, ServiceLifetime.Transient));
                    return this;
                }
            }
            return this;
        }

        #region Internal helpers
        /// <summary>
        /// Register the Mediator class
        /// </summary>
        /// <param name="services">Service container for the Dependency Injection</param>
        internal void RegisterMediator()
        {
            serviceCollection.AddTransient<IMediator>(service => new Mediator.Mediator(service));
        }

        /// <summary>
        /// Register the query handers and all the decorators
        /// </summary>
        /// <param name="services">Service container for the Dependency Injection</param>
        internal void RegisterQueryHandlers()
        {
            foreach (var assembly in ServiceAssemblies)
            {
                var queryHandlers = GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(IQueryHandler<,>));

                foreach (var handlerType in queryHandlers)
                {
                    var interfaceType = GetGenericInterfaceInType(handlerType, typeof(IQueryHandler<,>));

                    // Register the handler by it's interface
                    serviceCollection.Add(new ServiceDescriptor(interfaceType, handlerType, ServiceLifetime.Transient));

                    // Register the handler by it's own type
                    serviceCollection.Add(new ServiceDescriptor(handlerType, handlerType, ServiceLifetime.Transient));

                    DecorateHandlerdescriptors(interfaceType, typeof(TransactionalQueryHandlerDecorator<,>));
                    DecorateHandlerdescriptors(interfaceType, typeof(ExceptionQueryHandlerDecorator<,>));
                    DecorateHandlerdescriptors(interfaceType, typeof(LoggingQueryHandlerDecorator<,>));
                }
            }
        }

        /// <summary>
        /// Register the command handers and all the decorators
        /// </summary>
        /// <param name="services">Service container for the Dependency Injection</param>
        internal void RegisterCommandHandlers()
        {
            foreach (var assembly in ServiceAssemblies)
            {
                var commandHandlers = GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(ICommandHandler<>));

                foreach (var handlerType in commandHandlers)
                {
                    var interfaceType = GetGenericInterfaceInType(handlerType, typeof(ICommandHandler<>));

                    // Register the handler by it's interface
                    serviceCollection.Add(new ServiceDescriptor(interfaceType, handlerType, ServiceLifetime.Transient));

                    // Register the handler by it's own type
                    serviceCollection.Add(new ServiceDescriptor(handlerType, handlerType, ServiceLifetime.Transient));

                    DecorateHandlerdescriptors(interfaceType, typeof(ValidatingCommandHandlerDecorator<>), typeof(ValidateCommandAttribute), GetICommandValidatorInterfaceType(interfaceType));
                    DecorateHandlerdescriptors(interfaceType, typeof(TransactionalCommandHandlerDecorator<>));
                    DecorateHandlerdescriptors(interfaceType, typeof(ExceptionCommandHandlerDecorator<>));
                    DecorateHandlerdescriptors(interfaceType, typeof(LoggingCommandHandlerDecorator<>));
                }
            }
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Register the Validator classes used to validate commands and entities
        /// </summary>
        public void RegisterValidators()
        {
            RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(ICommandValidator<>));
            RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(IValidator<>));
        }

        /// <summary>
        /// Scan all assemblies of the Service Layer, and registers all Types implementing the Generic Interfaces
        /// </summary>
        /// <param name="genericInterface">Generic interface</param>
        private void RegisterAllTypesInServiceAssembliesImplementingInterface(Type genericInterface)
        {
            foreach (var assembly in ServiceAssemblies)
            {
                var validatorTypes = GetGenericTypesImplementingInterfaceInAssembly(assembly, genericInterface);

                foreach (var validatorType in validatorTypes)
                {
                    var interfaceType = GetGenericInterfaceInType(validatorType, genericInterface);
                    serviceCollection.Add(new ServiceDescriptor(interfaceType, validatorType, ServiceLifetime.Transient));
                }
            }
        }

        /// <summary>
        /// Get the service descripts from the service collection for the given service Type
        /// </summary>
        /// <param name="serviceType">Type of the registered service</param>
        /// <returns>List of service descriptors matching the give Type</returns>
        private List<ServiceDescriptor> GetDescriptors(Type serviceType)
        {
            var descriptors = serviceCollection.Where(service => service.ServiceType == serviceType).ToList();

            if (descriptors.Count == 0)
            {
                throw new InvalidOperationException($"Unable to find registered services for the type '{serviceType.FullName}'");
            }

            return descriptors;
        }

        /// <summary>
        /// Get the ICommandValidator generic type
        /// </summary>
        /// <param name="interfaceType">Generic type to pass to the interface</param>
        /// <returns>Generic typed interface</returns>
        private static Type GetICommandValidatorInterfaceType(Type interfaceType)
        {
            // Create the generic interface type that an hypothetical command validator would have, the validators are optional
            return typeof(ICommandValidator<>).MakeGenericType(interfaceType.GetGenericArguments());
        }

        /// <summary>
        /// This method decorates handlers or decorators
        /// </summary>
        /// <param name="interfaceType">Type of the ICommandHandler interface including the generic type</param>
        /// <param name="genericDecoratorType">Decorator type to use for the current decoration</param>
        /// <param name="requiredAttributeType">Optional attribute that if provided must be present in the command or query in order to enable the current decoration</param>
        /// <param name="optionalDependencyType">Optional interface type that if provided will be used to create an instance passed as parameter to the current decorator</param>
        private void DecorateHandlerdescriptors(Type interfaceType, Type genericDecoratorType, Type requiredAttributeType = null, Type optionalDependencyType = null)
        {
            if (requiredAttributeType != null)
            {
                // Target type could be ICommand or IQuery
                var targetType = interfaceType.GetGenericArguments().FirstOrDefault();
                if (targetType == null) return;

                var attribute = TypeDescriptor.GetAttributes(targetType)[requiredAttributeType];

                // If the required attribute is not present, do not register the current decorator
                if (attribute == null) return;
            }

            foreach (var descriptor in GetDescriptors(interfaceType))
            {
                object Factory(IServiceProvider serviceProvider)
                {
                    // Get the instance of the previous decorator
                    var handler = descriptor.ImplementationType != null
                        // Used when decorating the handler the first time
                        ? serviceProvider.GetService(descriptor.ImplementationType)
                        // Used when decorating another decorator
                        : descriptor.ImplementationFactory(serviceProvider);

                    // Create the decorator type including generic types
                    var decoratorType = genericDecoratorType.MakeGenericType(interfaceType.GetGenericArguments());

                    // Create the logger type
                    var loggerType = typeof(ILogger<>).MakeGenericType(decoratorType);

                    return optionalDependencyType == null
                        // Standard decorator and handler constructor
                        ? Activator.CreateInstance(decoratorType, handler, serviceProvider.GetService(loggerType))
                        // Custom decorator constructor that receives an additional type
                        : Activator.CreateInstance(decoratorType, handler, serviceProvider.GetService(loggerType), serviceProvider.GetService(optionalDependencyType));
                }

                serviceCollection.Replace(ServiceDescriptor.Describe(descriptor.ServiceType, Factory, ServiceLifetime.Transient));
            }
        }

        /// <summary>
        /// Get the type of the generic interface
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericInferface"></param>
        /// <returns>Generic Type for the given interface</returns>
        private static Type GetGenericInterfaceInType(Type type, Type genericInferface) =>
                type.GetInterfaces()
                    .FirstOrDefault(i => i.GetTypeInfo().IsGenericType &&
                                         i.GetGenericTypeDefinition() == genericInferface);

        /// <summary>
        /// Get the Types implementing the generic interface provided
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <param name="genericInferface">Generic Interface</param>
        /// <returns>All types implementing the generic interface</returns>
        private static IEnumerable<Type> GetGenericTypesImplementingInterfaceInAssembly(Assembly assembly,
                Type genericInferface) =>
                assembly.GetTypes().Where(t => t.GetInterfaces().Any(i => i.GetTypeInfo().IsGenericType &&
                                                                          i.GetGenericTypeDefinition() ==
                                                                          genericInferface));

        /// <summary>
        /// Scan all assemblies matching the criteria AssemblyNameFilter criteria, if this is null all assemblies will be scanned
        /// </summary>
        private IEnumerable<Assembly> ServiceAssemblies
        {
            get
            {
                if (AssemblyNameFilter == null)
                    AssemblyNameFilter = (a) => { return true; };

                var assemblies = DependencyContext.Default.GetDefaultAssemblyNames()
                    .Where(a => AssemblyNameFilter(a)).ToList();

                foreach (var assemblyName in assemblies)
                {
                    yield return Assembly.Load(assemblyName);
                }
            }
        }
        #endregion
    }
}
