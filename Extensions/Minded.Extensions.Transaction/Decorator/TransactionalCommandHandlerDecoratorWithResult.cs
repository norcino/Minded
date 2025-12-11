using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Transaction.Configuration;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Transaction.Decorator
{
    /// <summary>
    /// Decorator that wraps command execution in a database transaction for commands with result.
    /// Commands decorated with [TransactionCommand] attribute will execute within a TransactionScope.
    /// All database operations, including nested commands/queries invoked via IMediator, will participate
    /// in the same transaction and can be rolled back on error.
    /// </summary>
    /// <typeparam name="TCommand">The type of command being handled</typeparam>
    /// <typeparam name="TResult">The type of result returned by the command</typeparam>
    public class TransactionalCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>,
        ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly ILogger<TransactionalCommandHandlerDecorator<TCommand, TResult>> _logger;
        private readonly IOptions<Configuration.TransactionOptions> _options;

        /// <summary>
        /// Initializes a new instance of the TransactionalCommandHandlerDecorator class.
        /// </summary>
        /// <param name="commandHandler">The decorated command handler</param>
        /// <param name="logger">Logger for transaction lifecycle events</param>
        /// <param name="options">Transaction configuration options</param>
        public TransactionalCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> commandHandler,
            ILogger<TransactionalCommandHandlerDecorator<TCommand, TResult>> logger,
            IOptions<Configuration.TransactionOptions> options)
            : base(commandHandler)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// Handles the command execution within a transaction scope if the command has [TransactionCommand] attribute.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The command response with result</returns>
        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = (TransactionCommandAttribute)TypeDescriptor.GetAttributes(command)[typeof(TransactionCommandAttribute)];

            if (attribute == null)
            {
                // No transaction required, execute normally
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }

            // Determine timeout: use attribute value if specified, otherwise use default
            TimeSpan timeout = attribute.TimeoutSeconds > 0
                ? TimeSpan.FromSeconds(attribute.TimeoutSeconds)
                : _options.Value.DefaultTimeout;

            // Create transaction scope with async flow enabled
            using (TransactionScope scope = TransactionManager.CreateTransactionScope(
                attribute.TransactionScopeOption,
                attribute.IsolationLevel,
                timeout))
            {
                if (_options.Value.EnableLogging)
                {
                    TransactionManager.LogTransactionStarting(_logger, typeof(TCommand), attribute.IsolationLevel);
                }

                try
                {
                    ICommandResponse<TResult> response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

                    // Determine if transaction should be committed
                    var shouldCommit = response.Successful || !_options.Value.RollbackOnUnsuccessfulResponse;

                    if (shouldCommit)
                    {
                        scope.Complete();

                        if (_options.Value.EnableLogging)
                        {
                            TransactionManager.LogTransactionComplete(_logger, typeof(TCommand));
                        }
                    }
                    else
                    {
                        // Don't call Complete() - transaction will roll back
                        if (_options.Value.EnableLogging)
                        {
                            TransactionManager.LogTransactionRolledBackDueToUnsuccessfulResponse(_logger, typeof(TCommand));
                        }
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    // Transaction automatically rolls back when scope is disposed without Complete()
                    if (_options.Value.EnableLogging)
                    {
                        TransactionManager.LogTransactionRolledBackDueToException(_logger, typeof(TCommand), ex);
                    }
                    throw;
                }
            }
        }
    }
}

