using System;
using Microsoft.Extensions.DependencyInjection;
using Minded.Extensions.Configuration;
using Minded.Extensions.Transaction.Configuration;

namespace Minded.Extensions.Transaction.Decorator
{
    /// <summary>
    /// Extension methods for registering transaction decorators with the Minded framework.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Transaction decorator for Commands using configuration from appsettings.json.
        /// Commands decorated with [TransactionalCommand] attribute will execute within a database transaction.
        /// All database operations, including nested commands/queries invoked via IMediator, will participate
        /// in the same transaction and can be rolled back on error.
        ///
        /// WARNING: This transaction does NOT cover:
        /// - Remote service calls (HTTP, gRPC, etc.)
        /// - Message queue operations (RabbitMQ, Azure Service Bus, etc.)
        /// - File system operations
        /// - External API calls
        /// 
        /// For distributed scenarios, use Saga pattern or Transactional Outbox pattern.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        public static MindedBuilder AddCommandTransactionDecorator(this MindedBuilder builder)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(TransactionalCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(TransactionalCommandHandlerDecorator<,>)));

            builder.ServiceCollection.Configure<TransactionOptions>(builder.Configuration.GetSection("Minded:TransactionOptions"));
            return builder;
        }

        /// <summary>
        /// Adds the Transaction decorator for Commands with custom configuration action.
        /// Commands decorated with [TransactionCommand] attribute will execute within a database transaction.
        /// All database operations, including nested commands/queries invoked via IMediator, will participate
        /// in the same transaction and can be rolled back on error.
        /// 
        /// WARNING: This transaction does NOT cover:
        /// - Remote service calls (HTTP, gRPC, etc.)
        /// - Message queue operations (RabbitMQ, Azure Service Bus, etc.)
        /// - File system operations
        /// - External API calls
        /// 
        /// For distributed scenarios, use Saga pattern or Transactional Outbox pattern.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <param name="configureOptions">Action to configure TransactionOptions</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        /// <example>
        /// <code>
        /// builder.AddCommandTransactionDecorator(options => {
        ///     options.DefaultIsolationLevel = IsolationLevel.ReadCommitted;
        ///     options.DefaultTimeout = TimeSpan.FromMinutes(2);
        ///     options.RollbackOnUnsuccessfulResponse = true;
        ///     options.EnableLogging = true;
        /// });
        /// </code>
        /// </example>
        public static MindedBuilder AddCommandTransactionDecorator(this MindedBuilder builder, Action<TransactionOptions> configureOptions)
        {
            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(TransactionalCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(TransactionalCommandHandlerDecorator<,>)));

            builder.ServiceCollection.Configure(configureOptions);
            return builder;
        }

        /// <summary>
        /// Adds the Transaction decorator for Queries using configuration from appsettings.json.
        /// Queries decorated with [TransactionQuery] attribute will execute within a database transaction.
        /// 
        /// NOTE: Most read-only queries do NOT need transactions. Use this decorator only for:
        /// - Queries requiring consistent snapshot across multiple tables
        /// - Queries with specific isolation level requirements (Snapshot, Serializable)
        /// - Queries that perform temporary table operations
        /// 
        /// For better performance, consider using database-level snapshot isolation instead.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        public static MindedBuilder AddQueryTransactionDecorator(this MindedBuilder builder)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(TransactionalQueryHandlerDecorator<,>)));

            builder.ServiceCollection.Configure<TransactionOptions>(builder.Configuration.GetSection("Minded:TransactionOptions"));
            return builder;
        }

        /// <summary>
        /// Adds the Transaction decorator for Queries with custom configuration action.
        /// Queries decorated with [TransactionQuery] attribute will execute within a database transaction.
        /// 
        /// NOTE: Most read-only queries do NOT need transactions. Use this decorator only for:
        /// - Queries requiring consistent snapshot across multiple tables
        /// - Queries with specific isolation level requirements (Snapshot, Serializable)
        /// - Queries that perform temporary table operations
        /// 
        /// For better performance, consider using database-level snapshot isolation instead.
        /// </summary>
        /// <param name="builder">MindedBuilder instance</param>
        /// <param name="configureOptions">Action to configure TransactionOptions</param>
        /// <returns>MindedBuilder for fluent chaining</returns>
        /// <example>
        /// <code>
        /// builder.AddQueryTransactionDecorator(options => {
        ///     options.DefaultIsolationLevel = IsolationLevel.Snapshot;
        ///     options.DefaultTimeout = TimeSpan.FromSeconds(30);
        ///     options.EnableLogging = true;
        /// });
        /// </code>
        /// </example>
        public static MindedBuilder AddQueryTransactionDecorator(this MindedBuilder builder, Action<TransactionOptions> configureOptions)
        {
            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(TransactionalQueryHandlerDecorator<,>)));

            builder.ServiceCollection.Configure(configureOptions);
            return builder;
        }
    }
}

