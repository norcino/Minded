using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minded.Extensions.Authorization.Decorator;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Authorization.Configuration
{
    /// <summary>
    /// Extension methods for registering authorization decorators with the Minded framework.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Authorization decorator for Commands.
        /// Registers command decorators for both ICommand and ICommand&lt;TResult&gt;,
        /// configures options, registers the default evaluator, and eagerly validates attributes.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <param name="configure">Optional action to configure AuthorizationOptions</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        public static MindedBuilder AddCommandAuthorizationDecorator(this MindedBuilder builder, Action<AuthorizationOptions> configure = null)
        {
            // Configure options
            if (configure != null)
            {
                builder.ServiceCollection.Configure(configure);
            }
            else
            {
                builder.ServiceCollection.TryAddSingleton<IOptions<AuthorizationOptions>>(
                    sp => Options.Create(new AuthorizationOptions()));
            }

            // Register default evaluator if not already registered
            builder.ServiceCollection.TryAddSingleton<IRequestAuthorizationEvaluator, DefaultRequestAuthorizationEvaluator>();

            // Queue command decorator registration for ICommand
            builder.QueueCommandDecoratorRegistrationAction((b, i) =>
                b.DecorateHandlerDescriptors(i, typeof(AuthorizationCommandHandlerDecorator<>)));

            // Queue command decorator registration for ICommand<TResult>
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) =>
                b.DecorateHandlerDescriptors(i, typeof(AuthorizationCommandHandlerDecorator<,>)));

            // Eagerly validate all discovered RBAC attributes at startup
            EagerlyValidateAttributes(builder, typeof(ICommandHandler<>));
            EagerlyValidateAttributes(builder, typeof(ICommandHandler<,>));

            return builder;
        }

        /// <summary>
        /// Adds the Authorization decorator for Queries.
        /// Registers query decorators, configures options, registers the default evaluator,
        /// and eagerly validates attributes.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <param name="configure">Optional action to configure AuthorizationOptions</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        public static MindedBuilder AddQueryAuthorizationDecorator(this MindedBuilder builder, Action<AuthorizationOptions> configure = null)
        {
            // Configure options
            if (configure != null)
            {
                builder.ServiceCollection.Configure(configure);
            }
            else
            {
                builder.ServiceCollection.TryAddSingleton<IOptions<AuthorizationOptions>>(
                    sp => Options.Create(new AuthorizationOptions()));
            }

            // Register default evaluator if not already registered
            builder.ServiceCollection.TryAddSingleton<IRequestAuthorizationEvaluator, DefaultRequestAuthorizationEvaluator>();

            // Queue query decorator registration
            builder.QueueQueryDecoratorRegistrationAction((b, i) =>
                b.DecorateHandlerDescriptors(i, typeof(AuthorizationQueryHandlerDecorator<,>)));

            // Eagerly validate all discovered RBAC attributes at startup
            EagerlyValidateAttributes(builder, typeof(IQueryHandler<,>));

            return builder;
        }

        /// <summary>
        /// Registers a scoped IAuthorizationContextAccessor implementation.
        /// </summary>
        /// <typeparam name="TAccessor">The accessor implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAuthorizationContextAccessor<TAccessor>(this IServiceCollection services)
            where TAccessor : class, IAuthorizationContextAccessor
        {
            services.AddScoped<IAuthorizationContextAccessor, TAccessor>();
            return services;
        }

        /// <summary>
        /// Registers a singleton IRequestAuthorizationEvaluator implementation.
        /// </summary>
        /// <typeparam name="TEvaluator">The evaluator implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRequestAuthorizationEvaluator<TEvaluator>(this IServiceCollection services)
            where TEvaluator : class, IRequestAuthorizationEvaluator
        {
            services.AddSingleton<IRequestAuthorizationEvaluator, TEvaluator>();
            return services;
        }

        /// <summary>
        /// Eagerly validates RBAC attributes on all discovered handler types at startup.
        /// </summary>
        private static void EagerlyValidateAttributes(MindedBuilder builder, Type handlerInterfaceType)
        {
            foreach (var assembly in builder.SourceAssemblies(builder.AssemblyFilter))
            {
                var handlerTypes = builder.GetGenericTypesImplementingInterfaceInAssembly(assembly, handlerInterfaceType);

                foreach (var handlerType in handlerTypes)
                {
                    var interfaceType = builder.GetGenericInterfaceInType(handlerType, handlerInterfaceType);
                    if (interfaceType == null) continue;

                    // The first generic argument is the command/query type
                    var requestType = interfaceType.GetGenericArguments().FirstOrDefault();
                    if (requestType == null) continue;

                    AttributeValidator.Validate(requestType);
                }
            }
        }
    }
}
