﻿using Minded.Extensions.Configuration;

namespace Minded.Extensions.Logging.Decorator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the Logging decorator for Commands
        /// </summary>
        public static MindedBuilder AddCommandLoggingDecorator(this MindedBuilder builder)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingCommandHandlerDecorator<,>)));
            return builder;
        }

        /// <summary>
        /// Add the Logging decorator for Queries
        /// </summary>
        public static MindedBuilder AddQueryLoggingDecorator(this MindedBuilder builder)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(LoggingQueryHandlerDecorator<,>)));
            return builder;
        }
    }
}
