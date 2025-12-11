using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Transaction.Decorator
{
    /// <summary>
    /// Decorator that wraps query execution in a database transaction.
    /// Queries decorated with [TransactionQuery] attribute will execute within a TransactionScope.
    ///
    /// NOTE: Most read-only queries do NOT need transactions. Use this decorator only for:
    /// - Queries requiring consistent snapshot across multiple tables
    /// - Queries with specific isolation level requirements (Snapshot, Serializable)
    /// - Queries that perform temporary table operations
    ///
    /// For better performance, consider using database-level snapshot isolation instead.
    /// </summary>
    /// <typeparam name="TQuery">The type of query being handled</typeparam>
    /// <typeparam name="TResult">The type of result returned by the query</typeparam>
    public class TransactionalQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>,
        IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly ILogger<TransactionalQueryHandlerDecorator<TQuery, TResult>> _logger;
        private readonly IOptions<Configuration.TransactionOptions> _options;

        /// <summary>
        /// Initializes a new instance of the TransactionalQueryHandlerDecorator class.
        /// </summary>
        /// <param name="queryHandler">The decorated query handler</param>
        /// <param name="logger">Logger for transaction lifecycle events</param>
        /// <param name="options">Transaction configuration options</param>
        public TransactionalQueryHandlerDecorator(
            IQueryHandler<TQuery, TResult> queryHandler,
            ILogger<TransactionalQueryHandlerDecorator<TQuery, TResult>> logger,
            IOptions<Configuration.TransactionOptions> options)
            : base(queryHandler)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// Handles the query execution within a transaction scope if the query has [TransactionQuery] attribute.
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The query result</returns>
        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var attribute = (TransactionQueryAttribute)TypeDescriptor.GetAttributes(query)[typeof(TransactionQueryAttribute)];

            if (attribute == null)
            {
                // No transaction required, execute normally
                return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
            }

            // Determine timeout: use attribute value if specified, otherwise use default
            TimeSpan timeout = attribute.TimeoutSeconds > 0
                ? TimeSpan.FromSeconds(attribute.TimeoutSeconds)
                : _options.Value.DefaultTimeout;

            // Create transaction scope with async flow enabled
            using (System.Transactions.TransactionScope scope = TransactionManager.CreateTransactionScope(
                attribute.TransactionScopeOption,
                attribute.IsolationLevel,
                timeout))
            {
                if (_options.Value.EnableLogging)
                {
                    TransactionManager.LogTransactionStarting(_logger, typeof(TQuery), attribute.IsolationLevel);
                }

                try
                {
                    TResult result = await DecoratedQueryHandler.HandleAsync(query, cancellationToken);

                    // Queries always complete successfully (no Successful property to check)
                    scope.Complete();

                    if (_options.Value.EnableLogging)
                    {
                        TransactionManager.LogTransactionComplete(_logger, typeof(TQuery));
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    // Transaction automatically rolls back when scope is disposed without Complete()
                    if (_options.Value.EnableLogging)
                    {
                        TransactionManager.LogTransactionRolledBackDueToException(_logger, typeof(TQuery), ex);
                    }
                    throw;
                }
            }
        }
    }
}
