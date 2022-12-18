using Minded.Extensions.Configuration;

namespace Minded.Extensions.Exception.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Exception decorator for Commands
        /// </summary>
        public static MindedBuilder AddCommandLoggingDecorator(this MindedBuilder builder)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<>)));
            return builder;
        }

        /// <summary>
        /// Add the Exception decorator for Queries
        /// </summary>
        public static MindedBuilder AddQueryLoggingDecorator(this MindedBuilder builder)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
