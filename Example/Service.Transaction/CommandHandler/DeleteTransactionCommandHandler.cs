using System.Threading;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using Service.Transaction.Command;

namespace Service.Transaction.CommandHandler
{
    /// <summary>
    /// Handler for deleting transactions.
    /// The validator ensures the transaction exists before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class DeleteTransactionCommandHandler : ICommandHandler<DeleteTransactionCommand>
    {
        private readonly IMindedExampleContext _context;

        public DeleteTransactionCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Deletes the transaction from the database.
        /// Assumes the transaction exists (validated by DeleteTransactionCommandValidator).
        /// </summary>
        /// <param name="command">The delete command containing the transaction ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response</returns>
        public async Task<ICommandResponse> HandleAsync(DeleteTransactionCommand command, CancellationToken cancellationToken = default)
        {
            Data.Entity.Transaction transaction = await _context.Transactions.SingleOrDefaultAsync(t => t.Id == command.TransactionId, cancellationToken);

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse
            {
                Successful = true
            };
        }
    }
}

