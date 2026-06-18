using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for deleting users.
    /// The validator ensures the user exists before this handler is called.
    /// If validation fails (user not found), this handler will not be executed.
    /// </summary>
    public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteUserCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Deletes the user from the database.
        /// Assumes the user exists (validated by DeleteUserCommandValidator).
        /// </summary>
        /// <param name="command">The delete command containing the user ID</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response</returns>
        public async Task<ICommandResponse> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse { Successful = false };
            }

            MindedExample.Domain.User user = await _context.Users
                .SingleOrDefaultAsync(u => u.Id == command.UserId && u.TenantId == _currentUserAccessor.TenantId.Value, cancellationToken);

            // Note: user should never be null here due to validation, but defensive programming
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return CommandResponse.Success();
        }
    }
}

