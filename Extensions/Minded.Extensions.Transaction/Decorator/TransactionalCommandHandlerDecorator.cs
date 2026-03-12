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
    /// Decorator that wraps command execution in a database transaction.
    /// Commands decorated with [TransactionalCommand] attribute will execute within a TransactionScope.
    /// All database operations, including nested commands/queries invoked via IMediator, will participate
    /// in the same transaction and can be rolled back on error.
    /// </summary>
    /// <typeparam name="TCommand">The type of command being handled</typeparam>
    public class TransactionalCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>,
        ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger<TransactionalCommandHandlerDecorator<TCommand>> _logger;
        private readonly IOptions<Configuration.TransactionOptions> _options;

        /// <summary>
        /// Initializes a new instance of the TransactionalCommandHandlerDecorator class.
        /// </summary>
        /// <param name="commandHandler">The decorated command handler</param>
        /// <param name="logger">Logger for transaction lifecycle events</param>
        /// <param name="options">Transaction configuration options</param>
        public TransactionalCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler,
            ILogger<TransactionalCommandHandlerDecorator<TCommand>> logger,
            IOptions<Configuration.TransactionOptions> options)
            : base(commandHandler)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// Handles the command execution within a transaction scope if the command has [TransactionalCommand] attribute.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The command response</returns>
        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var attribute = (TransactionalCommandAttribute)TypeDescriptor.GetAttributes(command)[typeof(TransactionalCommandAttribute)];

            if (attribute == null)
            {
                // No transaction required, execute normally
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }

            // Determine timeout: use attribute value if specified, otherwise use default
            TimeSpan timeout = attribute.TimeoutSeconds > 0
                ? TimeSpan.FromSeconds(attribute.TimeoutSeconds)
                : _options.Value.GetEffectiveDefaultTimeout();

            // Create transaction scope with async flow enabled
            using (TransactionScope scope = TransactionManager.CreateTransactionScope(
                attribute.TransactionScopeOption,
                attribute.IsolationLevel,
                timeout))
            {
                if (_options.Value.GetEffectiveEnableLogging())
                {
                    TransactionManager.LogTransactionStarting(_logger, typeof(TCommand), attribute.IsolationLevel);
                }

                try
                {
                    ICommandResponse response = await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);

                    // Determine if transaction should be committed
                    var shouldCommit = response.Successful || !_options.Value.GetEffectiveRollbackOnUnsuccessfulResponse();

                    if (shouldCommit)
                    {
                        scope.Complete();

                        if (_options.Value.GetEffectiveEnableLogging())
                        {
                            TransactionManager.LogTransactionComplete(_logger, typeof(TCommand));
                        }
                    }
                    else
                    {
                        // Don't call Complete() - transaction will roll back
                        if (_options.Value.GetEffectiveEnableLogging())
                        {
                            TransactionManager.LogTransactionRolledBackDueToUnsuccessfulResponse(_logger, typeof(TCommand));
                        }
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    // Transaction automatically rolls back when scope is disposed without Complete()
                    if (_options.Value.GetEffectiveEnableLogging())
                    {
                        TransactionManager.LogTransactionRolledBackDueToException(_logger, typeof(TCommand), ex);
                    }
                    throw;
                }
            }
        }
    }
}
