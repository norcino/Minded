using Minded.Extensions.Configuration;
using Minded.Extensions.DataProtection;
using Minded.Extensions.DataProtection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Minded.Extensions.Exception.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Exception decorator for Commands
        /// </summary>
        public static MindedBuilder AddCommandExceptionDecorator(this MindedBuilder builder)
        {
            // Register NullDataSanitizer as fallback if no IDataSanitizer is registered
            // This allows the decorator to work without requiring DataProtection to be configured
            builder.ServiceCollection.TryAddSingleton<IDataSanitizer, NullDataSanitizer>();

            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<,>)));
            return builder;
        }

        /// <summary>
        /// Add the Exception decorator for Queries
        /// </summary>
        public static MindedBuilder AddQueryExceptionDecorator(this MindedBuilder builder)
        {
            // Register NullDataSanitizer as fallback if no IDataSanitizer is registered
            // This allows the decorator to work without requiring DataProtection to be configured
            builder.ServiceCollection.TryAddSingleton<IDataSanitizer, NullDataSanitizer>();

            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
