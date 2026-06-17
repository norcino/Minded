using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Transaction.Command;

namespace MindedExample.Application.Transaction.CommandHandler
{
    /// <summary>
    /// Handler for creating new transactions.
    /// All validation (including tenant/user checks) is performed by the decorator pipeline
    /// before this handler is invoked.
    /// </summary>
    public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, MindedExample.Domain.Transaction>
    {
        private readonly IMindedExampleContext _context;

        public CreateTransactionCommandHandler(IMindedExampleContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new transaction in the database.
        /// </summary>
        /// <param name="command">The create command containing the transaction to create.</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
        /// <returns>Successful command response with the created transaction.</returns>
        public async Task<ICommandResponse<MindedExample.Domain.Transaction>> HandleAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default)
        {
            await _context.Transactions.AddAsync(command.Transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return CommandResponse<MindedExample.Domain.Transaction>.Success(command.Transaction);
        }
    }
}
