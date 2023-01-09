using Minded.Extensions.Configuration;

namespace Minded.Extensions.Exception.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Exception decorator for Commands
        /// </summary>
        public static MindedBuilder AddCommandExceptionDecorator(this MindedBuilder builder)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionCommandHandlerDecorator<,>)));
            return builder;
        }

        /// <summary>
        /// Add the Exception decorator for Queries
        /// </summary>
        public static MindedBuilder AddQueryExceptionDecorator(this MindedBuilder builder)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ExceptionQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
