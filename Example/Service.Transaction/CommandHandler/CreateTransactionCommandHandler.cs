using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Minded.Framework.CQRS.Command;
using Service.Transaction.Command;

namespace Service.Transaction.CommandHandler
{
    /// <summary>
    /// Handler for creating new transactions.
    /// The validator ensures the transaction data is valid before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, Data.Entity.Transaction>
    {
        private readonly IMindedExampleContext _context;

        public CreateTransactionCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new transaction in the database.
        /// Uses async AddAsync and SaveChangesAsync for proper async/await pattern.
        /// </summary>
        /// <param name="command">The create command containing the transaction to create</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the created transaction</returns>
        public async Task<ICommandResponse<Data.Entity.Transaction>> HandleAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default)
        {
            await _context.Transactions.AddAsync(command.Transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse<Data.Entity.Transaction>(command.Transaction)
            {
                Successful = true
            };
        }
    }
}
