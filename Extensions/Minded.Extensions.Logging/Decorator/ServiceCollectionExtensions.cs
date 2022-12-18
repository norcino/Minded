using Minded.Extensions.Configuration;

namespace Minded.Extensions.Logging.Decorator
{
    public static class ServiceCollectionExtensions
    {
        public static MindedBuilder AddCommandExceptionDecorator(this MindedBuilder builder)
        {
            builder.QueueCommandDecoratorRegistrationAction((b,i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<>)));
            return builder;
        }

        public static MindedBuilder AddQueryExceptionDecorator(this MindedBuilder builder)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
