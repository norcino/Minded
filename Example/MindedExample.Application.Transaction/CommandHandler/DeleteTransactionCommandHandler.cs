using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Transaction.Command;

namespace MindedExample.Application.Transaction.CommandHandler
{
    /// <summary>
    /// Handler for deleting transactions.
    /// The validator ensures the transaction exists before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class DeleteTransactionCommandHandler : ICommandHandler<DeleteTransactionCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteTransactionCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
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
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            MindedExample.Domain.Transaction transaction = await _context.Transactions
                .SingleOrDefaultAsync(t => t.Id == command.TransactionId && t.User.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);

            if (transaction == null)
            {
                return new CommandResponse { Successful = false };
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse
            {
                Successful = true
            };
        }
    }
}

