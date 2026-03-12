using Minded.Extensions.Configuration;
using Minded.Extensions.DataProtection;
using Minded.Extensions.DataProtection.Abstractions;
using Minded.Extensions.Exception.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

namespace Minded.Extensions.Exception.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Exception decorator for Commands with optional configuration
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <param name="configureOptions">Optional action to configure exception handling options</param>
        public static MindedBuilder AddCommandExceptionDecorator(this MindedBuilder builder, Action<ExceptionOptions> configureOptions = null)
        {
            // Register NullDataSanitizer as fallback if no IDataSanitizer is registered
            // This allows the decorator to work without requiring DataProtection to be configured
            builder.ServiceCollection.TryAddSingleton<IDataSanitizer, NullDataSanitizer>();

            // Configure options if provided
            if (configureOptions != null)
            {
                builder.ServiceCollection.Configure(configureOptions);
            }
            else
            {
                // Register default options if not already registered
                builder.ServiceCollection.TryAddSingleton<IOptions<ExceptionOptions>>(
                    sp => Options.Create(new ExceptionOptions()));
            }

            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<,>)));
            return builder;
        }

        /// <summary>
        /// Add the Exception decorator for Queries with optional configuration
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <param name="configureOptions">Optional action to configure exception handling options</param>
        public static MindedBuilder AddQueryExceptionDecorator(this MindedBuilder builder, Action<ExceptionOptions> configureOptions = null)
        {
            // Register NullDataSanitizer as fallback if no IDataSanitizer is registered
            // This allows the decorator to work without requiring DataProtection to be configured
            builder.ServiceCollection.TryAddSingleton<IDataSanitizer, NullDataSanitizer>();

            // Configure options if provided
            if (configureOptions != null)
            {
                builder.ServiceCollection.Configure(configureOptions);
            }
            else
            {
                // Register default options if not already registered
                builder.ServiceCollection.TryAddSingleton<IOptions<ExceptionOptions>>(
                    sp => Options.Create(new ExceptionOptions()));
            }

            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
