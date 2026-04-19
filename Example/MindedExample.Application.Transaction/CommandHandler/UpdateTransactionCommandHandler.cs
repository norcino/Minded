using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Transaction.Command;

namespace MindedExample.Application.Transaction.CommandHandler
{
    /// <summary>
    /// Handler for updating transactions.
    /// The validator ensures the transaction exists before this handler is called.
    /// If validation fails (transaction not found), this handler will not be executed.
    /// </summary>
    public class UpdateTransactionCommandHandler : ICommandHandler<UpdateTransactionCommand, MindedExample.Domain.Transaction>
    {
        private readonly IMindedExampleContext _context;

        public UpdateTransactionCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Updates the transaction in the database.
        /// Assumes the transaction exists (validated by UpdateTransactionCommandValidator).
        /// </summary>
        /// <param name="command">The update command containing the transaction ID and updated data</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the updated transaction</returns>
        public async Task<ICommandResponse<MindedExample.Domain.Transaction>> HandleAsync(UpdateTransactionCommand command, CancellationToken cancellationToken = default)
        {
            MindedExample.Domain.Transaction transaction = await _context.Transactions.SingleOrDefaultAsync(p => p.Id == command.TransactionId, cancellationToken);
            transaction.Description = command.Transaction.Description;
            transaction.CategoryId = command.Transaction.CategoryId;
            transaction.Credit = command.Transaction.Credit;
            transaction.Debit = command.Transaction.Debit;
            transaction.Recorded = command.Transaction.Recorded;

            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse<MindedExample.Domain.Transaction>(transaction)
            {
                Successful = true
            };
        }
    }
}
