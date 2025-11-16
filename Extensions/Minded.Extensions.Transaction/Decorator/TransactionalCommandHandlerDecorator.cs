using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Minded.Framework.Decorator;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Transaction.Decorator
{
    public class TransactionalCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>,
        ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ILogger<TransactionalCommandHandlerDecorator<TCommand>> _logger;

        public TransactionalCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger<TransactionalCommandHandlerDecorator<TCommand>> logger)
            : base(commandHandler)
        {
            _logger = logger;
        }

        public Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            //ICommandResponse retVal;
            var attribute = (TransactionCommandAttribute)TypeDescriptor.GetAttributes(command)[typeof(TransactionCommandAttribute)];

            if (attribute != null)
            {
     //           TransactionScopeOption? transactionScopeOption = TransactionManager.GetTransactionScopeFromObject<ICommandWithTransactionScopeOptionOverride>(command) ?? attribute.TransactionScopeOption;
     //           IsolationLevel? isolationLevel = TransactionManager.GetIsolationLevelFromObject<ICommandWithTransactionIsolationLevelOverride>(command) ?? attribute.IsolationLevel;

 //               using (var transactionScope = TransactionManager.CreateTransactionScope(transactionScopeOption, isolationLevel))
 //               {
  //                  TransactionManager.LogTransactionStarting(_logger, command);

                    //retVal = await CommmandHandler.HandleAsync(command, cancellationToken);
//                    transactionScope.Complete();

 //                   TransactionManager.LogTransactionComplete(_logger, command);
 //               }
            }
            else
            {
//                retVal = await CommmandHandler.HandleAsync(command, cancellationToken);
            }

            return null; //retVal;
        }
    }
}
