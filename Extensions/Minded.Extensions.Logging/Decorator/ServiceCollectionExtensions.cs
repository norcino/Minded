using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;
using Minded.Extensions.Logging.Configuration;

namespace Minded.Extensions.Logging.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Logging decorator for Commands
        /// </summary>
        public static MindedBuilder AddCommandLoggingDecorator(this MindedBuilder builder, IConfiguration configuration)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<,>)));

            builder.ServiceCollection.Configure<LoggingOptions>(configuration.GetSection("Minded:LoggingOptions"));
            return builder;
        }

        /// <summary>
        /// Add the Logging decorator for Queries
        /// </summary>
        public static MindedBuilder AddQueryLoggingDecorator(this MindedBuilder builder, IConfiguration configuration)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingQueryHandlerDecorator<,>)));

            builder.ServiceCollection.Configure<LoggingOptions>(configuration.GetSection("Minded:LoggingOptions"));
            return builder;
        }
    }
}
