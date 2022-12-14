using System.ComponentModel;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Decorator;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Transaction.Decorator
{
    public class TransactionalQueryHandlerDecorator<TQuery, TResult> : QueryHandlerDecoratorBase<TQuery, TResult>,
        IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly ILogger<TransactionalQueryHandlerDecorator<TQuery, TResult>> _logger;

        public TransactionalQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decoratedQueryHandler, ILogger<TransactionalQueryHandlerDecorator<TQuery, TResult>> logger)
            : base(decoratedQueryHandler)
        {
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query)
        {
            TResult retVal;
            var attribute = (TransactionQueryAttribute)TypeDescriptor.GetAttributes(query)[typeof(TransactionQueryAttribute)];

            if (attribute != null)
            {
                TransactionScopeOption? transactionScopeOption = TransactionManager.GetTransactionScopeFromObject<IQueryWithTransactionScopeOptionOverride<TResult>>(query) ?? attribute.TransactionScopeOption;
                IsolationLevel? isolationLevel = TransactionManager.GetIsolationLevelFromObject<IQueryWithTransactionIsolationLevelOverride<TResult>>(query) ?? attribute.IsolationLevel;

                using (var transactionScope = TransactionManager.CreateTransactionScope(transactionScopeOption, isolationLevel))
                {
                    TransactionManager.LogTransactionStarting(_logger, query);

                    retVal = await DecoratedQueryHandler.HandleAsync(query);
                    transactionScope.Complete();

                    TransactionManager.LogTransactionComplete(_logger, query);
                }
            }
            else
            {
                retVal = await DecoratedQueryHandler.HandleAsync(query);
            }

            return retVal;
        }
    }
}
