using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Framework.Mediator
{
    public static class ServiceCollectionExtensions
    {        
        /// <summary>
        /// Register all the query handlers with the related decorators
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="lifeTime"></param>
        public static void AddQueryHandlers(this MindedBuilder builder, Action<MindedBuilder, Type> decorators = null, Func<AssemblyName, bool> assemblyFilter = null, ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            foreach (var assembly in builder.SourceAssemblies(assemblyFilter ?? builder.AssemblyFilter))
            {
                var queryHandlers = builder.GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(IQueryHandler<,>));

                foreach (var handlerType in queryHandlers)
                {
                    var interfaceType = builder.GetGenericInterfaceInType(handlerType, typeof(IQueryHandler<,>));

                    // Register the handler by it's interface
                    builder.Register(sc => sc.Add(new ServiceDescriptor(interfaceType, handlerType, lifeTime)));

                    // Register the handler by it's own type
                    builder.Register(sc => sc.Add(new ServiceDescriptor(handlerType, handlerType, lifeTime)));

                    builder.QueuedQueryDecoratorsRegistrationAction.ForEach(a => a(builder, interfaceType));
                }
            }
        }

        /// <summary>
        /// Register all the command handlers with the related decorators
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="lifeTime"></param>
        public static void AddCommandHandlers(this MindedBuilder builder, Func<AssemblyName, bool> assemblyFilter = null, ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            foreach (var assembly in builder.SourceAssemblies(assemblyFilter ?? builder.AssemblyFilter))
            {
                var commandHandlers = builder.GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(ICommandHandler<>));

                foreach (var handlerType in commandHandlers)
                {
                    var interfaceType = builder.GetGenericInterfaceInType(handlerType, typeof(ICommandHandler<>));

                    // Register the handler by it's interface
                    builder.Register(sc => sc.Add(new ServiceDescriptor(interfaceType, handlerType, lifeTime)));

                    // Register the handler by it's own type
                    builder.Register(sc => sc.Add(new ServiceDescriptor(handlerType, handlerType, lifeTime)));

                    builder.QueuedCommandDecoratorsRegistrationAction.ForEach(a => a(builder,interfaceType));
                }

                commandHandlers = builder.GetGenericTypesImplementingInterfaceInAssembly(assembly, typeof(ICommandHandler<,>));

                foreach (var handlerType in commandHandlers)
                {
                    var interfaceType = builder.GetGenericInterfaceInType(handlerType, typeof(ICommandHandler<,>));

                    // Register the handler by it's interface
                    builder.Register(sc => sc.Add(new ServiceDescriptor(interfaceType, handlerType, lifeTime)));

                    // Register the handler by it's own type
                    builder.Register(sc => sc.Add(new ServiceDescriptor(handlerType, handlerType, lifeTime)));

                    builder.QueuedCommandWithResultDecoratorsRegistrationAction.ForEach(a => a(builder, interfaceType));
                }
            }
        }
    }
}
