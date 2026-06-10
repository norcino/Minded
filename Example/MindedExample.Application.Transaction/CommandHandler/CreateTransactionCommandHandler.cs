using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Transaction.Command;

namespace MindedExample.Application.Transaction.CommandHandler
{
    /// <summary>
    /// Handler for creating new transactions.
    /// The validator ensures the transaction data is valid before this handler is called.
    /// If validation fails, this handler will not be executed.
    /// </summary>
    public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, MindedExample.Domain.Transaction>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public CreateTransactionCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Creates a new transaction in the database.
        /// Uses async AddAsync and SaveChangesAsync for proper async/await pattern.
        /// </summary>
        /// <param name="command">The create command containing the transaction to create</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the created transaction</returns>
        public async Task<ICommandResponse<MindedExample.Domain.Transaction>> HandleAsync(CreateTransactionCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse<MindedExample.Domain.Transaction>(default(MindedExample.Domain.Transaction), false);
            }

            var tenantId = _currentUserAccessor.TenantId.Value;
            var userExistsInTenant = await _context.Users
                .AnyAsync(u => u.Id == command.Transaction.UserId && u.TenantId == tenantId, cancellationToken);
            if (!userExistsInTenant)
            {
                return new CommandResponse<MindedExample.Domain.Transaction>(default(MindedExample.Domain.Transaction), false);
            }

            await _context.Transactions.AddAsync(command.Transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new CommandResponse<MindedExample.Domain.Transaction>(command.Transaction)
            {
                Successful = true
            };
        }
    }
}
