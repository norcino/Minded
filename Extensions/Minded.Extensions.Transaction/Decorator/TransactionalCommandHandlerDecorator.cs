using System.ComponentModel;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Decorator;
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

        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            ICommandResponse retVal;
            var attribute = (TransactionCommandAttribute)TypeDescriptor.GetAttributes(command)[typeof(TransactionCommandAttribute)];

            if (attribute != null)
            {
     //           TransactionScopeOption? transactionScopeOption = TransactionManager.GetTransactionScopeFromObject<ICommandWithTransactionScopeOptionOverride>(command) ?? attribute.TransactionScopeOption;
     //           IsolationLevel? isolationLevel = TransactionManager.GetIsolationLevelFromObject<ICommandWithTransactionIsolationLevelOverride>(command) ?? attribute.IsolationLevel;

 //               using (var transactionScope = TransactionManager.CreateTransactionScope(transactionScopeOption, isolationLevel))
 //               {
  //                  TransactionManager.LogTransactionStarting(_logger, command);

                    retVal = await CommmandHandler.HandleAsync(command);
//                    transactionScope.Complete();

 //                   TransactionManager.LogTransactionComplete(_logger, command);
 //               }
            }
            else
            {
                retVal = await CommmandHandler.HandleAsync(command);
            }

            return retVal;
        }
    }
}
