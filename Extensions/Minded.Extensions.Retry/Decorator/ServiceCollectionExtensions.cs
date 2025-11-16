using System;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;
using Minded.Extensions.Retry.Configuration;

namespace Minded.Extensions.Retry.Decorator
{
    /// <summary>
    /// Extension methods for registering retry decorators in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Retry decorator for Commands with default configuration.
        /// Retries commands marked with RetryCommandAttribute.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <returns>The MindedBuilder for method chaining</returns>
        public static MindedBuilder AddCommandRetryDecorator(this MindedBuilder builder)
        {
            return AddCommandRetryDecorator(builder, null);
        }

        /// <summary>
        /// Adds the Retry decorator for Commands with custom default configuration.
        /// Retries commands marked with RetryCommandAttribute.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <param name="configureOptions">Action to configure default retry options</param>
        /// <returns>The MindedBuilder for method chaining</returns>
        public static MindedBuilder AddCommandRetryDecorator(this MindedBuilder builder, Action<RetryOptions> configureOptions)
        {
            // Configure options
            if (configureOptions != null)
            {
                builder.ServiceCollection.Configure(configureOptions);
            }
            else
            {
                builder.ServiceCollection.Configure<RetryOptions>(builder.Configuration.GetSection("Minded:RetryOptions"));
            }

            // Register decorators
            builder.QueueCommandDecoratorRegistrationAction((b, i) => 
                b.DecorateHandlerDescriptors(i, typeof(RetryCommandHandlerDecorator<>), typeof(RetryCommandAttribute)));

            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => 
                b.DecorateHandlerDescriptors(i, typeof(RetryCommandHandlerDecorator<,>), typeof(RetryCommandAttribute)));

            return builder;
        }

        /// <summary>
        /// Adds the Retry decorator for Queries with default configuration.
        /// Retries queries marked with RetryQueryAttribute.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <returns>The MindedBuilder for method chaining</returns>
        public static MindedBuilder AddQueryRetryDecorator(this MindedBuilder builder)
        {
            return AddQueryRetryDecorator(builder, applyToAllQueries: false, configureOptions: null);
        }

        /// <summary>
        /// Adds the Retry decorator for Queries with option to apply to all queries.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <param name="applyToAllQueries">If true, applies retry logic to all queries even without RetryQueryAttribute</param>
        /// <returns>The MindedBuilder for method chaining</returns>
        public static MindedBuilder AddQueryRetryDecorator(this MindedBuilder builder, bool applyToAllQueries)
        {
            return AddQueryRetryDecorator(builder, applyToAllQueries, configureOptions: null);
        }

        /// <summary>
        /// Adds the Retry decorator for Queries with custom configuration.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <param name="applyToAllQueries">If true, applies retry logic to all queries even without RetryQueryAttribute</param>
        /// <param name="configureOptions">Action to configure default retry options</param>
        /// <returns>The MindedBuilder for method chaining</returns>
        public static MindedBuilder AddQueryRetryDecorator(
            this MindedBuilder builder, 
            bool applyToAllQueries, 
            Action<RetryOptions> configureOptions)
        {
            // Configure options
            if (configureOptions != null)
            {
                builder.ServiceCollection.Configure<RetryOptions>(options =>
                {
                    options.ApplyToAllQueries = applyToAllQueries;
                    configureOptions(options);
                });
            }
            else
            {
                builder.ServiceCollection.Configure<RetryOptions>(options =>
                {
                    options.ApplyToAllQueries = applyToAllQueries;
                });
                builder.ServiceCollection.Configure<RetryOptions>(builder.Configuration.GetSection("Minded:RetryOptions"));
            }

            // Register decorator - no attribute filter if applyToAllQueries is true
            if (applyToAllQueries)
            {
                builder.QueueQueryDecoratorRegistrationAction((b, i) => 
                    b.DecorateHandlerDescriptors(i, typeof(RetryQueryHandlerDecorator<,>)));
            }
            else
            {
                builder.QueueQueryDecoratorRegistrationAction((b, i) => 
                    b.DecorateHandlerDescriptors(i, typeof(RetryQueryHandlerDecorator<,>), typeof(RetryQueryAttribute)));
            }

            return builder;
        }

        /// <summary>
        /// Adds the Retry decorator for Queries with custom default configuration.
        /// </summary>
        /// <param name="builder">The MindedBuilder instance</param>
        /// <param name="configureOptions">Action to configure default retry options</param>
        /// <returns>The MindedBuilder for method chaining</returns>
        public static MindedBuilder AddQueryRetryDecorator(this MindedBuilder builder, Action<RetryOptions> configureOptions)
        {
            return AddQueryRetryDecorator(builder, applyToAllQueries: false, configureOptions: configureOptions);
        }
    }
}

